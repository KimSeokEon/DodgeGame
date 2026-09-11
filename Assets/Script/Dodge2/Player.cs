using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

// =============================================================================
// Player.cs
// -----------------------------------------------------------------------------
// 역할 : 플레이어 캐릭터의 이동/구르기/피격/사망을 전부 담당하는 메인 컨트롤러.
// 붙는 곳 : Assets/Resources/Player (1).prefab (Fusion 네트워크 프리팹)
//           PlayerSpawner.cs가 이 프리팹을 Runner.Spawn()으로 생성해서 붙여준다.
// 네트워크 : Fusion의 NetworkBehaviour를 상속. 캐릭터가 여러 명 접속해도
//           "이게 내 캐릭터인지"는 HasInputAuthority로 판단한다.
//   - 이동/구르기 입력 : owner(HasInputAuthority)에서만 FixedUpdateNetwork로 처리, 위치는 NetworkTransform이 동기화
//   - 애니메이션 상태(isRun/isWalk/Dodge) : [Networked]로 공유, Render()에서 전 클라가 Animator에 반영
//   - 체력/다운(Health/IsDead) : owner만 변경. 적과의 충돌은 owner가 FixedUpdateNetwork에서
//     OverlapSphere로 매 틱 직접 검사해 스스로 깎는다 (왕복 지연 없음)
//     하트 UI/피격 연출/다운·부활 애니메이션은 Render()에서 값 변화를 감지해 전 클라가 재생
//   - 부활 : 체력 0이면 "다운" 상태(몸은 남음). 살아있는 팀원이 reviveRange 안에 reviveDuration초
//     머물면 owner가 스스로 부활(Health=reviveHealth). 전원 다운일 때만 게임오버.
// =============================================================================
public class Player : NetworkBehaviour
{
    private MeshRenderer[] bodyRenderers; //캐릭터 몸 파츠
    public float invincibleDuration = 1.2f; // 무적 지속 시간
    public float flashInterval = 0.1f; // 깜박이는 간격

    // ── 피격/체력/사망 (네트워크 동기화) ──────────────────────────────
    // Health/IsDead는 이 캐릭터의 owner(Shared Mode에선 StateAuthority)만 값을 바꾸고,
    // 모든 클라는 Render()에서 값 변화를 감지해 하트 UI/피격 연출/사망 애니메이션을 재생한다.
    // 적과 부딪힌 판정은 맞은 플레이어 본인이 FixedUpdateNetwork의 OverlapSphere로 한다.
    [Networked] public int Health { get; private set; }
    [Networked] public bool IsDead { get; private set; }
    [Networked] private TickTimer InvincibleTimer { get; set; } // 피격 후 무적(i-frame)


    public bool WantsRestart => Object != null && Object.IsValid && GameClock.Instance != null
        && GameClock.Instance.Object != null && GameClock.Instance.Object.IsValid
        && GameClock.Instance.RestartVotePhase == GameClock.VoteOpen
        && GameClock.Instance.RestartBallots.TryGet(Object.InputAuthority, out int vote) && vote == 1;

    private int _lastSeenHealth;  // Render에서 "이번에 체력이 줄었나" 판단용
    private bool _lastSeenIsDead; // Render에서 "이번에 죽었나/부활했나" 판단용
    private float _downedSince;   // 언제 다운됐는지 (전멸 게임오버 유예 시간 계산용)

    // ★ 피격 판정용. OnTriggerEnter 대신 매 틱 직접 검사한다 — 게스트 쪽 적은
    //   프록시(NetworkTransform 보간으로 움직임)라 OnTriggerEnter가 잘 안 떴었음
    //   (호스트만 피격되고 게스트는 무적이던 버그의 원인).
    [Header("피격 판정")]
    public float hitRadius = 0.6f;                        // 내 몸 반경 (Scene에서 보며 조정)
    public Vector3 hitOffset = new Vector3(0f, 1f, 0f);   // 검사 구의 중심 (발밑 기준 위로)
    readonly Collider[] _hitBuf = new Collider[16];       // OverlapSphere 결과 버퍼 (재사용, GC 방지)

    [Header("부활 (다운된 팀원 살리기)")]
    public float reviveRange = 2f;    // 이 거리 안에 살아있는 팀원이 있으면 부활 진행
    public float reviveDuration = 3f; // 부활에 필요한 시간(초)
    public int reviveHealth = 1;      // 부활 시 돌려주는 하트 수

    // 다운 상태에서 근처 팀원이 채우는 부활 게이지 (0 ~ reviveDuration). owner만 씀.
    [Networked] private float ReviveProgress { get; set; }

    // 부활 게이지 진행률 0~1 (머리 위 UI 등에서 읽어쓰기 좋게). 다운 상태에서만 의미 있음.
    public float ReviveProgress01 => reviveDuration > 0f ? Mathf.Clamp01(ReviveProgress / reviveDuration) : 0f;

    // 매 틱 FixedUpdateNetwork()에서 읽어서 이동에 쓰는 입력값들
    float hAxis;
    float vAxis;
    bool wDown; // 걷기(Walk) 버튼이 눌려있는지
    public float speed; // 기본 이동 속도

    public Transform cam; // 이동 방향 계산 기준이 되는 카메라. 비어있으면 Start()에서 Camera.main으로 채움

    // GameManager2가 생존 타이머/게임오버를 전부 담당함 (SurvivalTimer.cs는 GameManager2로 통합됨)
    private GameManager2 gameManager;

    [Header("Heart3 -> 2 -> 1  순서로 등록")] public Animator[] heartAnimators; // 하트 UI 3개, 체력이 깎일 때마다 순서대로 Disappear 애니메이션 재생

    private Rigidbody rb;
    Vector3 moveVec; // 이번 틱에 이동할 방향(정규화됨)

    private Animator anim; // 캐릭터 모델의 Animator (isRun/isWalk/Dodge/Die 파라미터 제어)

    // 이동/걷기 애니메이션 상태를 네트워크로 공유한다.
    [Networked] private bool NetIsRun { get; set; }
    [Networked] private bool NetIsWalk { get; set; }

    // 구르기는 "한 번 터지는" 트리거라 bool로는 못 넘긴다. authority가 구를 때마다
    // 이 숫자를 1 올리고, 모든 클라가 Render()에서 값이 바뀐 걸 감지하면 Dodge 트리거를 쏜다.
    [Networked] private int DodgeVersion { get; set; }
    private int _lastSeenDodgeVersion; // 이 클라가 마지막으로 반영한 DodgeVersion

    [Header("구르기")]
    public float dodgespeedMultiplier = 2f; // 구르는 동안 속도 배율
    public float dodgeDuration = 0.4f;      // 구르기 지속 시간
    public float dodgeCooldown = 1f;        // 구르기 재사용 대기시간
    private float dodgeCooldownTimer = 0f;
    private bool isDodging = false;
    private bool dodgeRequested = false; // Update()에서 눌림을 감지해 저장해두고 FixedUpdateNetwork()에서 소비

    // 구르기 쿨타임 진행률 (0 = 바로 쓸 수 있음, 1 = 방금 써서 쿨타임 꽉 참).
    public float DodgeCooldownProgress01 => dodgeCooldown > 0f ? Mathf.Clamp01(dodgeCooldownTimer / dodgeCooldown) : 0f;

    void Awake()
    {
        anim = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody>();
        bodyRenderers = GetComponentsInChildren<MeshRenderer>(true); // 피격 시 빨갛게 깜박일 대상들
    }

    void Start()
    {
        if (cam == null)
        {
            cam = Camera.main.transform;
        }

        gameManager = FindFirstObjectByType<GameManager2>(); // 죽었을 때 타이머 정지 / 게임오버 화면을 띄우기 위해 미리 캐싱
    }

    public override void Spawned()
    {
        // owner만 초기 체력을 세팅한다 (proxy는 네트워크로 값을 받음).
        if (HasStateAuthority)
            Health = heartAnimators.Length;

        // 늦게 접속한 경우 현재 상태를 기준값으로 잡아, 스폰되자마자 옛날 연출이
        // 한 번 재생되는 걸 막는다.
        _lastSeenDodgeVersion = DodgeVersion;
        _lastSeenHealth = Health;
        _lastSeenIsDead = IsDead;
    }

    // 일반 Update(): 화면 프레임마다 확실히 호출되므로, "눌린 순간에만 true인" 입력(GetKeyDown)은
    // 반드시 여기서 잡아야 한다.
    void Update()
    {
        if (!HasInputAuthority) return; // 내 캐릭터가 아니면 여기서 끝
        if (PauseMenuManager.InputLocked) return; // 일시정지 메뉴 열림 → 입력 무시

        if (Input.GetKeyDown(KeyCode.Space))
        {
            dodgeRequested = true;
        }
        // 게임오버 상태에서 R → 내 "재시작 원함" 표시
        if (Input.GetKeyDown(KeyCode.R) && gameManager != null && gameManager.IsGameOver)
        {
            if (GameClock.Instance != null) GameClock.Instance.RequestRestart();
        }
    }

    // Fusion이 네트워크 시뮬레이션 틱마다 호출하는 함수. 실제 이동/애니메이션 처리는 전부 여기서 한다.
    public override void FixedUpdateNetwork()
    {
        if (!HasInputAuthority) return; // 내 캐릭터가 아니면 여기서 끝

        // ★ 내 주변에 적이 있는지 매 틱 직접 검사한다 (OnTriggerEnter 대신).
        //   OverlapSphere는 레이어 충돌 매트릭스와 무관하게, 그 순간 그 자리에 콜라이더가
        //   있으면(트리거 포함) 잡는다. 적이 프록시라 NetworkTransform으로 보간 이동해도
        //   확실히 잡힘 — 호스트만 피격되고 게스트는 무적이던 버그의 해결책.
        if (!IsDead && InvincibleTimer.ExpiredOrNotRunning(Runner))
        {
            int n = Physics.OverlapSphereNonAlloc(
                transform.position + hitOffset, hitRadius, _hitBuf,
                ~0, QueryTriggerInteraction.Collide); // 적 콜라이더는 트리거라 Collide 필수

            for (int i = 0; i < n; i++)
            {
                var enemy = _hitBuf[i].GetComponentInParent<Enemy>();
                if (enemy == null) continue; // 적이 아니면 무시 (벽/하트/다른 플레이어 등)

                Health = Mathf.Max(0, Health - 1);
                InvincibleTimer = TickTimer.CreateFromSeconds(Runner, invincibleDuration);
                if (Health <= 0) IsDead = true;

                // 이 적을 없앨 수 있는 건 그 적의 주인(마스터)뿐 → RPC로 요청
                if (enemy.Object != null && enemy.Object.IsValid)
                    enemy.RPC_Consume();

                break; // 한 틱에 한 대만 맞는다
            }
        }

        if (IsDead)
        {
            rb.linearVelocity = Vector3.zero;
            if (HasStateAuthority)
                UpdateRevive();

            // Restart is handled by GameClock, including while players are alive.
            return;
        }

        // 구르기 쿨타임은 항상 흐른다 (게임은 안 멈추므로, 메뉴 열려 있어도 계속 회복)
        if (dodgeCooldownTimer > 0f)
            dodgeCooldownTimer -= Runner.DeltaTime;

        // 일시정지 메뉴가 열려 있으면 "입력"만 막는다 (이동/새 구르기 시작). 시간은 안 멈춤.
        if (PauseMenuManager.InputLocked)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            NetIsRun = false;
            NetIsWalk = false;
            dodgeRequested = false;
            return;
        }

        if (cam == null)
        {
            return;
        }

        // 이동 입력 읽기 (WASD/방향키 + Walk 버튼)
        hAxis = Input.GetAxisRaw("Horizontal");
        vAxis = Input.GetAxisRaw("Vertical");
        wDown = Input.GetButton("Walk");

        // 카메라가 바라보는 방향 기준으로 이동 방향을 계산
        Vector3 camForward = cam.forward;
        Vector3 camRight = cam.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        moveVec = (camForward * vAxis + camRight * hAxis).normalized;

        // Update()에서 저장해둔 구르기 요청을 여기서 소비
        if (dodgeRequested)
        {
            dodgeRequested = false; // 이번 틱에서 소비. 못 쓰더라도(쿨다운 중 등) 다시 대기시키지 않음

            if (!isDodging && dodgeCooldownTimer <= 0f)
            {
                DodgeVersion++; // 모든 클라에 "구르기 시작" 알림 (Render에서 트리거 재생)
                StartCoroutine(DodgeRoutine());
            }
        }

        // 애니메이션 상태는 네트워크 변수에만 쓴다 (실제 Animator 반영은 Render()에서 전 클라가 함)
        NetIsRun = moveVec != Vector3.zero;
        NetIsWalk = wDown;

        if (moveVec != Vector3.zero)
            transform.LookAt(transform.position + moveVec); //나아가는 방향으로 바라본다

        // 구르는 중이면 빠르게, 걷기 버튼 누르면 느리게, 평소엔 기본 속도
        float speedMultiplier = isDodging ? dodgespeedMultiplier : (wDown ? 0.3f : 1f);
        Vector3 horizontalVelocity = moveVec * speed * speedMultiplier;
        rb.linearVelocity = new Vector3(horizontalVelocity.x, rb.linearVelocity.y, horizontalVelocity.z); // y(중력)는 그대로 두고 수평 이동만 덮어씀
    }

    // Fusion이 렌더 프레임마다 호출한다 (내 캐릭터/상대 캐릭터 모두).
    public override void Render()
    {
        if (anim == null) return;
        anim.SetBool("isRun", NetIsRun);
        anim.SetBool("isWalk", NetIsWalk);

        // authority가 구르면 DodgeVersion이 바뀐다 → 모든 클라(내/상대)가 여기서 트리거를 쏜다
        if (DodgeVersion != _lastSeenDodgeVersion)
        {
            _lastSeenDodgeVersion = DodgeVersion;
            anim.SetTrigger("Dodge");
        }

        // 체력이 줄었으면: 줄어든 하트를 왼쪽부터 순서대로 사라지게 + (죽지 않았으면) 빨간 피격 연출
        if (Health < _lastSeenHealth)
        {
            for (int h = _lastSeenHealth; h > Health; h--)
            {
                int heartIndex = heartAnimators.Length - h;
                if (heartIndex >= 0 && heartIndex < heartAnimators.Length && heartAnimators[heartIndex] != null)
                    heartAnimators[heartIndex].SetTrigger("Disappear");
            }

            if (!IsDead)
                StartCoroutine(HitFlashRoutine());

            // 화면 빨간 비네트 깜빡임 — "내 캐릭터"가 맞았을 때 내 화면에서만
            if (HasInputAuthority && DamageVignette.Instance != null)
                DamageVignette.Instance.Flash(IsDead ? 3 : DamageVignette.Instance.blinks);
        }
        // 체력이 늘었으면(부활): 돌아온 하트를 다시 보이게
        else if (Health > _lastSeenHealth)
        {
            for (int i = heartAnimators.Length - Health; i < heartAnimators.Length; i++)
            {
                if (i >= 0 && i < heartAnimators.Length && heartAnimators[i] != null)
                    heartAnimators[i].Rebind();
            }
        }
        _lastSeenHealth = Health;

        // 다운/부활 순간 연출은 모든 클라에서
        if (IsDead != _lastSeenIsDead)
        {
            _lastSeenIsDead = IsDead;
            if (IsDead) HandleDeath();
            else        HandleRevive();
        }

        // 전원 다운(co-op 전멸) → 게임오버. 내 화면에서만 처리하고, 마지막 다운 후 잠깐 유예.
        if (IsDead && HasInputAuthority && Time.time - _downedSince > 1.2f && AllPlayersDowned())
        {
            if (gameManager != null) gameManager.Endgame();
        }
    }

    // 구르기 진행: 짧은 시간 동안 isDodging을 켜서 속도 배율을 올리고, 쿨다운을 시작한다.
    private IEnumerator DodgeRoutine()
    {
        isDodging = true;
        dodgeCooldownTimer = dodgeCooldown;

        yield return new WaitForSeconds(dodgeDuration);

        isDodging = false;
    }

    // ★ 삭제됨 : RPC_ApplyHit()
    //   예전엔 마스터의 Enemy.OnTriggerEnter가 이 RPC로 데미지를 통보했지만,
    //   이제 맞은 플레이어가 FixedUpdateNetwork의 OverlapSphere로 직접 처리한다.
    //   (에디터 디버그 버튼 Editor_Hit은 Health를 직접 깎으므로 영향 없음)

    // 최대 체력(하트 칸 수). heartAnimators 길이를 그대로 쓴다.
    public int MaxHealth => heartAnimators != null ? heartAnimators.Length : 3;

    // 하트 아이템을 먹을 수 있는 상태인가 (살아있고 체력이 안 찬 경우).
    public bool CanPickUpHeart => !IsDead && Health < MaxHealth;

    // 하트 아이템 획득 시 HeartPickup 이 호출하는 RPC.
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_Heal()
    {
        if (IsDead) return;
        if (Health >= MaxHealth) return;

        Health = Mathf.Min(MaxHealth, Health + 1);
    }

    // 다운 순간 처리. Render()에서 IsDead가 false→true로 바뀐 걸 감지하면 모든 클라에서 불린다.
    private void HandleDeath()
    {
        anim.SetTrigger("Die");

        if (HasStateAuthority)
        {
            rb.linearVelocity = Vector3.zero; // 쓰러지는 순간 미끄러지지 않게
            ReviveProgress = 0f;
        }

        _downedSince = Time.time;
    }

    // 부활 순간 처리. Render()에서 IsDead가 true→false로 바뀐 걸 감지하면 모든 클라에서 불린다.
    private void HandleRevive()
    {
        // Die 상태는 빠져나가는 전이가 없는 막다른 상태라, 직접 Idle로 되돌린다.
        anim.ResetTrigger("Die");
        anim.Play("Idle", 0, 0f);
    }

    // 다운 상태에서 매 틱 실행 (내 캐릭터의 StateAuthority에서만).
    private void UpdateRevive()
    {
        bool beingRevived = false;

        foreach (var p in FindObjectsByType<Player>(FindObjectsSortMode.None))
        {
            if (p == this || p.IsDead) continue;
            if (Vector3.Distance(p.transform.position, transform.position) <= reviveRange)
            {
                beingRevived = true;
                break;
            }
        }

        if (beingRevived)
        {
            ReviveProgress += Runner.DeltaTime;
            if (ReviveProgress >= reviveDuration)
            {
                Health = Mathf.Clamp(reviveHealth, 1, heartAnimators.Length);
                IsDead = false;
                ReviveProgress = 0f;
                InvincibleTimer = TickTimer.CreateFromSeconds(Runner, invincibleDuration); // 부활 직후 잠깐 무적
            }
        }
        else
        {
            ReviveProgress = 0f; // 팀원이 범위를 벗어나면 진행 초기화
        }
    }

    // 씬의 모든 플레이어가 다운 상태인가 (co-op 전멸 판정).
    private static bool AllPlayersDowned()
    {
        var players = FindObjectsByType<Player>(FindObjectsSortMode.None);
        if (players.Length == 0) return false;
        foreach (var p in players)
            if (!p.IsDead) return false;
        return true;
    }

    // 맞았을 때(죽지 않은 경우) 잠깐 빨갛게 깜박이는 연출. 무적 시간 자체는 InvincibleTimer가 담당.
    private IEnumerator HitFlashRoutine()
    {
        float timer = 0f;
        bool showRed = true;

        while (timer < invincibleDuration)
        {
            if (showRed)
                SetBodyColor(Color.red);
            else
                ClearBodyColor(); // 원래 머티리얼 색으로 정확히 복원
            showRed = !showRed;

            yield return new WaitForSeconds(flashInterval);
            timer += flashInterval;
        }

        ClearBodyColor();
    }

    // 캐릭터 몸 파츠 전체를 지정한 색으로 덮어씌우는 헬퍼 (피격 깜박임용).
    private void SetBodyColor(Color color)
    {
        var block = new MaterialPropertyBlock();
        foreach (var r in bodyRenderers)
        {
            r.GetPropertyBlock(block);
            block.SetColor("_BaseColor", color);
            block.SetColor("_EmissionColor", color);
            r.SetPropertyBlock(block);
        }
    }

    // SetBodyColor로 덮어씌운 색을 걷어내고 머티리얼 원래 색으로 되돌린다.
    private void ClearBodyColor()
    {
        foreach (var r in bodyRenderers)
        {
            r.SetPropertyBlock(null);
        }
    }

#if UNITY_EDITOR
    // ── 에디터 디버그용 (빌드에서 제외됨). PlayerEditor 커스텀 인스펙터의 버튼이 호출한다.
    public void Editor_Hit()
    {
        if (!HasStateAuthority || IsDead) return;
        Health = Mathf.Max(0, Health - 1);
        InvincibleTimer = TickTimer.CreateFromSeconds(Runner, invincibleDuration);
        if (Health <= 0) IsDead = true;
    }

    public void Editor_Heal()
    {
        if (!HasStateAuthority || IsDead) return;
        Health = Mathf.Min(MaxHealth, Health + 1);
    }

    public void Editor_ForceDown()
    {
        if (!HasStateAuthority || IsDead) return;
        Health = 0;
        IsDead = true;
    }

    public void Editor_Revive()
    {
        if (!HasStateAuthority || !IsDead) return;
        Health = Mathf.Clamp(reviveHealth, 1, heartAnimators != null ? heartAnimators.Length : 3);
        IsDead = false;
        ReviveProgress = 0f;
        InvincibleTimer = TickTimer.CreateFromSeconds(Runner, invincibleDuration);
    }
#endif
}
