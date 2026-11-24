using UnityEngine;

public class PlayerSFX : MonoBehaviour
{
    [Header("Audio Source")]
    public AudioSource audioSource;

    [Header("Clips (ลากไฟล์เสียงมาใส่ใน Inspector)")]
    public AudioClip walkClip;
    public AudioClip jumpClip;
    public AudioClip dashClip;
    public AudioClip clickAttackClip;
    public AudioClip chargeHoldClip;
    public AudioClip chargeReleaseClip;

    [Header("Walk Settings")]
    public float stepInterval = 0.35f;
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.2f;

    [Header("Input Keys (ปรับให้ตรงกับปุ่มในเกม)")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode dashKey = KeyCode.LeftShift;
    public float holdThreshold = 0.2f;

    private Rigidbody2D rb;
    private Collider2D col;
    private Vector3 lastPosition;
    private float stepTimer = 0f;
    private float holdTimer = 0f;
    private bool playedHoldSound = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        lastPosition = transform.position;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
    }

    void Update()
    {
        HandleWalkAndJump();
        HandleDash();
        HandleClickAndHoldAttack();
        lastPosition = transform.position;
    }

    bool CheckGrounded()
    {
        // ถ้าตั้ง Ground Layer ไว้ใน Inspector จะยิง Raycast เช็กพื้นได้แม่นที่สุด
        if (groundLayer.value != 0 && col != null)
        {
            Vector2 origin = new Vector2(col.bounds.center.x, col.bounds.min.y + 0.05f);
            RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, groundCheckDistance, groundLayer);
            return hit.collider != null;
        }

        // กรณียังไม่ได้ตั้ง Ground Layer ให้เช็กจากความเร็วแกน Y แบบผ่อนปรนขึ้น
        if (rb != null)
            return Mathf.Abs(rb.linearVelocity.y) < 0.2f;

        return true;
    }

    void HandleWalkAndJump()
    {
        bool isGrounded = CheckGrounded();

        // เสียงกระโดด
        if (Input.GetKeyDown(jumpKey) && isGrounded)
        {
            PlaySound(jumpClip);
        }

        // เช็กการเดินจากทั้งปุ่มซ้าย-ขวา (A/D หรือลูกศร) และการเปลี่ยนตำแหน่งแกน X จริง
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
            // รีเซ็ตให้ก้าวแรกดังทันทีเมื่อเริ่มเดินครั้งถัดไป
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

    void PlaySound(AudioClip clip, float minPitch = 0.95f, float maxPitch = 1.05f)
    {
        if (clip == null || audioSource == null) return;
        audioSource.pitch = Random.Range(minPitch, maxPitch);
        audioSource.PlayOneShot(clip);
    }
}