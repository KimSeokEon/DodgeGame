using System;
using System.Net.Mime;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// =============================================================================
// GameManager2.cs
// -----------------------------------------------------------------------------
// 역할 : PlayScene의 게임 상태(생존 타이머, 게임오버 연출, 최고기록, 재시작)를
//        전부 관리하는 매니저. Player.cs가 죽으면 이 스크립트의 Endgame()을 호출한다.
// 붙는 곳 : PlayScene.unity의 "GameManager" 오브젝트 (씬에 하나만 존재)
// 참고 : 원래 타이머 로직은 별도 파일 SurvivalTimer.cs였는데, 사실상 GameManager의
//        일부라 이 파일로 통합했다. isGameOver / isTimerRunning 두 플래그가 따로
//        있는 이유는 "죽는 즉시 타이머는 멈추지만(Die), 게임오버 UI는 죽는 애니메이션이
//        끝난 1.5초 뒤에 뜨기(Endgame) 때문" — 두 시점이 다르다.
// =============================================================================
public class GameManager2 : MonoBehaviour
{
    private const string BestTimeKey = "BestTime_Dodge2"; // PlayerPrefs에 최고기록을 저장할 때 쓰는 키

    [Header("캐릭터 사망 시 켜줄 게임오버 로고 오브젝트")]


    [Header("로고 애니메이션 끝나고 페이드인 할 재시작 안내 문구")]


    [Header("생존 타이머 (죽으면 멈춰야 함)")]
    public TMP_Text timerText; // 화면 상단에 카운트업되는 생존 시간 텍스트

    public GameObject restartText; // "Press R to Restart" 안내 문구
    public GameObject gameOverLogo; // 게임오버 로고 오브젝트 (평소엔 비활성화, 죽으면 켜짐)
    [Header("최고기록 컴포넌트를 담아둘 변수")]
    public TMP_Text besttime; // 게임오버 화면에 표시할 "BEST TIME : 00:00.00" 텍스트

    // gameOverLogo 연출(스케일/페이드)에 쓰는 캐시된 컴포넌트들
    private RectTransform logoRect;
    private CanvasGroup logoGroup;
    private Vector3 logoOriginalScale;   // 에디터에서 설정한 원래 크기를 기억할 변수

    private CanvasGroup restartTextGroup;
    private CanvasGroup bestTimeGroup;
    private bool isGameOver; // 게임오버 UI가 떴는지 (R키로 재시작 가능해지는 시점)

    // 원래 SurvivalTimer.cs에 있던 상태 (여기로 통합)
    // isGameOver와는 별개 플래그: Die()에서 즉시 멈추고, Endgame()은 1.5초 뒤에 불림
    private float elapsedTime; // 지금까지 생존한 시간(초)
    private bool isTimerRunning = false; // 타이머가 흐르고 있는 중인지

    void OnEnable()
    {
        PlayerSpawner.LocalPlayerSpawned += HandleGameStart;
    }

    void OnDisable()
    {
        PlayerSpawner.LocalPlayerSpawned -= HandleGameStart;
    }

    private void HandleGameStart()
    {
        isTimerRunning = true;
    }

    // 시작할 때 게임오버/재시작/최고기록 UI를 전부 숨겨두는 초기화 작업
    void Start()
    {
        isGameOver = false;

        if (gameOverLogo != null)
        {
            logoRect = gameOverLogo.GetComponent<RectTransform>();
            logoOriginalScale = logoRect.localScale; // 지금 인스펙터에 세팅된 스케일을 기억
            logoGroup = gameOverLogo.GetComponent<CanvasGroup>();
            if (logoGroup == null)
                logoGroup = gameOverLogo.AddComponent<CanvasGroup>();

            gameOverLogo.SetActive(false);
        }

        if (restartText != null)
        {
            restartTextGroup = restartText.GetComponent<CanvasGroup>();
            if (restartTextGroup == null)
                restartTextGroup = restartText.AddComponent<CanvasGroup>();

            restartTextGroup.alpha = 0f; // 처음엔 안 보이게
        }

        if (besttime != null)
        {
            bestTimeGroup = besttime.GetComponent<CanvasGroup>();
            if (bestTimeGroup == null)
                bestTimeGroup = besttime.gameObject.AddComponent<CanvasGroup>();

            bestTimeGroup.alpha = 0f; // 처음엔 안 보이게
        }
    }

    // 매 프레임: 타이머 카운트업 + 게임오버 상태에서 R키 눌리면 씬 재시작
    void Update()
    {
        if (isTimerRunning)
        {
            elapsedTime += Time.deltaTime;
            if (timerText != null)
            {
                timerText.text = FormatTime(elapsedTime);
            }
        }

        if (isGameOver && Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    // 원래 SurvivalTimer.StopTimer() — Player.Die()에서 죽는 즉시 호출됨
    public void StopTimer()
    {
        isTimerRunning = false;
    }

    // 원래 SurvivalTimer.GetElapsedTime()
    public float GetElapsedTime()
    {
        return elapsedTime;
    }

    // Player가 죽었을 때 호출: 게임오버 로고를 띄운다
    public void Endgame()
    {
        if (isGameOver) return; // 중복 호출 방지
        isGameOver = true;

        StopTimer(); // 죽은 순간의 생존 시간에서 멈춤 (Die()에서 이미 멈췄다면 여기선 아무 효과 없음)

        UpdateBestTime();

        if (gameOverLogo != null)
        {
            // 로고를 0 크기 + 투명 상태로 초기화한 뒤
            gameOverLogo.SetActive(true);
            logoRect.localScale = Vector3.zero;
            logoGroup.alpha = 0f;

            // DOTween 시퀀스로 순서대로 연출: 페이드인 + 팝업 스케일 -> 살짝 흔들림 -> 재시작 문구/최고기록 페이드인
            Sequence seq = DOTween.Sequence();
            seq.Append(logoGroup.DOFade(1f, 0.3f));
            seq.Join(logoRect.DOScale(logoOriginalScale, 0.5f).SetEase(Ease.OutBack));
            seq.Append(logoRect.DOPunchScale(Vector3.one * 0.1f, 0.3f, 4, 0.5f)); // 착지 후 살짝 흔들림

            // 로고 연출이 다 끝난 뒤 "Press 'R' to Restart" 문구와 최고기록을 함께 페이드인
            if (restartTextGroup != null)
            {
                seq.Append(restartTextGroup.DOFade(1f, 0.6f));
            }
            if (bestTimeGroup != null)
            {
                seq.Join(bestTimeGroup.DOFade(1f, 0.6f));
            }

            seq.SetUpdate(true); // Time.timeScale을 0으로 멈춰도 애니메이션은 재생되게
        }
    }

    // 이번 생존 시간과 저장된 최고기록을 비교해서 갱신하고, besttime 텍스트에 표시
    private void UpdateBestTime()
    {
        float surviveTime = GetElapsedTime();
        float bestTime = PlayerPrefs.GetFloat(BestTimeKey, 0f);

        if (surviveTime > bestTime)
        {
            bestTime = surviveTime;
            PlayerPrefs.SetFloat(BestTimeKey, bestTime);
        }

        if (besttime != null)
        {
            besttime.text = "BEST TIME : " + FormatTime(bestTime);
        }
    }

    // 원래 SurvivalTimer.FormatTime() — 초 단위 시간을 "00:00.00" 형태로 바꿔주는 공통 함수
    public static string FormatTime(float elapsedTime)
    {
        int minutes = Mathf.FloorToInt(elapsedTime / 60f);
        float seconds = elapsedTime % 60f;
        return string.Format("{0:00}:{1:00.00}", minutes, seconds);
    }
}
