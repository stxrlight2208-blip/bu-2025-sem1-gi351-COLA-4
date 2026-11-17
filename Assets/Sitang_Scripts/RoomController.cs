using UnityEngine;

public class RoomController : MonoBehaviour
{
    [Header("Doors in this Room")]
    public BarrierController entranceDoor; // ประตูทางเข้า (ด้านหลัง)
    public BarrierController exitDoor;     // ประตูทางออก (ด้านหน้า)

    [Header("Enemies in this Room")]
    public GameObject enemiesParent;       // กลุ่มศัตรูในห้องนี้ (ตอนแรกปิด Active ไว้)

    private bool roomActivated = false;

    void OnTriggerEnter2D(Collider2D collision)
    {
        // เมื่อ Player เดินเข้าสู่โซนห้อง และห้องนี้ยังไม่เคยทำงาน
        if (!roomActivated && (collision.CompareTag("Player") || collision.name == "Player"))
        {
            roomActivated = true;
            ActivateRoom();
        }
    }

    void ActivateRoom()
    {
        Debug.Log("ล็อคห้อง! ประตูเด้งปิดทั้งหน้าและหลัง!");

        // 1. สั่งปิดประตูทางเข้า (ด้านหลัง)
        if (entranceDoor != null)
        {
            entranceDoor.CloseBarrier();
        }

        // 2. สั่งปิดประตูทางออก (ด้านหน้า)
        if (exitDoor != null)
        {
            exitDoor.CloseBarrier();
        }

        // 3. ปลุกศัตรูในห้องให้เริ่มทำงาน
        if (enemiesParent != null)
        {
            enemiesParent.SetActive(true);
        }
    }
}