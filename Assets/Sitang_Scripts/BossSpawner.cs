
using UnityEngine;
using UnityEngine.UI;

public class BossSpawner : MonoBehaviour
{
    public GameObject bossPrefab;
    public Transform spawnPoint;
    public Slider bossHPBar;
    public GameObject bossHUD;
    public BossDialogue bossDialogue;

    [Header("Boss Defeat UI")]
    public GameObject continuePrompt;
    public SceneFade sceneFade;

    private bool hasSpawned = false;

    void Start()
    {
        SpawnBoss();
    }

    public void SpawnBoss()
    {
        if (hasSpawned) return;

        if (bossPrefab == null || spawnPoint == null)
        {
            Debug.LogError("Boss Prefab or Spawn Point is missing!");
            return;
        }

        GameObject boss = Instantiate(
            bossPrefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        BossHealth bossHealth = boss.GetComponent<BossHealth>();

        if (bossHealth != null)
        {
            bossHealth.healthSlider = bossHPBar;
            bossHealth.bossHUD = bossHUD;
            bossHealth.bossDialogue = bossDialogue;

            bossHealth.continuePrompt = continuePrompt;
            bossHealth.sceneFade = sceneFade;
        }
        else
        {
            Debug.LogError("BossHealth component not found!");
        }

        hasSpawned = true;
    }
}
