using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// =============================================================================
// Bullet.cs   [사용 안 함 / 레거시]
// -----------------------------------------------------------------------------
// 역할 : 아주 초기 버전의 장애물(직선으로 날아가다 플레이어에 닿으면 죽이는 총알).
// 상태 : 지금 실제로 쓰이는 건 이 스크립트가 아니라 Enemy.cs(Assets/Script/Dodge2)다.
//        비활성화된 옛날 계층(DODGE1) 소속이라 지금은 실행되지 않는다.
// =============================================================================
public class Bullet : MonoBehaviour
{
    public float speed = 8f;
    private Rigidbody bulletRigidBody;

    // 생성되자마자 앞 방향으로 날아가기 시작하고, 3초 뒤 자동으로 사라진다
    void Start()
    {
        bulletRigidBody = GetComponent<Rigidbody>();
        bulletRigidBody.linearVelocity = transform.forward * speed;

        Destroy(gameObject, 3f);
    }

    //트리거 충돌 시 자동으로 실행되는 메서드
    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player") // 플레이어 태그와 충돌했다면
        {
            PlayerMovement playerMovement = other.GetComponent<PlayerMovement>();
            //상대방 게임옵젝 playermovement 컴포넌트 가져오기

            //playermovement 를 성공적으로 가져왔다면
            if (playerMovement != null)
            {
                playerMovement.Die(); //playermovement 컴포넌트 Die 메서드 실행
            }
        }
    }

    void Update()
    {

    }
}
