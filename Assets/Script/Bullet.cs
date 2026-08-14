using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Bullet : MonoBehaviour
{
    public float speed = 8f;
    private Rigidbody bulletRigidBody;
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
