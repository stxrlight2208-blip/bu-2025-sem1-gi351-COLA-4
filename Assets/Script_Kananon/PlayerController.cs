using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // ประเภทของตัวละคร (เลือกได้ 2 ตัว)
    public enum CharacterType { BlueWarrior, RedWarrior }
    [Header("Character Selection")]
    public CharacterType currentCharacter = CharacterType.BlueWarrior;

    [Header("Movement Settings")]
    public float walkSpeed = 4f;
    public float runSpeed = 6.5f; // ปรับความเร็ววิ่งให้เพิ่มขึ้นจากเดิมนิดหน่อย (เดิน 4 -> วิ่ง 6.5)
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

    // สถานะการเคลื่อนไหว
    private float horizontalInput;
    private bool isRunning;
    private bool isGrounded;
    public Transform groundCheck;
    public LayerMask groundLayer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        
        // กำหนดค่าความสามารถตามตัวละคร
        ApplyCharacterStats();
    }

    void Update()
    {
        // หากกำลังแดชอยู่ ไม่รับอินพุตปกติ
        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f)
            {
                isDashing = false;
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }
            UpdateAnimations();
            return;
        }

        // รับค่าการเคลื่อนไหวซ้าย-ขวา (A, D หรือ ลูกศร)
        horizontalInput = Input.GetAxisRaw("Horizontal");

        // เปลี่ยนกลับมาเป็น "กด Shift ค้างไว้" เพื่อวิ่ง
        isRunning = Input.GetKey(KeyCode.LeftShift);

        // เช็คการกระโดด (กด Spacebar)
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            Jump();
        }

        // เช็คการพุ่งตัว (Dash - กดปุ่ม Q)
        if (Input.GetKeyDown(KeyCode.Q) && canDash)
        {
            StartDash();
        }

        // คูลดาวน์แดช
        if (!canDash)
        {
            dashCooldownTimer -= Time.deltaTime;
            if (dashCooldownTimer <= 0f)
            {
                canDash = true;
            }
        }

        // พลิกด้าน Sprite ตามทิศทางเดิน
        Flip();

        // อัปเดตอนิเมชัน
        UpdateAnimations();
    }

    void FixedUpdate()
    {
        if (isDashing) return;

        // เช็คว่าอยู่บนพื้นหรือไม่
        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);
        }

        // คำนวณความเร็ว (เดินหรือวิ่ง)
        float currentSpeed = isRunning ? runSpeed : walkSpeed;
        rb.linearVelocity = new Vector2(horizontalInput * currentSpeed, rb.linearVelocity.y);
    }

    void UpdateAnimations()
    {
        if (animator == null) return;

        // คำนวณความเร็วเดิน/วิ่ง
        float moveSpeed = Mathf.Abs(horizontalInput) * (isRunning ? 1.5f : 1f);

        animator.SetFloat("Speed", moveSpeed);
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetBool("IsDashing", isDashing);
        
        // ส่งค่าความเร็วแกน Y (ถ้าค่าบวกแปลว่าพุ่งขึ้น, ค่าลบแปลว่าร่วงลง)
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

        float dir = transform.localScale.x > 0 ? 1f : -1f;
        rb.linearVelocity = new Vector2(dir * dashSpeed, 0f);
    }

    void Flip()
    {
        if (horizontalInput > 0.01f)
        {
            transform.localScale = new Vector3(1f, 1f, 1f);
        }
        else if (horizontalInput < -0.01f)
        {
            transform.localScale = new Vector3(-1f, 1f, 1f);
        }
    }

    void ApplyCharacterStats()
    {
        if (currentCharacter == CharacterType.BlueWarrior)
        {
            dashSpeed = 15f;
            runSpeed = 6.5f; // วิ่งเร็วขึ้นกว่าเดินนิดหน่อยกำลังสวย
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