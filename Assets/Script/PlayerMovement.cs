using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// =============================================================================
// PlayerMovement.cs   [사용 안 함 / 레거시]
// -----------------------------------------------------------------------------
// 역할 : 아주 초기 버전의 싱글플레이 캐릭터 이동 스크립트 (WASD 이동 + 사망 처리).
// 상태 : 지금 실제로 쓰이는 건 이 스크립트가 아니라 Player.cs(Assets/Script/Dodge2,
//        Fusion 네트워크 캐릭터)다. PlayScene.unity에 이 스크립트가 붙은 "Player"
//        오브젝트가 남아있지만, 비활성화된 옛날 계층(DODGE1) 소속이라 지금은
//        실행되지 않는다. 이름이 새 버전과 똑같이 "Player"라서 헷갈리기 쉬우니 참고할 것.
// =============================================================================
public class PlayerMovement : MonoBehaviour
{
    public Rigidbody PlayerRigidbody;
    public float speed = 10f;

    void Start()
    {
        PlayerRigidbody = GetComponent<Rigidbody>();
    }

    // 매 프레임 입력을 그대로 속도로 변환해서 이동 (가속/감속 없음)
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

    // Bullet.cs가 충돌 시 호출: 캐릭터를 비활성화하고 게임오버 처리
    public void Die()
    {
        gameObject.SetActive(false);

        GameManager gameManager = FindFirstObjectByType<GameManager>();
        gameManager.Endgame();
    }
}
