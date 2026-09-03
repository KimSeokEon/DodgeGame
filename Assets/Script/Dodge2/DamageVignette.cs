using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

// =============================================================================
// DamageVignette.cs
// -----------------------------------------------------------------------------
// 피격 시 화면 가장자리를 빨갛게 깜빡이는 연출.
// 전용 Global Volume(Vignette 오버라이드, Weight 0) 오브젝트에 붙인다.
// Player.cs 가 "내 캐릭터"가 맞았을 때 DamageVignette.Instance.Flash() 를 호출.
// 색/세기 등 룩은 Volume Profile 에서 조정하고, 이 스크립트는 weight 만 애니메이션한다.
// =============================================================================
[RequireComponent(typeof(Volume))]
public class DamageVignette : MonoBehaviour
{
    public static DamageVignette Instance { get; private set; }

    [SerializeField] private Volume volume;

    [Header("연출")]
    [Range(0f, 1f)] public float peakWeight = 1f;
    [Min(1)] public int blinks = 2;
    public float blinkUpTime = 0.05f;
    public float blinkDownTime = 0.12f;
    public float fadeOutTime = 0.25f;
    [Tooltip("깜빡이는 사이에 이만큼은 남겨둠 (0 = 매번 완전히 꺼짐)")]
    [Range(0f, 0.5f)] public float betweenWeight = 0.12f;

    private Coroutine _co;

    void Awake()
    {
        Instance = this;
        if (volume == null) volume = GetComponent<Volume>();
        if (volume != null) volume.weight = 0f;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // 일반 피격
    public void Flash() => Flash(blinks);

    // 횟수를 지정해서 호출 (예: 사망 시 더 세게)
    public void Flash(int blinkCount)
    {
        if (volume == null) return;
        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(FlashRoutine(Mathf.Max(1, blinkCount)));
    }

    private IEnumerator FlashRoutine(int n)
    {
        for (int i = 0; i < n; i++)
        {
            yield return Ramp(volume.weight, peakWeight, blinkUpTime);
            float low = (i == n - 1) ? 0f : peakWeight * betweenWeight;
            yield return Ramp(volume.weight, low, blinkDownTime);
        }
        yield return Ramp(volume.weight, 0f, fadeOutTime);
        volume.weight = 0f;
        _co = null;
    }

    private IEnumerator Ramp(float from, float to, float dur)
    {
        if (dur <= 0f) { volume.weight = to; yield break; }
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime; // timeScale 0 (게임오버 등) 이어도 재생
            volume.weight = Mathf.Lerp(from, to, Mathf.Clamp01(t / dur));
            yield return null;
        }
        volume.weight = to;
    }
}
