using UnityEngine;

public class MeleeAtack : MonoBehaviour, IAtackHandler
{
    public bool rotate = false;
    private Transform currentTarget = null;

    private float nextAttackTime;

    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    public Transform canAttack()
    {
        if (Time.time < nextAttackTime)
        {   
            if (gameObject.CompareTag("Player"))
                Debug.Log("Não ataca pq está em cooldown");
            return null;
        }
           

        currentTarget = MeleeAttackSystem.GetAttackTarget(
            transform,
            currentTarget,
            GetComponent<StatusController>().attackRange,
            GetComponent<StatusController>().targetLayer
        );

        if (currentTarget == null)
        {
            Debug.Log("Nenhum alvo disponível para ataque");
        }

        return currentTarget;
    }

    public void Atack()
    {
        if (currentTarget != null)
        {
            GetComponent<IAnimController>()?.Attack();

            float animLength = animator.GetCurrentAnimatorStateInfo(0).length;
            float normalizedTime = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;

            float remainingTime = animLength * (1f - normalizedTime);

            nextAttackTime = Time.time + remainingTime;
        }
    }

    public void OnHitFrame()
    {
        currentTarget
            .GetComponent<IAnimController>()
            ?.GetDamage(GetComponent<StatusController>().damage);
    }
}
