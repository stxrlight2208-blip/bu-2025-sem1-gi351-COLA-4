using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Stats")]
    public int maxHealth = 10;
    private int currentHealth;

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
        
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
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
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        // จะแสดงเฉพาะตอนที่ Dummy โดนฟันและเสียเลือดเท่านั้น
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