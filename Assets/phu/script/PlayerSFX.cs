using UnityEngine;

public class PlayerSFX : MonoBehaviour
{
    public static PlayerSFX Instance;

    [Header("Audio Source")]
    public AudioSource audioSource;

    [Header("Movement & Basic Attack Clips")]
    public AudioClip walkClip;
    public AudioClip jumpClip;
    public AudioClip dashClip;
    public AudioClip clickAttackClip;
    public AudioClip chargeHoldClip;
    public AudioClip chargeReleaseClip;

    [Header("Combat & QTE Clips (เสียงใหม่ที่เพิ่มเข้ามา)")]
    public AudioClip slamGroundClip;      // เสียงตอน Slam กระแทกพื้น
    public AudioClip enemyHitClip;        // เสียงตอนฟันโดน Enemy
    public AudioClip playerHurtClip;      // เสียงตอนเราโดนฟัน/โดนดาเมจ
    public AudioClip qteStartClip;        // เสียงตอนเริ่ม Quick Time Event (กด E)

    [Header("Walk & Slam Settings")]
    public float stepInterval = 0.35f;
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.2f;
    public float slamDownwardSpeedThreshold = -12f; // ความเร็วพุ่งลงพื้นขั้นต่ำที่จะนับว่า Slam

    [Header("Input Keys (ปรับให้ตรงกับปุ่มในเกม)")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode dashKey = KeyCode.LeftShift;
    public float holdThreshold = 0.2f;

    private Rigidbody2D rb;
    private Collider2D col;
    private PlayerHealth playerHealth;
    private Vector3 lastPosition;
    private float stepTimer = 0f;
    private float holdTimer = 0f;
    private bool playedHoldSound = false;

    // ตัวแปรจับสถานะ Slam, Hurt และ QTE
    private bool wasGrounded = true;
    private bool isSlammingDown = false;
    private int lastKnownHealth = -1;
    private bool wasQTEActive = false;

    void Awake()
    {
        Instance = this;

        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        playerHealth = GetComponent<PlayerHealth>();
        lastPosition = transform.position;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
    }

    void Start()
    {
        if (playerHealth != null)
            lastKnownHealth = playerHealth.currentHealth;
    }

    void Update()
    {
        HandleWalkJumpAndSlam();
        HandleDash();
        HandleClickAndHoldAttack();
        HandlePlayerHurtCheck();
        HandleQTECheck();

        lastPosition = transform.position;
    }

    bool CheckGrounded()
    {
        if (groundLayer.value != 0 && col != null)
        {
            Vector2 origin = new Vector2(col.bounds.center.x, col.bounds.min.y + 0.05f);
            RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, groundCheckDistance, groundLayer);
            return hit.collider != null;
        }

        if (rb != null)
            return Mathf.Abs(rb.linearVelocity.y) < 0.2f;

        return true;
    }

    void HandleWalkJumpAndSlam()
    {
        bool isGrounded = CheckGrounded();

        // 1. เช็กจังหวะกด S + คลิกซ้ายกลางอากาศ หรือพุ่งดิ่งลงพื้นอย่างเร็ว (Slam)
        if (!isGrounded)
        {
            if (Input.GetKey(KeyCode.S) && Input.GetMouseButtonDown(0))
                isSlammingDown = true;

            if (rb != null && rb.linearVelocity.y <= slamDownwardSpeedThreshold)
                isSlammingDown = true;
        }

        // จังหวะตัวละครกระแทกถึงพื้นหลังจากพุ่งลงมา -> เล่นเสียง Slam พื้น
        if (isGrounded && !wasGrounded && isSlammingDown)
        {
            PlaySlamSound();
            isSlammingDown = false;
        }
        else if (isGrounded)
        {
            isSlammingDown = false;
        }

        wasGrounded = isGrounded;

        // 2. เสียงกระโดด
        if (Input.GetKeyDown(jumpKey) && isGrounded)
        {
            PlaySound(jumpClip);
        }

        // 3. เสียงเดิน
        float moveInput = Input.GetAxisRaw("Horizontal");
        float movedX = Mathf.Abs(transform.position.x - lastPosition.x) / Mathf.Max(Time.deltaTime, 0.0001f);
        bool isMoving = Mathf.Abs(moveInput) > 0.1f || movedX > 0.2f;

        if (isMoving && isGrounded)
        {
            stepTimer -= Time.deltaTime;
            if (stepTimer <= 0f)
            {
                PlaySound(walkClip, 0.92f, 1.08f);
                stepTimer = stepInterval;
            }
        }
        else
        {
            stepTimer = 0f;
        }
    }

    void HandleDash()
    {
        if (Input.GetKeyDown(dashKey))
        {
            PlaySound(dashClip, 0.95f, 1.1f);
        }
    }

    void HandleClickAndHoldAttack()
    {
        if (Input.GetMouseButtonDown(0))
        {
            holdTimer = 0f;
            playedHoldSound = false;
        }

        if (Input.GetMouseButton(0))
        {
            holdTimer += Time.unscaledDeltaTime;

            if (holdTimer >= holdThreshold && !playedHoldSound)
            {
                playedHoldSound = true;
                PlaySound(chargeHoldClip, 1f, 1f);
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (playedHoldSound)
            {
                PlaySound(chargeReleaseClip, 0.95f, 1.05f);
            }
            else
            {
                PlaySound(clickAttackClip, 0.92f, 1.08f);
            }

            holdTimer = 0f;
            playedHoldSound = false;
        }
    }

    // ตรวจจับอัตโนมัติเมื่อเลือด Player ลดลง -> เล่นเสียงโดนฟัน
    void HandlePlayerHurtCheck()
    {
        if (playerHealth == null) return;

        if (lastKnownHealth != -1 && playerHealth.currentHealth < lastKnownHealth)
        {
            PlayPlayerHurtSound();
        }
        lastKnownHealth = playerHealth.currentHealth;
    }

    // ตรวจจับอัตโนมัติเมื่อเข้าสู่โหมด Finisher QTE -> เล่นเสียง QTE
    void HandleQTECheck()
    {
        bool qteNow = FinisherManager.isQTEActive;
        if (qteNow && !wasQTEActive)
        {
            PlayQTESound();
        }
        wasQTEActive = qteNow;
    }

    // --- ฟังก์ชันสาธารณะสำหรับให้สคริปต์อื่นเรียกใช้ได้โดยตรง ---
    public void PlaySlamSound()
    {
        PlaySound(slamGroundClip, 0.95f, 1.05f);
    }

    public void PlayEnemyHitSound()
    {
        PlaySound(enemyHitClip, 0.92f, 1.12f);
    }

    public void PlayPlayerHurtSound()
    {
        PlaySound(playerHurtClip, 0.95f, 1.05f);
    }

    public void PlayQTESound()
    {
        PlaySound(qteStartClip, 1f, 1f);
    }

    void PlaySound(AudioClip clip, float minPitch = 0.95f, float maxPitch = 1.05f)
    {
        if (clip == null || audioSource == null) return;
        audioSource.pitch = Random.Range(minPitch, maxPitch);
        audioSource.PlayOneShot(clip);
    }
}