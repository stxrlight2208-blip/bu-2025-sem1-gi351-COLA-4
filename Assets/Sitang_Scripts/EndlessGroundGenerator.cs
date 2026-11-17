
using UnityEngine;
using System.Collections.Generic;

public class EndlessGroundGenerator : MonoBehaviour
{
    [Header("Player")]
    public Transform player;

    [Header("Ground Prefab")]
    public GameObject groundPrefab;

    [Header("Ground Settings")]
    public float groundY = 0f;
    public float groundZ = 0f;

    [Header("Ground Scale")]
    public Vector2 groundScale = new Vector2(1f, 1f);

    [Header("Generation")]
    public float spawnAhead = 20f;
    public float deleteBehind = 20f;
    public float initialGroundBehind = 10f;

    private float nextSpawnX;

    private List<GameObject> grounds =
        new List<GameObject>();

    void Start()
    {
        if (player == null || groundPrefab == null)
        {
            Debug.LogError("Please assign Player and Ground Prefab!");
            enabled = false;
            return;
        }

        // เริ่มสร้างพื้นจากด้านหลัง Player
        nextSpawnX = player.position.x - initialGroundBehind;

        // สร้างพื้นล่วงหน้าทั้งด้านหลังและด้านหน้า
        while (nextSpawnX < player.position.x + spawnAhead)
        {
            GenerateGround();
        }
    }

    void Update()
    {
        if (player == null)
            return;

        // สร้างพื้นด้านหน้า Player
        while (nextSpawnX < player.position.x + spawnAhead)
        {
            GenerateGround();
        }

        // ลบพื้นด้านหลัง Player
        CleanupGround();
    }

    void GenerateGround()
    {
        GameObject ground = Instantiate(
            groundPrefab,
            Vector3.zero,
            Quaternion.identity,
            transform
        );

        // กำหนดขนาดพื้น
        ground.transform.localScale = new Vector3(
            groundScale.x,
            groundScale.y,
            1f
        );

        // หาขอบเขต Sprite ทั้งหมด
        SpriteRenderer[] renderers =
            ground.GetComponentsInChildren<SpriteRenderer>();

        if (renderers.Length == 0)
        {
            Debug.LogError("Ground Prefab needs SpriteRenderer!");
            Destroy(ground);
            return;
        }

        float leftEdge = float.MaxValue;
        float rightEdge = float.MinValue;
        float topEdge = float.MinValue;

        foreach (SpriteRenderer sr in renderers)
        {
            leftEdge = Mathf.Min(leftEdge, sr.bounds.min.x);
            rightEdge = Mathf.Max(rightEdge, sr.bounds.max.x);
            topEdge = Mathf.Max(topEdge, sr.bounds.max.y);
        }

        // จัดขอบซ้ายให้ต่อกับพื้นชิ้นก่อนหน้า
        float offsetX = nextSpawnX - leftEdge;

        // จัดขอบบนให้ตรงกับระดับพื้น
        float offsetY = groundY - topEdge;

        ground.transform.position += new Vector3(
            offsetX,
            offsetY,
            groundZ
        );

        grounds.Add(ground);

        // ใช้ขอบขวาจริงเป็นจุดเริ่มของชิ้นถัดไป
        nextSpawnX = rightEdge + offsetX;
    }

    void CleanupGround()
    {
        for (int i = grounds.Count - 1; i >= 0; i--)
        {
            GameObject ground = grounds[i];

            if (ground == null)
            {
                grounds.RemoveAt(i);
                continue;
            }

            SpriteRenderer[] renderers =
                ground.GetComponentsInChildren<SpriteRenderer>();

            float rightEdge = float.MinValue;

            foreach (SpriteRenderer sr in renderers)
            {
                rightEdge = Mathf.Max(
                    rightEdge,
                    sr.bounds.max.x
                );
            }

            if (rightEdge < player.position.x - deleteBehind)
            {
                Destroy(ground);
                grounds.RemoveAt(i);
            }
        }
    }
}
