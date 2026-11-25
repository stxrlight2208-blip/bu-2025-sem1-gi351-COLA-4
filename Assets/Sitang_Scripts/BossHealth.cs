
using UnityEngine;
using UnityEngine.UI;

public class BossHealth : MonoBehaviour
{
    [Header("Boss HP")]
    public float maxHealth = 500f;
    public float currentHealth;

    [Header("HP UI")]
    public Slider healthSlider;
    public GameObject bossHUD;

    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;

        if (healthSlider != null)
        {
            healthSlider.minValue = 0;
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }
    }

    void Update()
    {
        // TEST: กด H เพื่อลดเลือดบอส 50
        if (Input.GetKeyDown(KeyCode.H))
        {
            TakeDamage(50f);
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        UpdateHealthUI();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void UpdateHealthUI()
    {
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth;
        }
    }

    void Die()
    {
        if (isDead) return;

        isDead = true;

        Debug.Log("BOSS DEFEATED!");

        // ซ่อนหลอดเลือดบอส
        if (bossHUD != null)
        {
            bossHUD.SetActive(false);
        }

        // ทำลายตัวบอส
        Destroy(gameObject);
    }
}
