using UnityEngine;

// Level(맵) 전체를 Y축 기준으로 회전시켜서,
// 그 안에 담긴 벽(WALL x4), Plane, BulletSpawner들이 다같이 빙글빙글 도는 연출을 만듭니다.
public class LevelRotator : MonoBehaviour
{
    [Tooltip("초당 회전 속도 (도/초). 값이 클수록 더 빠르게 돕니다.")]
    public float rotationSpeed = 90f;

    [Tooltip("회전축. 보통 Y축(위쪽)을 기준으로 빙글빙글 돕니다.")]
    public Vector3 rotationAxis = Vector3.up;

    void Update()
    {
        transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime, Space.World);
    }
}
