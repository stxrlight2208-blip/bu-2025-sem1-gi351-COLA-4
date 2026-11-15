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

    [Header("Movement & AI (Follow Player)")]
    public float moveSpeed = 2.5f;
    public float detectionRange = 6f;
    private Transform playerTransform;
    private Rigidbody2D rb;

    [Header("Attack Player Settings")]
    public int enemyDamage = 1;
    public float attackCooldown = 1.0f;
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

    void Start()
    {
        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
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
                    isBoss: false,
                    onSuccess: () => Die(),
                    onFail: () => RecoverFromStun()
                );
            }
            return;
        }

        if (isKnockedBack) return;

        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer <= detectionRange)
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
        if (playerTransform == null || rb == null) return;

        float direction = playerTransform.position.x > transform.position.x ? 1f : -1f;
        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);

        if (direction > 0)
        {
            transform.localScale = new Vector3(1f, 1f, 1f);
        }
        else if (direction < 0)
        {
            transform.localScale = new Vector3(-1f, 1f, 1f);
        }
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
            if (Time.time - lastAttackTime >= attackCooldown)
            {
                lastAttackTime = Time.time;
                playerHealth.TakeDamage(enemyDamage);
            }
        }
    }

    public void TakeDamage(int damage)
    {
        if (Time.time - lastComboHitTime <= 1.5f)
        {
            currentComboReceived++;
        }
        else
        {
            currentComboReceived = 1;
        }
        lastComboHitTime = Time.time;

        currentHealth -= damage;
        ComboCounterUI.RegisterHit(damage);

        bool isHeavyHit = damage >= 3;
        DamagePopup.Create(transform.position, damage, isHeavyHit);

        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlaySFX(AudioManager.instance.hitImpactClip);
        }

        if (CameraFollow.instance != null)
        {
            CameraFollow.instance.Shake(0.1f, 0.15f);
        }

        if (hitVfxPrefab != null)
        {
            GameObject vfx = Instantiate(hitVfxPrefab, transform.position, Quaternion.identity);
            Destroy(vfx, 0.5f);
        }

        if (!isFinishable && currentHealth > 0 && currentHealth <= maxHealth * finisherHealthPercent && currentComboReceived >= requiredComboHits)
        {
            EnterFinishableState();
            return;
        }

        if (rb != null && playerTransform != null && !isFinishable)
        {
            StopCoroutine(nameof(ApplyKnockback));
            StartCoroutine(ApplyKnockback());
        }

        if (spriteRenderer != null && !isFinishable)
        {
            StartCoroutine(FlashRed());
        }

        if (currentHealth <= 0)
        {
            Time.timeScale = 1f;
            Die();
        }
        else
        {
            StartCoroutine(DoHitStop());
        }
    }

    void EnterFinishableState()
    {
        isFinishable = true;
        if (rb != null) rb.linearVelocity = Vector2.zero;
        if (spriteRenderer != null) spriteRenderer.color = new Color(1f, 0.3f, 0.9f, 1f);

        finishSignObj = new GameObject("FinishHimPrompt");
        finishSignObj.transform.SetParent(transform);
        finishSignObj.transform.localPosition = new Vector3(0f, 1.3f, 0f);

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
        if (!isFinishable) spriteRenderer.color = originalColor;
    }

    void Die()
    {
        if (finishSignObj != null) Destroy(finishSignObj);
        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlaySFX(AudioManager.instance.enemyDieClip);
        }
        Destroy(gameObject);
    }
}