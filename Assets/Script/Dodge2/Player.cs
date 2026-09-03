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
//   - 체력/다운(Health/IsDead) : owner만 변경, 데미지는 마스터 Enemy가 RPC_ApplyHit()로 통보
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
    // 데미지는 마스터의 Enemy.OnTriggerEnter가 RPC_ApplyHit()로 통보해서 들어온다.
    [Networked] public int Health { get; private set; }
    [Networked] public bool IsDead { get; private set; }
    [Networked] private TickTimer InvincibleTimer { get; set; } // 피격 후 무적(i-frame)
    
    
    [Networked] public bool WantsRestart { get; private set; }
    private bool _restartTriggered; // 마스터가 씬 리로드를 한 번만 호출하도록

    private int _lastSeenHealth;  // Render에서 "이번에 체력이 줄었나" 판단용
    private bool _lastSeenIsDead; // Render에서 "이번에 죽었나/부활했나" 판단용
    private float _downedSince;   // 언제 다운됐는지 (전멸 게임오버 유예 시간 계산용)

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
    // FixedUpdateNetwork()는 내 캐릭터(HasInputAuthority)에서만 도므로, 이 값을 쓰지 않으면
    // 상대 클라에서는 그 캐릭터의 Animator가 갱신되지 않아 계속 Idle로 보인다.
    // authority 쪽에서 값을 쓰고, Render()에서 모든 클라가 Animator에 반영한다.
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
    // DodgeCooldownUI.cs가 이 값을 읽어서 쿨타임 게이지(회색 오버레이)를 채운다.
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
    // 반드시 여기서 잡아야 한다. FixedUpdateNetwork() 안에서 직접 읽으면 놓칠 수 있음(아래 주석 참고).
    void Update()
    {
        if (!HasInputAuthority) return; // 내 캐릭터가 아니면 여기서 끝

        // GetKeyDown은 눌린 그 프레임에만 true라, FixedUpdateNetwork 틱과 타이밍이
        // 안 맞으면 눌러도 씹힐 수 있음. 그래서 여기서 확실히 잡아뒀다가 다음 네트워크
        // 틱에서 소비한다 (입력 버퍼링).
        if (Input.GetKeyDown(KeyCode.Space))
        {
            dodgeRequested = true;
        }
        // 게임오버 상태에서 R → 내 "재시작 원함" 표시 (LobbyPlayer.ToggleReady와 같은 패턴)
        if (Input.GetKeyDown(KeyCode.R) && gameManager != null && gameManager.IsGameOver)
        {
            WantsRestart = true;
        }
        
    }

    // Fusion이 네트워크 시뮬레이션 틱마다 호출하는 함수. 실제 이동/애니메이션 처리는 전부 여기서 한다.
    public override void FixedUpdateNetwork()
    {
        if (!HasInputAuthority) return; // 내 캐릭터가 아니면 여기서 끝

        if (IsDead)
        {
            rb.linearVelocity = Vector3.zero;
            if (HasStateAuthority)
                UpdateRevive();

            // 마스터(또는 싱글)의 캐릭터가 전원 의사를 확인하고 씬을 리로드. 딱 한 번.
            if (!_restartTriggered
                && Runner != null && (Runner.IsServer || Runner.IsSharedModeMasterClient)
                && AllPlayersWantRestart())
            {
                _restartTriggered = true;
                int idx = SceneManager.GetActiveScene().buildIndex;
                Runner.LoadScene(SceneRef.FromIndex(idx));
            }

            return;
        }

        if (dodgeCooldownTimer > 0f)
            dodgeCooldownTimer -= Runner.DeltaTime;

        if (cam == null)
        {
            return;
        }

        // 이동 입력 읽기 (WASD/방향키 + Walk 버튼)
        hAxis = Input.GetAxisRaw("Horizontal");
        vAxis = Input.GetAxisRaw("Vertical");
        wDown = Input.GetButton("Walk");

        // 카메라가 바라보는 방향 기준으로 이동 방향을 계산 (쿼터뷰 게임이라 카메라 기준 이동이 자연스러움)
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
    // 네트워크로 공유된 이동 상태를 Animator에 반영해서, 상대 클라에서도
    // 그 캐릭터가 뛰거나 걷는 모습이 보이게 한다.
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
        // 체력이 늘었으면(부활): 돌아온 하트를 다시 보이게 (Heart 애니메이터를 기본 상태로 리셋)
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

        // Dodge 애니메이션 트리거는 Render()에서 DodgeVersion 변화를 감지해 재생한다
        // (여기서 직접 쏘면 상대 클라에는 안 보임)

        yield return new WaitForSeconds(dodgeDuration);

        isDodging = false;
    }

    // 마스터의 Enemy.OnTriggerEnter가 "이 플레이어가 적에 맞았다"고 통보하는 RPC.
    // RpcSources.All  : 아무 클라(=마스터)나 호출 가능
    // RpcTargets.StateAuthority : 이 캐릭터의 owner에서만 실행됨 → 거기서 Health를 깎는다
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_ApplyHit()
    {
        if (IsDead) return;
        if (!InvincibleTimer.ExpiredOrNotRunning(Runner)) return; // 무적(i-frame) 중이면 무시

        Health = Mathf.Max(0, Health - 1);
        InvincibleTimer = TickTimer.CreateFromSeconds(Runner, invincibleDuration);

        if (Health <= 0)
            IsDead = true;
    }

    // 다운 순간 처리. Render()에서 IsDead가 false→true로 바뀐 걸 감지하면 모든 클라에서 불린다.
    // 여기서 죽이지 않고 "쓰러진" 상태로 둔다 — 팀원이 부활시킬 수 있음. 전원 다운 시에만 게임오버.
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
    // reviveRange 안에 살아있는 팀원이 있으면 ReviveProgress를 채우고, 다 차면 스스로 부활한다.
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

    // 씬의 모든 플레이어가 다운 상태인가 (co-op 전멸 판정). 혼자 플레이면 = 내가 다운되면 true.
    private static bool AllPlayersDowned()
    {
        var players = FindObjectsByType<Player>(FindObjectsSortMode.None);
        if (players.Length == 0) return false;
        foreach (var p in players)
            if (!p.IsDead) return false;
        return true;
    }
    // 씬의 모든 플레이어가 재시작을 원하는가
    private static bool AllPlayersWantRestart()
    {
        var players = FindObjectsByType<Player>(FindObjectsSortMode.None);
        if (players.Length == 0) return false;
        foreach (var p in players)
            if (!p.WantsRestart) return false;
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
                ClearBodyColor(); // 원래 머티리얼 색(흰색이 아님)으로 정확히 복원
            showRed = !showRed;

            yield return new WaitForSeconds(flashInterval);
            timer += flashInterval;
        }

        ClearBodyColor();
    }

    // 캐릭터 몸 파츠 전체를 지정한 색으로 덮어씌우는 헬퍼 (피격 깜박임용).
    // 셰이더(XRay_Mesh2DLit_Default)의 실제 색상 프로퍼티는 _BaseColor다. (예전엔 _White를 썼는데,
    // 지금 셰이더엔 그런 프로퍼티가 없어서 조용히 무시되고 있었음 — 그래서 빨간 점등이 안 보였던 것)
    private void SetBodyColor(Color color)
    {
        var block = new MaterialPropertyBlock();
        foreach (var r in bodyRenderers)
        {
            r.GetPropertyBlock(block);
            block.SetColor("_BaseColor", color);
            // 이 머티리얼은 _GlowEnabled=1, _GlowIntensity=10짜리 강한 흰색 발광(Emission)이 켜져 있어서,
            // _BaseColor만 바꾸면 발광이 덮어버려서 색이 전혀 안 보인다. 발광 색도 같이 바꿔줘야
            // 실제로 화면에서 빨갛게 보인다. (스크린샷으로 직접 확인한 원인)
            block.SetColor("_EmissionColor", color);
            r.SetPropertyBlock(block);
        }
    }

    // SetBodyColor로 덮어씌운 색을 걷어내고 머티리얼 원래 색으로 되돌린다.
    // Color.white로 강제로 되돌리지 않는 이유: 이 캐릭터 머티리얼의 원래 베이스 컬러는
    // 흰색이 아니라 아주 어두운 회색(0.03,0.03,0.03)이라, 흰색으로 덮으면 오히려 밝기가 이상해짐.
    private void ClearBodyColor()
    {
        foreach (var r in bodyRenderers)
        {
            r.SetPropertyBlock(null);
        }
    }

#if UNITY_EDITOR
    // ── 에디터 디버그용 (빌드에서 제외됨). PlayerEditor 커스텀 인스펙터의 버튼이 호출한다.
    //    자기 캐릭터(StateAuthority)에만 먹는다.
    public void Editor_Hit()
    {
        if (!HasStateAuthority || IsDead) return;
        Health = Mathf.Max(0, Health - 1);
        InvincibleTimer = TickTimer.CreateFromSeconds(Runner, invincibleDuration);
        if (Health <= 0) IsDead = true;
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
