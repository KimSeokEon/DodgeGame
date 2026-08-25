using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// =============================================================================
// BulletSpawner.cs   [사용 안 함 / 레거시]
// -----------------------------------------------------------------------------
// 역할 : 아주 초기 버전의 장애물(Bullet) 스포너. 일정 주기로 총알을 생성해서
//        플레이어 쪽으로 날려보낸다.
// 상태 : 지금 실제로 쓰이는 건 이 스크립트가 아니라 EnemySpawner.cs
//        (Assets/Script/Dodge2)다. 비활성화된 옛날 계층(DODGE1) 소속이라
//        지금은 실행되지 않는다.
// =============================================================================
public class BulletSpawner : MonoBehaviour
{
    public GameObject bulletPrefab;
    public float spawnRateMin = 0.5f;
    public float spawnRateMax = 3f;

    private Transform target; // 조준 대상 (플레이어)
    private float spawnRate; // 다음 스폰까지 걸리는 시간
    private float timeAfterSpawn; // 마지막 스폰 이후 경과 시간

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        timeAfterSpawn = 0f;
        spawnRate = Random.Range(spawnRateMin, spawnRateMax);
        target = FindFirstObjectByType<PlayerMovement>().transform;
    }

    // 시간을 세다가 spawnRate가 지나면 총알 하나를 생성해서 플레이어 쪽으로 날린다
    void Update()
    {
        timeAfterSpawn += Time.deltaTime;

        if (timeAfterSpawn >= spawnRate)
        {
            timeAfterSpawn = 0f;

            GameObject bullet
                = Instantiate(bulletPrefab, transform.position, transform.rotation);
            bullet.transform.LookAt(target);

            spawnRate = Random.Range(spawnRateMin, spawnRateMax);
        }
    }
}
