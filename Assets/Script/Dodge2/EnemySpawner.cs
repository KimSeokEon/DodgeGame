#region 기존 코드
// using System;
// using UnityEngine;
// using Random = UnityEngine.Random;
//
// // =============================================================================
// // EnemySpawner.cs
// // -----------------------------------------------------------------------------
// // 역할 : 일정 주기로 아레나(벽 4면) 바깥쪽에서 Enemy A~D 프리팹을 랜덤하게
// //        생성해서, 플레이어 방향 또는 랜덤 방향으로 날려보낸다.
// // 붙는 곳 : PlayScene.unity의 "EnemySpawner" 오브젝트
// // 참고 : Bullet / BulletSpawner(레거시 DODGE1) 구조를 참고해서 만든 스크립트.
// // 주의(멀티플레이) : Instantiate()로 로컬에만 생성하고, 조준 대상도
// //        FindFirstObjectByType<Player>()로 씬에서 아무 플레이어나 하나 찾아온다.
// //        플레이어가 여러 명이면 항상 같은 한 명만 조준 대상이 되고, 적도
// //        클라이언트마다 따로 생성돼서 서로 다른 화면을 보게 된다.
// //        (Enemy.cs와 마찬가지로 멀티플레이 후속 작업 대상)
// // =============================================================================
// public class EnemySpawner : MonoBehaviour
// {
//     [Header("스폰할 Enemy 프리팹 (Enemy A~D 드래그)")]
//     public GameObject[] enemyPrefabs;
//
//     [Header("스폰 주기")]
//     public float spawnRateMin = 0.5f;
//     public float spawnRateMax = 2f;
//
//     [Header("이동 관련")]
//     public float enemySpeed = 6f;
//     [Range(0f, 1f)]
//     public float aimAtPlayerChance = 0.5f; // 플레이어를 조준해서 날아올 확률 (나머지는 랜덤 방향)
//
//     [Header("아레나 크기 (Wall 안쪽 기준)")]
//     public float arenaHalfWidth = 50f;  // X 절반 크기
//     public float arenaHalfDepth = 50f;  // Z 절반 크기
//     public float spawnInset = 3f;       // 벽에서 안쪽으로 얼마나 띄워서 스폰할지
//     public float spawnHeight = 1f;
//
//     private Transform target; // 조준 대상 (플레이어)
//     private float spawnRate; // 다음 스폰까지 걸리는 시간 (매번 랜덤하게 다시 뽑음)
//     private float timeAfterSpawn; // 마지막 스폰 이후 경과 시간
//     private bool gameStarted = false; // 게임시작 신호 받기 전엔 false
//     
//     void OnEnable()
//     {
//         PlayerSpawner.LocalPlayerSpawned += HandleGameStart;
//     }
//
//     void OnDisable()
//     {
//         PlayerSpawner.LocalPlayerSpawned -= HandleGameStart;
//     }
//
//     private void HandleGameStart()
//     {
//         // 조준 대상(플레이어)을 찾고, 첫 스폰 주기를 랜덤으로 정한다
//         timeAfterSpawn = 0f;
//         spawnRate = Random.Range(spawnRateMin, spawnRateMax);
//
//         Player player = FindFirstObjectByType<Player>(); // 캐릭터가 실제로 존재
//
//         if (player != null)
//         {
//             target = player.transform;
//         }
//
//         gameStarted = true;
//     }
//
//     
//     void Start()
//     {
//         //HandleGameStart로 옮겨뒀으니 비워둠
//     }
//
//     // 시간을 세다가 spawnRate가 지나면 적 하나를 스폰하고 다음 주기를 다시 뽑는다
//     void Update()
//     {
//         if (!gameStarted) return; //신호를 받기 전엔 아무것도 안함
//         
//         timeAfterSpawn += Time.deltaTime;
//
//         if (timeAfterSpawn >= spawnRate)
//         {
//             timeAfterSpawn = 0f;
//             SpawnEnemy();
//             spawnRate = Random.Range(spawnRateMin, spawnRateMax);
//         }
//     }
//
//     void SpawnEnemy()
//     {
//         if (enemyPrefabs == null || enemyPrefabs.Length == 0)
//         {
//             return;
//         }
//
//         // 1. 벽 하나를 랜덤으로 선택 (0:남, 1:북, 2:동, 3:서)
//         int side = Random.Range(0, 4);
//         Vector3 spawnPos;
//         Vector3 inwardNormal; // 그 벽에서 아레나 안쪽을 향하는 방향
//
//         switch (side)
//         {
//             case 0: // 남쪽 벽 (-Z)
//                 spawnPos = new Vector3(Random.Range(-arenaHalfWidth, arenaHalfWidth), spawnHeight, -arenaHalfDepth + spawnInset);
//                 inwardNormal = Vector3.forward;
//                 break;
//             case 1: // 북쪽 벽 (+Z)
//                 spawnPos = new Vector3(Random.Range(-arenaHalfWidth, arenaHalfWidth), spawnHeight, arenaHalfDepth - spawnInset);
//                 inwardNormal = Vector3.back;
//                 break;
//             case 2: // 동쪽 벽 (+X)
//                 spawnPos = new Vector3(arenaHalfWidth - spawnInset, spawnHeight, Random.Range(-arenaHalfDepth, arenaHalfDepth));
//                 inwardNormal = Vector3.left;
//                 break;
//             default: // 서쪽 벽 (-X)
//                 spawnPos = new Vector3(-arenaHalfWidth + spawnInset, spawnHeight, Random.Range(-arenaHalfDepth, arenaHalfDepth));
//                 inwardNormal = Vector3.right;
//                 break;
//         }
//
//         // 2. 프리팹 랜덤 선택 (Enemy A~D)
//         GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
//         GameObject enemyObj = Instantiate(prefab, spawnPos, Quaternion.identity);
//
//         Enemy enemy = enemyObj.GetComponent<Enemy>();
//         if (enemy == null)
//         {
//             enemy = enemyObj.AddComponent<Enemy>(); // 프리팹에 Enemy 컴포넌트가 안 붙어있으면 여기서 붙여줌
//         }
//         enemy.speed = enemySpeed;
//
//         // 3. 방향 결정: 플레이어 조준 or 랜덤(안쪽 방향 기준 좌우로 흩어짐)
//         Vector3 direction;
//         if (target != null && Random.value < aimAtPlayerChance)
//         {
//             direction = target.position - spawnPos; // 플레이어를 향해 직선으로
//         }
//         else
//         {
//             float randomAngle = Random.Range(-60f, 60f);
//             direction = Quaternion.Euler(0f, randomAngle, 0f) * inwardNormal; // 안쪽 방향에서 좌우로 최대 60도 틀어서
//         }
//
//         enemy.SetDirection(direction);
//     }
// }
#endregion

using UnityEngine;
using Fusion;
using Random = UnityEngine.Random;

// =============================================================================
// EnemySpawner.cs
// -----------------------------------------------------------------------------
// 역할 : 일정 주기로 아레나(벽 4면) 바깥쪽에서 Enemy A~D 프리팹을 랜덤하게
//        스폰해서, 플레이어 방향 또는 랜덤 방향으로 날려보낸다.
// 붙는 곳 : PlayScene.unity의 "EnemySpawner" 오브젝트
//
// 네트워크(Fusion Shared Mode) :
//   적 스폰은 "마스터 클라이언트(IsSharedModeMasterClient == true)" 한 명만 수행한다.
//   - 마스터만 Random을 굴려서 벽/위치/프리팹/방향을 정하고 Runner.Spawn() 한다.
//   - 나머지 클라이언트는 이 스크립트에서 아무것도 스폰하지 않고,
//     마스터가 스폰한 NetworkObject(적)를 네트워크로 복제받아서 본다.
//   => 모든 화면에서 "같은 적이 같은 위치/방향"으로 뜬다.
//
//   예전에는 각 클라가 Instantiate()로 로컬 적을 따로 만들었고 Random 시드도
//   공유되지 않아서, 클라마다 완전히 다른 적을 보고 있었다. (그게 "화면마다
//   적 위치가 다르다"의 원인이었음)
//
// 남은 작업(2번 스텝) : 조준 대상 target을 아직 FindFirstObjectByType<Player>()로
//   "아무 플레이어나 하나" 잡는다. 플레이어가 여러 명이면 항상 같은 한 명만
//   노리게 되는데, 이건 피격 동기화 작업과 함께 다듬을 예정.
// =============================================================================
public class EnemySpawner : MonoBehaviour
{
    [Header("스폰할 Enemy 프리팹 (Enemy A~D 드래그)")]
    public GameObject[] enemyPrefabs; // NetworkObject + Enemy(NetworkBehaviour)가 붙어있어야 함

    [Header("스폰 주기")]
    public float spawnRateMin = 0.5f;
    public float spawnRateMax = 2f;

    [Header("이동 관련")]
    public float enemySpeed = 6f;
    [Range(0f, 1f)]
    public float aimAtPlayerChance = 0.5f; // 플레이어를 조준해서 날아올 확률 (나머지는 랜덤 방향)

    [Header("아레나 크기 (Wall 안쪽 기준)")]
    public float arenaHalfWidth = 50f;  // X 절반 크기
    public float arenaHalfDepth = 50f;  // Z 절반 크기
    public float spawnInset = 3f;       // 벽에서 안쪽으로 얼마나 띄워서 스폰할지
    public float spawnHeight = 1f;

    private Transform target;      // 조준 대상 (플레이어). 마스터 화면 기준으로 하나 잡는다.
    private float spawnRate;       // 다음 스폰까지 걸리는 시간 (매번 랜덤하게 다시 뽑음)
    private float timeAfterSpawn;  // 마지막 스폰 이후 경과 시간
    private bool gameStarted = false; // 내 캐릭터가 스폰됐다는 신호를 받기 전엔 false

    // 씬에서 찾아 캐싱해두는 Fusion 러너.
    // 이걸로 (1) 내가 마스터 클라인지 판별하고 (2) Runner.Spawn()을 호출한다.
    // ※ SimulationBehaviour의 Runner 프로퍼티를 안 쓰고 직접 찾는 이유:
    //   로비를 거쳐 씬을 로드한 경우 그 프로퍼티가 끝까지 null로 남는 버그가 있었음
    //   (PlayerSpawner.cs에서 겪은 것과 동일). FindFirstObjectByType이 확실하다.
    private NetworkRunner runner;

    void OnEnable()
    {
        // 내 캐릭터가 스폰 완료되면 HandleGameStart가 불린다 (PlayerSpawner가 쏘는 신호)
        PlayerSpawner.LocalPlayerSpawned += HandleGameStart;
    }

    void OnDisable()
    {
        PlayerSpawner.LocalPlayerSpawned -= HandleGameStart;
    }

    // 내 캐릭터 스폰이 끝난 시점: 조준 대상을 찾고, 첫 스폰 주기를 정하고, 카운트 시작
    private void HandleGameStart()
    {
        timeAfterSpawn = 0f;
        spawnRate = Random.Range(spawnRateMin, spawnRateMax);

        Player player = FindFirstObjectByType<Player>(); // 씬에 실제로 존재하는 플레이어
        if (player != null)
        {
            target = player.transform;
        }

        gameStarted = true;
    }

    void Start()
    {
        // 초기화는 HandleGameStart로 옮겨서 여기는 비워둠
    }

    // 매 프레임: (마스터일 때만) 시간을 세다가 spawnRate가 지나면 적을 하나 스폰
    void Update()
    {
        if (!gameStarted) return; // 내 캐릭터 스폰 신호를 받기 전엔 아무것도 안 함

        // 러너를 아직 못 찾았으면 계속 시도 (세션 접속 직후엔 아직 없을 수 있음)
        if (runner == null)
        {
            runner = FindFirstObjectByType<NetworkRunner>();
        }

        // ★ 핵심: "적을 스폰할 권한이 있는 쪽"에서만 스폰한다.
        //   - 싱글플레이(GameMode.Single) / Host / Server  → runner.IsServer 가 true
        //   - Shared Mode                                 → 마스터 한 명만 IsSharedModeMasterClient 가 true
        //   나머지 클라는 여기서 멈추고, 스폰된 적을 네트워크로 받아온다.
        //   (IsSharedModeMasterClient 는 Shared Mode 전용이라, 싱글에선 항상 false → 이것만 보면 싱글에서 적이 안 나옴)
        bool canSpawn = runner != null && (runner.IsServer || runner.IsSharedModeMasterClient);
        if (!canSpawn) return;

        timeAfterSpawn += Time.deltaTime;

        if (timeAfterSpawn >= spawnRate)
        {
            timeAfterSpawn = 0f;
            SpawnEnemy();
            spawnRate = Random.Range(spawnRateMin, spawnRateMax); // 다음 주기 다시 뽑기
        }
    }

    // 적 하나를 네트워크 스폰한다. (마스터 클라에서만 호출됨)
    void SpawnEnemy()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0) return;
        if (runner == null) return; // 방어 (Update에서 걸러지지만 한 번 더)

        // 1. 벽 하나를 랜덤으로 선택 (0:남, 1:북, 2:동, 3:서)
        int side = Random.Range(0, 4);
        Vector3 spawnPos;
        Vector3 inwardNormal; // 그 벽에서 아레나 안쪽을 향하는 방향

        switch (side)
        {
            case 0: // 남쪽 벽 (-Z)
                spawnPos = new Vector3(Random.Range(-arenaHalfWidth, arenaHalfWidth), spawnHeight, -arenaHalfDepth + spawnInset);
                inwardNormal = Vector3.forward;
                break;
            case 1: // 북쪽 벽 (+Z)
                spawnPos = new Vector3(Random.Range(-arenaHalfWidth, arenaHalfWidth), spawnHeight, arenaHalfDepth - spawnInset);
                inwardNormal = Vector3.back;
                break;
            case 2: // 동쪽 벽 (+X)
                spawnPos = new Vector3(arenaHalfWidth - spawnInset, spawnHeight, Random.Range(-arenaHalfDepth, arenaHalfDepth));
                inwardNormal = Vector3.left;
                break;
            default: // 서쪽 벽 (-X)
                spawnPos = new Vector3(-arenaHalfWidth + spawnInset, spawnHeight, Random.Range(-arenaHalfDepth, arenaHalfDepth));
                inwardNormal = Vector3.right;
                break;
        }

        // 2. 프리팹 선택 + 이동 방향을 "스폰 전에" 계산한다.
        //    SetDirection() 안에서 transform.forward를 돌리면 runner.Spawn 직후엔
        //    NetworkTransform 초기화에 덮여서 방향을 안 보게 된다. 그래서 방향을 먼저
        //    구해서 runner.Spawn의 회전 인자(Quaternion.LookRotation)로 넘긴다.
        //    → 스폰 회전이 네트워크 초기 상태에 포함되어 모든 클라가 정확히 받는다.
        GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];

        Vector3 direction;
        if (target != null && Random.value < aimAtPlayerChance)
        {
            direction = target.position - spawnPos; // 플레이어를 향해 직선으로
        }
        else
        {
            float randomAngle = Random.Range(-60f, 60f);
            direction = Quaternion.Euler(0f, randomAngle, 0f) * inwardNormal; // 안쪽 방향에서 좌우로 최대 60도
        }
        direction.y = 0f;
        direction.Normalize();

        // 3. 방향을 바라보는 회전으로 네트워크 스폰.
        //    Instantiate()가 아니라 runner.Spawn() → NetworkObject가 생성되고 전 클라에 복제된다.
        //    Shared Mode에서는 스폰을 호출한 클라(=마스터)가 그 오브젝트의 StateAuthority를 가진다.
        //    그래서 Enemy.cs에서 "HasStateAuthority인 클라만 이동/파괴" 처리가 성립한다.
        Quaternion spawnRot = direction != Vector3.zero
            ? Quaternion.LookRotation(direction)
            : Quaternion.identity;

        NetworkObject netObj = runner.Spawn(prefab, spawnPos, spawnRot);
        if (netObj == null) return;

        Enemy enemy = netObj.GetComponent<Enemy>();
        if (enemy == null)
        {
            // 네트워크 프리팹은 런타임 AddComponent로 Enemy를 붙일 수 없다(위빙 안 됨).
            // 프리팹 자체에 Enemy가 붙어있어야 한다 → 없으면 에러 찍고 되돌린다.
            Debug.LogError($"{prefab.name} 프리팹에 Enemy 컴포넌트가 없습니다.");
            runner.Despawn(netObj);
            return;
        }

        // 마스터에서 호출되므로 Enemy의 [Networked] 프로퍼티에 값을 써도 된다.
        // 이후 실제 이동은 Enemy.FixedUpdateNetwork()가 매 틱 처리한다.
        enemy.Speed = enemySpeed;
        enemy.SetDirection(direction);
    }
}