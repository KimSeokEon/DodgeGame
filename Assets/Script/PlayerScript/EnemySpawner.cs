using UnityEngine;

// Bullet / BulletSpawner 구조를 참고해서 만든 별도의 Enemy 스폰 스크립트
// Wall 4면 중 하나에서 Enemy A~D 프리팹을 랜덤하게 생성해서
// 플레이어 방향 또는 랜덤 방향으로 날아가게 만든다.
public class EnemySpawner : MonoBehaviour
{
    [Header("스폰할 Enemy 프리팹 (Enemy A~D 드래그)")]
    public GameObject[] enemyPrefabs;

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

    private Transform target;
    private float spawnRate;
    private float timeAfterSpawn;

    void Start()
    {
        timeAfterSpawn = 0f;
        spawnRate = Random.Range(spawnRateMin, spawnRateMax);

        Player player = FindFirstObjectByType<Player>();
        if (player != null)
        {
            target = player.transform;
        }
    }

    void Update()
    {
        timeAfterSpawn += Time.deltaTime;

        if (timeAfterSpawn >= spawnRate)
        {
            timeAfterSpawn = 0f;
            SpawnEnemy();
            spawnRate = Random.Range(spawnRateMin, spawnRateMax);
        }
    }

    void SpawnEnemy()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            return;
        }

        // 1. 벽 하나를 랜덤으로 선택 (0:남, 1:북, 2:동, 3:서)
        int side = Random.Range(0, 4);
        Vector3 spawnPos;
        Vector3 inwardNormal;

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

        // 2. 프리팹 랜덤 선택 (Enemy A~D)
        GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
        GameObject enemyObj = Instantiate(prefab, spawnPos, Quaternion.identity);

        Enemy enemy = enemyObj.GetComponent<Enemy>();
        if (enemy == null)
        {
            enemy = enemyObj.AddComponent<Enemy>();
        }
        enemy.speed = enemySpeed;

        // 3. 방향 결정: 플레이어 조준 or 랜덤(안쪽 방향 기준 좌우로 흩어짐)
        Vector3 direction;
        if (target != null && Random.value < aimAtPlayerChance)
        {
            direction = target.position - spawnPos;
        }
        else
        {
            float randomAngle = Random.Range(-60f, 60f);
            direction = Quaternion.Euler(0f, randomAngle, 0f) * inwardNormal;
        }

        enemy.SetDirection(direction);
    }
}
