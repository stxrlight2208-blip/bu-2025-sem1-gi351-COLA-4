
using UnityEngine;
using System.Collections.Generic;

public class BackgroundGenerator : MonoBehaviour
{
    [Header("Player")]
    public Transform player;

    [Header("Ground Reference")]
    public Transform groundLine;

    [Header("Background Prefabs")]
    public GameObject[] backgroundPrefabs;

    [Header("Spawn Settings")]
    public float spawnAheadDistance = 40f;
    public float cleanupDistance = 30f;

    [Header("Random Spacing")]
    public float minSpacing = 0.5f;
    public float maxSpacing = 2f;

    [Header("Random Scale")]
    public float minScale = 2f;
    public float maxScale = 3f;

    [Header("Position")]
    public float startX = 0f;
    public float initialSpawnBehind = 10f;

    private float nextSpawnX;

    private List<GameObject> spawnedObjects =
        new List<GameObject>();

    void Start()
    {
        nextSpawnX = player.position.x - initialSpawnBehind;
    }

    void Update()
    {
        if (player == null || groundLine == null)
            return;

        if (backgroundPrefabs == null ||
            backgroundPrefabs.Length == 0)
            return;

        while (nextSpawnX <
               player.position.x + spawnAheadDistance)
        {
            SpawnBackground();
        }

        CleanupBackground();
    }

    void SpawnBackground()
    {
        int index = Random.Range(
            0, backgroundPrefabs.Length);

        GameObject prefab = backgroundPrefabs[index];

        if (prefab == null)
            return;

        GameObject obj = Instantiate(
            prefab,
            Vector3.zero,
            Quaternion.identity,
            transform
        );

        // สุ่มขนาด
        float scale = Random.Range(minScale, maxScale);

        obj.transform.localScale =
            prefab.transform.localScale * scale;

        SpriteRenderer[] renderers =
            obj.GetComponentsInChildren<SpriteRenderer>();

        if (renderers.Length == 0)
        {
            Destroy(obj);
            return;
        }

        // หาขอบของภาพ
        float left = float.MaxValue;
        float right = float.MinValue;
        float bottom = float.MaxValue;

        foreach (SpriteRenderer sr in renderers)
        {
            left = Mathf.Min(left, sr.bounds.min.x);
            right = Mathf.Max(right, sr.bounds.max.x);
            bottom = Mathf.Min(bottom, sr.bounds.min.y);
        }

        // จัดฐานภาพให้อยู่ระดับพื้น
        float offsetX = nextSpawnX - left;
        float offsetY = groundLine.position.y - bottom;

        obj.transform.position += new Vector3(
            offsetX,
            offsetY,
            0f
        );

        spawnedObjects.Add(obj);

        // คำนวณตำแหน่งสร้างชิ้นต่อไป
        float width = right - left;

        nextSpawnX += width +
            Random.Range(minSpacing, maxSpacing);
    }

    void CleanupBackground()
    {
        for (int i = spawnedObjects.Count - 1;
             i >= 0; i--)
        {
            GameObject obj = spawnedObjects[i];

            if (obj == null)
            {
                spawnedObjects.RemoveAt(i);
                continue;
            }

            SpriteRenderer[] renderers =
                obj.GetComponentsInChildren<SpriteRenderer>();

            float right = float.MinValue;

            foreach (SpriteRenderer sr in renderers)
            {
                right = Mathf.Max(right, sr.bounds.max.x);
            }

            if (right < player.position.x - cleanupDistance)
            {
                Destroy(obj);
                spawnedObjects.RemoveAt(i);
            }
        }
    }
}
