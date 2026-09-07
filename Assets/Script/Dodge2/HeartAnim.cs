using UnityEngine;

// =============================================================================
// HeartAnim.cs
// -----------------------------------------------------------------------------
// 역할 : 하트 아이템의 겉보기 연출 — 위아래로 천천히 둥실거리고, 시계방향으로
//        천천히 돈다. 순수 로컬 연출이라 네트워크 동기화는 안 한다
//        (각 클라가 스폰 시점 기준으로 알아서 재생하면 충분).
// 붙는 곳 : "Item Heart Variant" 프리팹의 자식 "Mesh Object"
//          (루트가 아니라 자식에 붙여야 HeartPickup 의 트리거 콜라이더가 안 흔들린다)
// =============================================================================
public class HeartAnim : MonoBehaviour
{
    [Header("위아래 둥실거림")]
    [Tooltip("기준 위치에서 위아래로 흔들리는 폭(m)")]
    public float bobAmplitude = 0.15f;
    [Tooltip("한 번 왕복하는 데 걸리는 시간(초)")]
    public float bobPeriod = 2f;

    [Header("회전")]
    [Tooltip("Y축 회전 속도(초당 각도). 양수 = 위에서 볼 때 시계방향. 방향 반대면 음수로.")]
    public float spinSpeed = 40f;

    private Vector3 _baseLocalPos;
    private float _phase; // 인스턴스마다 살짝 다른 시작 위상 (여러 개 떠 있을 때 동시 출렁 방지)

    void OnEnable()
    {
        _baseLocalPos = transform.localPosition;
        _phase = Random.value * Mathf.PI * 2f;
    }

    void Update()
    {
        // 위아래
        float omega = bobPeriod > 0.01f ? (Mathf.PI * 2f / bobPeriod) : 0f;
        float y = Mathf.Sin(Time.time * omega + _phase) * bobAmplitude;
        transform.localPosition = _baseLocalPos + new Vector3(0f, y, 0f);

        // 회전 (월드 Y축 기준)
        transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
    }
}
