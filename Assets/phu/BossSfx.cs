using System.Reflection;
using UnityEngine;

public class BossSFX : MonoBehaviour
{
    public static BossSFX Instance;

    [Header("Audio Source")]
    public AudioSource audioSource;

    [Header("Boss Combat Clips")]
    public AudioClip bossAttackClip;      // เสียงตอนบอสเหวี่ยงอาวุธ / โจมตี
    public AudioClip bossHeavySkillClip;  // เสียงท่าใหญ่ / คำราม / พุ่งชน
    public AudioClip bossHurtClip;        // เสียงตอนบอสโดนฟัน
    public AudioClip bossDeathClip;       // เสียงตอนบอสตาย

    [Header("Boss QTE Event Clips — ครบทุกจังหวะ")]
    public AudioClip qteStartClip;        // 1. เสียงแจ้งเตือนเริ่ม QTE บอส
    public AudioClip qteTapClip;          // 2. เสียงตอนกดปุ่ม QTE บอสทุกจังหวะ
    public AudioClip qteSuccessClip;      // 3. เสียงตอนชนะ QTE บอส (ฟันปิดฉาก)
    public AudioClip qteFailClip;         // 4. เสียงตอนกด QTE บอสพลาด / หมดเวลา

    [Header("QTE Input Keys (ปุ่มที่ใช้กดใน QTE บอส)")]
    public KeyCode[] qteKeys = new KeyCode[] { KeyCode.E, KeyCode.Space, KeyCode.F, KeyCode.Q, KeyCode.R, KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D };

    [Header("Detection Settings")]
    public float attackSoundCooldown = 0.6f;

    private Animator anim;
    private Collider2D col;
    private MonoBehaviour bossHealthScript;
    private FieldInfo currentHealthField;

    private int lastKnownHealth = -1;
    private bool isDead = false;
    private float lastAttackTime = -99f;

    // ตัวแปรจับสถานะ QTE ของบอส
    private bool wasQTEActive = false;
    private float qteStartTime = 0f;
    private int qteTapComboCount = 0;
    private GameObject cachedQTEUI;

    void Awake()
    {
        Instance = this;

        anim = GetComponent<Animator>();
        col = GetComponent<Collider2D>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D Sound ให้ได้ยินดังชัดเจนทั่วฉาก

        FindHealthField();
    }

    void Start()
    {
        lastKnownHealth = ReadCurrentHealth();
    }

    void Update()
    {
        if (isDead) return;

        HandleBossHealthAndDeath();
        HandleBossAttackAnimation();
        HandleBossQTE();
    }

    // ค้นหาตัวแปร currentHealth จากสคริปต์ของบอส (รองรับทั้ง BossEnemy และ Enemy)
    void FindHealthField()
    {
        foreach (var comp in GetComponents<MonoBehaviour>())
        {
            if (comp == null || comp == this) continue;
            var field = comp.GetType().GetField("currentHealth", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null && field.FieldType == typeof(int))
            {
                bossHealthScript = comp;
                currentHealthField = field;
                return;
            }
        }
    }

    int ReadCurrentHealth()
    {
        if (bossHealthScript != null && currentHealthField != null)
            return (int)currentHealthField.GetValue(bossHealthScript);
        return -1;
    }

    void HandleBossHealthAndDeath()
    {
        int currentHP = ReadCurrentHealth();

        // ตรวจจับตอนบอสโดนตี (เลือดลด)
        if (currentHP != -1 && lastKnownHealth != -1 && currentHP < lastKnownHealth && currentHP > 0)
        {
            PlayBossHurt();
        }

        // ตรวจจับตอนบอสตาย (เลือด <= 0 หรือ Collider ถูกปิด)
        if ((currentHP != -1 && currentHP <= 0) || (col != null && !col.enabled))
        {
            isDead = true;
            PlayBossDeath();
            return;
        }

        if (currentHP != -1)
            lastKnownHealth = currentHP;
    }

    void HandleBossAttackAnimation()
    {
        if (anim == null || Time.time - lastAttackTime < attackSoundCooldown) return;

        AnimatorStateInfo state = anim.GetCurrentAnimatorStateInfo(0);
        if (state.IsName("Attack") || state.IsName("Boss_Attack") || state.IsName("Slash") || state.IsTag("Attack"))
        {
            lastAttackTime = Time.time;
            PlayBossAttack();
        }
        else if (state.IsName("Skill") || state.IsName("HeavyAttack") || state.IsName("DashAttack") || state.IsTag("Skill"))
        {
            lastAttackTime = Time.time;
            PlayBossHeavySkill();
        }
    }

    // ตรวจจับ QTE Event ของบอสทุกจังหวะ
    void HandleBossQTE()
    {
        bool qteNow = IsBossQTEActive();

        // 1. จังหวะเริ่ม QTE
        if (qteNow && !wasQTEActive)
        {
            qteStartTime = Time.unscaledTime;
            qteTapComboCount = 0;
            PlayQTEStart();
        }

        // 2. จังหวะกดปุ่มใน QTE ทุกครั้ง (ไล่ Pitch สูงขึ้นเรื่อยๆ ตามจำนวนครั้งที่กด)
        if (qteNow && wasQTEActive)
        {
            if (Time.unscaledTime - qteStartTime > 0.08f && IsAnyQTEKeyPressed())
            {
                PlayQTETap();
            }
        }

        // 3. จังหวะจบ QTE (สำเร็จ vs พลาด/หมดเวลา)
        if (!qteNow && wasQTEActive)
        {
            if (qteTapComboCount > 0)
                PlayQTESuccess();
            else
                PlayQTEFail();
        }

        wasQTEActive = qteNow;
    }

    bool IsBossQTEActive()
    {
        if (FinisherManager.isQTEActive) return true;

        if (cachedQTEUI != null && cachedQTEUI.activeInHierarchy)
            return true;

        GameObject[] allObjs = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        for (int i = 0; i < allObjs.Length; i++)
        {
            GameObject obj = allObjs[i];
            if (obj == null || !obj.activeInHierarchy) continue;

            string n = obj.name.ToLower();
            if ((n.Contains("qte") || n.Contains("finisher")) &&
                !n.Contains("manager") && !n.Contains("sfx") && !n.Contains("prompt"))
            {
                cachedQTEUI = obj;
                return true;
            }
        }

        return false;
    }

    bool IsAnyQTEKeyPressed()
    {
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
            return true;

        if (qteKeys != null)
        {
            for (int i = 0; i < qteKeys.Length; i++)
            {
                if (Input.GetKeyDown(qteKeys[i]))
                    return true;
            }
        }
        return false;
    }

    // --- ฟังก์ชันสำหรับเรียกใช้ตรงๆ จากสคริปต์ของบอส (BossEnemy) ---
    public void PlayBossAttack() => PlayClip(bossAttackClip, 0.92f, 1.05f);
    public void PlayBossHeavySkill() => PlayClip(bossHeavySkillClip, 0.95f, 1.02f);
    public void PlayBossHurt() => PlayClip(bossHurtClip, 0.92f, 1.08f);
    public void PlayBossDeath() => PlayClip(bossDeathClip, 0.95f, 1.0f);

    public void PlayQTEStart() => PlayClip(qteStartClip, 1f, 1f);

    public void PlayQTETap()
    {
        if (qteTapClip == null || audioSource == null) return;
        float pitch = Mathf.Clamp(1.0f + (qteTapComboCount * 0.04f), 1.0f, 1.45f);
        qteTapComboCount++;
        audioSource.pitch = pitch;
        audioSource.PlayOneShot(qteTapClip);
    }

    public void PlayQTESuccess() => PlayClip(qteSuccessClip, 1f, 1f);
    public void PlayQTEFail() => PlayClip(qteFailClip, 0.95f, 1.0f);

    void PlayClip(AudioClip clip, float minPitch, float maxPitch)
    {
        if (clip == null || audioSource == null) return;
        audioSource.pitch = Random.Range(minPitch, maxPitch);
        audioSource.PlayOneShot(clip);
    }
}