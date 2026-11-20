using UnityEngine;

public class ForceAllVFXFront : MonoBehaviour
{
    [Header("Layer Settings")]
    public string targetSortingLayer = "New Layer 3";
    public int targetSortingOrder = 100;

    // คำค้นหาชื่อ Prefab เอฟเฟกต์ในโปรเจกต์ (แก้เพิ่มได้ตามชื่อไฟล์)
    private readonly string[] vfxKeywords = { "VFX", "Slash", "Impact", "Hit", "Explosion", "Aura", "Effect" };

    void LateUpdate()
    {
        // ดัน SpriteRenderer ของเอฟเฟกต์ทั้งหมดขึ้นมาด้านหน้า
        foreach (SpriteRenderer sr in FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
        {
            if (IsVFXObject(sr.gameObject))
            {
                if (sr.sortingLayerName != targetSortingLayer || sr.sortingOrder < targetSortingOrder)
                {
                    sr.sortingLayerName = targetSortingLayer;
                    sr.sortingOrder = targetSortingOrder;
                }
            }
        }

        // ดัน ParticleSystemRenderer ของเอฟเฟกต์แบบ Particle ทั้งหมดขึ้นมาด้านหน้า
        foreach (ParticleSystemRenderer psr in FindObjectsByType<ParticleSystemRenderer>(FindObjectsSortMode.None))
        {
            if (IsVFXObject(psr.gameObject))
            {
                if (psr.sortingLayerName != targetSortingLayer || psr.sortingOrder < targetSortingOrder)
                {
                    psr.sortingLayerName = targetSortingLayer;
                    psr.sortingOrder = targetSortingOrder;
                }
            }
        }
    }

    bool IsVFXObject(GameObject obj)
    {
        // เช็กชื่อตัวเองและชื่อตัวแม่ (Parent) ว่ามีคำเกี่ยวกับ VFX หรือไม่
        Transform current = obj.transform;
        while (current != null)
        {
            string upperName = current.name.ToUpper();
            foreach (string key in vfxKeywords)
            {
                if (upperName.Contains(key.ToUpper()))
                    return true;
            }
            current = current.parent;
        }
        return false;
    }
}