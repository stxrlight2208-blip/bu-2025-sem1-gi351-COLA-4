using UnityEngine;

public class PlayerHitbox : MonoBehaviour
{
    public Collider2D attackCollider;

    [Header("Damage Settings for Each Move")]
    public int slashDamage = 1;
    public int upperDamage = 2;
    public int heavyDamage = 3;
    public int comboDamage = 4;

    [HideInInspector] public int currentAttackDamage;

    void Start()
    {
        if (attackCollider != null)
            attackCollider.enabled = false;
        
        currentAttackDamage = slashDamage;
    }

    public void EnableHitbox()
    {
        if (attackCollider != null)
        {
            attackCollider.enabled = true;
        }
    }

    public void DisableHitbox()
    {
        if (attackCollider != null)
        {
            attackCollider.enabled = false;
        }
    }

    public void SetSlashDamage() { currentAttackDamage = slashDamage; }
    public void SetUpperDamage() { currentAttackDamage = upperDamage; }
    public void SetHeavyDamage() { currentAttackDamage = heavyDamage; }
    public void SetComboDamage() { currentAttackDamage = comboDamage; }
}