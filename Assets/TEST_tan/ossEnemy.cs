using System.Collections;
using UnityEngine;

public class BossEnemy : MonoBehaviour
{
    [Header("Boss Stats")]
    public int maxHealth = 40;
    private int currentHealth;
    private bool isPhaseTwo = false;

    [Header("Movement & Detection")]
    public float moveSpeed = 2.2f;
    public float detectionRange = 10f;
    public float attackRange = 4.5f; // ระยะที่บอสเริ่มง้างท่าพุ่งโจมตี
    private Transform playerTransform;
    private Rigidbody2D rb;

    [Header("Boss Dash Attack & Telegraph")]
    public int dashDamage = 2;
    public float telegraphDuration = 0.7f; // เวลาง้างตัวโชว์เครื่องหมาย ! ก่อนพุ่ง
    public float dashSpeed = 12f;
    public float dashDuration = 0.35f;
    public float attackCooldown = 2.5f;
    private float lastAttackTime;
    private bool isAttacking = false;

    [Header("Visual & Hit Feel")]
    private SpriteRenderer spriteRenderer;
    public Color damageColor = Color.white;
    public Color phaseTwoColor = new Color(1f, 0.45f, 0.45f, 1f);
    private Color baseColor;
    public GameObject hitVfxPrefab;

    [Header("Hit Cooldown")]
    private float lastHitTime;
    public float hitCooldown = 0.25f;

    void Start()
    {
        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();

        if (spriteRenderer != null)
        {
            baseColor = spriteRenderer.color;
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
        else
        {
            GameObject namedPlayer = GameObject.Find("Player");
            if (namedPlayer != null)
            {
                playerTransform = namedPlayer.transform;
            }
        }
    }

    void Update()
    {
        if (playerTransform == null || isAttacking) return;

        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        // หากอยู่ในระยะโจมตีและหมดคูลดาวน์ ให้เริ่มท่าง้างพุ่งชน
        if (distanceToPlayer <= attackRange && Time.time - lastAttackTime >= attackCooldown)
        {
            StartCoroutine(TelegraphAndDashAttack());
        }
        // หากอยู่ในระยะมองเห็น ให้เดินเข้าหาผู้เล่น
        else if (distanceToPlayer <= detectionRange)
        {
            MoveTowardsPlayer();
        }
        else
        {
            if (rb != null)
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }
        }
    }

    void MoveTowardsPlayer()
    {
        if (rb == null || playerTransform == null) return;

        float direction = playerTransform.position.x > transform.position.x ? 1f : -1f;
        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);

        // รักษาสเกลขนาดใหญ่ของบอสไว้ตอนหันซ้าย-ขวา
        float scaleX = Mathf.Abs(transform.localScale.x);
        transform.localScale = new Vector3(direction > 0 ? scaleX : -scaleX, transform.localScale.y, transform.localScale.z);
    }

    // --- ท่าไม้ตายบอส: ง้างตัวโชว์เครื่องหมาย ! แล้วพุ่งฟัน ---
    IEnumerator TelegraphAndDashAttack()
    {
        isAttacking = true;
        lastAttackTime = Time.time;

        // 1. หยุดนิ่งและหันหน้าล็อกเป้าไปทางผู้เล่น
        if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        float dashDir = playerTransform.position.x > transform.position.x ? 1f : -1f;
        float scaleX = Mathf.Abs(transform.localScale.x);
        transform.localScale = new Vector3(dashDir > 0 ? scaleX : -scaleX, transform.localScale.y, transform.localScale.z);

        // 2. สร้างเครื่องหมายตกใจ "!" สีแดงบนหัวบอสเพื่อเตือนผู้เล่น
        GameObject warningObj = CreateWarningSign();
        if (spriteRenderer != null) spriteRenderer.color = Color.yellow; // กะพริบสีเหลืองเตือน

        yield return new WaitForSeconds(telegraphDuration);

        // ลบเครื่องหมายเตือนและคืนสีตัวละคร
        if (warningObj != null) Destroy(warningObj);
        if (spriteRenderer != null) spriteRenderer.color = baseColor;

        // 3. พุ่งตัวเข้าโจมตีด้วยความเร็วสูง
        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlaySFX(AudioManager.instance.dashClip);
        }

        if (CameraFollow.instance != null)
        {
            CameraFollow.instance.Shake(0.12f, 0.15f);
        }

        float timer = 0f;
        while (timer < dashDuration)
        {
            if (rb != null)
            {
                rb.linearVelocity = new Vector2(dashDir * dashSpeed, rb.linearVelocity.y);
            }
            timer += Time.deltaTime;
            yield return null;
        }

        // 4. เบรกหยุดหลังพุ่งจบ และพักเหนื่อยสั้นๆ เปิดช่องให้ผู้เล่นสวนกลับ
        if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        yield return new WaitForSeconds(0.5f);

        isAttacking = false;
    }

    GameObject CreateWarningSign()
    {
        GameObject warn = new GameObject("BossWarning");
        warn.transform.SetParent(transform);
        warn.transform.localPosition = new Vector3(0f, 1.4f, 0f);
        // ป้องกันตัวอักษรกลับด้านตามหน้าบอส
        warn.transform.localScale = new Vector3(transform.localScale.x >= 0 ? 1f : -1f, 1f, 1f);

        TextMesh tm = warn.AddComponent<TextMesh>();
        tm.text = "!";
        tm.fontSize = 80;
        tm.characterSize = 0.1f;
        tm.fontStyle = FontStyle.Bold;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = Color.red;

        MeshRenderer mr = warn.GetComponent<MeshRenderer>();
        if (mr != null) mr.sortingOrder = 105;

        return warn;
    }

    // --- ตรวจจับการชนเพื่อทำดาเมจใส่ Player ---
    private void OnCollisionStay2D(Collision2D collision)
    {
        CheckAndDamagePlayer(collision.gameObject);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        // เช็กกรณีโดนดาบของผู้เล่นฟัน
        if (collision.gameObject.name == "AttackPoint" || collision.CompareTag("PlayerAttack"))
        {
            if (Time.time - lastHitTime >= hitCooldown)
            {
                lastHitTime = Time.time;

                PlayerHitbox playerAttack = collision.GetComponent<PlayerHitbox>();
                if (playerAttack == null)
                {
                    playerAttack = collision.GetComponentInParent<PlayerHitbox>();
                }

                int damageToTake = playerAttack != null ? playerAttack.currentAttackDamage : 1;
                TakeDamage(damageToTake);
            }
            return;
        }

        CheckAndDamagePlayer(collision.gameObject);
    }

    void CheckAndDamagePlayer(GameObject targetObj)
    {
        PlayerHealth playerHealth = targetObj.GetComponent<PlayerHealth>();
        if (playerHealth == null)
        {
            playerHealth = targetObj.GetComponentInParent<PlayerHealth>();
        }

        if (playerHealth != null)
        {
            // บอสทำดาเมจแรงเป็นพิเศษตอนพุ่งชน
            int dmg = isAttacking ? dashDamage : 1;
            playerHealth.TakeDamage(dmg);
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log("<color=orange>BOSS โดนดาเมจ: " + damage + " | เลือดบอสคงเหลือ: " + currentHealth + "</color>");

        // เด้งตัวเลขดาเมจเหนือหัวบอส
        DamagePopup.Create(transform.position + Vector3.up * 0.5f, damage, damage >= 3);

        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlaySFX(AudioManager.instance.hitImpactClip);
        }

        if (CameraFollow.instance != null)
        {
            CameraFollow.instance.Shake(0.08f, 0.12f);
        }

        if (hitVfxPrefab != null)
        {
            GameObject vfx = Instantiate(hitVfxPrefab, transform.position, Quaternion.identity);
            Destroy(vfx, 0.5f);
        }

        // เช็กเข้าสู่ Phase 2 เมื่อเลือดบอสต่ำกว่าครึ่งหลอด
        if (!isPhaseTwo && currentHealth <= maxHealth / 2)
        {
            EnterPhaseTwo();
        }

        if (spriteRenderer != null && !isAttacking)
        {
            StartCoroutine(FlashHit());
        }

        if (currentHealth <= 0)
        {
            Time.timeScale = 1f;
            Die();
        }
    }

    void EnterPhaseTwo()
    {
        isPhaseTwo = true;
        baseColor = phaseTwoColor;
        if (spriteRenderer != null) spriteRenderer.color = baseColor;

        moveSpeed *= 1.4f;
        dashSpeed *= 1.25f;
        attackCooldown *= 0.65f;
        telegraphDuration = 0.5f;

        if (CameraFollow.instance != null)
        {
            CameraFollow.instance.Shake(0.3f, 0.25f);
        }
        Debug.Log("<color=red>★ BOSS ENTERED PHASE 2! บอสเข้าสู่โหมดคลั่ง!</color>");
    }

    IEnumerator FlashHit()
    {
        spriteRenderer.color = damageColor;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = baseColor;
    }

    void Die()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlaySFX(AudioManager.instance.enemyDieClip);
        }

        if (CameraFollow.instance != null)
        {
            CameraFollow.instance.Shake(0.4f, 0.3f);
        }

        Debug.Log("<color=yellow>★ VICTORY! กำจัดบอสสำเร็จ!</color>");
        Destroy(gameObject);
    }
}