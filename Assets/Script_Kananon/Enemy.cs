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
        }
        else
        {
            GameObject namedPlayer = GameObject.Find("Player");

            if (namedPlayer != null)
                playerTransform = namedPlayer.transform;
        }
    }

    void Update()
    {
        if (isDead || playerTransform == null)
            return;

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

        float distanceX = playerTransform.position.x - transform.position.x;

        // กำหนดระยะห่างขั้นต่ำที่จะให้ตัวละครทำการพลิกหน้า (Deadzone)
        float flipThreshold = 0.2f;
        float direction = 0f;

        if (Mathf.Abs(distanceX) > 0.05f)
        {
            direction = distanceX > 0f ? 1f : -1f;
        }

        // ขยับตัวเดินเข้าหา
        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
        SetMoving(true);

        // พลิกตัวเฉพาะเมื่อระยะห่างเกินค่า Threshold ที่กำหนด เพื่อกันอาการหัวสั่นเวลาประชิด
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
        PlayerHealth playerHealth = playerTransform.GetComponent<PlayerHealth>();
        if (playerHealth == null)
        {
            playerHealth = playerTransform.GetComponentInParent<PlayerHealth>();
        }

        if (playerHealth != null)
        {
            if (Time.time - lastAttackTime >= attackCooldown)
            {
                lastAttackTime = Time.time;
                PlayAttackAnimation();
                playerHealth.TakeDamage(enemyDamage);
            }
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

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (isDead) return;

        // เช็คว่าชนโดน PlayerHitbox หรือมีสคริปต์ PlayerHitbox ติดอยู่ไหม
        PlayerHitbox playerAttack = collision.GetComponent<PlayerHitbox>();
        if (playerAttack == null)
        {
            playerAttack = collision.GetComponentInParent<PlayerHitbox>();
        }

        // ถ้าเจอ PlayerHitbox และอยู่ในช่วงเวลาที่รับดาเมจได้
        if (playerAttack != null && playerAttack.attackCollider != null && playerAttack.attackCollider.enabled)
        {
            if (Time.time - lastHitTime >= hitCooldown)
            {
                lastHitTime = Time.time;

                int damageToTake = playerAttack.currentAttackDamage;

                // สั่งลดเลือดศัตรูตามปกติ
                TakeDamage(damageToTake);

                // ⭐ สั่งบวกแต้มสไตล์เข้า StyleRankManager ทันทีที่ฟันโดน!
                if (StyleRankManager.Instance != null)
                {
                    float stylePoints = damageToTake * 25f;
                    Debug.Log("🎯 ฟันโดน Enemy สำเร็จ! ส่งแต้มสไตล์: " + stylePoints);
                    StyleRankManager.Instance.AddStylePoints(stylePoints);
                }
            }
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;

        Debug.Log("Enemy โดนดาเมจ: " + damage +
                  " | เลือดคงเหลือ: " + currentHealth);

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

        Debug.Log("Enemy ตายแล้ว!");

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        Collider2D[] colliders =
            GetComponents<Collider2D>();

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