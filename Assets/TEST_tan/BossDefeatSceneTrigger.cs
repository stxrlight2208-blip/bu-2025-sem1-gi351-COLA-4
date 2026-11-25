using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BossDefeatSceneTrigger : MonoBehaviour
{
    [Header("ตั้งค่าชื่อฉากหลังบอสตาย (ต้องตรงกับใน Scenes In Build เป๊ะๆ)")]
    public string defeatSceneName = "Boss_Defeat";
    public float delayBeforeSwitch = 1.5f;

    private bool hasTriggered = false;
    private bool cloneHasSpawned = false;
    private GameObject trackedBoss;

    void Update()
    {
        if (hasTriggered) return;

        // 1. ค้นหา BOSS(Clone) หรือตัว Boss ที่เปิดใช้งานอยู่จริงในฉาก (ไม่นับตัวต้นแบบที่ปิดตาไว้)
        if (trackedBoss == null)
        {
            GameObject cloneObj = GameObject.Find("BOSS(Clone)");
            if (cloneObj != null && cloneObj.activeInHierarchy)
            {
                trackedBoss = cloneObj;
                cloneHasSpawned = true;
            }
            else
            {
                Boss[] allBosses = FindObjectsByType<Boss>(FindObjectsSortMode.None);
                foreach (Boss b in allBosses)
                {
                    if (b.gameObject.activeInHierarchy)
                    {
                        trackedBoss = b.gameObject;
                        cloneHasSpawned = true;
                        break;
                    }
                }
            }
        }

        // 2. ถ้าเคยเจอ BOSS(Clone) แล้ว ตอนนี้มันหายไปหรือถูกปิดใช้งาน = ตายแล้ว!
        if (cloneHasSpawned && (trackedBoss == null || !trackedBoss.activeInHierarchy))
        {
            TriggerDefeat();
            return;
        }

        // 3. เช็กสถานะภายในตัว BOSS(Clone) ขณะกำลังเล่นแอนิเมชันตาย
        if (trackedBoss != null)
        {
            Collider2D col = trackedBoss.GetComponent<Collider2D>();
            Rigidbody2D rb = trackedBoss.GetComponent<Rigidbody2D>();

            // เช็กจากฟิสิกส์/การชนที่ถูกปิดตอนตาย
            if ((col != null && !col.enabled) || (rb != null && !rb.simulated))
            {
                TriggerDefeat();
                return;
            }

            // เช็กค่าเลือด (currentHealth / health / hp) จากสคริปต์ Boss โดยตรง
            Boss bossScript = trackedBoss.GetComponent<Boss>();
            if (bossScript != null && IsBossHealthZero(bossScript))
            {
                TriggerDefeat();
            }
        }
    }

    bool IsBossHealthZero(Boss bossScript)
    {
        FieldInfo[] fields = bossScript.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (FieldInfo f in fields)
        {
            string lower = f.Name.ToLower();
            if (lower.Contains("currenthealth") || lower == "health" || lower == "hp" || lower == "currenthp")
            {
                if (f.FieldType == typeof(int) && (int)f.GetValue(bossScript) <= 0)
                    return true;
                if (f.FieldType == typeof(float) && (float)f.GetValue(bossScript) <= 0f)
                    return true;
            }
        }
        return false;
    }

    void TriggerDefeat()
    {
        hasTriggered = true;
        StartCoroutine(LoadDefeatScene());
    }

    IEnumerator LoadDefeatScene()
    {
        yield return new WaitForSecondsRealtime(delayBeforeSwitch);
        Time.timeScale = 1f;
        SceneManager.LoadScene(defeatSceneName);
    }
}