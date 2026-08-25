using UnityEngine;

// =============================================================================
// Heart.cs
// -----------------------------------------------------------------------------
// 역할 : 하트 아이콘이 항상 카메라를 바라보도록(빌보드) 회전시켜주는 스크립트.
// 붙는 곳 : 체력 하트 UI 오브젝트들 (Player 프리팹 하위의 하트 3개)
// =============================================================================
public class Heart : MonoBehaviour
{
    public Transform target; // (현재 코드에서는 사용되지 않음 — LookAt은 카메라 기준으로만 동작)
    public Vector3 offset = new Vector3(0, 2f, 0);
    private Transform cam;

    void Start()
    {
        if (Camera.main != null) cam = Camera.main.transform;
    }


    // 매 프레임 늦게(다른 오브젝트 이동이 끝난 뒤) 카메라 쪽을 바라보게 회전
    void LateUpdate()
    {
        // transform.rotation = cam.rotation;
        transform.LookAt(cam);
    }
}
