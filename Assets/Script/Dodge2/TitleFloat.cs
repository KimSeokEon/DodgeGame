using System;
using UnityEngine;

// =============================================================================
// TitleFloat.cs
// -----------------------------------------------------------------------------
// 역할 : UI 요소(로고/타이틀 등)를 사인파를 이용해 위아래로 둥실둥실 떠다니게
//        만드는 장식용 애니메이션.
// 붙는 곳 : MainMenu.unity의 타이틀 이미지/텍스트 (RectTransform이 있어야 함)
// =============================================================================
public class TitleFloat : MonoBehaviour
{
    public float amplitude = 15f; // 위아래로 움직이는 폭 (픽셀)
    public float speed = 1.5f;    // 움직이는 속도

    private RectTransform rectTransform;
    private Vector2 startPos; // 처음(에디터에서 세팅된) 위치, 여기를 기준으로 위아래로 흔들림

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        startPos = rectTransform.anchoredPosition;
    }


    void Start()
    {

    }


    // 시간에 따른 사인파(Sin)로 y좌표만 흔들어서 둥실거리는 효과를 만든다
    void Update()
    {
        float offsetY = Mathf.Sin(Time.time * speed) * amplitude;
        rectTransform.anchoredPosition = new Vector2(startPos.x, startPos.y + offsetY);
    }
}
