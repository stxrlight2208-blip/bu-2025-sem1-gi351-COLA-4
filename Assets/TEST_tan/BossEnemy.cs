using System.Collections;
using UnityEngine;

public class BossEnemy : MonoBehaviour
{
    [Header("Boss Stats")]
    public int maxHealth = 40;
    private int currentHealth;
    private bool isPhaseTwo = false;

    [Header("Boss Finisher QTE Settings (5 ปุ่มสุดอลังการ)")]
    [Range(0.05f, 0.5f)]
    public float bossFinisherPercent = 0.2f;
    public float finisherInteractRange = 5.0f;
    private bool isFinishable = false;
    private bool hasTriggeredFinisherStun = false;
    private GameObject finishSignObj;

    [Header("Movement & Detection")]
    public float moveSpeed = 2.2f;
    public float detectionRange = 10f;
    public float attackRange = 4.5f;
    private Transform playerTransform;
    private Rigidbody2D rb;
    private BossHealth bossHealth;

    [Header("Boss Dash Attack & Telegraph")]
    public int dashDamage = 2;
    public float telegraphDuration = 0.7f;
    public float dashSpeed = 12f;
    public float dashDuration = 0.35f;
    public float attackCooldown = 2.5f;
    private float lastAttackTime;
    private bool isAttacking = false;

    [Header("Visual & Hit Feel")]
    private SpriteRenderer spriteRenderer;
    public Color damageColor = Color.white;
    public Color phaseTwoColor = new Color(1f, 0.45f, 0.45f, 1f);
    private Color baseColor;
    public GameObject hitVfxPrefab;

    [Header("Hit Cooldown")]
    private float lastHitTime;
    public float hitCooldown = 0.25f;

    void Start()
    {
        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        bossHealth = GetComponent<BossHealth>();

        if (spriteRenderer != null)
        {
            baseColor = spriteRenderer.color;
        }

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

        if (FinisherManager.isQTEActive)
        {
            if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        if (isFinishable)
        {
            if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

            if (finishSignObj != null)
            {
                finishSignObj.transform.localScale = new Vector3(transform.localScale.x >= 0 ? 1f : -1f, 1f, 1f);
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
                    isBoss: true,
                    onSuccess: () => FinishBoss(),
                    onFail: () => RecoverBossFromStun()
                );
            }
            return;
        }

        if (isAttacking) return;

        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer <= attackRange && Time.time - lastAttackTime >= attackCooldown)
        {
            StartCoroutine(TelegraphAndDashAttack());
        }
        else if (distanceToPlayer <= detectionRange)
        {
            MoveTowardsPlayer();
        }
        else
        {
            if (rb != null)
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }
        }
    }

    void MoveTowardsPlayer()
    {
        if (rb == null || playerTransform == null) return;

        float direction = playerTransform.position.x > transform.position.x ? 1f : -1f;
        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);

        float scaleX = Mathf.Abs(transform.localScale.x);
        transform.localScale = new Vector3(direction > 0 ? scaleX : -scaleX, transform.localScale.y, transform.localScale.z);
    }

    IEnumerator TelegraphAndDashAttack()
    {
        isAttacking = true;
        lastAttackTime = Time.time;

        if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        float dashDir = playerTransform.position.x > transform.position.x ? 1f : -1f;
        float scaleX = Mathf.Abs(transform.localScale.x);
        transform.localScale = new Vector3(dashDir > 0 ? scaleX : -scaleX, transform.localScale.y, transform.localScale.z);

        GameObject warningObj = CreateWarningSign();
        if (spriteRenderer != null) spriteRenderer.color = Color.yellow;

        yield return new WaitForSeconds(telegraphDuration);

        if (warningObj != null) Destroy(warningObj);
        if (isFinishable || FinisherManager.isQTEActive)
        {
            isAttacking = false;
            yield break;
        }

        if (spriteRenderer != null) spriteRenderer.color = baseColor;

        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlaySFX(AudioManager.instance.dashClip);
        }

        if (CameraFollow.instance != null)
        {
            CameraFollow.instance.Shake(0.12f, 0.15f);
        }

        float timer = 0f;
        while (timer < dashDuration)
        {
            if (isFinishable || FinisherManager.isQTEActive) break;
            if (rb != null)
            {
                rb.linearVelocity = new Vector2(dashDir * dashSpeed, rb.linearVelocity.y);
            }
            timer += Time.deltaTime;
            yield return null;
        }

        if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        yield return new WaitForSeconds(0.5f);

        isAttacking = false;
    }

    GameObject CreateWarningSign()
    {
        GameObject warn = new GameObject("BossWarning");
        warn.transform.SetParent(transform);
        warn.transform.localPosition = new Vector3(0f, 1.4f, 0f);
        warn.transform.localScale = new Vector3(transform.localScale.x >= 0 ? 1f : -1f, 1f, 1f);

        TextMesh tm = warn.AddComponent<TextMesh>();
        tm.text = "!";
        tm.fontSize = 80;
        tm.characterSize = 0.1f;
        tm.fontStyle = FontStyle.Bold;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = Color.red;

        MeshRenderer mr = warn.GetComponent<MeshRenderer>();
        if (mr != null) mr.sortingOrder = 105;

        return warn;
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (isFinishable || FinisherManager.isQTEActive) return;
        CheckAndDamagePlayer(collision.gameObject);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (FinisherManager.isQTEActive) return;

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

                int damageToTake = playerAttack != null ? playerAttack.currentAttackDamage : 1;
                TakeDamage(damageToTake);
            }
            return;
        }

        if (!isFinishable)
        {
            CheckAndDamagePlayer(collision.gameObject);
        }
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
            int dmg = isAttacking ? dashDamage : 1;
            playerHealth.TakeDamage(dmg);
        }
    }

    public void TakeDamage(int damage)
    {
        if (isFinishable) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);

        // ส่งดาเมจไปยังระบบหลอด HP
        if (bossHealth != null)
        {
            bossHealth.TakeDamage(damage);
        }

        ComboCounterUI.RegisterHit(damage);
        DamagePopup.Create(transform.position + Vector3.up * 0.5f, damage, damage >= 3);

        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlaySFX(AudioManager.instance.hitImpactClip);
        }

        if (CameraFollow.instance != null)
        {
            CameraFollow.instance.Shake(0.08f, 0.12f);
        }

        if (hitVfxPrefab != null)
        {
            GameObject vfx = Instantiate(hitVfxPrefab, transform.position, Quaternion.identity);
            Destroy(vfx, 0.5f);
        }

        if (currentHealth <= 0)
        {
            Time.timeScale = 1f;

            if (bossHealth == null)
                Die();

            return;
        }

        if (!hasTriggeredFinisherStun &&
            currentHealth <= maxHealth * bossFinisherPercent)
        {
            EnterBossFinishableState();
            return;
        }

        if (!isPhaseTwo && currentHealth <= maxHealth / 2)
        {
            EnterPhaseTwo();
        }

        if (spriteRenderer != null && !isAttacking)
        {
            StartCoroutine(FlashHit());
        }
    }

    void EnterBossFinishableState()
    {
        hasTriggeredFinisherStun = true;
        isFinishable = true;
        isAttacking = false;
        StopAllCoroutines();

        if (rb != null) rb.linearVelocity = Vector2.zero;
        if (spriteRenderer != null) spriteRenderer.color = new Color(1f, 0.3f, 0.9f, 1f);

        finishSignObj = new GameObject("BossFinishPrompt");
        finishSignObj.transform.SetParent(transform);
        finishSignObj.transform.localPosition = new Vector3(0f, 1.5f, 0f);

        TextMesh tm = finishSignObj.AddComponent<TextMesh>();
        tm.text = "[E] EXECUTE BOSS!";
        tm.fontSize = 60;
        tm.characterSize = 0.08f;
        tm.fontStyle = FontStyle.Bold;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = new Color(1f, 0.85f, 0.1f, 1f);

        MeshRenderer mr = finishSignObj.GetComponent<MeshRenderer>();
        if (mr != null) mr.sortingOrder = 115;

        StartCoroutine(BossStunTimeout());
    }

    IEnumerator BossStunTimeout()
    {
        yield return new WaitForSeconds(5.0f);
        if (isFinishable && !FinisherManager.isQTEActive)
        {
            RecoverBossFromStun();
        }
    }

    void RecoverBossFromStun()
    {
        isFinishable = false;
        if (finishSignObj != null) Destroy(finishSignObj);
        if (spriteRenderer != null) spriteRenderer.color = baseColor;
    }

    void EnterPhaseTwo()
    {
        isPhaseTwo = true;
        baseColor = phaseTwoColor;
        if (spriteRenderer != null) spriteRenderer.color = baseColor;

        moveSpeed *= 1.4f;
        dashSpeed *= 1.25f;
        attackCooldown *= 0.65f;
        telegraphDuration = 0.5f;

        if (CameraFollow.instance != null)
        {
            CameraFollow.instance.Shake(0.3f, 0.25f);
        }
    }

    IEnumerator FlashHit()
    {
        spriteRenderer.color = damageColor;
        yield return new WaitForSeconds(0.1f);
        if (!isFinishable) spriteRenderer.color = baseColor;
    }

    void Die()
    {
        if (finishSignObj != null) Destroy(finishSignObj);
        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlaySFX(AudioManager.instance.enemyDieClip);
        }

        if (CameraFollow.instance != null)
        {
            CameraFollow.instance.Shake(0.4f, 0.3f);
        }

        Destroy(gameObject);
    }
    void FinishBoss()
    {
        isFinishable = false;

        if (finishSignObj != null)
            Destroy(finishSignObj);

        if (bossHealth != null)
        {
            bossHealth.TakeDamage(999999f);
        }
        else
        {
            Die();
        }
    }
}