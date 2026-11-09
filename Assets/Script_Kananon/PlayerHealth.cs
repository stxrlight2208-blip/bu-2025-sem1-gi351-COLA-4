using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 5;
    public int currentHealth;

    [Header("Invincibility (Dash / Hit Feedback)")]
    public bool isInvincible = false; // จะถูกสั่งเปิดจากสคริปต์ Dash หรือตอนโดนตี
    public float iFrameDuration = 0.5f; // ระยะเวลาอมตะชั่วคราวหลังโดนตีปกติ

    [Header("Visual Feedback")]
    private SpriteRenderer spriteRenderer;
    public Color damageColor = Color.red;
    private Color originalColor;

    void Start()
    {
        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    public void TakeDamage(int damage)
    {
        // ถ้าอยู่ในสถานะอมตะ (เช่น กำลัง Dash) จะไม่ได้รับดาเมจใดๆ
        if (isInvincible)
        {
            Debug.Log("<color=cyan>★ DODGE / INVINCIBLE! หลบการโจมตีได้!</color>");
            return;
        }

        currentHealth -= damage;
        Debug.Log("<color=red>Player โดนโจมตี! เลือดเหลือ: " + currentHealth + "</color>");

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
            // อมตะชั่วคราวหลังโดนตี (i-frame กันโดนตีซ้ำรัวๆ)
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
        isInvincible = false;
    }

    void Die()
    {
        Debug.Log("Player ตายแล้ว!");
        // ใส่โค้ด Game Over หรือ Reload Scene ได้ตรงนี้
    }
}