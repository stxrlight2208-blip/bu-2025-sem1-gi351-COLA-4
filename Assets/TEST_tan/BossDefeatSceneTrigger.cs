using UnityEngine;
using UnityEngine.SceneManagement;

public class BossDefeatSceneTrigger : MonoBehaviour
{
    public string defeatSceneName = "Boss_Defeat";
    public string bossCloneName = "BOSS(Clone)";

    private GameObject bossClone;
    private bool hasFoundClone = false;
    private bool isLoading = false;

    void Update()
    {
        if (isLoading) return;

        // 1. ค้นหาตัว BOSS(Clone) ที่ถูกเสกออกมา
        if (!hasFoundClone)
        {
            bossClone = GameObject.Find(bossCloneName);
            if (bossClone != null)
            {
                hasFoundClone = true;
            }
            return;
        }

        // 2. เคยเจอ BOSS(Clone) แล้ว และตอนนี้มันตาย (โดน Destroy หายไปจากฉาก) -> ตัดฉากทันที!
        if (bossClone == null || !bossClone.activeInHierarchy)
        {
            isLoading = true;
            Time.timeScale = 1f;
            SceneManager.LoadScene(defeatSceneName);
        }
    }
}