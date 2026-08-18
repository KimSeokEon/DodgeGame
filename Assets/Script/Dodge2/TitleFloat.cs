using System;
using UnityEngine;

public class TitleFloat : MonoBehaviour
{
    public float amplitude = 15f;
    public float speed = 1.5f;

    private RectTransform rectTransform;
    private Vector2 startPos;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        startPos = rectTransform.anchoredPosition;
    }
    

    void Start()
    {
        
    }


    void Update()
    {
        float offsetY = Mathf.Sin(Time.time * speed) * amplitude;
        rectTransform.anchoredPosition = new Vector2(startPos.x, startPos.y + offsetY);
    }
}
