using System.Collections.Generic;
using UnityEngine;

public class PlayerHitbox : MonoBehaviour
{
    public Collider2D attackCollider;

    [Header("Damage Settings for Each Move")]
    public int slashDamage = 3;
    public int upperDamage = 4;
    public int heavyDamage = 5;
    public int comboDamage = 7;

    [HideInInspector] public int currentAttackDamage;

    // เก็บรายชื่อศัตรูที่โดนฟันไปแล้วใน 1 จังหวะการเหวี่ยงดาบ ป้องกันดาเมจเบิ้ลหรือแต้มสไตล์เด้งรัว
    private HashSet<GameObject> hitTargetsThisSwing = new HashSet<GameObject>();

    void Start()
    {
        if (attackCollider != null)
            attackCollider.enabled = false;

        currentAttackDamage = slashDamage;
    }

    public void EnableHitbox()
    {
        hitTargetsThisSwing.Clear(); // ล้างรายชื่อเป้าหมายทุกครั้งที่เริ่มเหวี่ยงดาบครั้งใหม่
        if (attackCollider != null)
        {
            attackCollider.enabled = true;
        }
    }

    public void DisableHitbox()
    {
        if (attackCollider != null)
        {
            attackCollider.enabled = false;
        }
        hitTargetsThisSwing.Clear();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        HitEnemy(other);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        HitEnemy(other);
    }

    void HitEnemy(Collider2D other)
    {
        if (attackCollider == null || !attackCollider.enabled)
            return;

        // 1. เช็กศัตรูทั่วไป (Enemy)
        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy == null) enemy = other.GetComponentInParent<Enemy>();

        if (enemy != null)
        {
            if (hitTargetsThisSwing.Contains(enemy.gameObject)) return;
            hitTargetsThisSwing.Add(enemy.gameObject);

            enemy.TakeDamage(currentAttackDamage);

            if (StyleRankManager.Instance != null)
            {
                StyleRankManager.Instance.AddStylePoints(currentAttackDamage * 25f);
            }
            return;
        }

        // 2. เช็กบอส (BossEnemy)
        BossEnemy boss = other.GetComponent<BossEnemy>();
        if (boss == null) boss = other.GetComponentInParent<BossEnemy>();

        if (boss != null)
        {
            if (hitTargetsThisSwing.Contains(boss.gameObject)) return;
            hitTargetsThisSwing.Add(boss.gameObject);

            boss.TakeDamage(currentAttackDamage);

            if (StyleRankManager.Instance != null)
            {
                StyleRankManager.Instance.AddStylePoints(currentAttackDamage * 25f);
            }
            return;
        }

       

        // 3. กรณีเป้าหมายเป็น Dummy ที่ยังไม่ได้ติดสคริปต์ Enemy แต่มี Tag "Enemy" หรือชื่อ Dummy
        if (other.CompareTag("Enemy") || other.transform.root.name.Contains("Dummy"))
        {
            GameObject targetRoot = other.gameObject;
            if (hitTargetsThisSwing.Contains(targetRoot)) return;
            hitTargetsThisSwing.Add(targetRoot);

            if (StyleRankManager.Instance != null)
            {
                StyleRankManager.Instance.AddStylePoints(currentAttackDamage * 25f);
            }
        }
    }

    public void SetSlashDamage() { currentAttackDamage = slashDamage; }
    public void SetUpperDamage() { currentAttackDamage = upperDamage; }
    public void SetHeavyDamage() { currentAttackDamage = heavyDamage; }
    public void SetComboDamage() { currentAttackDamage = comboDamage; }
}