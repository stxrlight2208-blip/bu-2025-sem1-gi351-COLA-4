using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 5;
    public int currentHealth;
    private bool isDead = false;

    [Header("Invincibility & Dodge")]
    public bool isInvincible = false; 
    public bool isHitInvincible = false; 
    public float iFrameDuration = 0.8f; 

    [Header("Visual Feedback")]
    private SpriteRenderer spriteRenderer;
    public Color damageColor = Color.red;
    public Color dodgeColor = new Color(0.3f, 0.9f, 1f, 1f); // ⭐ สีฟ้าสว่างสดใสสำหรับ Perfect Dodge
    private Color originalColor;

    [Header("Animation")]
    private Animator animator;
    private PlayerController playerController;

    void Start()
    {
        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        playerController = GetComponent<PlayerController>();

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead || isHitInvincible)
        {
            return;
        }

        bool isDashingNow = (playerController != null && playerController.isDashing);

        if (isInvincible || isDashingNow)
        {
            Debug.Log("<color=cyan>★ Perfect Dodge! หลบการโจมตีได้อย่างสมบูรณ์แบบ!</color>");
            
            // ⭐ สั่งให้ตัวละคร/สไปรต์กระพริบเป็นสีฟ้าทันทีที่เกิด Perfect Dodge
            if (spriteRenderer != null)
            {
                StopAllCoroutines();
                StartCoroutine(FlashPerfectDodgeVisual());
            }

            if (StyleRankManager.Instance != null)
            {
                StyleRankManager.Instance.AddStylePoints(50f); 
            }
            return; 
        }

        // กรณีโดนตีปกติ -> เลือดลดและกระพริบแดง
        currentHealth -= damage;
        
        if (currentHealth < 0)
        {
            currentHealth = 0;
        }

        Debug.Log("<color=red>Player โดนโจมตี! เลือดเหลือ: " + currentHealth + "</color>");

        if (StyleRankManager.Instance != null)
        {
            StyleRankManager.Instance.OnPlayerHit();
        }

        if (spriteRenderer != null)
        {
            StopAllCoroutines();
            StartCoroutine(FlashDamage());
        }

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(TemporaryInvincibility(iFrameDuration));
        }
    }

    // ⭐ โครูทีนกะพริบสีฟ้าแบบรัวๆ หรือกะพริบเน้นๆ ตอนติด Perfect Dodge
    IEnumerator FlashPerfectDodgeVisual()
    {
        if (spriteRenderer != null)
        {
            // กระพริบสลับสีฟ้ากับสีเดิม 2 รอบเพื่อให้เห็นชัดเจน
            for (int i = 0; i < 2; i++)
            {
                spriteRenderer.color = dodgeColor;
                yield return new WaitForSeconds(0.08f);
                spriteRenderer.color = originalColor;
                yield return new WaitForSeconds(0.08f);
            }
            spriteRenderer.color = originalColor;
        }
    }

    IEnumerator FlashDamage()
    {
        spriteRenderer.color = damageColor;
        yield return new WaitForSeconds(0.15f);
        spriteRenderer.color = originalColor;
    }

    IEnumerator TemporaryInvincibility(float duration)
    {
        isHitInvincible = true; 

        float elapsed = 0f;
        float flashInterval = 0.1f; 

        while (elapsed < duration)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = (spriteRenderer.color == originalColor) ? damageColor : originalColor;
            }

            yield return new WaitForSeconds(flashInterval);
            elapsed += flashInterval;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }

        if (!isDead)
        {
            isHitInvincible = false; 
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("Player ตายแล้ว!");

        if (animator != null)
        {
            animator.SetTrigger("Death");
        }

        PlayerController controller = GetComponent<PlayerController>();
        if (controller != null)
        {
            controller.enabled = false;
        }

        PlayerHitbox hitbox = GetComponentInChildren<PlayerHitbox>();
        if (hitbox != null)
        {
            hitbox.DisableHitbox(); 
            hitbox.enabled = false; 
        }

        PlayerAttack attackScript = GetComponent<PlayerAttack>();
        if (attackScript != null)
        {
            attackScript.enabled = false;
        }

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
    }
}