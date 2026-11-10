
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
    private bool isDashing = false;
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

    // เก็บขนาดเดิมของ Player
    private Vector3 originalScale;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        playerHealth = GetComponent<PlayerHealth>();

        originalScale = transform.localScale;

        ApplyCharacterStats();
    }

    void Update()
    {
        if (isDashing)
        {
            dashTimer -= Time.deltaTime;

            if (dashTimer <= 0f)
            {
                isDashing = false;
                rb.linearVelocity =
                    new Vector2(0f, rb.linearVelocity.y);

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
        if (isDashing) return;

        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapCircle(
                groundCheck.position,
                0.2f,
                groundLayer
            );
        }

        float currentSpeed = isRunning ? runSpeed : walkSpeed;

        rb.linearVelocity = new Vector2(
            horizontalInput * currentSpeed,
            rb.linearVelocity.y
        );
    }

    void UpdateAnimations()
    {
        if (animator == null) return;

        float moveSpeed =
            Mathf.Abs(horizontalInput) * (isRunning ? 1.5f : 1f);

        animator.SetFloat("Speed", moveSpeed);
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetBool("IsDashing", isDashing);
        animator.SetFloat("VerticalVelocity", rb.linearVelocity.y);
    }

    void Jump()
    {
        rb.linearVelocity =
            new Vector2(rb.linearVelocity.x, jumpForce);
    }

    void StartDash()
    {
        isDashing = true;
        canDash = false;

        dashTimer = dashTime;
        dashCooldownTimer = dashCooldown;

        if (playerHealth != null)
        {
            playerHealth.isInvincible = true;
        }

        float dir = transform.localScale.x > 0 ? 1f : -1f;

        rb.linearVelocity = new Vector2(dir * dashSpeed, 0f);
    }

    void Flip()
    {
        if (horizontalInput > 0.01f)
        {
            transform.localScale = new Vector3(
                Mathf.Abs(originalScale.x),
                originalScale.y,
                originalScale.z
            );
        }
        else if (horizontalInput < -0.01f)
        {
            transform.localScale = new Vector3(
                -Mathf.Abs(originalScale.x),
                originalScale.y,
                originalScale.z
            );
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
