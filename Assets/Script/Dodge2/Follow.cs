using UnityEngine;

// =============================================================================
// Follow.cs
// -----------------------------------------------------------------------------
// 역할 : target으로 지정된 트랜스폼(보통 내 캐릭터)의 위치 + offset을 매 프레임
//        그대로 따라가는 단순 카메라 추적 스크립트.
// 붙는 곳 : PlayScene.unity의 Camera 오브젝트. target은 PlayerSpawner.cs가
//        내 캐릭터가 생성된 직후에 자동으로 연결해준다.
// =============================================================================
public class Follow : MonoBehaviour
{
    public Transform target; // 따라갈 대상 (내 캐릭터)
    public Vector3 offset;   // 대상 위치에서 얼마나 떨어져서 볼지 (카메라 각도/거리)

    void Start()
    {

    }

    // target 위치 + offset을 그대로 자기 위치로 사용 (target이 없으면 NullReferenceException 발생 주의)
    void Update()
    {
        transform.position = target.position + offset;
    }
}
