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

    [Header("Dodge VFX Prefab")]
    public GameObject perfectDodgeVfxPrefab; // ⭐ ลาก Prefab VFX ตอน Perfect Dodge มาใส่ตรงนี้
    public float dodgeVfxLifetime = 0.6f;
    public string vfxSortingLayer = "New Layer 3";
    public int vfxSortingOrder = 135;

    [Header("Visual Feedback")]
    private SpriteRenderer spriteRenderer;
    public Color damageColor = Color.red;
    public Color dodgeColor = new Color(0.3f, 0.9f, 1f, 1f);
    private Color originalColor;

    [Header("Animation")]
    private Animator animator;
    private PlayerController playerController;
    private PlayerParry playerParry;

    void Start()
    {
        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        playerController = GetComponent<PlayerController>();
        playerParry = GetComponent<PlayerParry>();

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

        if (playerParry != null && playerParry.isParryWindowActive)
        {
            Debug.Log("<color=yellow>◆ PERFECT PARRY! สวนกลับการโจมตีสำเร็จ!</color>");
            playerParry.OnPerfectParrySuccess();
            return;
        }

        if (playerParry != null && playerParry.isHoldingBlock)
        {
            Debug.Log("<color=white>■ BLOCKED! กันการโจมตีไว้ได้</color>");
            StartCoroutine(TemporaryInvincibility(0.35f));
            return;
        }

        bool isDashingNow = (playerController != null && playerController.isDashing);

        if (isInvincible || isDashingNow)
        {
            Debug.Log("<color=cyan>★ Perfect Dodge! หลบการโจมตีได้อย่างสมบูรณ์แบบ!</color>");

            SpawnPerfectDodgeVfx();

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

    void SpawnPerfectDodgeVfx()
    {
        if (perfectDodgeVfxPrefab == null) return;

        GameObject vfx = Instantiate(perfectDodgeVfxPrefab, transform.position + new Vector3(0f, 0.5f, 0f), Quaternion.identity);
        float facingDir = transform.localScale.x >= 0 ? 1f : -1f;
        Vector3 s = vfx.transform.localScale;
        vfx.transform.localScale = new Vector3(Mathf.Abs(s.x) * facingDir, s.y, s.z);

        SpriteRenderer[] srs = vfx.GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer sr in srs)
        {
            sr.sortingLayerName = vfxSortingLayer;
            sr.sortingOrder = vfxSortingOrder;
        }

        Destroy(vfx, dodgeVfxLifetime);
    }

    IEnumerator FlashPerfectDodgeVisual()
    {
        if (spriteRenderer != null)
        {
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

        if (playerParry != null)
        {
            playerParry.CancelBlock();
            playerParry.enabled = false;
        }

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