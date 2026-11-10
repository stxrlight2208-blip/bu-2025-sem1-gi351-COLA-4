
using UnityEngine;

public class PlayerHitbox : MonoBehaviour
{
    public Collider2D attackCollider;

    [Header("Damage Settings")]
    public int slashDamage = 1;
    public int upperDamage = 2;
    public int heavyDamage = 3;
    public int comboDamage = 4;

    [HideInInspector]
    public int currentAttackDamage;

    void Start()
    {
        currentAttackDamage = slashDamage;

        if (attackCollider != null)
            attackCollider.enabled = false;
        else
            Debug.LogError("ไม่ได้กำหนด Attack Collider!");
    }

    public void EnableHitbox()
    {
        if (attackCollider != null)
        {
            attackCollider.enabled = true;
            Debug.Log("เปิด Hitbox แล้ว!");
        }
    }

    public void DisableHitbox()
    {
        if (attackCollider != null)
            attackCollider.enabled = false;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Player ตรวจพบ Collider: " + other.name);
        HitEnemy(other);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        Debug.Log("Player กำลังสัมผัส Collider: " + other.name);
        HitEnemy(other);
    }

    void HitEnemy(Collider2D other)
    {
        if (attackCollider == null ||
            !attackCollider.enabled)
            return;

        Enemy enemy = other.GetComponent<Enemy>();

        if (enemy == null)
            enemy = other.GetComponentInParent<Enemy>();

        if (enemy != null)
        {
            Debug.Log("Player โจมตีโดน Enemy!");

            enemy.TakeDamage(currentAttackDamage);
        }
    }

    public void SetSlashDamage()
    {
        currentAttackDamage = slashDamage;
    }

    public void SetUpperDamage()
    {
        currentAttackDamage = upperDamage;
    }

    public void SetHeavyDamage()
    {
        currentAttackDamage = heavyDamage;
    }

    public void SetComboDamage()
    {
        currentAttackDamage = comboDamage;
    }
}
