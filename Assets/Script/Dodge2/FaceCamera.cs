using UnityEngine;

// =============================================================================
// FaceCamera.cs
// -----------------------------------------------------------------------------
// 지정한 카메라(없으면 Camera.main)를 항상 바라보게 하는 빌보드.
// 로비 캐릭터 머리 위 닉네임 텍스트 등에 사용.
// =============================================================================
[ExecuteAlways]
public class FaceCamera : MonoBehaviour
{
    public Camera target;
    [Tooltip("체크하면 Y축(수평) 회전만 — 텍스트가 기울지 않음")]
    public bool keepUpright = true;

    void LateUpdate()
    {
        var cam = target != null ? target : Camera.main;
        if (cam == null) return;

        // 텍스트가 카메라와 같은 방향을 보게 한다 → 카메라에서 봤을 때 정방향으로 읽힘
        // (position 차이로 LookRotation 하면 뒷면을 보게 돼서 글자가 좌우 반전됨)
        Vector3 fwd = cam.transform.forward;
        if (keepUpright)
        {
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.0001f) return;
        }
        transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
    }
}
