using System.Collections;
using UnityEngine;

public class PlayerAirLauncher : MonoBehaviour
{
    [Header("Input Settings")]
    public KeyCode launcherHoldKey = KeyCode.W; // กด W (หรือลูกศรขึ้น) + คลิกซ้าย
    public bool followEnemyIntoAir = true;     // งัดโดนแล้วดีดตัวตามขึ้นไปฟันกลางอากาศ

    [Header("Launcher Stats & Physics")]
    public int launcherDamage = 2;
    public float launchForce = 11.5f;          // แรงงัดศัตรูลอยขึ้นฟ้า
    public float airHangDuration = 0.95f;      // ระยะเวลามอนลอยค้างกลางอากาศ
    public float playerFollowJumpForce = 10.5f;// แรงกระโดดตามขึ้นไปฟันกลางอากาศ
    public float launcherRange = 2.4f;
    public float cooldown = 0.5f;

    [Header("Visual Effect")]
    public GameObject launcherVfxPrefab;
    public string vfxSortingLayer = "New Layer 3";
    public int vfxSortingOrder = 125;

    private Rigidbody2D rb;
    private Animator animator;
    private float lastLaunchTime = -10f;
    private float lastGroundedTime = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (FinisherManager.isQTEActive || rb == null) return;

        // จำเวลาล่าสุดที่เท้าเหยียบพื้น (เผื่อกด W แล้วตัวละครเพิ่งเริ่มกระโดดขึ้น)
        if (Mathf.Abs(rb.linearVelocity.y) < 0.1f)
        {
            lastGroundedTime = Time.time;
        }

        bool canLaunchFromGround = (Time.time - lastGroundedTime <= 0.25f);
        bool holdingUp = Input.GetKey(launcherHoldKey) || Input.GetKey(KeyCode.UpArrow);

        // กด W (หรือ UpArrow) + คลิกซ้าย (ทั้งตอนยืนบนพื้น และตอนเพิ่งกด W กระโดดขึ้นไม่เกิน 0.25 วิ)
        if (canLaunchFromGround && holdingUp && Input.GetMouseButtonDown(0))
        {
            if (Time.time - lastLaunchTime >= cooldown)
            {
                PerformAirLauncher();
            }
        }
    }

    void PerformAirLauncher()
    {
        lastLaunchTime = Time.time;

        // 1. เล่นแอนิเมชันฟัน + เสกเอฟเฟกต์เสยขึ้นทันที (จะได้เห็นว่าท่าออกแล้ว)
        TriggerAttackAnim();
        SpawnLauncherVFX();

        // 2. ตรวจจับศัตรูด้านหน้า (ขยายวงเช็กให้กว้างขึ้นเพื่อไม่ให้ฟันวืด)
        float facingDir = transform.localScale.x >= 0 ? 1f : -1f;
        Vector2 hitCenter = (Vector2)transform.position + new Vector2(facingDir * (launcherRange * 0.55f), 0.5f);
        Collider2D[] hits = Physics2D.OverlapCircleAll(hitCenter, launcherRange * 0.8f);

        bool hitAnyEnemy = false;
        foreach (Collider2D col in hits)
        {
            Enemy enemy = col.GetComponent<Enemy>() ?? col.GetComponentInParent<Enemy>();
            if (enemy != null)
            {
                enemy.LaunchIntoAir(launcherDamage, launchForce, airHangDuration);
                hitAnyEnemy = true;
            }
        }

        // 3. ดีดตัวผู้เล่นตามขึ้นฟ้า
        if (hitAnyEnemy)
        {
            if (StyleRankManager.Instance != null)
                StyleRankManager.Instance.AddStylePoints(100f);

            if (followEnemyIntoAir)
            {
                StopAllCoroutines();
                StartCoroutine(DelayedFollowJump());
            }
        }
    }

    IEnumerator DelayedFollowJump()
    {
        yield return new WaitForSeconds(0.1f);
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, playerFollowJumpForce);
        }
    }

    void TriggerAttackAnim()
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;
        string[] triggers = { "Attack1", "Attack", "Slash" };
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

    void SpawnLauncherVFX()
    {
        if (launcherVfxPrefab == null) return;

        float facingDir = transform.localScale.x >= 0 ? 1f : -1f;
        Vector3 spawnPos = transform.position + new Vector3(facingDir * 1.1f, 0.7f, 0f);
        GameObject vfx = Instantiate(launcherVfxPrefab, spawnPos, Quaternion.Euler(0f, 0f, facingDir > 0 ? 65f : -65f));

        Vector3 s = vfx.transform.localScale;
        vfx.transform.localScale = new Vector3(Mathf.Abs(s.x) * facingDir, s.y, s.z);

        SpriteRenderer sr = vfx.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingLayerName = vfxSortingLayer;
            sr.sortingOrder = vfxSortingOrder;
        }
        Destroy(vfx, 0.4f);
    }
}