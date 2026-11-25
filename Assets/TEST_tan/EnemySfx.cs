using UnityEngine;

public class EnemySFX : MonoBehaviour
{
    [Header("Audio Source")]
    public AudioSource audioSource;

    [Header("Enemy Sound Clips (ลากไฟล์เสียงใส่ใน Inspector)")]
    public AudioClip attackSwingClip;    // เสียงตอน Enemy ฟัน / เหวี่ยงอาวุธ
    public AudioClip hurtClip;           // เสียงตอน Enemy โดนฟัน
    public AudioClip deathClip;          // เสียงตอน Enemy ตาย

    [Header("Attack Detection Settings")]
    public float attackSoundCooldown = 0.7f;
    public Color telegraphDetectColor = new Color(1f, 0.7f, 0.1f, 1f); // สีเหลืองตอนง้างตีใน Enemy.cs

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Collider2D col;

    private float lastAttackSoundTime = -10f;
    private float lastHurtSoundTime = -10f;
    private bool wasInAttackState = false;
    private bool wasTelegraphing = false;
    private bool hasPlayedDeath = false;

    void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
    }

    void Update()
    {
        if (hasPlayedDeath) return;

        // 1. ตรวจจับตอนตาย (ใน Enemy.cs เมื่อตายจะปิด Collider2D ทันที)
        if (col != null && !col.enabled)
        {
            hasPlayedDeath = true;
            PlayDeathSound();
            return;
        }

        // 2. ตรวจจับจังหวะ Enemy ฟัน (จากทั้ง Animator และจังหวะเปลี่ยนสีง้างตีใน Enemy.cs)
        DetectEnemyAttack();

        // 3. ตรวจจับจังหวะ Enemy โดนฟัน (จากสีแฟลชขาว/แดงตอนรับดาเมจใน Enemy.cs)
        DetectEnemyHurt();
    }

    void DetectEnemyAttack()
    {
        bool isAttackingNow = false;

        // เช็กจาก Animator State ว่ากำลังเล่นท่า Attack หรือไม่
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.IsName("Attack") || state.IsTag("Attack"))
            {
                isAttackingNow = true;
            }
        }

        // เช็กจากจังหวะเปลี่ยนสี Telegraph (สีเหลืองส้มก่อนฟัน 0.25 วิ ใน Enemy.cs)
        bool isTelegraphingNow = false;
        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            isTelegraphingNow = Mathf.Abs(c.r - telegraphDetectColor.r) < 0.08f &&
                                Mathf.Abs(c.g - telegraphDetectColor.g) < 0.08f &&
                                Mathf.Abs(c.b - telegraphDetectColor.b) < 0.08f;
        }

        // เล่นเสียงตอนเริ่มเข้าท่าฟัน หรือจังหวะง้างตีเสร็จแล้วฟันออกไป
        if ((isAttackingNow && !wasInAttackState) || (!isTelegraphingNow && wasTelegraphing))
        {
            PlayAttackSound();
        }

        wasInAttackState = isAttackingNow;
        wasTelegraphing = isTelegraphingNow;
    }

    void DetectEnemyHurt()
    {
        if (spriteRenderer == null) return;

        // ใน Enemy.cs ตอนโดนฟันสีจะเปลี่ยนเป็นสีขาวหรือสีแดง (R สูง, G/B ต่ำ)
        Color c = spriteRenderer.color;
        bool isFlashingRed = (c.r > 0.9f && c.g < 0.2f && c.b < 0.2f);

        if (isFlashingRed && Time.time - lastHurtSoundTime >= 0.2f)
        {
            lastHurtSoundTime = Time.time;
            PlayHurtSound();
        }
    }

    // --- ฟังก์ชันเล่นเสียง (สามารถเรียกใช้จาก Animation Event ได้ด้วย) ---
    public void PlayAttackSound()
    {
        if (Time.time - lastAttackSoundTime < attackSoundCooldown) return;
        lastAttackSoundTime = Time.time;
        PlayClip(attackSwingClip, 0.92f, 1.08f);
    }

    public void PlayHurtSound()
    {
        PlayClip(hurtClip, 0.9f, 1.12f);
    }

    public void PlayDeathSound()
    {
        if (deathClip == null) return;
        // ใช้ PlayClipAtPoint เพื่อให้เสียงตายเล่นจนจบแม้ตัว Enemy จะโดน Destroy ทิ้งทันที
        AudioSource.PlayClipAtPoint(deathClip, transform.position, audioSource != null ? audioSource.volume : 1f);
    }

    void PlayClip(AudioClip clip, float minPitch, float maxPitch)
    {
        if (clip == null || audioSource == null) return;
        audioSource.pitch = Random.Range(minPitch, maxPitch);
        audioSource.PlayOneShot(clip);
    }
}