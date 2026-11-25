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

    [Header("Combat Clips")]
    public AudioClip slamGroundClip;      // เสียงตอน Slam กระแทกพื้น
    public AudioClip enemyHitClip;        // เสียงตอนฟันโดน Enemy
    public AudioClip playerHurtClip;      // เสียงตอนเราโดนฟัน/โดนดาเมจ

    [Header("Quick Time Event (QTE) Clips — ครบทุกจังหวะ")]
    public AudioClip qteStartClip;        // 1. เสียงตอนเริ่ม QTE
    public AudioClip qteTapClip;          // 2. เสียงตอนกดปุ่มใน QTE ทุกครั้ง
    public AudioClip qteSuccessClip;      // 3. เสียงตอนกด QTE สำเร็จ (ปิดฉาก)
    public AudioClip qteFailClip;         // 4. เสียงตอนกด QTE พลาด / หมดเวลา

    [Header("QTE Input Detection (ปุ่มที่ให้ผู้เล่นกดใน QTE)")]
    public KeyCode[] qteKeys = new KeyCode[] { KeyCode.E, KeyCode.Space, KeyCode.F, KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D };

    [Header("Walk & Slam Settings")]
    public float stepInterval = 0.35f;
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.2f;
    public float slamDownwardSpeedThreshold = -12f;

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
    private float qteStartTime = 0f;
    private int qteTapComboCount = 0;

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
        HandleQTECheck();
        HandlePlayerHurtCheck();

        // ถ้าระหว่างเล่น QTE ให้ข้ามเสียงเดิน/กระโดด/โจมตีปกติไปก่อน เสียงจะได้ไม่ตีกัน
        if (FinisherManager.isQTEActive)
        {
            lastPosition = transform.position;
            return;
        }

        HandleWalkJumpAndSlam();
        HandleDash();
        HandleClickAndHoldAttack();

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

        if (!isGrounded)
        {
            if (Input.GetKey(KeyCode.S) && Input.GetMouseButtonDown(0))
                isSlammingDown = true;

            if (rb != null && rb.linearVelocity.y <= slamDownwardSpeedThreshold)
                isSlammingDown = true;
        }

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

        if (Input.GetKeyDown(jumpKey) && isGrounded)
        {
            PlaySound(jumpClip);
        }

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

    void HandlePlayerHurtCheck()
    {
        if (playerHealth == null) return;

        if (lastKnownHealth != -1 && playerHealth.currentHealth < lastKnownHealth)
        {
            PlayPlayerHurtSound();
        }
        lastKnownHealth = playerHealth.currentHealth;
    }

    // ตรวจจับทุกจังหวะของ Quick Time Event อัตโนมัติ
    void HandleQTECheck()
    {
        bool qteNow = FinisherManager.isQTEActive;

        // 1. จังหวะเริ่ม QTE
        if (qteNow && !wasQTEActive)
        {
            qteStartTime = Time.unscaledTime;
            qteTapComboCount = 0;
            PlayQTEStartSound();
        }

        // 2. จังหวะกำลังกดปุ่มรัวๆ / กดถูกจังหวะระหว่าง QTE
        if (qteNow && wasQTEActive)
        {
            // หน่วง 0.08 วิ กันเสียงปุ่ม E ตอนกดเริ่มไปทับกับเสียงกดครั้งแรก
            if (Time.unscaledTime - qteStartTime > 0.08f && IsAnyQTEInputDown())
            {
                PlayQTETapSound();
            }
        }

        // 3. จังหวะจบ QTE (สำเร็จ vs พลาด/หมดเวลา)
        if (!qteNow && wasQTEActive)
        {
            // ถ้าใน FinisherManager ไม่ได้เรียก PlayQTESuccess/Fail เอง โค้ดนี้จะประเมินให้ทันที
            if (qteTapComboCount > 0)
                PlayQTESuccessSound();
            else
                PlayQTEFailSound();
        }

        wasQTEActive = qteNow;
    }

    bool IsAnyQTEInputDown()
    {
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
            return true;

        if (qteKeys != null)
        {
            for (int i = 0; i < qteKeys.Length; i++)
            {
                if (Input.GetKeyDown(qteKeys[i]))
                    return true;
            }
        }
        return false;
    }

    // --- ฟังก์ชันสาธารณะสำหรับเรียกใช้ตรงๆ จาก FinisherManager หรือสคริปต์อื่น ---
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

    public void PlayQTEStartSound()
    {
        PlaySound(qteStartClip, 1f, 1f);
    }

    public void PlayQTETapSound()
    {
        if (qteTapClip == null || audioSource == null) return;
        // ไล่ระดับเสียง Pitch สูงขึ้นเรื่อยๆ ตามจำนวนครั้งที่กดรัว (1.0 -> สูงสุด 1.45)
        float dynamicPitch = Mathf.Clamp(1.0f + (qteTapComboCount * 0.04f), 1.0f, 1.45f);
        qteTapComboCount++;
        audioSource.pitch = dynamicPitch;
        audioSource.PlayOneShot(qteTapClip);
    }

    public void PlayQTESuccessSound()
    {
        PlaySound(qteSuccessClip, 1f, 1f);
    }

    public void PlayQTEFailSound()
    {
        PlaySound(qteFailClip, 0.95f, 1.0f);
    }

    void PlaySound(AudioClip clip, float minPitch = 0.95f, float maxPitch = 1.05f)
    {
        if (clip == null || audioSource == null) return;
        audioSource.pitch = Random.Range(minPitch, maxPitch);
        audioSource.PlayOneShot(clip);
    }
}