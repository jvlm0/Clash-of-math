using UnityEngine;

public class MeleeAtack : MonoBehaviour, IAtackHandler
{
    public bool rotate = false;
    private Transform currentTarget = null;
    

    public Transform canAttack()
    {
        currentTarget = MeleeAttackSystem.GetAttackTarget(
            transform,
            currentTarget,
            GetComponent<StatusController>().attackRange,
            GetComponent<StatusController>().targetLayer
        );

        return currentTarget;
    }

    public void Atack()
    {
        
        if (currentTarget != null)
        {
            GetComponent<IAnimController>()?.Attack();
            

            if (rotate)
            {
                Vector3 direction = (currentTarget.position - transform.position).normalized;
                direction.y = 0; // Mantém a rotação apenas no eixo Y
                if (direction != Vector3.zero)
                {
                    Quaternion lookRotation = Quaternion.LookRotation(direction);
                    transform.rotation = lookRotation;
                }
            }
        }
    }

    public void OnHitFrame()
    {
        currentTarget.GetComponent<IAnimController>()?.GetDamage(GetComponent<StatusController>().damage);
    }
}