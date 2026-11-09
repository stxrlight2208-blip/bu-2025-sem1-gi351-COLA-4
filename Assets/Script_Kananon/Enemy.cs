using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Stats")]
    public int maxHealth = 10;
    private int currentHealth;

    [Header("Movement & AI (Follow Player)")]
    public float moveSpeed = 2.5f;       // ความเร็วในการเดินตามผู้เล่น
    public float detectionRange = 6f;    // ระยะที่ศัตรูเริ่มมองเห็นและเดินตามผู้เล่น
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

    void Start()
    {
        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        // ค้นหาตำแหน่งตัว Player อัตโนมัติจาก Tag หรือชื่อ
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
        if (playerTransform == null) return;

        // คำนวณระยะห่างระหว่าง Enemy กับ Player
        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        // ถ้าผู้เล่นเข้ามาอยู่ในระยะตรวจจับ ให้เดินตาม
        if (distanceToPlayer <= detectionRange)
        {
            MoveTowardsPlayer();
        }
        else
        {
            // ถ้าอยู่นอกระยะ ให้หยุดนิ่งอยู่กับที่
            if (rb != null)
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }
        }
    }

    void MoveTowardsPlayer()
    {
        if (playerTransform == null || rb == null) return;

        // เช็คทิศทางว่า Player อยู่ซ้ายหรือขวา
        float direction = playerTransform.position.x > transform.position.x ? 1f : -1f;

        // เคลื่อนที่เข้าหาผู้เล่น (รักษาความเร็วแกน Y ไว้สำหรับแรงโน้มถ่วง)
        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);

        // พลิกหน้า Sprite หันตามทิศทางที่เดิน
        if (direction > 0)
        {
            transform.localScale = new Vector3(1f, 1f, 1f);
        }
        else if (direction < 0)
        {
            transform.localScale = new Vector3(-1f, 1f, 1f);
        }
    }

    // --- ระบบตรวจสอบการชนเพื่อทำดาเมจใส่ Player ---
    private void OnCollisionStay2D(Collision2D collision)
    {
        CheckAndDamagePlayer(collision.gameObject);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        // เช็คกรณีโดนดาบของผู้เล่นฟัน
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

                int damageToTake = 1;
                if (playerAttack != null)
                {
                    damageToTake = playerAttack.currentAttackDamage;
                }

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
            if (Time.time - lastAttackTime >= attackCooldown)
            {
                lastAttackTime = Time.time;
                playerHealth.TakeDamage(enemyDamage);
            }
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log("Dummy โดนดาเมจ: " + damage + " | เลือดคงเหลือ: " + currentHealth);

        if (spriteRenderer != null)
        {
            StopAllCoroutines();
            StartCoroutine(FlashRed());
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    IEnumerator FlashRed()
    {
        spriteRenderer.color = damageColor;
        yield return new WaitForSeconds(flashDuration);
        spriteRenderer.color = originalColor;
    }

    void Die()
    {
        Debug.Log("Dummy ตายแล้ว!");
        Destroy(gameObject);
    }
}