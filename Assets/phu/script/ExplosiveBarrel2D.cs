using UnityEngine;

public interface IDamageable
{
    void TakeDamage(int amount);
}

public class ExplosiveBarrel2D : MonoBehaviour, IDamageable
{
    [Header("Explosion")]
    public float explosionRadius = 3.5f;
    public int explosionDamage = 999; // ตั้ง 999 เพื่อให้ศัตรูในรัศมีตายทันที
    public float pushForce = 8f;

    [Header("Trigger Settings")]
    public bool explodeOnPlayerTouch = true;
    public bool explodeOnEnemyTouch = true;

    [Header("Effects")]
    public GameObject explosionEffectPrefab;
    public AudioClip explosionSound;
    public float cameraShakeDuration = 0.2f;
    public float cameraShakeMagnitude = 0.35f;

    private bool hasExploded = false;

    // ทำงานเมื่อตั้ง Collider ของถังเป็น Is Trigger = true
    private void OnTriggerEnter2D(Collider2D other)
    {
        CheckTouchAndExplode(other.gameObject);
    }

    // ทำงานเมื่อตั้ง Collider ของถังเป็น Is Trigger = false (ชนแบบวัตถุแข็ง)
    private void OnCollisionEnter2D(Collision2D collision)
    {
        CheckTouchAndExplode(collision.gameObject);
    }

    private void CheckTouchAndExplode(GameObject toucher)
    {
        if (hasExploded) return;

        bool isPlayer = toucher.CompareTag("Player") || toucher.GetComponentInParent<PlayerHealth>() != null;
        bool isEnemy = toucher.CompareTag("Enemy") || toucher.GetComponentInParent<Enemy>() != null || toucher.GetComponentInParent<BossEnemy>() != null;

        if ((explodeOnPlayerTouch && isPlayer) || (explodeOnEnemyTouch && isEnemy))
        {
            Explode();
        }
    }

    public void TakeDamage(int amount)
    {
        if (!hasExploded && amount > 0)
        {
            Explode();
        }
    }

    public void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;

        if (explosionSound != null)
            AudioSource.PlayClipAtPoint(explosionSound, transform.position);
        else if (AudioManager.instance != null && AudioManager.instance.hitImpactClip != null)
            AudioManager.instance.PlaySFX(AudioManager.instance.hitImpactClip);

        if (explosionEffectPrefab != null)
        {
            GameObject vfx = Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
            Destroy(vfx, 2f);
        }

        if (CameraFollow.instance != null)
            CameraFollow.instance.Shake(cameraShakeDuration, cameraShakeMagnitude);

        // ค้นหาทุกวัตถุในรัศมีระเบิด (ไม่ต้องตั้ง LayerMask ให้ยุ่งยาก)
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue;

            // ดาเมจศัตรูทั่วไป
            Enemy enemy = hit.GetComponent<Enemy>() ?? hit.GetComponentInParent<Enemy>();
            if (enemy != null)
            {
                float dir = enemy.transform.position.x >= transform.position.x ? 1f : -1f;
                enemy.TakeDamage(explosionDamage, isHeavyHit: true, overrideKnockbackDir: dir, overrideForce: pushForce);
                continue;
            }

            // ดาเมจบอส
            BossEnemy boss = hit.GetComponent<BossEnemy>() ?? hit.GetComponentInParent<BossEnemy>();
            if (boss != null)
            {
                boss.TakeDamage(explosionDamage);
                continue;
            }

            // ระเบิดถังอื่นที่อยู่ใกล้ๆ ต่อเป็นลูกโซ่
            ExplosiveBarrel2D otherBarrel = hit.GetComponent<ExplosiveBarrel2D>() ?? hit.GetComponentInParent<ExplosiveBarrel2D>();
            if (otherBarrel != null && otherBarrel != this)
            {
                otherBarrel.Explode();
            }
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}