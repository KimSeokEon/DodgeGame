using System;
using UnityEngine;
using Random = UnityEngine.Random;

// =============================================================================
// EnemySpawner.cs
// -----------------------------------------------------------------------------
// 역할 : 일정 주기로 아레나(벽 4면) 바깥쪽에서 Enemy A~D 프리팹을 랜덤하게
//        생성해서, 플레이어 방향 또는 랜덤 방향으로 날려보낸다.
// 붙는 곳 : PlayScene.unity의 "EnemySpawner" 오브젝트
// 참고 : Bullet / BulletSpawner(레거시 DODGE1) 구조를 참고해서 만든 스크립트.
// 주의(멀티플레이) : Instantiate()로 로컬에만 생성하고, 조준 대상도
//        FindFirstObjectByType<Player>()로 씬에서 아무 플레이어나 하나 찾아온다.
//        플레이어가 여러 명이면 항상 같은 한 명만 조준 대상이 되고, 적도
//        클라이언트마다 따로 생성돼서 서로 다른 화면을 보게 된다.
//        (Enemy.cs와 마찬가지로 멀티플레이 후속 작업 대상)
// =============================================================================
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

    private Transform target; // 조준 대상 (플레이어)
    private float spawnRate; // 다음 스폰까지 걸리는 시간 (매번 랜덤하게 다시 뽑음)
    private float timeAfterSpawn; // 마지막 스폰 이후 경과 시간
    private bool gameStarted = false; // 게임시작 신호 받기 전엔 false
    
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
        // 조준 대상(플레이어)을 찾고, 첫 스폰 주기를 랜덤으로 정한다
        timeAfterSpawn = 0f;
        spawnRate = Random.Range(spawnRateMin, spawnRateMax);

        Player player = FindFirstObjectByType<Player>(); // 캐릭터가 실제로 존재

        if (player != null)
        {
            target = player.transform;
        }

        gameStarted = true;
    }

    
    void Start()
    {
        //HandleGameStart로 옮겨뒀으니 비워둠
    }

    // 시간을 세다가 spawnRate가 지나면 적 하나를 스폰하고 다음 주기를 다시 뽑는다
    void Update()
    {
        if (!gameStarted) return; //신호를 받기 전엔 아무것도 안함
        
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

        // 2. 프리팹 랜덤 선택 (Enemy A~D)
        GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
        GameObject enemyObj = Instantiate(prefab, spawnPos, Quaternion.identity);

        Enemy enemy = enemyObj.GetComponent<Enemy>();
        if (enemy == null)
        {
            enemy = enemyObj.AddComponent<Enemy>(); // 프리팹에 Enemy 컴포넌트가 안 붙어있으면 여기서 붙여줌
        }
        enemy.speed = enemySpeed;

        // 3. 방향 결정: 플레이어 조준 or 랜덤(안쪽 방향 기준 좌우로 흩어짐)
        Vector3 direction;
        if (target != null && Random.value < aimAtPlayerChance)
        {
            direction = target.position - spawnPos; // 플레이어를 향해 직선으로
        }
        else
        {
            float randomAngle = Random.Range(-60f, 60f);
            direction = Quaternion.Euler(0f, randomAngle, 0f) * inwardNormal; // 안쪽 방향에서 좌우로 최대 60도 틀어서
        }

        enemy.SetDirection(direction);
    }
}
