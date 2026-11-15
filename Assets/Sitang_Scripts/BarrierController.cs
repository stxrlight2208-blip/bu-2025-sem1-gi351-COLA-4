using UnityEngine;

public class BarrierController : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public SpriteRenderer spriteRenderer;

    [Header("Enemy Check Settings")]
    public string enemyTag = "Enemy";
    private bool isOpen = false;
    private bool isRoomActive = false;

    void Start()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        
        // **แก้ตรงนี้:** ไม่ต้องซ่อนตัว Object แต่ให้สั่งเปิด/ปิดเฉพาะ Collider กับ Sprite 
        // เพื่อให้ตอนเริ่มเกมประตูยังอยู่ แต่ถูกซ่อนไว้แบบพร้อมเด้งขึ้นมา
        ResetBarrierAsOpen();
    }

    void Update()
    {
        if (!isRoomActive || isOpen) return;

        GameObject[] remainingEnemies = GameObject.FindGameObjectsWithTag(enemyTag);

        if (remainingEnemies.Length == 0)
        {
            OpenBarrier();
        }
    }

    public void CloseBarrier()
    {
        isOpen = false;
        isRoomActive = true;

        if (spriteRenderer != null)
            spriteRenderer.enabled = true;

        Collider2D[] allCols = GetComponentsInChildren<Collider2D>(true);
        foreach (Collider2D col in allCols)
        {
            col.enabled = true;
        }

        if (animator != null)
            animator.SetTrigger("CloseBarrier");
    }

    public void OpenBarrier()
    {
        if (isOpen) return;

        isOpen = true;
        isRoomActive = false;

        if (animator != null)
        {
            animator.SetTrigger("OpenBarrier");
        }
        else
        {
            FinishOpening();
        }
    }

    public void FinishOpening()
    {
        Collider2D[] allCols = GetComponentsInChildren<Collider2D>(true);
        foreach (Collider2D col in allCols)
        {
            col.enabled = false;
        }

        if (spriteRenderer != null)
            spriteRenderer.enabled = false;
    }

    // ฟังก์ชันเซ็ตค่าเริ่มต้นให้ประตูเปิดโล่งอยู่ตอนยังไม่เข้าห้อง
    void ResetBarrierAsOpen()
    {
        isOpen = true;
        isRoomActive = false;

        Collider2D[] allCols = GetComponentsInChildren<Collider2D>(true);
        foreach (Collider2D col in allCols)
        {
            col.enabled = false;
        }

        if (spriteRenderer != null)
            spriteRenderer.enabled = false;
    }
}