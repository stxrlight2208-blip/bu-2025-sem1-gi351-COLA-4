using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Stats")]
    public int maxHealth = 10;
    private int currentHealth;

    [Header("Movement & AI")]
    public float moveSpeed = 2.5f;
    public float detectionRange = 6f;
    public float attackRange = 1.2f; // ระยะที่จะหยุดเดินแล้วเตรียมฟัน
    private Transform playerTransform;
    private PlayerHealth playerHealth; 
    private Rigidbody2D rb;

    [Header("Attack Player Settings")]
    public int enemyDamage = 1;
    public float attackCooldown = 1.5f;
    private float lastAttackTime;

    [Header("Visual Effects")]
    private SpriteRenderer spriteRenderer;
    public Color damageColor = Color.red;
    private Color originalColor;
    public float flashDuration = 0.15f;

    [Header("Hit Cooldown")]
    private float lastHitTime;
    public float hitCooldown = 0.3f;

    [Header("Animation")]
    private Animator animator;
    public float deathAnimationDuration = 1f;

    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            playerHealth = playerObj.GetComponent<PlayerHealth>();
        }
    }

    void Update()
    {
        transform.position = new Vector3(transform.position.x, transform.position.y, 0f);

        if (isDead || playerTransform == null || (playerHealth != null && playerHealth.currentHealth <= 0))
        {
            if (rb != null)
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            SetMoving(false);
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer <= detectionRange)
        {
            if (distanceToPlayer > attackRange)
            {
                MoveTowardsPlayer();
            }
            else
            {
                if (rb != null)
                    rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

                SetMoving(false);
                TryAttackPlayer();
            }
        }
        else
        {
            if (rb != null)
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

            SetMoving(false);
        }
    }

    void MoveTowardsPlayer()
    {
        if (playerTransform == null || rb == null || isDead) return;

        float distanceX = playerTransform.position.x - transform.position.x;
        float direction = Mathf.Abs(distanceX) > 0.05f ? (distanceX > 0f ? 1f : -1f) : 0f;

        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
        SetMoving(true);

        if (Mathf.Abs(distanceX) > 0.2f)
        {
            transform.localScale = new Vector3(direction > 0 ? 0.5f : -0.5f, 0.5f, 1f);
        }
    }

    void TryAttackPlayer()
    {
        if (playerHealth == null || playerHealth.currentHealth <= 0) return;

        if (Time.time - lastAttackTime >= attackCooldown)
        {
            lastAttackTime = Time.time;
            if (animator != null) animator.SetTrigger("Attack");

            // ⭐ ทำดาเมจเช็กระยะประชิดทันทีที่ออกท่าโจมตี (ตัดปัญหาเรื่องกล่องฮิตบ็อกซ์ไม่เปิด)
            StartCoroutine(CheckAttackHitWithDelay());
        }
    }

    // หน่วงเวลาเล็กน้อยให้ตรงกับจังหวะดาบฟันลงมาในแอนิเมชัน
    IEnumerator CheckAttackHitWithDelay()
    {
        yield return new WaitForSeconds(0.3f); // ปรับเวลาตรงนี้ให้ตรงกับจังหวะอนิเมชันฟันโดนตัว

        if (playerTransform != null && !isDead)
        {
            float hitCheckDistance = 1.8f; // ระยะที่ถือว่าฟันโดน
            if (Vector2.Distance(transform.position, playerTransform.position) <= hitCheckDistance)
            {
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(enemyDamage);
                }
            }
        }
    }

    void SetMoving(bool moving)
    {
        if (animator != null) animator.SetBool("IsMoving", moving);
    }

    // เช็กตอนที่ผู้เล่นใช้ดาบฟันโดนตัวมอนสเตอร์
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (isDead) return;

        PlayerHitbox playerAttack = collision.GetComponent<PlayerHitbox>();
        if (playerAttack == null) playerAttack = collision.GetComponentInParent<PlayerHitbox>();

        if (playerAttack != null && playerAttack.attackCollider != null && playerAttack.attackCollider.enabled)
        {
            if (Time.time - lastHitTime >= hitCooldown)
            {
                lastHitTime = Time.time;
                int damageToTake = playerAttack.currentAttackDamage;
                TakeDamage(damageToTake);

                if (StyleRankManager.Instance != null)
                {
                    StyleRankManager.Instance.AddStylePoints(damageToTake * 25f);
                }
            }
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;
        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        if (spriteRenderer != null)
        {
            StopAllCoroutines();
            StartCoroutine(FlashRed());
        }
    }

    IEnumerator FlashRed()
    {
        spriteRenderer.color = damageColor;
        yield return new WaitForSeconds(flashDuration);
        if (!isDead && spriteRenderer != null) spriteRenderer.color = originalColor;
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        foreach (Collider2D col in GetComponents<Collider2D>())
            col.enabled = false;

        if (animator != null) animator.SetTrigger("Death");
        StartCoroutine(DestroyAfterDeath());
    }

    IEnumerator DestroyAfterDeath()
    {
        yield return new WaitForSeconds(deathAnimationDuration);
        Destroy(gameObject);
    }
}