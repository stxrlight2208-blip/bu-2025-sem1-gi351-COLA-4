using System.Collections;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private PlayerHealth playerHealth;
    private Color originalColor;

    [Header("Combo Settings")]
    private float lastClickedTime = 0f;
    public float comboResetTimer = 0.8f;
    private int comboStep = 0;

    [Header("Charge & Explosion Settings (คลิกซ้ายค้าง)")]
    public float holdThreshold = 0.2f;       // กดค้างเกินกี่วินาทีถึงจะเริ่มชาร์จ
    public float maxChargeTime = 1.0f;       // ชาร์จนานกี่วินาทีถึงจะเต็มพลัง
    public float zoomInAmount = -1.6f;       // ระยะซูมกล้องเข้าตอนชาร์จ
    public float explosionRadius = 4.0f;     // รัศมีวงระเบิดพลังรอบตัว
    public int explosionDamage = 8;          // ดาเมจระเบิดเมื่อชาร์จเต็ม
    public Color chargingColor = new Color(0.4f, 0.9f, 1f, 1f); // สีตัวละครตอนชาร์จเต็ม (สีฟ้าสว่าง)
    public GameObject explosionVfxPrefab;    // (ถ้ามี) Prefab เอฟเฟกต์ระเบิด

    private float holdTimer = 0f;
    private bool isCharging = false;
    private bool isChargeReady = false;

    [Header("Camera Shake Settings")]
    public float normalShakeDuration = 0.08f;
    public float normalShakeStrength = 0.08f;
    public float finisherShakeDuration = 0.15f;
    public float finisherShakeStrength = 0.22f;

    [Header("VFX Settings")]
    public Transform attackPoint;
    public GameObject slashVfxPrefab;

    void Start()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerHealth = GetComponent<PlayerHealth>();

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    void Update()
    {
        // ห้ามโจมตีหรือชาร์จพลังหากผู้เล่นตายแล้ว หรือกำลังอยู่ในคัตซีน QTE
        if ((playerHealth != null && playerHealth.currentHealth <= 0) || FinisherManager.isQTEActive)
        {
            if (isCharging)
            {
                if (CameraFollow.instance != null) CameraFollow.instance.ResetZoom();
                if (spriteRenderer != null) spriteRenderer.color = originalColor;
                isCharging = false;
                isChargeReady = false;
            }
            return;
        }

        // 1. เริ่มกดคลิกซ้ายลง
        if (Input.GetMouseButtonDown(0))
        {
            holdTimer = 0f;
            isCharging = false;
            isChargeReady = false;
        }

        // 2. ระหว่างกดคลิกซ้ายค้างไว้
        if (Input.GetMouseButton(0))
        {
            holdTimer += Time.deltaTime;

            // กดค้างนานกว่า holdThreshold -> เข้าโหมดชาร์จ + ซูมกล้องเข้า
            if (holdTimer >= holdThreshold)
            {
                if (!isCharging)
                {
                    isCharging = true;
                    if (CameraFollow.instance != null)
                    {
                        CameraFollow.instance.SetZoomOffset(zoomInAmount);
                    }
                }

                // ค่อยๆ เปลี่ยนสีตัวละครตามระดับการชาร์จ
                float chargeProgress = Mathf.Clamp01((holdTimer - holdThreshold) / (maxChargeTime - holdThreshold));
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = Color.Lerp(originalColor, chargingColor, chargeProgress);
                }

                // แจ้งเตือนเมื่อชาร์จเต็ม 100% (สั่นเบาๆ 1 ครั้ง)
                if (chargeProgress >= 1f && !isChargeReady)
                {
                    isChargeReady = true;
                    if (CameraFollow.instance != null)
                    {
                        CameraFollow.instance.Shake(0.1f, 0.08f);
                    }
                }
            }
        }

        // 3. จังหวะปล่อยคลิกซ้าย
        if (Input.GetMouseButtonUp(0))
        {
            if (isCharging)
            {
                // คืนค่ากล้องและสีตัวละคร
                if (CameraFollow.instance != null) CameraFollow.instance.ResetZoom();
                if (spriteRenderer != null) spriteRenderer.color = originalColor;

                // ถ้าชาร์จถึงเกณฑ์ (หรือชาร์จเต็ม) -> ระเบิดพลัง!
                if (holdTimer >= maxChargeTime * 0.75f)
                {
                    ReleaseChargeExplosion();
                }
                else
                {
                    // ถ้าปล่อยก่อนชาร์จเต็ม -> ออกท่าฟันปกติแทน
                    AttackAction();
                }

                isCharging = false;
                isChargeReady = false;
            }
            else
            {
                // กดคลิกสั้นๆ -> ฟันคอมโบตามปกติ
                AttackAction();
            }
            holdTimer = 0f;
        }

        // รีเซ็ตคอมโบถ้ารอนานเกินไป
        if (!isCharging && Time.time - lastClickedTime > comboResetTimer)
        {
            comboStep = 0;
        }
    }

    // --- ปล่อยพลังระเบิดรอบตัว ---
    void ReleaseChargeExplosion()
    {
        if (animator != null)
        {
            animator.SetTrigger("Combo"); // เล่นท่าใหญ่สุดตอนระเบิดพลัง
        }

        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlaySFX(AudioManager.instance.swingClip, 1.2f, false);
        }

        // กล้องสั่นอย่างรุนแรงตอนระเบิดออก
        if (CameraFollow.instance != null)
        {
            CameraFollow.instance.Shake(0.28f, 0.35f);
        }

        // สร้างวงคลื่นระเบิดให้เห็นภาพชัดเจน
        StartCoroutine(SpawnShockwaveRing());

        if (explosionVfxPrefab != null)
        {
            GameObject vfx = Instantiate(explosionVfxPrefab, transform.position, Quaternion.identity);
            Destroy(vfx, 0.8f);
        }

        // ตรวจจับศัตรูและบอสทั้งหมดในรัศมี explosionRadius เพื่อทำดาเมจและผลักกระเด็น
        Collider2D[] hitTargets = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        foreach (Collider2D target in hitTargets)
        {
            if (target.gameObject == gameObject) continue;

            Enemy normalEnemy = target.GetComponent<Enemy>();
            if (normalEnemy != null)
            {
                normalEnemy.TakeDamage(explosionDamage);
                PushTargetAway(target.transform, 8f);
            }

            BossEnemy bossEnemy = target.GetComponent<BossEnemy>();
            if (bossEnemy != null)
            {
                bossEnemy.TakeDamage(explosionDamage);
                PushTargetAway(target.transform, 4f);
            }
        }
    }

    void PushTargetAway(Transform targetTransform, float force)
    {
        Rigidbody2D targetRb = targetTransform.GetComponent<Rigidbody2D>();
        if (targetRb != null)
        {
            float dir = targetTransform.position.x >= transform.position.x ? 1f : -1f;
            targetRb.linearVelocity = new Vector2(dir * force, 2f);
        }
    }

    IEnumerator SpawnShockwaveRing()
    {
        GameObject ringObj = new GameObject("ExplosionShockwave");
        ringObj.transform.position = transform.position;

        LineRenderer lr = ringObj.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop = true;
        lr.startWidth = 0.25f;
        lr.endWidth = 0.25f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = new Color(0.3f, 0.9f, 1f, 0.9f);
        lr.endColor = new Color(1f, 1f, 1f, 0.9f);
        lr.sortingOrder = 50;

        int segments = 36;
        lr.positionCount = segments;

        float elapsed = 0f;
        float duration = 0.25f;

        while (elapsed < duration)
        {
            float progress = elapsed / duration;
            float currentRadius = Mathf.Lerp(0.3f, explosionRadius, progress);
            float alpha = Mathf.Lerp(0.95f, 0f, progress);

            Color c = new Color(0.3f, 0.9f, 1f, alpha);
            lr.startColor = c;
            lr.endColor = c;
            lr.startWidth = Mathf.Lerp(0.3f, 0.05f, progress);
            lr.endWidth = lr.startWidth;

            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                lr.SetPosition(i, new Vector3(Mathf.Cos(angle) * currentRadius, Mathf.Sin(angle) * currentRadius, 0f));
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(ringObj);
    }

    void AttackAction()
    {
        lastClickedTime = Time.time;
        comboStep++;

        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlaySFX(AudioManager.instance.swingClip);
        }

        SpawnSlashVFX();

        if (comboStep == 1)
        {
            if (animator != null) animator.SetTrigger("Slash");
            TriggerCameraShake(normalShakeDuration, normalShakeStrength);
        }
        else if (comboStep == 2)
        {
            if (animator != null) animator.SetTrigger("Upper");
            TriggerCameraShake(normalShakeDuration, normalShakeStrength);
        }
        else if (comboStep == 3)
        {
            if (animator != null) animator.SetTrigger("Heavy");
            TriggerCameraShake(normalShakeDuration, normalShakeStrength * 1.2f);
        }
        else if (comboStep >= 4)
        {
            if (animator != null) animator.SetTrigger("Combo");
            TriggerCameraShake(finisherShakeDuration, finisherShakeStrength);
            comboStep = 0;
        }
    }

    void TriggerCameraShake(float duration, float strength)
    {
        if (CameraFollow.instance != null)
        {
            CameraFollow.instance.Shake(duration, strength);
        }
    }

    void SpawnSlashVFX()
    {
        if (slashVfxPrefab == null) return;

        Vector3 spawnPos = attackPoint != null ? attackPoint.position : transform.position;
        GameObject vfx = Instantiate(slashVfxPrefab, spawnPos, Quaternion.identity);

        Vector3 vfxScale = vfx.transform.localScale;
        vfxScale.x = Mathf.Abs(vfxScale.x) * (transform.localScale.x >= 0 ? 1f : -1f);
        vfx.transform.localScale = vfxScale;

        Destroy(vfx, 0.4f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}