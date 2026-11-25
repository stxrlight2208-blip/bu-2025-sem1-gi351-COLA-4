
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BossHealth : MonoBehaviour
{
    [Header("Boss HP")]
    public float maxHealth = 120f;
    public float currentHealth;

    [Header("HP UI")]
    public Slider healthSlider;
    public GameObject bossHUD;

    [Header("Continue UI")]
    public GameObject continuePrompt;
    public SceneFade sceneFade;
    public string nextSceneName = "Boss_Defeat";

    [Header("Boss")]
    public Animator animator;
    public MonoBehaviour[] bossScriptsToStop;

    // เก็บไว้เพื่อรองรับ BossSpawner เวอร์ชันเดิม
    public BossDialogue bossDialogue;

    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;

        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (continuePrompt != null)
            continuePrompt.SetActive(false);
    }

    void Update()
    {
        // TEST: กด H ลดเลือด 50
        if (!isDead && Input.GetKeyDown(KeyCode.H))
            TakeDamage(50f);

        // เมื่อบอสตายแล้ว กด E เพื่อไปหน้าถัดไป
        if (isDead && Input.GetKeyDown(KeyCode.E))
        {
            if (sceneFade != null)
                sceneFade.FadeToScene(nextSceneName);
            else
                Debug.LogError("SceneFade is not assigned!");
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth = Mathf.Clamp(
            currentHealth - damage, 0, maxHealth
        );

        if (healthSlider != null)
            healthSlider.value = currentHealth;

        if (currentHealth <= 0)
            StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        isDead = true;

        // หยุด AI และระบบโจมตี
        foreach (MonoBehaviour script in bossScriptsToStop)
        {
            if (script != null)
                script.enabled = false;
        }

        // ให้บอสยืน Idle
        if (animator != null)
            animator.Play("Idle");

        // ซ่อนหลอดเลือด
        if (healthSlider != null)
            healthSlider.gameObject.SetActive(false);

        // แสดงปุ่ม Continue
        if (continuePrompt != null)
            continuePrompt.SetActive(true);

        Debug.Log("BOSS DEFEATED! Press E to continue.");

        yield return null;
    }
}
