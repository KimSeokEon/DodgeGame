using UnityEngine;

// =============================================================================
// LevelRotator.cs
// -----------------------------------------------------------------------------
// 역할 : Level(맵) 전체를 Y축 기준으로 계속 회전시켜서, 그 안에 담긴 벽(WALL x4),
//        바닥(Plane), 스포너들이 다같이 빙글빙글 도는 배경 연출을 만든다.
// 붙는 곳 : PlayScene.unity의 "Level" 오브젝트 (벽/바닥 등을 자식으로 둔 루트)
// =============================================================================
public class LevelRotator : MonoBehaviour
{
    [Tooltip("초당 회전 속도 (도/초). 값이 클수록 더 빠르게 돕니다.")]
    public float rotationSpeed = 90f;

    [Tooltip("회전축. 보통 Y축(위쪽)을 기준으로 빙글빙글 돕니다.")]
    public Vector3 rotationAxis = Vector3.up;

    // 매 프레임 rotationAxis 기준으로 조금씩 계속 회전시킨다 (월드 좌표 기준)
    void Update()
    {
        transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime, Space.World);
    }
}
