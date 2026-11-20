using UnityEngine;

public class DamagePopup : MonoBehaviour
{
    private TextMesh textMesh;
    private float disappearTimer = 0.6f;
    private Color textColor;
    private Vector3 moveVector;

    // ฟังก์ชันสำหรับเรียกสร้างตัวเลขดาเมจจากที่ไหนก็ได้ในเกม
    public static void Create(Vector3 position, int damageAmount, bool isCritical = false)
    {
        GameObject popupObj = new GameObject("DamagePopup");
        // สุ่มขยับซ้าย-ขวานิดหน่อยไม่ให้ตัวเลขทับกันเวลาฟันรัวๆ
        Vector3 randomOffset = new Vector3(Random.Range(-0.3f, 0.3f), 0.8f, 0f);
        popupObj.transform.position = position + randomOffset;

        DamagePopup popup = popupObj.AddComponent<DamagePopup>();
        popup.Setup(damageAmount, isCritical);
    }

    public void Setup(int damageAmount, bool isCritical)
    {
        textMesh = gameObject.AddComponent<TextMesh>();
        textMesh.text = damageAmount.ToString();
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.fontStyle = FontStyle.Bold;
        textMesh.characterSize = 0.08f; // ปรับขนาดความคมชัดของฟอนต์

        // แยกสีและขนาดระหว่างตีปกติ กับ ท่าแรง/ปิดคอมโบ
        if (isCritical)
        {
            textMesh.fontSize = 65;
            textColor = new Color(1f, 0.85f, 0.1f, 1f); // สีเหลืองทอง (ท่าแรง/คริติคอล)
        }
        else
        {
            textMesh.fontSize = 48;
            textColor = Color.white; // สีขาว (ตีปกติ)
        }

        textMesh.color = textColor;

        // ดัน Sorting Layer และ Order ให้แสดงอยู่หน้าสุดเหนือฉาก New Layer 3
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.sortingLayerName = "New Layer 3";
            meshRenderer.sortingOrder = 150;
        }

        // ทิศทางการพุ่งเด้งขึ้นด้านบน
        moveVector = new Vector3(Random.Range(-0.8f, 0.8f), 3.5f, 0f);
    }

    void Update()
    {
        // เคลื่อนที่ลอยขึ้น
        transform.position += moveVector * Time.unscaledDeltaTime;
        // ค่อยๆ ลดความเร็วการพุ่ง (ให้ดูเหมือนเด้งแล้วลอยนิ่งๆ)
        moveVector -= moveVector * 4f * Time.unscaledDeltaTime;

        disappearTimer -= Time.unscaledDeltaTime;
        if (disappearTimer <= 0f)
        {
            // ค่อยๆ จางหายไป
            float fadeSpeed = 4f;
            textColor.a -= fadeSpeed * Time.unscaledDeltaTime;
            textMesh.color = textColor;

            if (textColor.a <= 0f)
            {
                Destroy(gameObject);
            }
        }
    }
}