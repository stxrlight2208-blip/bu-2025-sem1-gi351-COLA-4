using System.Collections;
using UnityEngine;

public class FlyingSwoopEnemy : MonoBehaviour
{
    private enum FlyState { Hovering, Swooping, Returning }
    private FlyState currentState = FlyState.Hovering;

    [Header("Stats")]
    public int maxHealth = 10;
    private int currentHealth;
    private bool isDead = false;

    [Header("Finisher QTE Settings (ต่อคอมโบดีถึงจะขึ้น)")]
    public int requiredComboHits = 3;
    [Range(0.1f, 0.9f)]
    public float finisherHealthPercent = 0.35f;
    public float finisherInteractRange = 4.5f;
    private int currentComboReceived = 0;
    private float lastComboHitTime = 0f;
    private bool isFinishable = false;
    private GameObject finishSignObj;

    [Header("Hover & Movement")]
    public float hoverSpeed = 3f;
    public float hoverHeightOffset = 2.5f;
    public float bobAmplitude = 0.35f;
    public float bobFrequency = 3f;
    public float detectionRange = 9f;

    [Header("Swoop Attack Settings")]
    public float swoopSpeed = 9f;
    public float returnSpeed = 5f;
    public float swoopCooldown = 2.5f;
    public float hitDistance = 1.3f;
    public int enemyDamage = 1;

    private float cooldownTimer = 1.5f;
    private Vector2 swoopTargetPos;
    private Vector2 returnHoverPos;
    private bool hasDealtDamageThisSwoop = false;

    [Header("Hit & Visual Feel Effects")]
    public Color damageColor = Color.red;
    private Color originalColor;
    public float flashDuration = 0.15f;
    public GameObject hitVfxPrefab;
    public float knockbackForce = 5.5f;
    public float knockbackDuration = 0.16f;
    public float heavyLaunchMultiplier = 1.8f;
    public float hitStopDuration = 0.04f;
    public float airPogoBounceForce = 11f;

    [Header("Hit Cooldown & Animation")]
    public float hitCooldown = 0.3f;
    public float deathAnimationDuration = 1.8f;

    private float lastHitTime;
    private float bobTimer;
    private bool isKnockedBack = false;
    private Vector3 baseScale;

    private Transform playerTransform;
    private PlayerHealth playerHealth;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;

    void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        baseScale = new Vector3(
            Mathf.Abs(transform.localScale.x) > 0.01f ? Mathf.Abs(transform.localScale.x) : 1f,
            Mathf.Abs(transform.localScale.y) > 0.01f ? Mathf.Abs(transform.localScale.y) : 1f,
            transform.localScale.z
        );

        if (rb != null)
        {
            rb.gravityScale = 0f;
        }

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

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
            if (!isDead && rb != null) rb.linearVelocity = Vector2.zero;
            return;
        }

        if (FinisherManager.isQTEActive)
        {
            if (rb != null) rb.linearVelocity = Vector2.zero;
            return;
        }

        // สถานะติดสตันรอปิดฉาก [E] FINISH
        if (isFinishable)
        {
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.gravityScale = 0f;
            }

            if (finishSignObj != null)
            {
                float signFlip = transform.localScale.x >= 0 ? 1f : -1f;
                finishSignObj.transform.localScale = new Vector3(signFlip / baseScale.x, 1f / baseScale.y, 1f);
            }

            float distX = Vector2.Distance(transform.position, playerTransform.position);
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
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        switch (currentState)
        {
            case FlyState.Hovering:
                HandleHovering(distanceToPlayer);
                break;

            case FlyState.Swooping:
                HandleSwooping();
                break;

            case FlyState.Returning:
                HandleReturning();
                break;
        }
    }

    void HandleHovering(float distanceToPlayer)
    {
        bobTimer += Time.deltaTime * bobFrequency;
        float bobOffset = Mathf.Sin(bobTimer) * bobAmplitude;

        float xOffset = (transform.position.x < playerTransform.position.x) ? -2f : 2f;
        Vector2 desiredHoverPos = new Vector2(
            playerTransform.position.x + xOffset,
            playerTransform.position.y + hoverHeightOffset + bobOffset
        );

        Vector2 direction = desiredHoverPos - (Vector2)transform.position;
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.linearVelocity = direction * hoverSpeed;
        }

        FaceTarget(playerTransform.position.x);

        if (distanceToPlayer <= detectionRange)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0f)
            {
                StartSwoop();
            }
        }
    }

    void StartSwoop()
    {
        currentState = FlyState.Swooping;
        hasDealtDamageThisSwoop = false;
        cooldownTimer = swoopCooldown;

        Vector2 dirToPlayer = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
        swoopTargetPos = (Vector2)playerTransform.position + dirToPlayer * 1.2f;
        returnHoverPos = new Vector2(
            playerTransform.position.x,
            playerTransform.position.y + hoverHeightOffset
        );

        FaceTarget(swoopTargetPos.x);

        if (animator != null && animator.runtimeAnimatorController != null && HasParameter("Swoop"))
        {
            animator.ResetTrigger("Swoop");
            animator.SetTrigger("Swoop");
        }
    }

    void HandleSwooping()
    {
        Vector2 toTarget = swoopTargetPos - (Vector2)transform.position;
        float distToTarget = toTarget.magnitude;

        if (rb != null)
        {
            rb.linearVelocity = toTarget.normalized * swoopSpeed;
        }

        float distToPlayer = Vector2.Distance(transform.position, playerTransform.position);
        if (!hasDealtDamageThisSwoop && distToPlayer <= hitDistance)
        {
            hasDealtDamageThisSwoop = true;
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(enemyDamage);
            }
        }

        if (distToTarget <= 0.35f)
        {
            currentState = FlyState.Returning;
            cooldownTimer = swoopCooldown;
        }
    }

    void HandleReturning()
    {
        Vector2 toHover = returnHoverPos - (Vector2)transform.position;
        if (rb != null)
        {
            rb.linearVelocity = toHover.normalized * returnSpeed;
        }

        FaceTarget(playerTransform.position.x);

        if (toHover.magnitude <= 0.4f)
        {
            currentState = FlyState.Hovering;
            cooldownTimer = swoopCooldown;
        }
    }

    void FaceTarget(float targetX)
    {
        float diffX = targetX - transform.position.x;
        if (Mathf.Abs(diffX) > 0.1f)
        {
            float sign = diffX > 0f ? 1f : -1f;
            transform.localScale = new Vector3(
                baseScale.x * sign,
                baseScale.y,
                baseScale.z
            );
        }
    }

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

                if (playerTransform != null && playerTransform.position.y > transform.position.y + 0.55f)
                {
                    Rigidbody2D playerRb = playerTransform.GetComponent<Rigidbody2D>();
                    if (playerRb != null)
                    {
                        playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, airPogoBounceForce);
                    }
                }

                TakeDamage(damageToTake, damageToTake >= 3);

                if (StyleRankManager.Instance != null)
                {
                    StyleRankManager.Instance.AddStylePoints(damageToTake * 25f);
                }
            }
        }
    }

    public void TakeDamage(int damage, bool isHeavyHit = false, float overrideKnockbackDir = 0f, float overrideForce = 0f)
    {
        if (isDead) return;

        if (Time.time - lastComboHitTime <= 1.5f)
            currentComboReceived++;
        else
            currentComboReceived = 1;

        lastComboHitTime = Time.time;
        currentHealth -= damage;

        DamagePopup.Create(transform.position, damage, isHeavyHit || damage >= 3);

        if (AudioManager.instance != null)
            AudioManager.instance.PlaySFX(AudioManager.instance.hitImpactClip);

        if (CameraFollow.instance != null)
            CameraFollow.instance.Shake(0.1f, isHeavyHit ? 0.22f : 0.15f);

        if (hitVfxPrefab != null)
        {
            GameObject vfx = Instantiate(hitVfxPrefab, transform.position, Quaternion.identity);
            SpriteRenderer vfxSr = vfx.GetComponentInChildren<SpriteRenderer>();
            if (vfxSr != null)
            {
                vfxSr.sortingLayerName = "New Layer 3";
                vfxSr.sortingOrder = 120;
            }
            Destroy(vfx, 0.5f);
        }

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
            StartCoroutine(ApplyKnockback(isHeavyHit, overrideKnockbackDir, overrideForce));
        }

        if (spriteRenderer != null && !isFinishable)
        {
            StopCoroutine(nameof(FlashDamage));
            StartCoroutine(FlashDamage());
        }

        StartCoroutine(DoHitStop());
    }

    void EnterFinishableState()
    {
        isFinishable = true;
        currentState = FlyState.Hovering;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 0f;
        }

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
        if (mr != null)
        {
            mr.sortingLayerName = "New Layer 3";
            mr.sortingOrder = 150;
        }

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

    IEnumerator ApplyKnockback(bool isHeavyHit, float overrideDir = 0f, float overrideForce = 0f)
    {
        isKnockedBack = true;

        float dir = overrideDir != 0f
            ? Mathf.Sign(overrideDir)
            : (transform.position.x >= playerTransform.position.x ? 1f : -1f);

        float basePush = overrideForce > 0f ? overrideForce : knockbackForce;
        float finalForce = isHeavyHit ? basePush * heavyLaunchMultiplier : basePush;

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.linearVelocity = new Vector2(dir * finalForce, 1.2f);
        }

        yield return new WaitForSeconds(knockbackDuration);

        if (!isDead && rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        isKnockedBack = false;
    }

    IEnumerator DoHitStop()
    {
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(hitStopDuration);
        Time.timeScale = 1f;
    }

    IEnumerator FlashDamage()
    {
        spriteRenderer.color = Color.white;
        yield return new WaitForSecondsRealtime(0.03f);

        if (!isDead && !isFinishable && spriteRenderer != null)
            spriteRenderer.color = damageColor;

        yield return new WaitForSeconds(flashDuration);

        if (!isDead && !isFinishable && spriteRenderer != null)
            spriteRenderer.color = originalColor;
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
            rb.gravityScale = 0.6f; // ร่วงช้าๆ ให้ดูหน่วงตอนตาย
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

    bool HasParameter(string paramName)
    {
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName) return true;
        }
        return false;
    }
}