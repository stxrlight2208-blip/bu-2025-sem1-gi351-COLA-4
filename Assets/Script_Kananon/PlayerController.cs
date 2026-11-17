using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public enum CharacterType { BlueWarrior, RedWarrior }
    [Header("Character Selection")]
    public CharacterType currentCharacter = CharacterType.BlueWarrior;

    [Header("Movement Settings")]
    public float walkSpeed = 4f;
    public float runSpeed = 6.5f;
    public float jumpForce = 12f;

    [Header("Dash Settings")]
    public float dashSpeed = 20f;
    public float dashTime = 0.2f;
    public float dashCooldown = 1f;
    private bool canDash = true;
    public bool isDashing = false;
    private float dashTimer;
    private float dashCooldownTimer;

    [Header("References")]
    private Rigidbody2D rb;
    private Animator animator;
    private PlayerHealth playerHealth;

    private float horizontalInput;
    private bool isRunning;
    private bool isGrounded;
    public Transform groundCheck;
    public LayerMask groundLayer;

    private Vector3 originalScale; // เก็บขนาดตัวละครเดิมที่ตั้งไว้ใน Inspector

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        playerHealth = GetComponent<PlayerHealth>();

        // จำขนาด Scale เริ่มต้นเอาไว้ไม่ให้หดกลับเป็น 1 ตอนเดินพลิกตัว
        originalScale = new Vector3(
            Mathf.Abs(transform.localScale.x) > 0.01f ? Mathf.Abs(transform.localScale.x) : 1f,
            Mathf.Abs(transform.localScale.y) > 0.01f ? transform.localScale.y : 1f,
            transform.localScale.z
        );

        ApplyCharacterStats();
    }

    void Update()
    {
        // ถ้ากำลังอยู่ในฉาก Finisher QTE ให้หยุดรับปุ่มเดิน/แดชชั่วคราว
        if (FinisherManager.isQTEActive)
        {
            if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetFloat("Speed", 0f);
            }
            return;
        }

        // ห้ามควบคุมหากผู้เล่นตายแล้ว
        if (playerHealth != null && playerHealth.currentHealth <= 0)
        {
            if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f)
            {
                isDashing = false;
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

                if (playerHealth != null)
                {
                    playerHealth.isInvincible = false;
                }
            }
            UpdateAnimations();
            return;
        }

        horizontalInput = Input.GetAxisRaw("Horizontal");
        isRunning = Input.GetKey(KeyCode.LeftShift);

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            Jump();
        }

        if (Input.GetKeyDown(KeyCode.Q) && canDash)
        {
            StartDash();
        }

        if (!canDash)
        {
            dashCooldownTimer -= Time.deltaTime;
            if (dashCooldownTimer <= 0f)
            {
                canDash = true;
            }
        }

        Flip();
        UpdateAnimations();
    }

    void FixedUpdate()
    {
        if (FinisherManager.isQTEActive || isDashing) return;
        if (playerHealth != null && playerHealth.currentHealth <= 0) return;

        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);
        }

        float currentSpeed = isRunning ? runSpeed : walkSpeed;
        rb.linearVelocity = new Vector2(horizontalInput * currentSpeed, rb.linearVelocity.y);
    }

    void UpdateAnimations()
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;

        float moveSpeed = Mathf.Abs(horizontalInput) * (isRunning ? 1.5f : 1f);

        animator.SetFloat("Speed", moveSpeed);
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetBool("IsDashing", isDashing);
        animator.SetFloat("VerticalVelocity", rb.linearVelocity.y);
    }

    void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
    }

    void StartDash()
    {
        isDashing = true;
        canDash = false;
        dashTimer = dashTime;
        dashCooldownTimer = dashCooldown;

        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlaySFX(AudioManager.instance.dashClip);
        }

        if (playerHealth != null)
        {
            playerHealth.isInvincible = true; // เปิดอมตะตลอดช่วงที่พุ่งหลบ
        }

        float dir = transform.localScale.x > 0 ? 1f : -1f;
        rb.linearVelocity = new Vector2(dir * dashSpeed, 0f);
    }

    void Flip()
    {
        if (horizontalInput > 0.01f)
        {
            transform.localScale = new Vector3(originalScale.x, originalScale.y, originalScale.z);
        }
        else if (horizontalInput < -0.01f)
        {
            transform.localScale = new Vector3(-originalScale.x, originalScale.y, originalScale.z);
        }
    }

    void ApplyCharacterStats()
    {
        if (currentCharacter == CharacterType.BlueWarrior)
        {
            dashSpeed = 15f;
            runSpeed = 6.5f;
        }
        else
        {
            dashSpeed = 16f;
            runSpeed = 5.5f;
        }
    }

    public void SwitchCharacter(CharacterType newType)
    {
        currentCharacter = newType;
        ApplyCharacterStats();
    }
}