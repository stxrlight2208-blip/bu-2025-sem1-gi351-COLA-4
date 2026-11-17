using UnityEngine;

public class EnemyHitbox : MonoBehaviour
{
    public int attackDamage = 1;
    private Collider2D col;
    private Enemy enemyScript;

    void Start()
    {
        col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false; // ปิดกล่องไว้ก่อน รอเปิดด้วย Animation Event

        // ดึงสคริปต์หลักจากตัวแม่
        enemyScript = GetComponentInParent<Enemy>();
    }

    public void EnableHitbox() { if (col != null) col.enabled = true; }
    public void DisableHitbox() { if (col != null) col.enabled = false; }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        CheckHitPlayer(collision);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        CheckHitPlayer(collision);
    }

    void CheckHitPlayer(Collider2D collision)
    {
        if (col == null || !col.enabled) return;

        PlayerHealth player = collision.GetComponent<PlayerHealth>();
        if (player == null) player = collision.GetComponentInParent<PlayerHealth>();

        if (player != null)
        {
            player.TakeDamage(attackDamage);
            col.enabled = false; // ปิดทันทีหลังโดน เพื่อไม่ให้ดาเมจเบิ้ลซ้ำรัวๆ
        }
    }
}