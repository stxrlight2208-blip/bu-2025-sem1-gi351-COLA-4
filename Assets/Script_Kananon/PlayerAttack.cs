using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    private Animator animator;
    private PlayerHealth playerHealth;
    private float lastClickedTime = 0f;
    public float comboResetTimer = 0.8f;
    private int comboStep = 0;

    void Start()
    {
        animator = GetComponent<Animator>();
        playerHealth = GetComponent<PlayerHealth>();
    }

    void Update()
    {
        if (playerHealth != null && playerHealth.currentHealth <= 0)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            AttackAction();
        }

        if (Time.time - lastClickedTime > comboResetTimer)
        {
            comboStep = 0;
        }
    }

    void AttackAction()
    {
        lastClickedTime = Time.time;
        comboStep++;

        if (comboStep == 1)
        {
            animator.SetTrigger("Slash");
        }
        else if (comboStep == 2)
        {
            animator.SetTrigger("Upper");
        }
        else if (comboStep == 3)
        {
            animator.SetTrigger("Heavy");
        }
        else if (comboStep >= 4)
        {
            animator.SetTrigger("Combo");
            comboStep = 0;
        }
    }
}
