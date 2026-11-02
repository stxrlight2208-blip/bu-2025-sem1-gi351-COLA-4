using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    private Animator animator;
    private float lastClickedTime = 0f;
    public float comboResetTimer = 0.8f; // เวลารีเซ็ตคอมโบถ้ารอนานเกินไป
    private int comboStep = 0;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        // คลิกซ้ายเพื่อออกท่าฟัน
        if (Input.GetMouseButtonDown(0))
        {
            AttackAction();
        }

        // ถ้ารอนานเกินไป ให้รีเซ็ตกลับไปเริ่มฟันท่าแรกใหม่
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
            animator.SetTrigger("Slash");   // เล่นท่า Ver_Slash
        }
        else if (comboStep == 2)
        {
            animator.SetTrigger("Upper");   // เล่นท่า Ver_Upper
        }
        else if (comboStep == 3)
        {
            animator.SetTrigger("Heavy");   // เล่นท่า Ver_Heavy
        }
        else if (comboStep >= 4)
        {
            animator.SetTrigger("Combo");   // เล่นท่า Ver_Combo ปิดฉาก
            comboStep = 0;                  // วนกลับมาเริ่มนับ 1 ใหม่
        }
    }
}