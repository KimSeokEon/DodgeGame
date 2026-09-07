using System.Collections;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

// =============================================================================
// PauseMenuManager.cs
// -----------------------------------------------------------------------------
// ESC 로 열고 닫는 일시정지 오버레이 메뉴.
// 붙는 곳 : PlayScene.unity 의 PauseMenu 캔버스(또는 그 하위 매니저 오브젝트).
//
// 설계 결정 (사용자 확정):
//   - 시간(timeScale)은 멈추지 않는다. 싱글/멀티 동일하게 "오버레이 + 화면 블러"만.
//   - 메뉴가 열려 있는 동안 캐릭터 조작 입력을 전부 막는다 (PauseMenuManager.InputLocked).
//   - 게임오버 화면에서도 ESC 로 열린다.
//   - 화면 블러/틴트는 PauseBlurFeature(URP 렌더 피처)가 담당. 여기선 켜고 끄고,
//     Weight 를 2초에 걸쳐 0→1 로 페이드한다.
//
// 버튼 배선 (캔버스 UI 는 사용자가 직접 연결):
//   Resume        -> OnResumeButton()
//   Restart       -> OnRestartButton()      ※ 기능 미구현. 아래 주석 참고.
//   Back to Main  -> OnMainMenuButton()
// =============================================================================
public class PauseMenuManager : MonoBehaviour
{
    // 메뉴가 열려 있는가. Player.cs 등이 이 값을 보고 입력을 막는다.
    public static bool IsOpen { get; private set; }
    public static bool InputLocked => IsOpen;

    [Header("메뉴 루트 (평소 비활성 상태로 두기)")]
    [SerializeField] private GameObject menuRoot;

    [Header("멀티플레이일 때 숨길 오브젝트 (예: Restart 버튼)")]
    [Tooltip("GameModeState.Mode 가 Single 이 아니면 여기 오브젝트들을 꺼둔다")]
    [SerializeField] private GameObject[] hideInMultiplayer;

    [Header("블러 페이드 시간(초)")]
    [SerializeField] private float fadeInTime = 2f;
    [SerializeField] private float fadeOutTime = 0.4f;

    private Coroutine _fade;

    void Awake()
    {
        IsOpen = false;
        if (menuRoot != null) menuRoot.SetActive(false);
        PauseBlurFeature.Active = false;
        PauseBlurFeature.Weight = 0f;
    }

    void OnDestroy()
    {
        // 씬을 벗어날 때 상태를 확실히 원복 (안 하면 다음 씬에 블러가 남는다)
        IsOpen = false;
        PauseBlurFeature.Active = false;
        PauseBlurFeature.Weight = 0f;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            Toggle();
    }

    // ── 열기 / 닫기 ────────────────────────────────────────────
    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    public void Open()
    {
        if (IsOpen) return;
        IsOpen = true;

        if (menuRoot != null) menuRoot.SetActive(true);
        ApplyMultiplayerVisibility();

        PauseBlurFeature.Active = true;
        StartFade(1f, fadeInTime);
    }

    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;

        if (menuRoot != null) menuRoot.SetActive(false);
        StartFade(0f, fadeOutTime, deactivateWhenZero: true);
    }

    // ── 블러 Weight 페이드 ─────────────────────────────────────
    private void StartFade(float target, float dur, bool deactivateWhenZero = false)
    {
        if (_fade != null) StopCoroutine(_fade);
        _fade = StartCoroutine(FadeRoutine(target, dur, deactivateWhenZero));
    }

    private IEnumerator FadeRoutine(float target, float dur, bool deactivateWhenZero)
    {
        float start = PauseBlurFeature.Weight;
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime; // 시간은 안 멈추지만 연출은 unscaled 로 통일
            PauseBlurFeature.Weight = Mathf.Lerp(start, target, dur > 0f ? t / dur : 1f);
            yield return null;
        }
        PauseBlurFeature.Weight = target;

        if (deactivateWhenZero && target <= 0f)
            PauseBlurFeature.Active = false;

        _fade = null;
    }

    // ── 버튼 핸들러 ────────────────────────────────────────────
    public void OnResumeButton() => Close();

    // Restart : 기능 미구현 (사용자 요청). 구현 방법 —
    //  ● 싱글플레이:
    //      var runner = FindFirstObjectByType<NetworkRunner>();
    //      runner.LoadScene(SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex));
    //    (Player.cs 의 게임오버 R키 재시작과 같은 경로)
    //  ● 멀티플레이 (전원 동의 방식 재활용):
    //      1) Player.cs 에 public 메서드 추가 → 내 Player 의 WantsRestart = true
    //      2) Player.FixedUpdateNetwork 의 재시작 검사(현재 IsDead 블록 안에 있음)를
    //         "게임오버가 아니어도" 돌도록 밖으로 빼서, AllPlayersWantRestart() 가
    //         true 면 마스터가 Runner.LoadScene(...) 호출
    //      3) 게임 도중 취소도 되게 하려면 Resume/Close 시 WantsRestart = false 로 되돌리기
    public void OnRestartButton()
    {
        Debug.Log("[PauseMenu] Restart 눌림 — 기능 미구현 (PauseMenuManager.OnRestartButton 참고)");
    }

    public void OnMainMenuButton()
    {
        StartCoroutine(BackToMainMenuRoutine());
    }

    private IEnumerator BackToMainMenuRoutine()
    {
        // 블러/입력잠금 상태 원복
        IsOpen = false;
        PauseBlurFeature.Active = false;
        PauseBlurFeature.Weight = 0f;

        // 로비를 거쳐 온 경우 러너는 DontDestroyOnLoad 라 직접 종료해야 한다.
        GameModeState.HasJoinedSession = false;

        var runner = FindFirstObjectByType<NetworkRunner>();
        if (runner != null && runner.IsRunning)
        {
            var task = runner.Shutdown();
            while (!task.IsCompleted)
                yield return null;
        }

        SceneManager.LoadScene("MainMenu");
    }

    // ── 헬퍼 ──────────────────────────────────────────────────
    private void ApplyMultiplayerVisibility()
    {
        if (hideInMultiplayer == null) return;

        bool isMultiplayer = GameModeState.Mode != GameMode.Single;
        foreach (var go in hideInMultiplayer)
            if (go != null) go.SetActive(!isMultiplayer);
    }
}
