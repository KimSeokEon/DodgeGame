using Fusion;
using UnityEngine;

// =============================================================================
// HeartPickup.cs
// -----------------------------------------------------------------------------
// 역할 : 맵에 떨어져 있는 회복 하트 아이템. 살아있고 체력이 안 찬 플레이어가
//        닿으면 그 플레이어 체력을 1칸 채우고 사라진다. (Enemy.cs 피격 구조의 반대)
// 붙는 곳 : Assets/Resources/Item Heart Variant.prefab
//          (NetworkObject + NetworkTransform + 트리거 Collider 필요)
// 네트워크 : NetworkBehaviour. 스폰/획득판정/디스폰은 전부 Shared Mode 마스터
//           (StateAuthority)만 수행. HeartPickupSpawner 도 마스터에서만 Spawn.
//   - 체력 회복은 여기서 직접 못 함(플레이어 오브젝트의 StateAuthority가 아님)
//     → Player.RPC_Heal() 로 해당 플레이어 owner에게 통보 (Enemy → RPC_ApplyHit 과 동일)
// 애니메이션(위아래 + 회전)은 자식 Mesh Object 에 붙는 HeartFloat.cs 가 로컬로 처리.
// (루트는 안 움직여야 트리거 판정이 안정적)
// =============================================================================
public class HeartPickup : NetworkBehaviour
{
    [Tooltip("0 이면 안 먹어도 안 사라짐. 0보다 크면 그 초 후 자동 소멸.")]
    [SerializeField] private float lifetime = 0f;

    [Networked] private TickTimer LifeTimer { get; set; }

    public override void Spawned()
    {
        if (HasStateAuthority && lifetime > 0f)
            LifeTimer = TickTimer.CreateFromSeconds(Runner, lifetime);
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        if (lifetime > 0f && LifeTimer.Expired(Runner))
            Runner.Despawn(Object);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!HasStateAuthority) return; // 획득 판정은 마스터만

        Player player = other.GetComponentInParent<Player>();

        // 못 먹는 경우(플레이어 아님 / 다운 상태 / 이미 풀피) → 그냥 통과, 하트는 그대로 둔다
        if (player == null || !player.CanPickUpHeart)
            return;

        player.RPC_Heal();        // 해당 플레이어 owner 에서 Health +1
        Runner.Despawn(Object);   // 하트 소모
    }
}
