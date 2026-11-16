using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    void Start()
    {
        if (CharacterManager.Instance != null)
        {
            GameObject selectedPrefab = CharacterManager.Instance.GetSelectedCharacter();
            if (selectedPrefab != null)
            {
                // 1. เสกตัวละครตรงจุด Spawn
                GameObject player = Instantiate(selectedPrefab, transform.position, Quaternion.identity);
                player.name = "Player";
                player.tag = "Player"; // บังคับใส่ Tag "Player" เผื่อกล้องใช้หา

                // 2. ผูกเป้าหมายให้กล้องติดตามทันที
                AssignCameraTarget(player.transform);
            }
        }
        else
        {
            Debug.LogWarning("ไม่พบ CharacterManager! (หากกด Play ตรงจากฉากนี้ ให้ลาก CharacterManager มาวางเทสก่อน)");
        }
    }

    void AssignCameraTarget(Transform target)
    {
        // กรณีใช้ Cinemachine (รองรับทั้งเวอร์ชันเก่าและ Unity 6)
        foreach (var comp in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            string typeName = comp.GetType().Name;
            if (typeName == "CinemachineVirtualCamera" || typeName == "CinemachineCamera")
            {
                var prop = comp.GetType().GetProperty("Follow");
                if (prop != null) prop.SetValue(comp, target);
                return;
            }
        }

        // กรณีใช้สคริปต์ CameraFollow ทั่วไปติดไว้ที่ Main Camera
        if (Camera.main != null)
        {
            foreach (var script in Camera.main.GetComponents<MonoBehaviour>())
            {
                var field = script.GetType().GetField("target") ?? script.GetType().GetField("player");
                if (field != null && field.FieldType == typeof(Transform))
                {
                    field.SetValue(script, target);
                    return;
                }
            }
        }
    }
}