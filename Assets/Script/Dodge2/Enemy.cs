// =============================================================================
// Enemy.cs (구버전 — 로컬 전용 MonoBehaviour. 네트워크 버전은 아래에 있음)
// =============================================================================
// public class Enemy : NetworkBehaviour
// {
//     public float speed = 6f; // 날아가는 속도
//
//     private Rigidbody enemyRigidbody;
//     private BoxCollider col;
//     [SerializeField] private float delaytime = 0.5f; // 생성 직후 이 시간 동안은 충돌 판정을 꺼둔다 (스폰 위치에서 바로 맞는 것 방지)
//
//     private float t; // delaytime을 세는 경과 시간
//
//     void Awake()
//     {
//         enemyRigidbody = GetComponent<Rigidbody>();
//     }
//
//     void Start()
//     {
//         col = GetComponent<BoxCollider>();
//         col.enabled = false; // 처음엔 충돌 꺼둠 (delaytime 지나면 Update에서 켠다)
//     }
//
//     // delaytime이 지나면 충돌 판정을 켜준다
//     private void Update()
//     {
//         t += Time.deltaTime;
//         if (t >= delaytime)
//         {
//             col.enabled = true;
//         }
//     }
//
//     // EnemySpawner가 생성 직후 이동 방향을 지정해줄 때 호출
//     public void SetDirection(Vector3 direction)
//     {
//         if (enemyRigidbody == null)
//         {
//             enemyRigidbody = GetComponent<Rigidbody>();
//         }
//
//         Vector3 dir = direction;
//         dir.y = 0f; // 위아래로는 안 날아가게 수평으로만
//         dir.Normalize();
//
//         enemyRigidbody.linearVelocity = dir * speed;
//
//         if (dir != Vector3.zero)
//         {
//             transform.forward = dir; // 날아가는 방향을 바라보게 회전
//         }
//
//         // 넉넉히 살아있다가, 벽에 못 맞고 어딘가로 새더라도 자동 정리되도록 안전장치
//         Destroy(gameObject, 20f);
//     }
//
//     // 벽에 부딪히면 사라지고, 플레이어에 부딪히면 데미지를 주고 사라진다
//     private void OnTriggerEnter(Collider other)
//     {
//
//         Debug.Log($"Other ::: {other.name}", other);
//
//         if (other.CompareTag("Wall"))
//         {
//             Destroy(gameObject);
//         }
//         else if (other.CompareTag("Player"))
//         {
//             Player player = other.GetComponent<Player>();
//             if (player != null)
//             {
//                 player.TakeDamage();
//             }
//
//             Destroy(gameObject);
//         }
//     }
// }

using Fusion;
using UnityEngine;

// =============================================================================
// Enemy.cs
// -----------------------------------------------------------------------------
// 역할 : 벽에서 날아오는 장애물(적) 하나의 동작.
// 붙는 곳 : Enemy A~D 프리팹 (NetworkObject + NetworkTransform 필요)
// 네트워크 : NetworkBehaviour. 스폰/이동/파괴는 전부 Shared Mode 마스터 클라이언트
//           (StateAuthority)만 수행하고, 나머지 클라는 NetworkTransform으로
//           위치를 받아서 본다. EnemySpawner도 마스터에서만 Runner.Spawn 한다.
// =============================================================================
public class Enemy : NetworkBehaviour
{
    [Networked] public Vector3 Direction { get; set; } // 날아가는 방향(정규화됨)
    [Networked] public float Speed { get; set; }       // 날아가는 속도

    [SerializeField] private float delaytime = 0.5f; // 스폰 직후 이 시간 동안 충돌 판정 off
    private float lifetime = 20f;                    // 이 시간 지나면 자동 정리

    [Networked] private TickTimer CollisionDelay { get; set; } // delaytime 대체
    [Networked] private TickTimer LifeTimer { get; set; }      // Destroy(,20f) 대체

    private BoxCollider col;

    // Start() 대신 Spawned() — 네트워크 오브젝트 초기화 시점
    public override void Spawned()
    {
        col = GetComponent<BoxCollider>();
        col.enabled = false;

        if (HasStateAuthority)
        {
            CollisionDelay = TickTimer.CreateFromSeconds(Runner, delaytime);
            LifeTimer = TickTimer.CreateFromSeconds(Runner, lifetime);
        }
    }

    // Update() 대신 FixedUpdateNetwork() — 네트워크 틱마다 호출
    public override void FixedUpdateNetwork()
    {
        // 충돌 판정 켜기: 모든 클라에서
        if (!col.enabled && CollisionDelay.Expired(Runner))
            col.enabled = true;

        // 이동 / 수명 관리: 마스터(StateAuthority)만
        if (!HasStateAuthority) return;

        transform.position += Direction * Speed * Runner.DeltaTime;

        if (LifeTimer.Expired(Runner))
            Runner.Despawn(Object);
    }

    // EnemySpawner(마스터)가 스폰 직후 호출.
    // 회전(정면 방향)은 EnemySpawner가 runner.Spawn의 회전 인자로 이미 넣었고,
    // NetworkTransform이 그걸 동기화한다. 여기선 [Networked] Direction만 저장한다.
    public void SetDirection(Vector3 direction)
    {
        Vector3 dir = direction;
        dir.y = 0f;
        dir.Normalize();

        Direction = dir;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!HasStateAuthority) return; // 충돌 판정도 마스터만

        if (other.CompareTag("Wall"))
        {
            Runner.Despawn(Object);
        }
        else if (other.CompareTag("Player"))
        {
            Player player = other.GetComponentInParent<Player>();

            // 다운된 플레이어(또는 판별 불가)는 그냥 통과한다.
            // 적을 despawn하지도, 데미지를 주지도 않음 → 쓰러진 몸이 "적 지우개"가 되지 않게.
            // 부활은 콜라이더가 아니라 거리(transform.position)로 판정하므로 영향 없음.
            if (player == null || player.IsDead)
                return;

            // 마스터가 유일한 충돌 판정자. 맞은 플레이어의 owner에게 RPC로 데미지를 통보한다.
            // (Health를 여기서 직접 못 깎는 이유: 마스터는 상대 플레이어 오브젝트의
            //  StateAuthority가 아니라서 그 [Networked] 값을 쓸 수 없음)
            player.RPC_ApplyHit();

            Runner.Despawn(Object);
        }
    }
}