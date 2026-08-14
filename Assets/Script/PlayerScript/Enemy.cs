using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class Enemy : MonoBehaviour
{
    public float speed = 6f;

    private Rigidbody enemyRigidbody;
    private BoxCollider col;
    [SerializeField] private float delaytime = 0.5f;

    private float t;
    
    void Awake()
    {
        enemyRigidbody = GetComponent<Rigidbody>();
    }

    void Start()
    {
        col = GetComponent<BoxCollider>();
        col.enabled = false;
    }

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
        dir.y = 0f;
        dir.Normalize();

        enemyRigidbody.linearVelocity = dir * speed;

        if (dir != Vector3.zero)
        {
            transform.forward = dir;
        }

        // 넉넉히 살아있다가, 벽에 못 맞고 어딘가로 새더라도 자동 정리되도록 안전장치
        Destroy(gameObject, 20f);
    }

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
