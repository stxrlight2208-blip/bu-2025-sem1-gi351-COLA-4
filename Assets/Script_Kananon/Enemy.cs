using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Stats")]
    public int maxHealth = 12;
    private int currentHealth;

    [Header("Finisher QTE Settings (ต่อคอมโบดีถึงจะขึ้น)")]
    public int requiredComboHits = 3;
    [Range(0.1f, 0.9f)]
    public float finisherHealthPercent = 0.35f;
    public float finisherInteractRange = 4.5f;
    private int currentComboReceived = 0;
    private float lastComboHitTime = 0f;
    private bool isFinishable = false;
    private GameObject finishSignObj;

    [Header("Movement & AI")]
    public float moveSpeed = 2.5f;
    public float detectionRange = 6f;
    public float attackRange = 1.2f; // ระยะที่จะหยุดเดินแล้วเตรียมฟัน
    private Transform playerTransform;
    private PlayerHealth playerHealth;
    private Rigidbody2D rb;
    private Vector3 baseScale; // จำขนาดตัวละครเดิมที่ตั้งไว้ใน Inspector

    [Header("Attack Player Settings")]
    public int enemyDamage = 1;
    public float attackCooldown = 1.5f;
    private float lastAttackTime;

    [Header("Visual & Hit Feel Effects")]
    private SpriteRenderer spriteRenderer;
    public Color damageColor = Color.red;
    private Color originalColor;
    public float flashDuration = 0.15f;
    public GameObject hitVfxPrefab;
    public float knockbackForce = 2f;
    public float knockbackDuration = 0.08f;
    public float hitStopDuration = 0.04f;

    [Header("Hit Cooldown")]
    private float lastHitTime;
    public float hitCooldown = 0.3f;
    private bool isKnockedBack = false;

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

        // จำขนาด Scale เริ่มต้นไว้ไม่ให้หดกลับเป็น 1 หรือ 0.5
        baseScale = new Vector3(
            Mathf.Abs(transform.localScale.x) > 0.01f ? Mathf.Abs(transform.localScale.x) : 1f,
            Mathf.Abs(transform.localScale.y) > 0.01f ? transform.localScale.y : 1f,
            transform.localScale.z
        );

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) playerObj = GameObject.Find("Player");

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
            if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            SetMoving(false);
            return;
        }

        // หยุดนิ่งระหว่างทำ Finisher QTE
        if (FinisherManager.isQTEActive)
        {
            if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            SetMoving(false);
            return;
        }

        // สถานะติดสตันรอปิดฉาก [E] FINISH
        if (isFinishable)
        {
            if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            SetMoving(false);

            if (finishSignObj != null)
            {
                float signFlip = transform.localScale.x >= 0 ? 1f : -1f;
                finishSignObj.transform.localScale = new Vector3(signFlip / baseScale.x, 1f / baseScale.y, 1f);
            }

            float distX = Mathf.Abs(transform.position.x - playerTransform.position.x);
            if (distX <= finisherInteractRange && Input.GetKeyDown(KeyCode.E))
            {
                if (FinisherManager.instance == null)
                {
                    GameObject fmObj = new GameObject("FinisherManager_Auto");
                    fmObj.AddComponent<FinisherManager>();
                }

                if (finishSignObj != null) Destroy(finishSignObj);
                FinisherManager.instance.StartFinisherQTE(
                    playerTransform.gameObject,
                    gameObject,
                    isBoss: false,
                    onSuccess: () => Die(),
                    onFail: () => RecoverFromStun()
                );
            }
            return;
        }

        if (isKnockedBack)
        {
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
                if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                SetMoving(false);
                TryAttackPlayer();
            }
        }
        else
        {
            if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
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
            transform.localScale = new Vector3(direction > 0 ? baseScale.x : -baseScale.x, baseScale.y, baseScale.z);
        }
    }

    void TryAttackPlayer()
    {
        if (isFinishable || FinisherManager.isQTEActive) return;
        if (playerHealth == null || playerHealth.currentHealth <= 0) return;

        if (Time.time - lastAttackTime >= attackCooldown)
        {
            lastAttackTime = Time.time;
            TriggerAnim("Attack");
            StartCoroutine(CheckAttackHitWithDelay());
        }
    }

    IEnumerator CheckAttackHitWithDelay()
    {
        yield return new WaitForSeconds(0.3f);

        if (playerTransform != null && !isDead && !isFinishable && !FinisherManager.isQTEActive)
        {
            float hitCheckDistance = 1.8f;
            if (Vector2.Distance(transform.position, playerTransform.position) <= hitCheckDistance)
            {
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(enemyDamage);
                }
            }
        }
    }

    // เช็กตอนที่ผู้เล่นใช้ดาบฟันโดนตัวมอนสเตอร์ (รองรับทั้งรูปแบบของเพื่อนและของ Tan)
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (isDead || FinisherManager.isQTEActive) return;

        PlayerHitbox playerAttack = collision.GetComponent<PlayerHitbox>();
        if (playerAttack == null) playerAttack = collision.GetComponentInParent<PlayerHitbox>();

        bool hitByPlayerHitbox = (playerAttack != null && playerAttack.attackCollider != null && playerAttack.attackCollider.enabled);
        bool hitByTagOrName = (collision.gameObject.name == "AttackPoint" || collision.CompareTag("PlayerAttack"));

        if (hitByPlayerHitbox || hitByTagOrName)
        {
            if (Time.time - lastHitTime >= hitCooldown)
            {
                lastHitTime = Time.time;
                int damageToTake = playerAttack != null ? playerAttack.currentAttackDamage : 1;
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

        if (Time.time - lastComboHitTime <= 1.5f)
            currentComboReceived++;
        else
            currentComboReceived = 1;

        lastComboHitTime = Time.time;
        currentHealth -= damage;

        // เอฟเฟกต์ตัวเลขเด้ง + เสียง + จอสั่น
        DamagePopup.Create(transform.position, damage, damage >= 3);

        if (AudioManager.instance != null)
            AudioManager.instance.PlaySFX(AudioManager.instance.hitImpactClip);

        if (CameraFollow.instance != null)
            CameraFollow.instance.Shake(0.1f, 0.15f);

        if (hitVfxPrefab != null)
        {
            GameObject vfx = Instantiate(hitVfxPrefab, transform.position, Quaternion.identity);
            Destroy(vfx, 0.5f);
        }

        // เช็กเงื่อนไขเข้าสถานะ Finisher QTE
        if (!isFinishable && currentHealth > 0 && currentHealth <= maxHealth * finisherHealthPercent && currentComboReceived >= requiredComboHits)
        {
            EnterFinishableState();
            return;
        }

        if (currentHealth <= 0)
        {
            Time.timeScale = 1f;
            Die();
            return;
        }

        if (rb != null && playerTransform != null && !isFinishable)
        {
            StopCoroutine(nameof(ApplyKnockback));
            StartCoroutine(ApplyKnockback());
        }

        if (spriteRenderer != null && !isFinishable)
        {
            StopCoroutine(nameof(FlashRed));
            StartCoroutine(FlashRed());
        }

        StartCoroutine(DoHitStop());
    }

    void EnterFinishableState()
    {
        isFinishable = true;
        if (rb != null) rb.linearVelocity = Vector2.zero;
        SetMoving(false);

        if (spriteRenderer != null) spriteRenderer.color = new Color(1f, 0.3f, 0.9f, 1f);

        finishSignObj = new GameObject("FinishHimPrompt");
        finishSignObj.transform.SetParent(transform);
        finishSignObj.transform.localPosition = new Vector3(0f, 1.3f / baseScale.y, 0f);
        finishSignObj.transform.localScale = new Vector3(1f / baseScale.x, 1f / baseScale.y, 1f);

        TextMesh tm = finishSignObj.AddComponent<TextMesh>();
        tm.text = "[E] FINISH";
        tm.fontSize = 55;
        tm.characterSize = 0.08f;
        tm.fontStyle = FontStyle.Bold;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = new Color(1f, 0.85f, 0.1f, 1f);

        MeshRenderer mr = finishSignObj.GetComponent<MeshRenderer>();
        if (mr != null) mr.sortingOrder = 110;

        StartCoroutine(StunTimeout());
    }

    IEnumerator StunTimeout()
    {
        yield return new WaitForSeconds(4.0f);
        if (isFinishable && !FinisherManager.isQTEActive)
        {
            RecoverFromStun();
        }
    }

    void RecoverFromStun()
    {
        isFinishable = false;
        currentComboReceived = 0;
        if (finishSignObj != null) Destroy(finishSignObj);
        if (spriteRenderer != null) spriteRenderer.color = originalColor;
    }

    IEnumerator ApplyKnockback()
    {
        isKnockedBack = true;
        float knockbackDir = transform.position.x > playerTransform.position.x ? 1f : -1f;
        rb.linearVelocity = new Vector2(knockbackDir * knockbackForce, 0f);

        yield return new WaitForSeconds(knockbackDuration);

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        isKnockedBack = false;
    }

    IEnumerator DoHitStop()
    {
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(hitStopDuration);
        Time.timeScale = 1f;
    }

    IEnumerator FlashRed()
    {
        spriteRenderer.color = damageColor;
        yield return new WaitForSeconds(flashDuration);
        if (!isDead && !isFinishable && spriteRenderer != null) spriteRenderer.color = originalColor;
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        if (finishSignObj != null) Destroy(finishSignObj);

        if (AudioManager.instance != null)
            AudioManager.instance.PlaySFX(AudioManager.instance.enemyDieClip);

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        foreach (Collider2D col in GetComponents<Collider2D>())
            col.enabled = false;

        if (animator != null && animator.runtimeAnimatorController != null && HasParameter("Death"))
        {
            animator.SetTrigger("Death");
            StartCoroutine(DestroyAfterDeath());
        }
        else
        {
            Destroy(gameObject);
        }
    }

    IEnumerator DestroyAfterDeath()
    {
        yield return new WaitForSeconds(deathAnimationDuration);
        Destroy(gameObject);
    }

    // --- ฟังก์ชันช่วยสั่ง Animator อย่างปลอดภัย ---
    void SetMoving(bool moving)
    {
        if (animator != null && animator.runtimeAnimatorController != null && HasParameter("IsMoving"))
            animator.SetBool("IsMoving", moving);
    }

    void TriggerAnim(string paramName)
    {
        if (animator != null && animator.runtimeAnimatorController != null && HasParameter(paramName))
            animator.SetTrigger(paramName);
    }

    bool HasParameter(string paramName)
    {
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName) return true;
        }
        return false;
    }
}