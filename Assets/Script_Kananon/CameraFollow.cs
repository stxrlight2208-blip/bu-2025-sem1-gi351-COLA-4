using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target;

    [Header("Camera Settings")]
    public float smoothSpeed = 10f; // ปรับเพิ่มความเร็วในการตาม (จาก 5 เป็น 10 จะช่วยลดอาการหน่วงและกระตุก)
    public Vector3 offset = new Vector3(0f, 1f, -10f);

    void LateUpdate()
    {
        if (target == null) return;

        // ตำแหน่งเป้าหมาย
        Vector3 desiredPosition = target.position + offset;

        // ใช้ Lerp ตามตำแหน่งตัวละครอย่างนุ่มนวล
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // ล็อกตำแหน่ง Z ของกล้องให้คงที่เสมอเพื่อไม่ให้ภาพกระพือ
        smoothedPosition.z = offset.z;

        transform.position = smoothedPosition;
    }
}