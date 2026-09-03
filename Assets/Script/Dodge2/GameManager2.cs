using System;
using System.Net.Mime;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// =============================================================================
// GameManager2.cs
// -----------------------------------------------------------------------------
// 역할 : PlayScene의 게임오버 연출 / 최고기록 / 타이머 표시를 관리하는 매니저.
//        Player가 전원 다운 상태가 되면 이 스크립트의 Endgame()을 호출한다.
// 붙는 곳 : PlayScene.unity의 "GameManager" 오브젝트 (씬에 하나만 존재)
// 참고 : 생존 시간 자체는 GameClock(NetworkBehaviour)이 네트워크로 공유·관리한다.
//        여기서는 GameClock.Instance.ElapsedSeconds를 읽어 timerText에 표시만 하고,
//        게임오버 시 StopTimer() → GameClock.Stop() 으로 정지 신호만 전달한다.
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
    public bool IsGameOver => isGameOver; // Player가 R키 입력 여부를 판단할 때 읽음

    // 생존 시간은 이제 GameClock(NetworkBehaviour)이 관리한다. 여기선 표시만 한다.
    // 예전엔 각 클라가 Time.deltaTime을 따로 누적해서 호스트/게스트가 어긋났었다.

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

    // 매 프레임: 공유 시계(GameClock)의 경과 시간을 그대로 화면에 표시
    void Update()
    {
        if (timerText != null && GameClock.Instance != null)
        {
            timerText.text = FormatTime(GameClock.Instance.ElapsedSeconds);
        }
    }

    // 게임오버 시 Endgame()에서 호출. 실제 정지는 마스터에서만 일어나고 전 클라에 복제된다.
    public void StopTimer()
    {
        if (GameClock.Instance != null)
            GameClock.Instance.Stop();
    }

    // 최고기록 비교용 — 공유 시계에서 읽는다
    public float GetElapsedTime()
    {
        return GameClock.Instance != null ? GameClock.Instance.ElapsedSeconds : 0f;
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
