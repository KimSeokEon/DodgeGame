using System;
using UnityEngine;

// =============================================================================
// Character_move.cs
// -----------------------------------------------------------------------------
// 역할 : 오브젝트가 사인파를 이용해 살짝 커졌다 작아졌다를 반복하는(숨쉬기 같은)
//        장식용 애니메이션. TitleFloat.cs와 세트로 메인 메뉴 캐릭터 연출에 쓰인다.
// 붙는 곳 : MainMenu.unity의 캐릭터 이미지/오브젝트
// =============================================================================
public class Character_move : MonoBehaviour
{
    public float amplitude = 0.04f; // 커지고 작아지는 정도 (기본 크기 대비 비율)
    public float speed = 1.2f;      // 움직이는 속도

    private Vector3 basescale; // 처음(에디터에서 세팅된) 크기, 여기를 기준으로 커졌다 작아졌다 함

    void Awake()
    {
        basescale = transform.localScale;
    }

    void Start()
    {

    }


    // 시간에 따른 사인파(Sin)로 스케일을 흔들어서 숨쉬는 듯한 효과를 만든다
    void Update()
    {
        float scaleFactor = 1f + Mathf.Sin(Time.time * speed) * amplitude;
        transform.localScale = basescale * scaleFactor;
    }
}
