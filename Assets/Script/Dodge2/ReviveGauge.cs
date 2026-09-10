using UnityEngine;
using UnityEngine.UI;

// =============================================================================
// ReviveGauge.cs
// -----------------------------------------------------------------------------
// 다운된 플레이어 머리 위 부활 게이지.
// Player (1) 프리팹 하위 "ReviveGauge" 오브젝트에 붙는다.
// Player.IsDead / ReviveProgress01 은 [Networked] 라 모든 클라가 같은 값을 본다 →
// 여기서는 그 값을 읽어서 표시만 한다. 네트워크 코드 불필요.
// =============================================================================
public class ReviveGauge : MonoBehaviour
{
    [SerializeField] Canvas canvas;         // 이 오브젝트에 붙은 Canvas
    [SerializeField] Image fill;            // Radial 360 으로 세팅된 Image
    [SerializeField] float lerpSpeed = 8f;  // 게이지가 목표치를 따라가는 속도

    Player player;

    void Awake()
    {
        player = GetComponentInParent<Player>();      // 부모의 Player 컴포넌트
        if (canvas == null) canvas = GetComponent<Canvas>();
    }

    void LateUpdate()
    {
        // 다운 상태일 때만 게이지를 보여준다
        bool show = player != null && player.IsDead;
        canvas.enabled = show;                        // 오브젝트는 켜둔 채 '렌더링'만 끔

        if (!show)
        {
            fill.fillAmount = 0f;
            return;
        }

        // 팀원이 다가오면 ReviveProgress01 이 0→1 로 차고, 벗어나면 0 으로 돌아감
        fill.fillAmount = Mathf.MoveTowards(
            fill.fillAmount, player.ReviveProgress01, lerpSpeed * Time.deltaTime);
    }
}