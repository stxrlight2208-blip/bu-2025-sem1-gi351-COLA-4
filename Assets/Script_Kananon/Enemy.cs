using System.Collections;
using System.Collections.Generic;
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
    public float attackRange = 1.65f;
    private Transform playerTransform;
    private PlayerHealth playerHealth;
    private Rigidbody2D rb;
    private Vector3 baseScale;

    [Header("Attack Player Settings")]
    public int enemyDamage = 1;
    public float attackCooldown = 1.5f;
    public Color telegraphColor = new Color(1f, 0.7f, 0.1f, 1f);
    private float lastAttackTime;
    private bool isAttacking = false;

    [Header("Visual & Hit Feel Effects")]
    private SpriteRenderer spriteRenderer;
    public Color damageColor = Color.red;
    private Color originalColor;
    public float flashDuration = 0.15f;
    public GameObject hitVfxPrefab;
    public float knockbackForce = 6.5f;      // เพิ่มแรงกระเด็นพื้นฐานให้ถอยไกลขึ้น
    public float knockbackDuration = 0.18f;  // เพิ่มระยะเวลาไถลถอยหลัง
    public float hitStopDuration = 0.04f;

    [Header("Chain Knockback / Enemy Bowling (ชนเพื่อนกระเด็นต่อ)")]
    public float heavyLaunchMultiplier = 2.0f; // ตัวคูณเมื่อโดนท่าแรง
    public int bowlingCollisionDamage = 2;     // ดาเมจเมื่อกระเด็นไปชนเพื่อน
    public float chainCollisionRadius = 1.1f;  // รัศมีตรวจจับตอนปลิวไปชนเพื่อน
    public float airPogoBounceForce = 11f;
    private bool isKnockbackProjectile = false;
    private float currentKnockbackDir = 0f;
    private HashSet<Enemy> hitFriendsThisLaunch = new HashSet<Enemy>();

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
            StopMovement();
            return;
        }

        if (FinisherManager.isQTEActive)
        {
            StopMovement();
            return;
        }

        // ระหว่างกำลังปลิวถอยหลัง เช็กว่าไปชนเพื่อนด้านหลังหรือไม่
        if (isKnockbackProjectile)
        {
            CheckChainKnockbackToFriends();
        }

        // สถานะติดสตันรอปิดฉาก [E] FINISH
        if (isFinishable)
        {
            StopMovement();

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

        float diffX = playerTransform.position.x - transform.position.x;
        if (!isAttacking && Mathf.Abs(diffX) > 0.1f)
        {
            transform.localScale = new Vector3(diffX > 0 ? baseScale.x : -baseScale.x, baseScale.y, baseScale.z);
        }

        if (isAttacking)
        {
            StopMovement();
            return;
        }

        float distanceToPlayer = Mathf.Abs(diffX);
        float effectiveStopRange = Mathf.Max(attackRange, 1.55f);

        if (distanceToPlayer <= detectionRange)
        {
            if (distanceToPlayer > effectiveStopRange)
            {
                MoveTowardsPlayer(diffX);
            }
            else
            {
                StopMovement();
                TryAttackPlayer();
            }
        }
        else
        {
            StopMovement();
        }
    }

    // ตรวจจับการกระเด็นไปชนมอนสเตอร์ตัวอื่น (ทำงานทะลุแม้ปิด Collider ระหว่างมอนไว้)
    void CheckChainKnockbackToFriends()
    {
        Collider2D[] nearby = Physics2D.OverlapCircleAll(transform.position, chainCollisionRadius);
        foreach (Collider2D col in nearby)
        {
            Enemy friend = col.GetComponent<Enemy>();
            if (friend == null) friend = col.GetComponentInParent<Enemy>();

            if (friend != null && friend != this && !friend.isDead && !hitFriendsThisLaunch.Contains(friend))
            {
                // เช็กว่าเพื่อนยืนอยู่ด้านหน้าทิศทางที่เรากำลังกระเด็นไป
                float dirToFriend = friend.transform.position.x - transform.position.x;
                if (Mathf.Sign(dirToFriend) == Mathf.Sign(currentKnockbackDir))
                {
                    hitFriendsThisLaunch.Add(friend);
                    friend.ReceiveChainKnockback(bowlingCollisionDamage, currentKnockbackDir, knockbackForce * 0.9f);

                    if (CameraFollow.instance != null)
                        CameraFollow.instance.Shake(0.12f, 0.18f);

                    if (StyleRankManager.Instance != null)
                        StyleRankManager.Instance.AddStylePoints(80f);
                }
            }
        }
    }

    // รับแรงกระแทกต่อจากเพื่อนที่ปลิวมาชน
    public void ReceiveChainKnockback(int damage, float pushDirection, float inheritedForce)
    {
        if (isDead || isFinishable) return;

        TakeDamage(damage, isHeavyHit: false, overrideKnockbackDir: pushDirection, overrideForce: inheritedForce);
    }

    void StopMovement()
    {
        if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        SetMoving(false);
    }

    void MoveTowardsPlayer(float distanceX)
    {
        if (playerTransform == null || rb == null || isDead) return;

        float direction = distanceX > 0f ? 1f : -1f;
        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
        SetMoving(true);
    }

    void TryAttackPlayer()
    {
        if (isFinishable || FinisherManager.isQTEActive || isAttacking) return;
        if (playerHealth == null || playerHealth.currentHealth <= 0) return;

        if (Time.time - lastAttackTime >= attackCooldown)
        {
            lastAttackTime = Time.time;
            StartCoroutine(TelegraphAndAttack());
        }
    }

    IEnumerator TelegraphAndAttack()
    {
        isAttacking = true;
        StopMovement();

        if (spriteRenderer != null && !isDead && !isFinishable)
            spriteRenderer.color = telegraphColor;

        yield return new WaitForSeconds(0.25f);

        if (spriteRenderer != null && !isDead && !isFinishable)
            spriteRenderer.color = originalColor;

        if (!isDead && !isFinishable && !FinisherManager.isQTEActive)
        {
            TriggerAnim("Attack");
            yield return new WaitForSeconds(0.15f);

            if (playerTransform != null && !isDead && !isFinishable && !FinisherManager.isQTEActive)
            {
                float hitCheckDistance = Mathf.Max(attackRange, 1.55f) + 0.55f;
                if (Vector2.Distance(transform.position, playerTransform.position) <= hitCheckDistance)
                {
                    if (playerHealth != null)
                    {
                        playerHealth.TakeDamage(enemyDamage);
                    }
                }
            }
        }

        yield return new WaitForSeconds(0.25f);
        isAttacking = false;
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

                if (playerTransform != null && playerTransform.position.y > transform.position.y + 0.65f)
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

        isAttacking = false;

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
    public void LaunchIntoAir(int slamDamage, float upwardForce = 9.5f, float floatDuration = 0.85f)
    {
        if (isDead || FinisherManager.isQTEActive) return;

        TakeDamage(slamDamage, isHeavyHit: false);

        if (!isDead && !isFinishable && rb != null)
        {
            StopCoroutine(nameof(ApplyKnockback));
            StopCoroutine(nameof(AirFloatRoutine));
            StartCoroutine(AirFloatRoutine(upwardForce, floatDuration));
        }
    }

    IEnumerator AirFloatRoutine(float upwardForce, float floatDuration)
    {
        isKnockedBack = true;
        float originalGravity = rb.gravityScale;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.3f, upwardForce);
        yield return new WaitForSeconds(0.18f);

        if (!isDead && !isFinishable)
        {
            rb.linearVelocity = new Vector2(0f, 0.5f);
            rb.gravityScale = 0.12f;
        }

        yield return new WaitForSeconds(floatDuration);

        if (rb != null)
            rb.gravityScale = originalGravity;

        isKnockedBack = false;
    }
    void EnterFinishableState()
    {
        isFinishable = true;
        isKnockbackProjectile = false;
        StopMovement();

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
        isKnockbackProjectile = true;
        hitFriendsThisLaunch.Clear();

        currentKnockbackDir = overrideDir != 0f
            ? Mathf.Sign(overrideDir)
            : (transform.position.x > playerTransform.position.x ? 1f : -1f);

        float basePush = overrideForce > 0f ? overrideForce : Mathf.Max(knockbackForce, 6.0f);
        float finalForce = isHeavyHit ? basePush * heavyLaunchMultiplier : basePush;
        float finalDuration = isHeavyHit ? knockbackDuration * 1.6f : knockbackDuration;
        float bounceUp = isHeavyHit ? 3.5f : 2.2f;

        rb.linearVelocity = new Vector2(currentKnockbackDir * finalForce, bounceUp);

        yield return new WaitForSeconds(finalDuration);

        if (!isDead)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        isKnockedBack = false;
        isKnockbackProjectile = false;
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
        isKnockbackProjectile = false;

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