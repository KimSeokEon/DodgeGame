using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Random = UnityEngine.Random;

// =============================================================================
// HeartPickupSpawner.cs
// -----------------------------------------------------------------------------
// 역할 : 일정 주기로 회복 하트(HeartPickup)를 랜덤 플레이어 주변에 스폰한다.
//        맵에 최대 maxHeartsOnMap 개까지만 유지, 주기마다 1개씩 채운다.
// 붙는 곳 : PlayScene.unity 의 "HeartPickupSpawner" 오브젝트 (EnemySpawner 옆)
//
// 네트워크(Fusion Shared Mode) : EnemySpawner 와 동일 패턴 —
//   마스터(IsServer || IsSharedModeMasterClient) 한 명만 Random 굴려서 Runner.Spawn.
//   나머지 클라는 복제만 받는다. 싱글플레이는 IsServer 로 통과.
// =============================================================================
public class HeartPickupSpawner : MonoBehaviour
{
    [Header("스폰할 하트 프리팹 (Item Heart Variant)")]
    public GameObject heartPrefab; // NetworkObject + HeartPickup 붙어있어야 함

    [Header("스폰 규칙")]
    [Tooltip("스폰 시도 간격(초). 맵이 꽉 차 있으면 이번 주기는 건너뛴다.")]
    public float spawnInterval = 8f;
    [Tooltip("맵에 동시에 존재할 수 있는 하트 최대 개수")]
    public int maxHeartsOnMap = 3;

    [Header("스폰 위치 (랜덤 플레이어 기준 링)")]
    public float minSpawnRadius = 3f;   // 플레이어에게서 최소 이만큼 떨어져서
    public float maxSpawnRadius = 6f;   // 최대 이만큼 안쪽에
    public float spawnHeight = 1f;      // 바닥(y=0) 기준 높이
    [Tooltip("이 XZ 범위 밖으로는 안 나가게 클램프 (맵 경계 안쪽 값)")]
    public float playAreaHalfExtent = 48f;

    private NetworkRunner runner;
    private float timer;
    private bool gameStarted;

    void OnEnable()
    {
        PlayerSpawner.LocalPlayerSpawned += HandleGameStart;
    }

    void OnDisable()
    {
        PlayerSpawner.LocalPlayerSpawned -= HandleGameStart;
    }

    private void HandleGameStart()
    {
        timer = 0f;
        gameStarted = true;
    }

    void Update()
    {
        if (!gameStarted) return;

        if (runner == null)
            runner = FindFirstObjectByType<NetworkRunner>();

        // 스폰 권한이 있는 쪽(마스터/서버/싱글)만
        bool canSpawn = runner != null && (runner.IsServer || runner.IsSharedModeMasterClient);
        if (!canSpawn) return;

        timer += Time.deltaTime;
        if (timer < spawnInterval) return;
        timer = 0f;

        TrySpawnHeart();
    }

    // 하트 하나를 네트워크 스폰 (마스터에서만 호출됨)
    private void TrySpawnHeart()
    {
        if (heartPrefab == null || runner == null) return;

        // 이미 최대치면 이번 주기는 건너뜀 (다음 주기에 다시 시도)
        int current = FindObjectsByType<HeartPickup>(FindObjectsInactive.Exclude).Length;
        if (current >= maxHeartsOnMap) return;

        // 대상 플레이어: 살아있는 사람 우선, 없으면 아무나
        var players = FindObjectsByType<Player>(FindObjectsInactive.Exclude);
        if (players.Length == 0) return;

        var alive = new List<Player>();
        foreach (var p in players)
            if (!p.IsDead) alive.Add(p);

        List<Player> pool = alive.Count > 0 ? alive : new List<Player>(players);
        Player target = pool[Random.Range(0, pool.Count)];

        // 대상 주변 링(minRadius~maxRadius) 안 랜덤 위치
        float angle = Random.Range(0f, Mathf.PI * 2f);
        float dist = Random.Range(minSpawnRadius, maxSpawnRadius);
        Vector3 pos = target.transform.position
                      + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * dist;

        // 플레이 영역 안으로 클램프 + 높이 고정
        pos.x = Mathf.Clamp(pos.x, -playAreaHalfExtent, playAreaHalfExtent);
        pos.z = Mathf.Clamp(pos.z, -playAreaHalfExtent, playAreaHalfExtent);
        pos.y = spawnHeight;

        runner.Spawn(heartPrefab, pos, Quaternion.identity);
    }
}
