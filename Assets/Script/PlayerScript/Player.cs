using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Player : MonoBehaviour
{
    float hAxis;
    float vAxis;
    bool wDown;
    public float speed;
    public Transform cam;

    [Header("Heart3 -> 2 -> 1  순서로 등록")] 
    public Animator[] heartAnimators;

    private int currentHealth;
    
    
    Vector3 moveVec;

    private Animator anim;

    void Awake()
    {
        anim = GetComponentInChildren<Animator>();
        currentHealth = heartAnimators.Length;
        
    }

    void Start()
    {
        if (cam == null)
        {
            cam = Camera.main.transform;
        }
    }

    
    void Update()
    {
        hAxis = Input.GetAxisRaw("Horizontal");
        vAxis = Input.GetAxisRaw("Vertical");
        wDown = Input.GetButton("Walk"); 
        
        Vector3 camForward = cam.forward;
        Vector3 camRight = cam.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();
        
        moveVec = (camForward * vAxis + camRight * hAxis).normalized;
        
        transform.position += moveVec * speed * (wDown ? 0.3f : 1f) * Time.deltaTime;
        anim.SetBool("isRun", moveVec != Vector3.zero);
        anim.SetBool("isWalk", wDown);
        
        transform.LookAt(transform.position + moveVec); //나아가는 방향으로 바라본다

    }

    public void TakeDamage()
    {
        if (currentHealth <= 0) return;

        int heartIndex = heartAnimators.Length - currentHealth;
        heartAnimators[heartIndex].SetTrigger("Disappear");

        currentHealth--;

        if (currentHealth <= 0)
        {
            Die();
        }
    }
    public void Die()
    {
        gameObject.SetActive(false);

        GameManager gameManager = FindFirstObjectByType<GameManager>();
        gameManager.Endgame();
    }
}