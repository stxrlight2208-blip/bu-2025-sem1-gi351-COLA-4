using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerParry : MonoBehaviour
{
    [Header("Input & Timing")]
    public KeyCode parryKey = KeyCode.Mouse1;
    public float parryWindowDuration = 0.25f;
    public float parryCooldown = 0.45f;

    [Header("Parry Rewards & Counter")]
    public int parryCounterDamage = 3;
    public float parryCounterRadius = 2.8f;
    public float counterKnockbackForce = 8f;
    public float stylePointsReward = 100f;

    [Header("Slow-Mo & Player Speed Boost")]
    [Range(0.1f, 0.9f)]
    public float slowTimeScale = 0.3f;
    public float slowDuration = 0.75f;
    public float playerSpeedMultiplierInSlow = 1.45f; // ⭐ ความเร็วการเคลื่อนที่ของ Player ตอน Slow
    public float playerAnimSpeedInSlow = 1.4f;     // ⭐ ความเร็วอนิเมชันของ Player ตอน Slow
    public Color paleOverlayColor = new Color(0.72f, 0.78f, 0.85f, 0.32f);
    public float overlayFadeSpeed = 10f;

    [Header("VFX Prefabs")]
    public GameObject parryStartVfxPrefab;
    public GameObject perfectParryVfxPrefab;
    public bool spawnLineRingWhenNoPrefab = true;
    public float vfxLifetime = 0.7f;

    [Header("Visual & Audio")]
    public Color parryFlashColor = new Color(1f, 0.9f, 0.25f, 1f);
    public Color blockHoldColor = new Color(0.75f, 0.85f, 1f, 0.9f);
    public string vfxSortingLayer = "New Layer 3";
    public int vfxSortingOrder = 135;

    [HideInInspector] public bool isParryWindowActive = false;
    [HideInInspector] public bool isHoldingBlock = false;
    [HideInInspector] public bool isSlowMoActive = false;

    private float parryWindowTimer = 0f;
    private float cooldownTimer = 0f;
    private Coroutine slowMoCoroutine;

    private PlayerHealth playerHealth;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private Rigidbody2D rb;
    private Color originalColor;

    private Image screenPaleImage;
    private Color currentOverlayColor = Color.clear;

    void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        CreateScreenPaleOverlay();
    }

    void CreateScreenPaleOverlay()
    {
        GameObject existing = GameObject.Find("ParryPaleScreenCanvas");
        if (existing != null)
        {
            screenPaleImage = existing.GetComponentInChildren<Image>();
            return;
        }

        GameObject canvasObj = new GameObject("ParryPaleScreenCanvas");
        DontDestroyOnLoad(canvasObj);

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject panelObj = new GameObject("PaleScreenOverlay");
        panelObj.transform.SetParent(canvasObj.transform, false);

        screenPaleImage = panelObj.AddComponent<Image>();
        screenPaleImage.color = Color.clear;
        screenPaleImage.raycastTarget = false;

        RectTransform rt = panelObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    void Update()
    {
        if (screenPaleImage != null)
        {
            Color targetColor = isSlowMoActive ? paleOverlayColor : Color.clear;
            currentOverlayColor = Color.Lerp(currentOverlayColor, targetColor, Time.unscaledDeltaTime * overlayFadeSpeed);
            screenPaleImage.color = currentOverlayColor;
        }

        if (FinisherManager.isQTEActive || (playerHealth != null && playerHealth.currentHealth <= 0))
        {
            CancelBlock();
            return;
        }

        if (cooldownTimer > 0f)
            cooldownTimer -= Time.unscaledDeltaTime;

        if (Input.GetKeyDown(parryKey) && cooldownTimer <= 0f)
        {
            StartParryOrBlock();
        }

        if (isParryWindowActive)
        {
            parryWindowTimer -= Time.unscaledDeltaTime;
            if (parryWindowTimer <= 0f)
            {
                isParryWindowActive = false;
            }
        }

        if (Input.GetKeyUp(parryKey))
        {
            CancelBlock();
        }
    }

    void StartParryOrBlock()
    {
        isHoldingBlock = true;
        isParryWindowActive = true;
        parryWindowTimer = parryWindowDuration;
        cooldownTimer = parryCooldown;

        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }

        TriggerParryAnim();
        SpawnVfxPrefab(parryStartVfxPrefab, transform.position + new Vector3(0f, 0.5f, 0f), followPlayer: true);

        if (spriteRenderer != null)
        {
            spriteRenderer.color = blockHoldColor;
        }
    }

    public void CancelBlock()
    {
        isHoldingBlock = false;
        isParryWindowActive = false;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
    }

    public void OnPerfectParrySuccess()
    {
        isParryWindowActive = false;

        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlaySFX(AudioManager.instance.hitImpactClip, 1.3f, true);
        }

        if (CameraFollow.instance != null)
        {
            CameraFollow.instance.Shake(0.14f, 0.25f);
        }

        if (StyleRankManager.Instance != null)
        {
            StyleRankManager.Instance.AddStylePoints(stylePointsReward);
        }

        if (perfectParryVfxPrefab != null)
        {
            SpawnVfxPrefab(perfectParryVfxPrefab, transform.position + new Vector3(0f, 0.6f, 0f), followPlayer: false);
        }
        else if (spawnLineRingWhenNoPrefab)
        {
            SpawnParryRing();
        }

        CounterNearbyEnemies();

        if (slowMoCoroutine != null)
            StopCoroutine(slowMoCoroutine);
        slowMoCoroutine = StartCoroutine(PlayerAdvantageSlowMoRoutine());
    }

    IEnumerator PlayerAdvantageSlowMoRoutine()
    {
        isSlowMoActive = true;
        Time.timeScale = slowTimeScale;

        if (animator != null)
        {
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            animator.speed = playerAnimSpeedInSlow;
        }

        if (spriteRenderer != null)
            spriteRenderer.color = parryFlashColor;

        yield return new WaitForSecondsRealtime(0.1f);

        if (spriteRenderer != null)
            spriteRenderer.color = isHoldingBlock ? blockHoldColor : originalColor;

        float remaining = Mathf.Max(0f, slowDuration - 0.1f);
        yield return new WaitForSecondsRealtime(remaining);

        EndSlowMo();
    }

    public void EndSlowMo()
    {
        isSlowMoActive = false;
        Time.timeScale = 1f;

        if (animator != null)
        {
            animator.speed = 1f;
            animator.updateMode = AnimatorUpdateMode.Normal;
        }

        if (spriteRenderer != null)
            spriteRenderer.color = isHoldingBlock ? blockHoldColor : originalColor;
    }

    void SpawnVfxPrefab(GameObject prefab, Vector3 spawnPos, bool followPlayer)
    {
        if (prefab == null) return;

        GameObject vfx = Instantiate(prefab, spawnPos, Quaternion.identity);
        if (followPlayer)
        {
            vfx.transform.SetParent(transform);
        }

        float facingDir = transform.localScale.x >= 0 ? 1f : -1f;
        Vector3 s = vfx.transform.localScale;
        vfx.transform.localScale = new Vector3(Mathf.Abs(s.x) * facingDir, s.y, s.z);

        SpriteRenderer[] srs = vfx.GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer sr in srs)
        {
            sr.sortingLayerName = vfxSortingLayer;
            sr.sortingOrder = vfxSortingOrder;
        }

        Destroy(vfx, vfxLifetime);
    }

    void CounterNearbyEnemies()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, parryCounterRadius);
        foreach (Collider2D col in hits)
        {
            if (col.gameObject == gameObject) continue;

            float dir = col.transform.position.x >= transform.position.x ? 1f : -1f;

            Enemy enemy = col.GetComponent<Enemy>() ?? col.GetComponentInParent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(parryCounterDamage, isHeavyHit: true, overrideKnockbackDir: dir, overrideForce: counterKnockbackForce);
                continue;
            }

            FlyingSwoopEnemy flyer = col.GetComponent<FlyingSwoopEnemy>() ?? col.GetComponentInParent<FlyingSwoopEnemy>();
            if (flyer != null)
            {
                flyer.TakeDamage(parryCounterDamage, isHeavyHit: true, overrideKnockbackDir: dir, overrideForce: counterKnockbackForce);
                continue;
            }

            BossEnemy boss = col.GetComponent<BossEnemy>() ?? col.GetComponentInParent<BossEnemy>();
            if (boss != null)
            {
                boss.TakeDamage(parryCounterDamage);
            }
        }
    }

    void SpawnParryRing()
    {
        GameObject ringObj = new GameObject("ParryShockwave");
        ringObj.transform.position = transform.position + new Vector3(0f, 0.6f, 0f);

        LineRenderer lr = ringObj.AddComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Sprites/Default"));
        int segs = 36;
        lr.positionCount = segs + 1;
        lr.loop = true;
        lr.startWidth = 0.2f;
        lr.endWidth = 0.2f;
        lr.startColor = parryFlashColor;
        lr.endColor = Color.white;
        lr.sortingLayerName = vfxSortingLayer;
        lr.sortingOrder = vfxSortingOrder;

        ParryRingExpand expand = ringObj.AddComponent<ParryRingExpand>();
        expand.maxRadius = parryCounterRadius;
        expand.duration = 0.2f;
    }

    void TriggerParryAnim()
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;
        string[] triggers = { "Parry", "Block" };
        foreach (string t in triggers)
        {
            foreach (AnimatorControllerParameter p in animator.parameters)
            {
                if (p.name == t && p.type == AnimatorControllerParameterType.Trigger)
                {
                    animator.SetTrigger(t);
                    return;
                }
            }
        }
    }

    void OnDisable()
    {
        EndSlowMo();
        if (screenPaleImage != null)
            screenPaleImage.color = Color.clear;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, parryCounterRadius);
    }
}

public class ParryRingExpand : MonoBehaviour
{
    public float maxRadius = 2.8f;
    public float duration = 0.2f;
    private float timer = 0f;
    private LineRenderer lr;

    void Awake() => lr = GetComponent<LineRenderer>();

    void Update()
    {
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / duration);
        float r = Mathf.Lerp(0.3f, maxRadius, t);

        Color c = lr.startColor;
        c.a = 1f - t;
        lr.startColor = c;
        lr.endColor = c;

        int segs = lr.positionCount - 1;
        for (int i = 0; i <= segs; i++)
        {
            float rad = ((float)i / segs) * Mathf.PI * 2f;
            lr.SetPosition(i, transform.position + new Vector3(Mathf.Cos(rad) * r, Mathf.Sin(rad) * r, 0f));
        }

        if (t >= 1f) Destroy(gameObject);
    }
}