using UnityEngine;

public class PlayerAirHover : MonoBehaviour
{
    [Header("Double Jump & Hover Controls")]
    public KeyCode jumpAndHoverKey = KeyCode.Space;
    public int maxAirJumps = 1;                  // จำนวนครั้งที่กระโดดกลางอากาศเพิ่มได้ (1 = Double Jump)
    public float doubleJumpForce = 12.5f;        // แรงกระโดดครั้งที่ 2 กลางอากาศ

    [Header("Air Hover Settings")]
    public float maxHoverTime = 1.6f;            // ลอยค้างได้สูงสุดกี่วินาที
    public float hoverGravityScale = 0.08f;      // แรงโน้มถ่วงตอนลอย (ค่าน้อย = ยิ่งลอยนิ่ง)
    public float holdDelayBeforeHover = 0.16f;   // ต้องกด Space ค้างเกินกี่วินาทีถึงจะเริ่มร่อนลอย

    [Header("Visual Effects")]
    public bool showHoverTint = true;
    public Color hoverTint = new Color(0.75f, 0.92f, 1f, 1f);
    public string vfxSortingLayer = "New Layer 3";
    public int vfxSortingOrder = 120;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private float defaultGravity;

    private int airJumpsRemaining;
    private float hoverTimer = 0f;
    private float spaceHoldTimer = 0f;
    private float airborneTimer = 0f;
    private bool isHovering = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (rb != null)
            defaultGravity = rb.gravityScale;

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        airJumpsRemaining = maxAirJumps;
    }

    void Update()
    {
        if (FinisherManager.isQTEActive || rb == null)
        {
            StopHover();
            return;
        }

        // เช็กว่าตัวละครเหยียบพื้นอยู่หรือไม่
        bool isGrounded = Mathf.Abs(rb.linearVelocity.y) < 0.05f && airborneTimer > 0.1f;

        if (isGrounded)
        {
            airborneTimer = 0f;
            hoverTimer = 0f;
            spaceHoldTimer = 0f;
            airJumpsRemaining = maxAirJumps; // รีเซ็ตจำนวนกระโดดกลางอากาศและเวลาลอยเมื่อแตะพื้น
            StopHover();
            return;
        }

        airborneTimer += Time.deltaTime;

        // 1. จังหวะกด Spacebar กลางอากาศ -> กระโดดครั้งที่ 2 (Double Jump)
        if (airborneTimer > 0.08f && Input.GetKeyDown(jumpAndHoverKey) && airJumpsRemaining > 0)
        {
            PerformDoubleJump();
        }

        // 2. จังหวะกด Spacebar ค้างกลางอากาศ -> ร่อนลอยตัว (Air Hover)
        if (Input.GetKey(jumpAndHoverKey))
        {
            spaceHoldTimer += Time.deltaTime;

            if (spaceHoldTimer >= holdDelayBeforeHover && hoverTimer < maxHoverTime)
            {
                StartOrMaintainHover();
            }
            else
            {
                StopHover();
            }
        }
        else
        {
            spaceHoldTimer = 0f;
            StopHover();
        }
    }

    void PerformDoubleJump()
    {
        airJumpsRemaining--;
        spaceHoldTimer = 0f; // รีเซ็ตตัวจับเวลาค้าง เพื่อไม่ให้ขัดจังหวะแรงดีดกระโดดครั้งที่ 2
        StopHover();

        // ดีดตัวขึ้นฟ้าอีกรอบ
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, doubleJumpForce);

        // เล่นแอนิเมชันกระโดด (ถ้ามี Trigger Jump)
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            foreach (AnimatorControllerParameter p in animator.parameters)
            {
                if (p.name == "Jump" && p.type == AnimatorControllerParameterType.Trigger)
                {
                    animator.SetTrigger("Jump");
                    break;
                }
            }
        }

        // เสกวงแหวนอากาศใต้เท้าตอนกระโดดครั้งที่ 2
        SpawnDoubleJumpRing();
    }

    void StartOrMaintainHover()
    {
        isHovering = true;
        hoverTimer += Time.deltaTime;

        rb.gravityScale = hoverGravityScale;
        if (rb.linearVelocity.y < -0.4f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -0.4f);
        }

        if (showHoverTint && spriteRenderer != null)
        {
            spriteRenderer.color = hoverTint;
        }
    }

    void StopHover()
    {
        if (!isHovering) return;

        isHovering = false;
        if (rb != null)
            rb.gravityScale = defaultGravity;

        if (showHoverTint && spriteRenderer != null)
            spriteRenderer.color = originalColor;
    }

    void SpawnDoubleJumpRing()
    {
        GameObject ringObj = new GameObject("DoubleJumpAirRing");
        ringObj.transform.position = transform.position + new Vector3(0f, -0.6f, 0f);

        LineRenderer lr = ringObj.AddComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Sprites/Default"));
        int segs = 32;
        lr.positionCount = segs + 1;
        lr.loop = true;
        lr.startWidth = 0.14f;
        lr.endWidth = 0.14f;
        lr.startColor = new Color(0.7f, 0.95f, 1f, 0.9f);
        lr.endColor = new Color(0.4f, 0.8f, 1f, 0.9f);
        lr.sortingLayerName = vfxSortingLayer;
        lr.sortingOrder = vfxSortingOrder;

        DoubleJumpRingExpand expand = ringObj.AddComponent<DoubleJumpRingExpand>();
        expand.maxRadius = 1.4f;
        expand.duration = 0.22f;
    }

    void OnDisable()
    {
        StopHover();
    }
}

public class DoubleJumpRingExpand : MonoBehaviour
{
    public float maxRadius = 1.4f;
    public float duration = 0.22f;
    private float timer = 0f;
    private LineRenderer lr;

    void Awake() => lr = GetComponent<LineRenderer>();

    void Update()
    {
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / duration);
        float r = Mathf.Lerp(0.25f, maxRadius, t);

        Color c = lr.startColor;
        c.a = 1f - t;
        lr.startColor = c;
        lr.endColor = c;

        int segs = lr.positionCount - 1;
        for (int i = 0; i <= segs; i++)
        {
            float rad = ((float)i / segs) * Mathf.PI * 2f;
            lr.SetPosition(i, transform.position + new Vector3(Mathf.Cos(rad) * r, Mathf.Sin(rad) * (r * 0.3f), 0f));
        }

        if (t >= 1f) Destroy(gameObject);
    }
}