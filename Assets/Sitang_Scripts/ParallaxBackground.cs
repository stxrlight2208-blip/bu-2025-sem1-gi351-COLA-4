using UnityEngine;

public class ParallaxBackground : MonoBehaviour
{
    public Transform player;
    public float parallaxEffect = 3.0f;

    private Vector3 startPosition;
    private float playerStartX;

    void Start()
    {
        startPosition = transform.position;

        // ถ้าไม่ได้ลากตัวละครใส่ช่อง Inspector ไว้ ให้ระบบช่วยค้นหาจาก Tag "Player" อัตโนมัติ
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }

        if (player != null)
        {
            playerStartX = player.position.x;
        }
    }

    void LateUpdate()
    {
        // ถ้ายังหาตัวละครไม่พบ ให้ลองหาใหม่อีกครั้ง (เผื่อตัวละครเกิดทีหลังตอนกดเริ่มเกม)
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                playerStartX = player.position.x;
            }
            return;
        }

        float distance = player.position.x - playerStartX;

        transform.position = new Vector3(
            startPosition.x + distance * parallaxEffect,
            startPosition.y,
            startPosition.z
        );
    }
}