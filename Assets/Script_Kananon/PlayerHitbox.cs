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

        // เช็คว่าชนโดน Object ที่มี Tag เป็น "Enemy" หรือมีคอมโพเนนต์เช็คความเสียหาย
        // (คุณสามารถเปลี่ยนคำว่า "Enemy" เป็น Tag ที่ใช้จริงกับตัวศัตรูในโปรเจกต์ได้เลยครับ)
        if (other.CompareTag("Enemy") || other.GetComponentInParent<Transform>().name.Contains("Dummy"))
        {
            Debug.Log("★ ฟันโดนศัตรูแล้ว! กำลังสั่งบวกแต้มสไตล์...");

            // ลองสั่งลดเลือดหรือจัดการศัตรูตามเดิม (ถ้ามีสคริปต์)
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy == null) enemy = other.GetComponentInParent<Enemy>();
            
            if (enemy != null)
            {
                enemy.TakeDamage(currentAttackDamage);
            }

            // สั่งบวกแต้มสไตล์เหมือนตอนกดปุ่ม T ไม่มีผิดเพี้ยน!
            if (StyleRankManager.Instance != null)
            {
                float stylePoints = currentAttackDamage * 25f; // ปรับแต่งแต้มตามความแรงได้
                StyleRankManager.Instance.AddStylePoints(stylePoints);
            }
        }
    }

    public void SetSlashDamage() { currentAttackDamage = slashDamage; }
    public void SetUpperDamage() { currentAttackDamage = upperDamage; }
    public void SetHeavyDamage() { currentAttackDamage = heavyDamage; }
    public void SetComboDamage() { currentAttackDamage = comboDamage; }
}