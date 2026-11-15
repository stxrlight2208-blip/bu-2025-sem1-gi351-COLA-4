using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 5;
    public int currentHealth;
    private bool isDead = false;

    [Header("Invincibility (Dash / Hit Feedback)")]
    public bool isInvincible = false; 
    public float iFrameDuration = 0.5f; 

    [Header("Visual Feedback")]
    private SpriteRenderer spriteRenderer;
    public Color damageColor = Color.red;
    private Color originalColor;

    [Header("Animation")]
    private Animator animator;

    void Start()
    {
        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead || isInvincible)
        {
            if (isInvincible)
            {
                Debug.Log("<color=cyan>★ DODGE / INVINCIBLE! หลบการโจมตีได้!</color>");
            }
            return;
        }

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

    IEnumerator FlashDamage()
    {
        spriteRenderer.color = damageColor;
        yield return new WaitForSeconds(0.15f);
        spriteRenderer.color = originalColor;
    }

    IEnumerator TemporaryInvincibility(float duration)
    {
        isInvincible = true;
        yield return new WaitForSeconds(duration);
        if (!isDead)
        {
            isInvincible = false;
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("Player ตายแล้ว!");

        // 1. สั่งเล่นอนิเมชันตาย
        if (animator != null)
        {
            animator.SetTrigger("Death");
        }

        // 2. ปิดการควบคุม
        PlayerController controller = GetComponent<PlayerController>();
        if (controller != null)
        {
            controller.enabled = false;
        }

        // 3. ปิด Hitbox และสคริปต์โจมตี
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

        // 4. ให้ Rigidbody ยังจำลองฟิสิกส์อยู่ (เพื่อให้แรงโน้มถ่วงดึงลงพื้น) แต่หยุดความเร็วพุ่ง
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            // ไม่ปิด rb.simulated เพื่อให้ตกลงพื้นได้
        }

        // ⚠️ หมายเหตุ: เราเอาคำสั่งปิด Collider ออกแล้ว เพื่อให้ตัวละครตกลงมาชนพื้นจริง ๆ 
        // (ถ้า Collider ถูกปิด ตัวละครจะค้างกลางอากาศตามที่เห็นครับ)
    }
}