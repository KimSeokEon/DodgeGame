using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class KeyGuide : MonoBehaviour
{
    [SerializeField] float fadeInTime  = 0.4f;
    [SerializeField] float visibleTime = 4f;    // 완전히 보이는 유지 시간
    [SerializeField] float fadeOutTime = 0.8f;
    [SerializeField] bool  disableOnEnd = true; // 끝나면 오브젝트 끄기

    CanvasGroup cg;
    Sequence seq;

    void Awake() => cg = GetComponent<CanvasGroup>();

    void OnEnable() => Play();

    public void Play()
    {
        seq?.Kill();          // 돌던 애니 있으면 정리
        cg.alpha = 0f;

        seq = DOTween.Sequence()
            .Append(cg.DOFade(1f, fadeInTime))
            .AppendInterval(visibleTime)
            .Append(cg.DOFade(0f, fadeOutTime));

        if (disableOnEnd)
            seq.OnComplete(() => gameObject.SetActive(false));
    }

    void OnDisable()
    {
        seq?.Kill();
        seq = null;
    }
}