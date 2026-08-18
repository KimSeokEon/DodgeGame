using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager2 : MonoBehaviour
{
    [Header("캐릭터 사망 시 켜줄 게임오버 로고 오브젝트")]
    public GameObject gameOverLogo;

    private RectTransform logoRect;
    private CanvasGroup logoGroup;
    private Vector3 logoOriginalScale;   // 에디터에서 설정한 원래 크기를 기억할 변수
    private bool isGameOver;

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
    }

    void Update()
    {
        if (isGameOver && Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    // Player가 죽었을 때 호출: 게임오버 로고를 띄운다
    public void Endgame()
    {
        if (isGameOver) return;
        isGameOver = true;

        if (gameOverLogo != null)
        {
            gameOverLogo.SetActive(true);
            logoRect.localScale = Vector3.zero;
            logoGroup.alpha = 0f;

            Sequence seq = DOTween.Sequence();
            seq.Append(logoGroup.DOFade(1f, 0.3f));
            seq.Join(logoRect.DOScale(logoOriginalScale, 0.5f).SetEase(Ease.OutBack));
            seq.Append(logoRect.DOPunchScale(Vector3.one * 0.1f, 0.3f, 4, 0.5f)); // 착지 후 살짝 흔들림
            seq.SetUpdate(true); // Time.timeScale을 0으로 멈춰도 애니메이션은 재생되게
        }
    }
}
