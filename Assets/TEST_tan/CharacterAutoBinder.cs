using UnityEngine;
using UnityEngine.UI;

public class CharacterSelectAutoBinder : MonoBehaviour
{
    void Start()
    {
        Time.timeScale = 1f;

        // ค้นหา CharacterManager ตัวจริงที่รอดชีวิตอยู่ (รวมถึงตัวใน DontDestroyOnLoad)
        CharacterManager cm = FindFirstObjectByType<CharacterManager>();
        if (cm == null) return;

        // ค้นหาปุ่มทั้งหมดในฉาก Character_Select แล้วเชื่อมฟังก์ชันให้ใหม่
        Button[] allButtons = FindObjectsByType<Button>(FindObjectsSortMode.None);
        foreach (Button btn in allButtons)
        {
            string btnName = btn.gameObject.name.ToLower();

            // ล้างการอ้างอิงเดิมที่อาจขึ้น Missing ออกก่อน
            btn.onClick.RemoveAllListeners();

            if (btnName.Contains("start"))
            {
                // เชื่อมปุ่ม START (ลองเรียกฟังก์ชันเริ่มเกมของ CharacterManager)
                btn.onClick.AddListener(() => cm.SendMessage("StartGame", SendMessageOptions.DontRequireReceiver));
                btn.onClick.AddListener(() => cm.SendMessage("OnStartButtonClicked", SendMessageOptions.DontRequireReceiver));
                btn.onClick.AddListener(() => cm.SendMessage("LoadMapScene", SendMessageOptions.DontRequireReceiver));
            }
            else if (btnName.Contains("1") || btnName.Contains("blue") || btnName.Contains("vergil") || btnName.Contains("left"))
            {
                btn.onClick.AddListener(() => cm.SendMessage("SelectCharacter", 0, SendMessageOptions.DontRequireReceiver));
                btn.onClick.AddListener(() => cm.SendMessage("SelectCharacter1", SendMessageOptions.DontRequireReceiver));
            }
            else if (btnName.Contains("2") || btnName.Contains("red") || btnName.Contains("dante") || btnName.Contains("right"))
            {
                btn.onClick.AddListener(() => cm.SendMessage("SelectCharacter", 1, SendMessageOptions.DontRequireReceiver));
                btn.onClick.AddListener(() => cm.SendMessage("SelectCharacter2", SendMessageOptions.DontRequireReceiver));
            }
        }
    }
}