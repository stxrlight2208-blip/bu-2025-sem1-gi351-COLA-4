using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boss : MonoBehaviour, IDamageable
{
    [Header("Boss Stats")]
    public int maxHealth = 120;
    private int currentHealth;
    private bool isDead = false;

    [Header("Audio Source & Boss SFX (ลากไฟล์เสียงมาใส่ใน Inspector)")]
    public AudioSource bossAudioSource;
    public AudioClip telegraphClip;
    public AudioClip meleeAttackClip;
    public AudioClip dashClip;
    public AudioClip blitzDashClip;
    public AudioClip shockwaveWarningClip;
    public AudioClip shockwaveSlamClip;
    public AudioClip swordWaveClip;
    public AudioClip enrageClip;
    public AudioClip staggerClip;

    [Header("Enrage Phase (เลือดต่ำกว่า % นี้จะโหดขึ้น)")]
    [Range(0.1f, 0.9f)]
    public float enrageHealthPercent = 0.5f;
    public float enrageSpeedMultiplier = 1.35f;
    public float enrageCooldownMultiplier = 0.7f;
    public Color enrageTint = new Color(1f, 0.35f, 0.55f, 1f);
    private bool isEnraged = false;

    [Header("Movement & AI")]
    public float moveSpeed = 2.8f;
    public float detectionRange = 12f;
    public float meleeRange = 2.0f;
    private Transform playerTransform;
    private PlayerHealth playerHealth;
    private PlayerParry playerParry;
    private Rigidbody2D rb;
    private Vector3 baseScale;

    [Header("Attack 1: Melee Slash")]
    public int meleeDamage = 2;
    public float attackCooldown = 1.6f;
    public Color telegraphColor = new Color(1f, 0.7f, 0.1f, 1f);
    private float lastActionTime;
    private bool isBusy = false;

    [Header("Attack 2: Dash Slam (พุ่งกระแทก)")]
    public bool enableDashAttack = true;
    public float dashMinRange = 3.5f;
    public float dashMaxRange = 8.5f;
    public float dashSpeed = 13f;
    public float dashDuration = 0.32f;
    public float dashWarningDuration = 0.32f;
    public int dashDamage = 2;
    private bool isDashing = false;
    private HashSet<GameObject> dashHitTargets = new HashSet<GameObject>();

    [Header("Attack 3: Ground Slam Shockwave (ทุบพื้นสร้างคลื่นกระแทก)")]
    public bool enableShockwave = true;
    public float shockwaveRadius = 4.2f;
    public float shockwaveWarningDuration = 0.65f;
    public int shockwaveDamage = 2;
    public float shockwavePushForce = 9f;
    public GameObject shockwaveVfxPrefab;

    [Header("Attack 4: Four-Dash Blitz (พุ่งโจมตีไป-มา 4 รอบติด)")]
    public bool enableFourDashBlitz = true;
    public int blitzDashCount = 4;
    public float blitzDashSpeed = 14.5f;
    public float blitzDashDuration = 0.22f;
    public float blitzDashWarningDuration = 0.22f;
    public float blitzIntervalDelay = 0.12f;
    public int blitzDashDamage = 1;
    private bool isBlitzDashing = false;
    private HashSet<GameObject> blitzHitThisDash = new HashSet<GameObject>();

    [Header("Attack 5: Sword Wave (ฟันคลื่นดาบ)")]
    public bool enableSwordWave = true;
    public float swordWaveMinRange = 3.0f;
    public float swordWaveMaxRange = 11.5f;
    public float swordWaveSpeed = 11f;
    public float swordWaveRange = 9.5f;
    public float swordWaveWarningDuration = 0.35f;
    public float swordWaveHitRadius = 0.85f;
    public int swordWaveDamage = 2;
    public float swordWavePushForce = 6.5f;
    public GameObject swordWavePrefab;

    [Header("Stagger & Finisher QTE")]
    public int staggerDamageThreshold = 35;
    public float staggerDuration = 3.5f;
    public float finisherInteractRange = 4.5f;
    private int accumulatedDamageForStagger = 0;
    private bool isStaggered = false;
    private GameObject finishSignObj;

    [Header("Visual & Hit Feel")]
    private SpriteRenderer spriteRenderer;
    public Color damageColor = Color.red;
    private Color originalColor;
    public float flashDuration = 0.12f;
    public GameObject hitVfxPrefab;
    public float knockbackResistance = 0.25f;
    public float hitStopDuration = 0.03f;

    [Header("Hit Cooldown")]
    private float lastHitTime;
    public float hitCooldown = 0.22f;

    [Header("Animation")]
    private Animator animator;
    public float deathAnimationDuration = 1.5f;

    void Start()
    {
        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        if (bossAudioSource == null)
            bossAudioSource = GetComponent<AudioSource>();
        if (bossAudioSource == null)
            bossAudioSource = gameObject.AddComponent<AudioSource>();
        bossAudioSource.playOnAwake = false;

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
            playerParry = playerObj.GetComponent<PlayerParry>();
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

        if (isStaggered)
        {
            StopMovement();
            UpdateFinishSignScale();

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
                    onSuccess: () => Die(),
                    onFail: () => RecoverFromStagger()
                );
            }
            return;
        }

        if (isDashing)
        {
            CheckDashHitPlayer(dashHitTargets, dashDamage, 7f, 3.5f);
            return;
        }

        if (isBlitzDashing)
        {
            CheckDashHitPlayer(blitzHitThisDash, blitzDashDamage, 6.5f, 2.5f);
            return;
        }

        if (isBusy)
        {
            StopMovement();
            return;
        }

        float diffX = playerTransform.position.x - transform.position.x;
        if (Mathf.Abs(diffX) > 0.1f)
        {
            transform.localScale = new Vector3(diffX > 0 ? baseScale.x : -baseScale.x, baseScale.y, baseScale.z);
        }

        float distanceToPlayer = Mathf.Abs(diffX);
        float currentMoveSpeed = isEnraged ? moveSpeed * enrageSpeedMultiplier : moveSpeed;
        float currentCooldown = isEnraged ? attackCooldown * enrageCooldownMultiplier : attackCooldown;

        if (distanceToPlayer <= detectionRange)
        {
            if (Time.time - lastActionTime >= currentCooldown)
            {
                ChooseAndStartPattern(distanceToPlayer, diffX);
            }
            else if (distanceToPlayer > meleeRange)
            {
                MoveHorizontal(Mathf.Sign(diffX) * currentMoveSpeed);
            }
            else
            {
                StopMovement();
            }
        }
        else
        {
            StopMovement();
        }
    }

    void PlayBossSound(AudioClip clip, float minPitch = 0.95f, float maxPitch = 1.05f)
    {
        if (clip == null) return;

        if (bossAudioSource != null)
        {
            bossAudioSource.pitch = Random.Range(minPitch, maxPitch);
            bossAudioSource.PlayOneShot(clip);
        }
        else if (AudioManager.instance != null)
        {
            AudioManager.instance.PlaySFX(clip);
        }
    }

    void ChooseAndStartPattern(float distanceToPlayer, float diffX)
    {
        lastActionTime = Time.time;
        float dir = Mathf.Sign(diffX);
        float roll = Random.value;

        // ท่าฟันคลื่นดาบ เมื่ออยู่ระยะกลาง-ไกล
        if (enableSwordWave && distanceToPlayer >= swordWaveMinRange && distanceToPlayer <= swordWaveMaxRange && roll < 0.32f)
        {
            StartCoroutine(SwordWaveRoutine(dir));
            return;
        }

        // ท่าพุ่งไป-มา 4 รอบติด
        if (enableFourDashBlitz && distanceToPlayer >= 2.2f && distanceToPlayer <= dashMaxRange + 1.5f && roll < 0.55f)
        {
            StartCoroutine(FourDashBlitzRoutine(dir));
            return;
        }

        // ระยะประชิด สุ่มฟันประชิดหรือทุบพื้นคลื่นกระแทก
        if (distanceToPlayer <= meleeRange + 0.35f)
        {
            if (enableShockwave && Random.value < 0.45f)
            {
                StartCoroutine(GroundSlamShockwaveRoutine());
            }
            else
            {
                StartCoroutine(MeleeAttackRoutine());
            }
            return;
        }

        // ท่าพุ่งกระแทก
        if (enableDashAttack && distanceToPlayer >= dashMinRange && distanceToPlayer <= dashMaxRange && Random.value < 0.6f)
        {
            StartCoroutine(DashSlamRoutine(dir));
            return;
        }

        float currentMoveSpeed = isEnraged ? moveSpeed * enrageSpeedMultiplier : moveSpeed;
        MoveHorizontal(dir * currentMoveSpeed);
    }

    IEnumerator MeleeAttackRoutine()
    {
        isBusy = true;
        StopMovement();

        PlayBossSound(telegraphClip, 0.95f, 1.05f);
        yield return TelegraphFlash(0.28f);

        if (!isDead && !isStaggered && !FinisherManager.isQTEActive)
        {
            PlayBossSound(meleeAttackClip, 0.92f, 1.08f);
            TriggerAnim("Attack");
            yield return new WaitForSeconds(0.15f);

            if (playerTransform != null && !isDead && !isStaggered && !FinisherManager.isQTEActive)
            {
                float hitCheckDistance = meleeRange + 0.65f;
                if (Vector2.Distance(transform.position, playerTransform.position) <= hitCheckDistance)
                {
                    if (playerHealth != null)
                        playerHealth.TakeDamage(meleeDamage);
                }
            }
        }

        yield return new WaitForSeconds(0.25f);
        isBusy = false;
    }

    IEnumerator DashSlamRoutine(float dir)
    {
        isBusy = true;
        StopMovement();

        float dashDir = Mathf.Abs(dir) > 0.01f ? Mathf.Sign(dir) : 1f;
        transform.localScale = new Vector3(dashDir >= 0 ? baseScale.x : -baseScale.x, baseScale.y, baseScale.z);

        PlayBossSound(telegraphClip, 0.9f, 1.05f);
        float previewDist = dashSpeed * dashDuration;
        bool interrupted = false;
        yield return StartCoroutine(ShowDashWarningLine(dashDir, previewDist, dashWarningDuration, result => interrupted = result));

        if (interrupted || isDead || isStaggered || FinisherManager.isQTEActive)
        {
            isBusy = false;
            yield break;
        }

        isDashing = true;
        dashHitTargets.Clear();
        PlayBossSound(dashClip, 0.95f, 1.1f);
        TriggerAnim("Dash");
        SetMoving(true);

        float elapsed = 0f;
        while (elapsed < dashDuration && !isDead && !isStaggered && !FinisherManager.isQTEActive)
        {
            if (rb != null)
                rb.linearVelocity = new Vector2(dashDir * dashSpeed, rb.linearVelocity.y);

            elapsed += Time.deltaTime;
            yield return null;
        }

        isDashing = false;
        StopMovement();

        yield return new WaitForSeconds(0.3f);
        isBusy = false;
    }

    IEnumerator FourDashBlitzRoutine(float initialDir)
    {
        isBusy = true;
        StopMovement();

        PlayBossSound(telegraphClip, 0.95f, 1.1f);
        yield return TelegraphFlash(0.22f);

        if (isDead || isStaggered || FinisherManager.isQTEActive)
        {
            isBusy = false;
            yield break;
        }

        float currentDir = Mathf.Abs(initialDir) > 0.01f ? Mathf.Sign(initialDir) : 1f;
        int totalDashes = Mathf.Max(1, blitzDashCount);

        for (int i = 0; i < totalDashes; i++)
        {
            if (isDead || isStaggered || FinisherManager.isQTEActive)
                break;

            if (i > 0)
            {
                if (playerTransform != null)
                {
                    float toPlayerX = playerTransform.position.x - transform.position.x;
                    if (Mathf.Abs(toPlayerX) > 0.25f)
                        currentDir = Mathf.Sign(toPlayerX);
                    else
                        currentDir = -currentDir;
                }
                else
                {
                    currentDir = -currentDir;
                }
            }

            transform.localScale = new Vector3(currentDir >= 0 ? baseScale.x : -baseScale.x, baseScale.y, baseScale.z);

            float previewDist = blitzDashSpeed * blitzDashDuration;
            bool interrupted = false;
            yield return StartCoroutine(ShowDashWarningLine(currentDir, previewDist, blitzDashWarningDuration, result => interrupted = result));

            if (interrupted || isDead || isStaggered || FinisherManager.isQTEActive)
                break;

            isBlitzDashing = true;
            blitzHitThisDash.Clear();

            AudioClip clipToPlay = blitzDashClip != null ? blitzDashClip : dashClip;
            PlayBossSound(clipToPlay, 0.98f + i * 0.03f, 1.08f + i * 0.03f);
            TriggerAnim("Dash");
            SetMoving(true);

            float elapsed = 0f;
            while (elapsed < blitzDashDuration && !isDead && !isStaggered && !FinisherManager.isQTEActive)
            {
                if (rb != null)
                    rb.linearVelocity = new Vector2(currentDir * blitzDashSpeed, rb.linearVelocity.y);

                elapsed += Time.deltaTime;
                yield return null;
            }

            isBlitzDashing = false;
            StopMovement();

            if (i < totalDashes - 1 && blitzIntervalDelay > 0f && !isDead && !isStaggered && !FinisherManager.isQTEActive)
            {
                yield return new WaitForSeconds(blitzIntervalDelay);
            }
        }

        isBlitzDashing = false;
        StopMovement();

        yield return new WaitForSeconds(0.35f);
        isBusy = false;
    }

    IEnumerator SwordWaveRoutine(float dir)
    {
        isBusy = true;
        StopMovement();

        float waveDir = Mathf.Abs(dir) > 0.01f ? Mathf.Sign(dir) : 1f;
        transform.localScale = new Vector3(waveDir >= 0 ? baseScale.x : -baseScale.x, baseScale.y, baseScale.z);

        PlayBossSound(telegraphClip, 0.95f, 1.08f);
        bool interrupted = false;
        yield return StartCoroutine(ShowDashWarningLine(waveDir, swordWaveRange, swordWaveWarningDuration, result => interrupted = result));

        if (interrupted || isDead || isStaggered || FinisherManager.isQTEActive)
        {
            isBusy = false;
            yield break;
        }

        TriggerAnim("Attack");
        PlayBossSound(swordWaveClip != null ? swordWaveClip : meleeAttackClip, 0.95f, 1.08f);

        Vector3 spawnPos = transform.position + new Vector3(waveDir * 0.9f, 0.1f, 0f);
        SpawnSwordWave(spawnPos, waveDir);

        yield return new WaitForSeconds(0.32f);
        isBusy = false;
    }

    void SpawnSwordWave(Vector3 spawnPos, float dir)
    {
        GameObject waveObj;

        if (swordWavePrefab != null)
        {
            waveObj = Instantiate(swordWavePrefab, spawnPos, Quaternion.identity);
            waveObj.transform.localScale = new Vector3(
                Mathf.Abs(waveObj.transform.localScale.x) * dir,
                waveObj.transform.localScale.y,
                waveObj.transform.localScale.z
            );
        }
        else
        {
            waveObj = new GameObject("BossSwordWave");
            waveObj.transform.position = spawnPos;

            LineRenderer lr = waveObj.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.positionCount = 9;
            lr.startWidth = 0.16f;
            lr.endWidth = 0.16f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.sortingOrder = 55;

            Color c = isEnraged ? new Color(1f, 0.15f, 0.45f, 1f) : new Color(1f, 0.25f, 0.2f, 1f);
            lr.startColor = c;
            lr.endColor = c;

            for (int i = 0; i < 9; i++)
            {
                float t = (i / 8f) * 2f - 1f; // -1 ถึง 1
                float arcX = (1f - t * t) * 0.55f * dir;
                float arcY = t * 0.85f;
                lr.SetPosition(i, new Vector3(arcX, arcY, 0f));
            }
        }

        BossSwordWaveProjectile proj = waveObj.AddComponent<BossSwordWaveProjectile>();
        proj.Initialize(dir, swordWaveSpeed, swordWaveRange, swordWaveHitRadius, swordWaveDamage, swordWavePushForce, gameObject);
    }

    IEnumerator ShowDashWarningLine(float dir, float distance, float duration, System.Action<bool> onComplete)
    {
        if (spriteRenderer != null && !isDead && !isStaggered)
            spriteRenderer.color = telegraphColor;

        GameObject lineObj = new GameObject("DashWarningLine");
        lineObj.transform.position = transform.position;

        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.startWidth = 0.14f;
        lr.endWidth = 0.14f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder = 52;

        float elapsed = 0f;
        bool interrupted = false;

        while (elapsed < duration)
        {
            if (isDead || isStaggered || FinisherManager.isQTEActive)
            {
                interrupted = true;
                break;
            }

            Vector3 startPos = transform.position;
            Vector3 endPos = startPos + new Vector3(dir * distance, 0f, 0f);
            lr.SetPosition(0, startPos);
            lr.SetPosition(1, endPos);

            float pulse = 0.6f + 0.4f * Mathf.Sin(elapsed * 32f);
            Color c = new Color(1f, 0.12f, 0.1f, pulse);
            lr.startColor = c;
            lr.endColor = c;

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (lineObj != null)
            Destroy(lineObj);

        if (spriteRenderer != null && !isDead && !isStaggered)
            spriteRenderer.color = GetActiveBaseColor();

        onComplete?.Invoke(interrupted);
    }

    void CheckDashHitPlayer(HashSet<GameObject> hitSet, int damageAmount, float knockX, float knockY)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, meleeRange * 0.95f);
        foreach (Collider2D hit in hits)
        {
            if (hit.gameObject == gameObject) continue;

            PlayerHealth ph = hit.GetComponent<PlayerHealth>() ?? hit.GetComponentInParent<PlayerHealth>();
            if (ph != null && !hitSet.Contains(ph.gameObject))
            {
                hitSet.Add(ph.gameObject);
                ph.TakeDamage(damageAmount);

                Rigidbody2D playerRb = ph.GetComponent<Rigidbody2D>();
                if (playerRb != null)
                {
                    float pushDir = ph.transform.position.x >= transform.position.x ? 1f : -1f;
                    playerRb.linearVelocity = new Vector2(pushDir * knockX, knockY);
                }
            }
        }
    }

    IEnumerator GroundSlamShockwaveRoutine()
    {
        isBusy = true;
        StopMovement();

        if (spriteRenderer != null && !isDead && !isStaggered)
            spriteRenderer.color = telegraphColor;

        PlayBossSound(shockwaveWarningClip != null ? shockwaveWarningClip : telegraphClip, 0.95f, 1.05f);

        GameObject warningObj = CreateShockwaveWarningRing(shockwaveRadius);
        LineRenderer outerRing = warningObj.GetComponent<LineRenderer>();

        GameObject innerObj = new GameObject("ShockwaveWarningFillRing");
        innerObj.transform.SetParent(warningObj.transform);
        innerObj.transform.localPosition = Vector3.zero;
        LineRenderer innerRing = innerObj.AddComponent<LineRenderer>();
        ConfigureCircleLineRenderer(innerRing, new Color(1f, 0.15f, 0.1f, 0.85f), 0.08f, 51);

        float elapsed = 0f;
        bool interrupted = false;

        while (elapsed < shockwaveWarningDuration)
        {
            if (isDead || isStaggered || FinisherManager.isQTEActive)
            {
                interrupted = true;
                break;
            }

            float progress = Mathf.Clamp01(elapsed / shockwaveWarningDuration);
            float currentInnerRadius = Mathf.Lerp(0.15f, shockwaveRadius, progress);

            float pulse = 0.65f + 0.35f * Mathf.Sin(elapsed * 28f);
            outerRing.startColor = new Color(1f, 0.1f, 0.1f, pulse);
            outerRing.endColor = outerRing.startColor;

            UpdateCirclePoints(outerRing, shockwaveRadius, 48);
            UpdateCirclePoints(innerRing, currentInnerRadius, 48);

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (warningObj != null)
            Destroy(warningObj);

        if (spriteRenderer != null && !isDead && !isStaggered)
            spriteRenderer.color = GetActiveBaseColor();

        if (!interrupted && !isDead && !isStaggered && !FinisherManager.isQTEActive)
        {
            TriggerAnim("Slam");
            yield return new WaitForSeconds(0.12f);

            if (shockwaveVfxPrefab != null)
            {
                GameObject vfx = Instantiate(shockwaveVfxPrefab, transform.position, Quaternion.identity);
                Destroy(vfx, 1.5f);
            }

            if (CameraFollow.instance != null)
                CameraFollow.instance.Shake(0.22f, 0.35f);

            if (shockwaveSlamClip != null)
                PlayBossSound(shockwaveSlamClip, 0.9f, 1.05f);
            else if (AudioManager.instance != null)
                AudioManager.instance.PlaySFX(AudioManager.instance.hitImpactClip);

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, shockwaveRadius);
            foreach (Collider2D hit in hits)
            {
                if (hit.gameObject == gameObject) continue;

                PlayerHealth ph = hit.GetComponent<PlayerHealth>() ?? hit.GetComponentInParent<PlayerHealth>();
                if (ph != null)
                {
                    bool isJumpingHigh = ph.transform.position.y > transform.position.y + 1.1f;
                    if (!isJumpingHigh)
                    {
                        ph.TakeDamage(shockwaveDamage);
                        Rigidbody2D playerRb = ph.GetComponent<Rigidbody2D>();
                        if (playerRb != null)
                        {
                            float pushDir = ph.transform.position.x >= transform.position.x ? 1f : -1f;
                            playerRb.linearVelocity = new Vector2(pushDir * shockwavePushForce, 3f);
                        }
                    }
                }
            }
        }

        yield return new WaitForSeconds(0.3f);
        isBusy = false;
    }

    GameObject CreateShockwaveWarningRing(float radius)
    {
        GameObject obj = new GameObject("ShockwaveWarningRing");
        obj.transform.position = transform.position;

        LineRenderer lr = obj.AddComponent<LineRenderer>();
        ConfigureCircleLineRenderer(lr, new Color(1f, 0.15f, 0.1f, 0.9f), 0.12f, 50);
        UpdateCirclePoints(lr, radius, 48);

        return obj;
    }

    void ConfigureCircleLineRenderer(LineRenderer lr, Color color, float width, int sortingOrder)
    {
        lr.useWorldSpace = false;
        lr.loop = true;
        lr.startWidth = width;
        lr.endWidth = width;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = color;
        lr.endColor = color;
        lr.sortingOrder = sortingOrder;
    }

    void UpdateCirclePoints(LineRenderer lr, float radius, int segments)
    {
        if (lr == null) return;
        lr.positionCount = segments;

        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            lr.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
        }
    }

    IEnumerator TelegraphFlash(float duration)
    {
        if (spriteRenderer != null && !isDead && !isStaggered)
            spriteRenderer.color = telegraphColor;

        yield return new WaitForSeconds(duration);

        if (spriteRenderer != null && !isDead && !isStaggered)
            spriteRenderer.color = GetActiveBaseColor();
    }

    Color GetActiveBaseColor()
    {
        return isEnraged ? enrageTint : originalColor;
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (isDead || FinisherManager.isQTEActive) return;

        PlayerHitbox playerAttack = collision.GetComponent<PlayerHitbox>() ?? collision.GetComponentInParent<PlayerHitbox>();
        bool hitByPlayerHitbox = (playerAttack != null && playerAttack.attackCollider != null && playerAttack.attackCollider.enabled);
        bool hitByTagOrName = (collision.gameObject.name == "AttackPoint" || collision.CompareTag("PlayerAttack"));

        if (hitByPlayerHitbox || hitByTagOrName)
        {
            if (Time.time - lastHitTime >= hitCooldown)
            {
                lastHitTime = Time.time;
                int damageToTake = playerAttack != null ? playerAttack.currentAttackDamage : 1;
                TakeDamage(damageToTake, damageToTake >= 3);

                if (StyleRankManager.Instance != null)
                {
                    StyleRankManager.Instance.AddStylePoints(damageToTake * 30f);
                }
            }
        }
    }

    public void TakeDamage(int amount)
    {
        TakeDamage(amount, amount >= 3);
    }

    public void TakeDamage(int damage, bool isHeavyHit = false)
    {
        if (isDead || damage <= 0) return;

        currentHealth -= damage;
        accumulatedDamageForStagger += damage;

        DamagePopup.Create(transform.position, damage, isHeavyHit || damage >= 3);

        if (AudioManager.instance != null)
            AudioManager.instance.PlaySFX(AudioManager.instance.hitImpactClip);

        if (CameraFollow.instance != null)
            CameraFollow.instance.Shake(0.08f, isHeavyHit ? 0.2f : 0.12f);

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

        if (currentHealth <= 0)
        {
            Time.timeScale = (playerParry != null && playerParry.isSlowMoActive) ? playerParry.slowTimeScale : 1f;
            Die();
            return;
        }

        if (!isEnraged && currentHealth <= maxHealth * enrageHealthPercent)
        {
            isEnraged = true;
            PlayBossSound(enrageClip, 0.95f, 1.05f);
            if (CameraFollow.instance != null)
                CameraFollow.instance.Shake(0.25f, 0.35f);
        }

        if (!isStaggered && staggerDamageThreshold > 0 && accumulatedDamageForStagger >= staggerDamageThreshold)
        {
            accumulatedDamageForStagger = 0;
            EnterStaggerFinishState();
            return;
        }

        if (rb != null && playerTransform != null && !isDashing && !isBlitzDashing && !isStaggered)
        {
            float dir = transform.position.x >= playerTransform.position.x ? 1f : -1f;
            float lightPush = (isHeavyHit ? 3.5f : 1.5f) * knockbackResistance;
            rb.linearVelocity = new Vector2(dir * lightPush, rb.linearVelocity.y);
        }

        if (spriteRenderer != null && !isStaggered)
        {
            StopCoroutine(nameof(FlashDamage));
            StartCoroutine(FlashDamage());
        }

        StartCoroutine(DoHitStop());
    }

    void EnterStaggerFinishState()
    {
        isStaggered = true;
        isDashing = false;
        isBlitzDashing = false;
        isBusy = false;
        StopMovement();
        PlayBossSound(staggerClip, 0.95f, 1.05f);

        if (spriteRenderer != null)
            spriteRenderer.color = new Color(1f, 0.3f, 0.9f, 1f);

        if (finishSignObj == null)
        {
            finishSignObj = new GameObject("BossFinishPrompt");
            finishSignObj.transform.SetParent(transform);
            finishSignObj.transform.localPosition = new Vector3(0f, 1.5f / baseScale.y, 0f);

            TextMesh tm = finishSignObj.AddComponent<TextMesh>();
            tm.text = "[E] FINISH";
            tm.fontSize = 60;
            tm.characterSize = 0.09f;
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
        }

        UpdateFinishSignScale();
        StopCoroutine(nameof(StaggerTimeoutRoutine));
        StartCoroutine(StaggerTimeoutRoutine());
    }

    void UpdateFinishSignScale()
    {
        if (finishSignObj == null) return;
        float signFlip = transform.localScale.x >= 0 ? 1f : -1f;
        finishSignObj.transform.localScale = new Vector3(signFlip / baseScale.x, 1f / baseScale.y, 1f);
    }

    IEnumerator StaggerTimeoutRoutine()
    {
        yield return new WaitForSeconds(staggerDuration);
        if (isStaggered && !FinisherManager.isQTEActive)
        {
            RecoverFromStagger();
        }
    }

    void RecoverFromStagger()
    {
        isStaggered = false;
        accumulatedDamageForStagger = 0;
        if (finishSignObj != null) Destroy(finishSignObj);
        if (spriteRenderer != null) spriteRenderer.color = GetActiveBaseColor();
    }

    IEnumerator DoHitStop()
    {
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(hitStopDuration);

        if (playerParry != null && playerParry.isSlowMoActive)
            Time.timeScale = playerParry.slowTimeScale;
        else
            Time.timeScale = 1f;
    }

    IEnumerator FlashDamage()
    {
        spriteRenderer.color = Color.white;
        yield return new WaitForSecondsRealtime(0.03f);

        if (!isDead && !isStaggered && spriteRenderer != null)
            spriteRenderer.color = damageColor;

        yield return new WaitForSeconds(flashDuration);

        if (!isDead && !isStaggered && spriteRenderer != null)
            spriteRenderer.color = GetActiveBaseColor();
    }

    void MoveHorizontal(float velX)
    {
        if (rb != null) rb.linearVelocity = new Vector2(velX, rb.linearVelocity.y);
        SetMoving(Mathf.Abs(velX) > 0.05f);
    }

    void StopMovement()
    {
        if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        SetMoving(false);
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;
        isDashing = false;
        isBlitzDashing = false;
        isBusy = false;

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
        if (animator == null || animator.runtimeAnimatorController == null) return false;
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName) return true;
        }
        return false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, meleeRange);

        Gizmos.color = new Color(1f, 0.35f, 0.1f, 0.85f);
        Gizmos.DrawWireSphere(transform.position, shockwaveRadius);
    }
}

public class BossSwordWaveProjectile : MonoBehaviour
{
    private float direction;
    private float speed;
    private float maxDistance;
    private float hitRadius;
    private int damage;
    private float pushForce;
    private GameObject ownerBoss;
    private Vector3 startPosition;
    private bool hasHitPlayer = false;

    public void Initialize(float dir, float waveSpeed, float range, float radius, int dmg, float push, GameObject boss)
    {
        direction = Mathf.Abs(dir) > 0.01f ? Mathf.Sign(dir) : 1f;
        speed = waveSpeed;
        maxDistance = range;
        hitRadius = radius;
        damage = dmg;
        pushForce = push;
        ownerBoss = boss;
        startPosition = transform.position;
    }

    void Update()
    {
        transform.position += new Vector3(direction * speed * Time.deltaTime, 0f, 0f);

        if (!hasHitPlayer)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, hitRadius);
            foreach (Collider2D hit in hits)
            {
                if (ownerBoss != null && (hit.gameObject == ownerBoss || hit.transform.IsChildOf(ownerBoss.transform)))
                    continue;

                PlayerHealth ph = hit.GetComponent<PlayerHealth>() ?? hit.GetComponentInParent<PlayerHealth>();
                if (ph != null)
                {
                    hasHitPlayer = true;
                    ph.TakeDamage(damage);

                    Rigidbody2D playerRb = ph.GetComponent<Rigidbody2D>();
                    if (playerRb != null)
                    {
                        playerRb.linearVelocity = new Vector2(direction * pushForce, 2.5f);
                    }

                    Destroy(gameObject);
                    return;
                }
            }
        }

        if (Vector3.Distance(startPosition, transform.position) >= maxDistance)
        {
            Destroy(gameObject);
        }
    }
}