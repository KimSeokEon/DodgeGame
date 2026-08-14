using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public Rigidbody PlayerRigidbody;
    public float speed = 10f;

    void Start()
    {
        PlayerRigidbody = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // 수평축 , 수직축 입력값 감지 후 저장
        float xInput = Input.GetAxisRaw("Horizontal");
        float zInput = Input.GetAxisRaw("Vertical");
        
        // 실제 이동 속도를 입력값과 이동 속력을 통해 결정
        float xSpeed = xInput * speed;
        float zSpeed = zInput * speed;
        
        //vector3 속도를 xspeed , 0 , zspeed 로 생성
        Vector3 newVelocity = new Vector3(xSpeed, 0f, zSpeed);
        //rigidbody 속도에 newVelocity 할당
        PlayerRigidbody.linearVelocity = newVelocity;
    }

    public void Die()
    {
        gameObject.SetActive(false);

        GameManager gameManager = FindFirstObjectByType<GameManager>();
        gameManager.Endgame();
    }
}