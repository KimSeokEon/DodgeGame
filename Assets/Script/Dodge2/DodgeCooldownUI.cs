using UnityEngine;
using UnityEngine.UI;

// =============================================================================
// DodgeCooldownUI.cs
// -----------------------------------------------------------------------------
// 역할 : 구르기 쿨타임에 맞춰, 이 오브젝트에 붙은 회색 오버레이 Image를
//        시계방향(Radial360)으로 채웠다가 걷어내서 쿨타임 게이지를 보여준다.
// 붙는 곳 : Canvas/dodgeimage 위에 겹쳐진 회색 오버레이 이미지
//        (Image의 Image Type = Filled, Fill Method = Radial 360, Clockwise 체크 필요)
// 동작 : 매 프레임 로컬 플레이어(HasInputAuthority)를 찾아서, 그 Player의
//        DodgeCooldownProgress01 값을 그대로 fillAmount에 반영한다.
//        (Fusion 캐릭터는 씬 로드 후 몇 초 뒤에 스폰되므로, 찾을 때까지 계속 재시도한다)
// =============================================================================
public class DodgeCooldownUI : MonoBehaviour
{
    private Image cooldownImage;
    private Player localPlayer;

    void Awake()
    {
        cooldownImage = GetComponent<Image>();
    }

    void Update()
    {
        // 아직 로컬 플레이어를 못 찾았으면(캐릭터가 아직 스폰 전이거나) 계속 찾아본다
        if (localPlayer == null)
        {
            foreach (var p in FindObjectsByType<Player>(FindObjectsSortMode.None))
            {
                if (p.HasInputAuthority)
                {
                    localPlayer = p;
                    break;
                }
            }
        }

        // 캐릭터가 아직 없으면 게이지를 비워둔 채로 대기
        if (localPlayer == null)
        {
            cooldownImage.fillAmount = 0f;
            return;
        }

        cooldownImage.fillAmount = localPlayer.DodgeCooldownProgress01;
    }
}
