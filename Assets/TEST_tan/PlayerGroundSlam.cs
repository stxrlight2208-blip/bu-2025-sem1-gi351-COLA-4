using UnityEngine;

public class PlayerGroundSlam : MonoBehaviour
{
    [Header("Input & Activation")]
    public KeyCode slamKey = KeyCode.S;        // กด S (หรือลูกศรลง) + คลิกซ้าย กลางอากาศ
    public float minAirHeight = 0.8f;          // ต้องอยู่สูงจากพื้นระดับหนึ่งถึงจะฟาดได้

    [Header("Slam Physics & Combat")]
    public float diveSpeed = 26f;              // ความเร็วพุ่งดิ่งลงพื้น
    public float shockwaveRadius = 3.8f;       // รัศมีคลื่นกระแทกรอบจุดที่ลงพื้น
    public int slamDamage = 3;                 // ดาเมจฟาดพื้น
    public float enemyLaunchForce = 9.5f;      // แรงงัดมอนสเตอร์ลอยขึ้นฟ้า
    public float enemyAirHangTime = 0.85f;     // ระยะเวลามอนลอยค้างกลางอากาศ (วินาที)
    public float cooldown = 0.8f;

    [Header("Visual Effect")]
    public string vfxSortingLayer = "New Layer 3";
    public int vfxSortingOrder = 130;

    private Rigidbody2D rb;
    private bool isSlamming = false;
    private float lastSlamTime = -10f;
    private float originalGravity;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null) originalGravity = rb.gravityScale;
    }

    void Update()
    {
        if (FinisherManager.isQTEActive || rb == null) return;

        bool isGrounded = Mathf.Abs(rb.linearVelocity.y) < 0.05f;

        // เงื่อนไข: อยู่กลางอากาศ + กดปุ่มลง (S หรือ DownArrow) ค้างไว้แล้วกดคลิกซ้าย
        if (!isSlamming && !isGrounded && Time.time - lastSlamTime >= cooldown)
        {
            bool holdingDown = Input.GetKey(slamKey) || Input.GetKey(KeyCode.DownArrow);
            if (holdingDown && Input.GetMouseButtonDown(0))
            {
                StartGroundSlam();
            }
        }

        // จังหวะพุ่งลงพื้นจนเท้าแตะพื้น (ความเร็วแกน Y หยุดลง)
        if (isSlamming && isGrounded)
        {
            TriggerShockwaveImpact();
        }
    }

    void StartGroundSlam()
    {
        isSlamming = true;
        lastSlamTime = Time.time;

        // พุ่งดิ่งลงพื้นแนวดิ่งทันที
        rb.linearVelocity = new Vector2(0f, -diveSpeed);
    }

    void TriggerShockwaveImpact()
    {
        isSlamming = false;
        rb.linearVelocity = Vector2.zero;

        // 1. สั่นกล้อง + เพิ่มคะแนน Style
        if (CameraFollow.instance != null)
            CameraFollow.instance.Shake(0.18f, 0.28f);

        if (StyleRankManager.Instance != null)
            StyleRankManager.Instance.AddStylePoints(120f);

        // 2. วาดคลื่นกระแทกบนพื้น (แสดงผลบน New Layer 3)
        SpawnSlamShockwave();

        // 3. ตรวจจับศัตรูในรัศมีรอบตัวแล้วงัดให้ลอยค้างกลางอากาศ
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, shockwaveRadius);
        foreach (Collider2D hit in hits)
        {
            Enemy enemy = hit.GetComponent<Enemy>();
            if (enemy == null) enemy = hit.GetComponentInParent<Enemy>();

            if (enemy != null)
            {
                enemy.LaunchIntoAir(slamDamage, enemyLaunchForce, enemyAirHangTime);
            }
        }
    }

    void SpawnSlamShockwave()
    {
        GameObject waveObj = new GameObject("GroundSlamShockwave");
        waveObj.transform.position = transform.position;

        LineRenderer lr = waveObj.AddComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Sprites/Default"));
        int segs = 40;
        lr.positionCount = segs + 1;
        lr.loop = true;
        lr.startWidth = 0.22f;
        lr.endWidth = 0.22f;
        lr.startColor = new Color(1f, 0.8f, 0.2f, 1f);
        lr.endColor = new Color(1f, 0.3f, 0.1f, 1f);
        lr.sortingLayerName = vfxSortingLayer;
        lr.sortingOrder = vfxSortingOrder;

        SlamWaveExpand expand = waveObj.AddComponent<SlamWaveExpand>();
        expand.maxRadius = shockwaveRadius;
        expand.duration = 0.25f;
    }

    // แสดงวงรัศมีในหน้า Scene เพื่อปรับขนาดง่ายๆ
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, shockwaveRadius);
    }
}

public class SlamWaveExpand : MonoBehaviour
{
    public float maxRadius = 3.8f;
    public float duration = 0.25f;
    private float timer = 0f;
    private LineRenderer lr;

    void Awake() => lr = GetComponent<LineRenderer>();

    void Update()
    {
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / duration);
        float r = Mathf.Lerp(0.4f, maxRadius, t);

        Color c = lr.startColor;
        c.a = 1f - t;
        lr.startColor = c;
        lr.endColor = c;

        int segs = lr.positionCount - 1;
        for (int i = 0; i <= segs; i++)
        {
            float rad = ((float)i / segs) * Mathf.PI * 2f;
            // วาดเป็นวงรีแบนราบไปกับพื้นให้ดูมีมิติ 2.5D
            lr.SetPosition(i, transform.position + new Vector3(Mathf.Cos(rad) * r, Mathf.Sin(rad) * (r * 0.35f), 0f));
        }

        if (t >= 1f) Destroy(gameObject);
    }
}