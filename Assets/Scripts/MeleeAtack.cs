using UnityEngine;

public class MeleeAtack : MonoBehaviour, IAtackHandler
{
    
    private Transform currentTarget = null;
    public void Atack()
    {
        currentTarget = MeleeAttackSystem.GetAttackTarget(
            transform,
            currentTarget,
            GetComponent<StatusController>().damage,
            GetComponent<StatusController>().targetLayer
        );

        if (currentTarget != null)
        {
            currentTarget.GetComponent<IAnimController>()?.GetDamage(GetComponent<StatusController>().damage);
        }
    }
}