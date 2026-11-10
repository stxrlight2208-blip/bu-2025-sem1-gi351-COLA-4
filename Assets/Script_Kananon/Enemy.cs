
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
            MoveTowardsPlayer();
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

        float direction =
            playerTransform.position.x > transform.position.x ? 1f : -1f;

        rb.linearVelocity =
            new Vector2(direction * moveSpeed, rb.linearVelocity.y);

        SetMoving(true);

        if (direction > 0)
            transform.localScale = new Vector3(0.5f, 0.5f, 1f);
        else
            transform.localScale = new Vector3(-0.5f, 0.5f, 1f);
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

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (isDead) return;

        CheckAndDamagePlayer(collision.gameObject);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (isDead) return;

        if (collision.gameObject.name == "AttackPoint" ||
            collision.CompareTag("PlayerAttack"))
        {
            if (Time.time - lastHitTime >= hitCooldown)
            {
                lastHitTime = Time.time;

                PlayerHitbox playerAttack =
                    collision.GetComponent<PlayerHitbox>();

                if (playerAttack == null)
                {
                    playerAttack =
                        collision.GetComponentInParent<PlayerHitbox>();
                }

                int damageToTake = 1;

                if (playerAttack != null)
                    damageToTake = playerAttack.currentAttackDamage;

                TakeDamage(damageToTake);
            }

            return;
        }

        CheckAndDamagePlayer(collision.gameObject);
    }

    void CheckAndDamagePlayer(GameObject targetObj)
    {
        if (isDead) return;

        PlayerHealth playerHealth =
            targetObj.GetComponent<PlayerHealth>();

        if (playerHealth == null)
        {
            playerHealth =
                targetObj.GetComponentInParent<PlayerHealth>();
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
