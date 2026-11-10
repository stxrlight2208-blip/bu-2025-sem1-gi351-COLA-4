
using UnityEngine;

public class BarrierController : MonoBehaviour
{
    public Animator animator;
    public Collider2D barrierCollider;

    private bool isOpen = false;

    public void OpenBarrier()
    {
        if (isOpen) return;

        isOpen = true;
        animator.SetTrigger("OpenBarrier");
    }


    public void FinishOpening()
    {
        barrierCollider.enabled = false;
        transform.parent.gameObject.SetActive(false);
    }



    // ใช้ทดสอบชั่วคราว กดปุ่ม O เพื่อเปิดยันต์
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.O))
        {
            OpenBarrier();
        }
    }
}
