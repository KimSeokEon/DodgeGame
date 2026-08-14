using System;
using UnityEngine;

public class Character_move : MonoBehaviour
{
    public float amplitude = 0.04f;
    public float speed = 1.2f;

    private Vector3 basescale;

    void Awake()
    {
        basescale = transform.localScale;
    }

    void Start()
    {
        
    }
    
    
    void Update()
    {
        float scaleFactor = 1f + Mathf.Sin(Time.time * speed) * amplitude;
        transform.localScale = basescale * scaleFactor;
    }
}
