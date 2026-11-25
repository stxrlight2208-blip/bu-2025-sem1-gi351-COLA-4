using UnityEngine;
using UnityEngine.EventSystems;

public class CharacterSelectFixer : MonoBehaviour
{
    void Awake()
    {
        // 1. คืนค่าเวลาเกมให้เดินปกติ
        Time.timeScale = 1f;

        // 2. เคลียร์ HUD หรือหน้าจอ Game Over ที่อาจติดมาจากฉากอื่น
        GameObject oldHud = GameObject.Find("PlayerHUD_Canvas");
        if (oldHud != null) Destroy(oldHud);

        GameObject oldGameOver = GameObject.Find("GameOver_Canvas");
        if (oldGameOver != null) Destroy(oldGameOver);

        // 3. เช็กให้ชัวร์ว่ามี EventSystem ให้กดปุ่มได้
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }
    }
}