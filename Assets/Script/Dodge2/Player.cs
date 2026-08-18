using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Player : MonoBehaviour
{
    private MeshRenderer[] bodyRenderers; //캐릭터 몸 파츠
    private bool isInvincible = false;
    public float invincibleDuration = 1.2f; // 무적 지속 시간
    public float flashInterval = 0.1f; // 깜박이는 간격

    float hAxis;
    float vAxis;
    bool wDown;
    public float speed;
    public Transform cam;

    [Header("Heart3 -> 2 -> 1  순서로 등록")] public Animator[] heartAnimators;

    private int currentHealth;

    private Rigidbody rb;
    Vector3 moveVec;

    private Animator anim;
    private bool isDead = false;

    [Header("구르기")] 
    public float dodgespeedMultiplier = 2f;
    public float dodgeDuration = 0.4f;
    public float dodgeCooldown = 1f;
    private float dodgeCooldownTimer = 0f;
    private bool isDodging = false;
    
    

void Awake()
    {
        anim = GetComponentInChildren<Animator>();
        currentHealth = heartAnimators.Length;
        rb = GetComponent<Rigidbody>();
        bodyRenderers = GetComponentsInChildren<MeshRenderer>(true);

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
        if (isDead) return; //죽은뒤엔 이동 정지

        if (dodgeCooldownTimer > 0f)
            dodgeCooldownTimer -= Time.deltaTime;
        
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

        if (Input.GetKeyDown(KeyCode.Space) && !isDodging && dodgeCooldownTimer <= 0f)
        {
            StartCoroutine(DodgeRoutine());
        }
        
        anim.SetBool("isRun", moveVec != Vector3.zero);
        anim.SetBool("isWalk", wDown);
        
        if (moveVec != Vector3.zero)
            transform.LookAt(transform.position + moveVec); //나아가는 방향으로 바라본다

        


    }

    private void FixedUpdate()
    {
        if (isDead) return; //죽은뒤엔 이동 정지
        
        float speedMultiplier = isDodging ? dodgespeedMultiplier : (wDown ? 0.3f : 1f);
        Vector3 horizontalVelocity = moveVec * speed * speedMultiplier;
        rb.linearVelocity = new Vector3(horizontalVelocity.x, rb.linearVelocity.y, horizontalVelocity.z);
        
    }

    private IEnumerator DodgeRoutine()
    {
        isDodging = true;
        dodgeCooldownTimer = dodgeCooldown;
        
        anim.SetTrigger("Dodge");

        yield return new WaitForSeconds(dodgeDuration);

        isDodging = false;
    }

    public void TakeDamage()
    {
        if (currentHealth <= 0 || isInvincible) return;

        int heartIndex = heartAnimators.Length - currentHealth;
        heartAnimators[heartIndex].SetTrigger("Disappear");

        currentHealth--;

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(HitFlashRoutine()); //죽지 않았을 때, 깜빡임 + 무적
        }
    }
    public void Die()
    {
        if (isDead) return; //죽은뒤엔 이동 정지
        isDead = true;

        rb.linearVelocity = Vector3.zero; //죽은 순간 미끄러지지 않게 정지
        anim.SetTrigger("Die");

        StartCoroutine(DieRoutine());
    }

    private IEnumerator HitFlashRoutine()
    {
        isInvincible = true;

        float timer = 0f;
        bool showRed = true;

        while (timer < invincibleDuration)
        {
            SetBodyColor(showRed ? Color.red : Color.white);
            showRed = !showRed;
            
            yield return new WaitForSeconds(flashInterval);
            timer += flashInterval;
        }

        SetBodyColor(Color.white);
        isInvincible = false;
    }

    private void SetBodyColor(Color color)
    {
        var block = new MaterialPropertyBlock();
        foreach (var r in bodyRenderers)
        {
            r.GetPropertyBlock(block);
            block.SetColor("_White",color);
            r.SetPropertyBlock(block);
        }
    }

    private IEnumerator DieRoutine()
    {
        yield return new WaitForSeconds(1.5f);

        gameObject.SetActive(false);

        GameManager2 gameManager = FindFirstObjectByType<GameManager2>();
        if (gameManager != null)
        {
            gameManager.Endgame();
        }
    }
}

