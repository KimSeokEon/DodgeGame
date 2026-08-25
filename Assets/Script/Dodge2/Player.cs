using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine.UIElements;


// =============================================================================
// Player.cs
// -----------------------------------------------------------------------------
// 역할 : 플레이어 캐릭터의 이동/구르기/피격/사망을 전부 담당하는 메인 컨트롤러.
// 붙는 곳 : Assets/Resources/Player (1).prefab (Fusion 네트워크 프리팹)
//           PlayerSpawner.cs가 이 프리팹을 Runner.Spawn()으로 생성해서 붙여준다.
// 네트워크 : Fusion의 NetworkBehaviour를 상속. 캐릭터가 여러 명 접속해도
//           "이게 내 캐릭터인지"는 HasInputAuthority로 판단한다.
//           (주의) 아직 currentHealth/isDead 같은 상태값이 [Networked]로
//           선언되어 있지 않아서, 다른 클라이언트 화면에는 체력/사망 상태가
//           동기화되지 않는다. 이건 멀티플레이 후속 작업 대상.
// =============================================================================
public class Player : NetworkBehaviour
{
    private MeshRenderer[] bodyRenderers; //캐릭터 몸 파츠
    private bool isInvincible = false;
    public float invincibleDuration = 1.2f; // 무적 지속 시간
    public float flashInterval = 0.1f; // 깜박이는 간격

    // 매 틱 FixedUpdateNetwork()에서 읽어서 이동에 쓰는 입력값들
    float hAxis;
    float vAxis;
    bool wDown; // 걷기(Walk) 버튼이 눌려있는지
    public float speed; // 기본 이동 속도

    public Transform cam; // 이동 방향 계산 기준이 되는 카메라. 비어있으면 Start()에서 Camera.main으로 채움

    // GameManager2가 생존 타이머/게임오버를 전부 담당함 (SurvivalTimer.cs는 GameManager2로 통합됨)
    private GameManager2 gameManager;

    [Header("Heart3 -> 2 -> 1  순서로 등록")] public Animator[] heartAnimators; // 하트 UI 3개, TakeDamage될 때마다 순서대로 Disappear 애니메이션 재생

    private int currentHealth; // 남은 하트 개수. Awake()에서 heartAnimators.Length로 초기화됨

    private Rigidbody rb;
    Vector3 moveVec; // 이번 틱에 이동할 방향(정규화됨)

    private Animator anim; // 캐릭터 모델의 Animator (isRun/isWalk/Dodge/Die 파라미터 제어)
    private bool isDead = false;

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
        currentHealth = heartAnimators.Length; // 하트 개수 = 시작 체력
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
        
    }

    // Fusion이 네트워크 시뮬레이션 틱마다 호출하는 함수. 실제 이동/애니메이션 처리는 전부 여기서 한다.
    public override void FixedUpdateNetwork()
    {
        if (!HasInputAuthority) return; // 내 캐릭터가 아니면 여기서 끝
        if (isDead) return; //죽은뒤엔 이동 정지

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
                StartCoroutine(DodgeRoutine());
            }
        }

        anim.SetBool("isRun", moveVec != Vector3.zero);
        anim.SetBool("isWalk", wDown);

        if (moveVec != Vector3.zero)
            transform.LookAt(transform.position + moveVec); //나아가는 방향으로 바라본다

        // 구르는 중이면 빠르게, 걷기 버튼 누르면 느리게, 평소엔 기본 속도
        float speedMultiplier = isDodging ? dodgespeedMultiplier : (wDown ? 0.3f : 1f);
        Vector3 horizontalVelocity = moveVec * speed * speedMultiplier;
        rb.linearVelocity = new Vector3(horizontalVelocity.x, rb.linearVelocity.y, horizontalVelocity.z); // y(중력)는 그대로 두고 수평 이동만 덮어씀
    }

    // 구르기 진행: 짧은 시간 동안 isDodging을 켜서 속도 배율을 올리고, 쿨다운을 시작한다.
    private IEnumerator DodgeRoutine()
    {
        isDodging = true;
        dodgeCooldownTimer = dodgeCooldown;

        anim.SetTrigger("Dodge");

        yield return new WaitForSeconds(dodgeDuration);

        isDodging = false;
    }

    // 적(Enemy)에게 맞았을 때 Enemy.cs가 호출하는 함수. 하트 하나를 깎고, 0이 되면 사망 처리.
    public void TakeDamage()
    {
        if (currentHealth <= 0 || isInvincible) return; // 이미 죽었거나 무적 중이면 무시

        int heartIndex = heartAnimators.Length - currentHealth; // 왼쪽 하트부터 순서대로 사라지게
        heartAnimators[heartIndex].SetTrigger("Disappear");

        currentHealth--;

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(HitFlashRoutine()); //죽지 않았을 때, 깜빡임 + 무적
        }
    }

    // 사망 처리 시작: 이동을 멈추고, 죽는 애니메이션을 재생한 뒤 DieRoutine()으로 마무리를 넘긴다.
    public void Die()
    {
        if (isDead) return; //죽은뒤엔 이동 정지
        isDead = true;

        if (gameManager != null)
        {
            gameManager.StopTimer(); // 죽는 그 순간 바로 타이머 정지
        }

        rb.linearVelocity = Vector3.zero; //죽은 순간 미끄럼지지 않게 정지
        anim.SetTrigger("Die");

        StartCoroutine(DieRoutine());
    }

    // 맞았을 때(죽지 않은 경우) 잠깐 빨갛게 깜박이면서 무적 시간을 부여하는 연출
    private IEnumerator HitFlashRoutine()
    {
        isInvincible = true;

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
        isInvincible = false;
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

    // 죽는 애니메이션이 끝날 시간(1.5초)만큼 기다렸다가 캐릭터를 비활성화하고 게임오버 화면을 띄운다.
    private IEnumerator DieRoutine()
    {
        yield return new WaitForSeconds(1.5f);

        gameObject.SetActive(false);

        if (gameManager != null)
        {
            gameManager.Endgame();
        }
    }
}
