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
    public float attackRange = 1.2f; // ระยะที่จะหยุดเดินแล้วโจมตี Player
    private Transform playerTransform;
    private PlayerHealth playerHealth; // ตัวแปรเช็คเลือดผู้เล่น
    private Rigidbody2D rb;

    [Header("Attack Player Settings")]
    public int enemyDamage = 1;
    public float attackCooldown = 1.0f;
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
        else
        {
            GameObject namedPlayer = GameObject.Find("Player");

            if (namedPlayer != null)
            {
                playerTransform = namedPlayer.transform;
                playerHealth = namedPlayer.GetComponent<PlayerHealth>();
            }
        }
    }

    void Update()
    {
        // ⭐ ล็อกตำแหน่งแกน Z ให้เสถียร ไม่ให้เหลื่อมมิติ
        transform.position = new Vector3(transform.position.x, transform.position.y, 0f);

        // ถ้าศัตรูตาย หรือ Player ตายแล้ว (เลือด <= 0) ให้หยุดเดินทันที
        if (isDead || playerTransform == null || (playerHealth != null && playerHealth.currentHealth <= 0))
        {
            if (rb != null)
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            SetMoving(false);
            return;
        }

        float distanceToPlayer =
            Vector2.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer <= detectionRange)
        {
            if (distanceToPlayer > attackRange)
            {
                // ยังอยู่ไกล ให้เดินเข้าหา Player
                MoveTowardsPlayer();
            }
            else
            {
                // ถึงระยะประชิดแล้ว หยุดเดินแล้วเตรียมโจมตี
                if (rb != null)
                    rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

                SetMoving(false);

                // เช็คเวลาและโจมตี Player
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
        if (playerTransform == null || rb == null || isDead)
            return;

        if (playerHealth != null && playerHealth.currentHealth <= 0)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            SetMoving(false);
            return;
        }

        float distanceX = playerTransform.position.x - transform.position.x;
        float flipThreshold = 0.2f;
        float direction = 0f;

        if (Mathf.Abs(distanceX) > 0.05f)
        {
            direction = distanceX > 0f ? 1f : -1f;
        }

        // ขยับตัวเดินเข้าหา
        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
        SetMoving(true);

        if (Mathf.Abs(distanceX) > flipThreshold)
        {
            if (direction > 0)
                transform.localScale = new Vector3(0.5f, 0.5f, 1f);
            else
                transform.localScale = new Vector3(-0.5f, 0.5f, 1f);
        }
    }

    void TryAttackPlayer()
    {
        if (playerHealth == null || playerHealth.currentHealth <= 0)
            return;

        if (Time.time - lastAttackTime >= attackCooldown)
        {
            lastAttackTime = Time.time;
            PlayAttackAnimation();
            playerHealth.TakeDamage(enemyDamage);
        }
    }

    void SetMoving(bool moving)
    {
        if (animator != null)
            animator.SetBool("IsMoving", moving);
    }

    void PlayAttackAnimation()
    {
        if (animator != null)
            animator.SetTrigger("Attack");
    }

    // ⭐ เพิ่มระบบตรวจจับการชนกับ Enemy ตัวอื่น เพื่อผลักออกเบาๆ ไม่ให้ทับซ้อนติดกัน
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (isDead) return;

        // เช็คว่าชนกับ Enemy ตัวอื่นไหม
        if (collision.gameObject.CompareTag(gameObject.tag) || collision.gameObject.GetComponent<Enemy>() != null)
        {
            // คำนวณทิศทางที่ชน แล้วออกแรงผลักออกด้านข้าง (แกน X) เล็กน้อย
            Vector2 pushDir = transform.position - collision.transform.position;
            pushDir.y = 0f; // ไม่ผลักขึ้นฟ้า ผลักเฉพาะแนวราบ
            pushDir.Normalize();

            if (rb != null)
            {
                // เพิ่มแรงผลักเบาๆ ออกจากกัน
                rb.AddForce(pushDir * 2f, ForceMode2D.Impulse);
            }
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (isDead) return;

        PlayerHitbox playerAttack = collision.GetComponent<PlayerHitbox>();
        if (playerAttack == null)
        {
            playerAttack = collision.GetComponentInParent<PlayerHitbox>();
        }

        if (playerAttack != null && playerAttack.attackCollider != null && playerAttack.attackCollider.enabled)
        {
            if (Time.time - lastHitTime >= hitCooldown)
            {
                lastHitTime = Time.time;

                int damageToTake = playerAttack.currentAttackDamage;

                TakeDamage(damageToTake);

                if (StyleRankManager.Instance != null)
                {
                    float stylePoints = damageToTake * 25f;
                    StyleRankManager.Instance.AddStylePoints(stylePoints);
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

        if (!isDead && spriteRenderer != null)
            spriteRenderer.color = originalColor;
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

        Collider2D[] colliders = GetComponents<Collider2D>();

        foreach (Collider2D col in colliders)
            col.enabled = false;

        if (animator != null)
            animator.SetTrigger("Death");

        StartCoroutine(DestroyAfterDeath());
    }

    IEnumerator DestroyAfterDeath()
    {
        yield return new WaitForSeconds(deathAnimationDuration);

        Destroy(gameObject);
    }
}