using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

// =============================================================================
// Enemy.cs
// -----------------------------------------------------------------------------
// 역할 : 벽에서 날아오는 장애물(적) 하나의 동작. EnemySpawner가 생성한 직후
//        SetDirection()을 호출해서 날아갈 방향/속도를 정해준다.
// 붙는 곳 : Enemy A~D 프리팹 (EnemySpawner.cs가 Instantiate로 생성)
// 주의(멀티플레이) : 아직 일반 MonoBehaviour라 네트워크 동기화가 안 된다.
//        각 클라이언트가 자기 화면에서 따로 적을 생성/이동시키므로, 접속자마다
//        서로 다른 적을 보게 된다. 이건 멀티플레이 후속 작업 대상.
// =============================================================================
public class Enemy : MonoBehaviour
{
    public float speed = 6f; // 날아가는 속도

    private Rigidbody enemyRigidbody;
    private BoxCollider col;
    [SerializeField] private float delaytime = 0.5f; // 생성 직후 이 시간 동안은 충돌 판정을 꺼둔다 (스폰 위치에서 바로 맞는 것 방지)

    private float t; // delaytime을 세는 경과 시간

    void Awake()
    {
        enemyRigidbody = GetComponent<Rigidbody>();
    }

    void Start()
    {
        col = GetComponent<BoxCollider>();
        col.enabled = false; // 처음엔 충돌 꺼둠 (delaytime 지나면 Update에서 켠다)
    }

    // delaytime이 지나면 충돌 판정을 켜준다
    private void Update()
    {
        t += Time.deltaTime;
        if (t >= delaytime)
        {
            col.enabled = true;
        }
    }

    // EnemySpawner가 생성 직후 이동 방향을 지정해줄 때 호출
    public void SetDirection(Vector3 direction)
    {
        if (enemyRigidbody == null)
        {
            enemyRigidbody = GetComponent<Rigidbody>();
        }

        Vector3 dir = direction;
        dir.y = 0f; // 위아래로는 안 날아가게 수평으로만
        dir.Normalize();

        enemyRigidbody.linearVelocity = dir * speed;

        if (dir != Vector3.zero)
        {
            transform.forward = dir; // 날아가는 방향을 바라보게 회전
        }

        // 넉넉히 살아있다가, 벽에 못 맞고 어딘가로 새더라도 자동 정리되도록 안전장치
        Destroy(gameObject, 20f);
    }

    // 벽에 부딪히면 사라지고, 플레이어에 부딪히면 데미지를 주고 사라진다
    private void OnTriggerEnter(Collider other)
    {

        Debug.Log($"Other ::: {other.name}", other);

        if (other.CompareTag("Wall"))
        {
            Destroy(gameObject);
        }
        else if (other.CompareTag("Player"))
        {
            Player player = other.GetComponent<Player>();
            if (player != null)
            {
                player.TakeDamage();
            }

            Destroy(gameObject);
        }
    }
}
