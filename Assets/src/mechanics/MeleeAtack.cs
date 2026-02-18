using System.Collections;
using UnityEngine;

public class MeleeAtack : MonoBehaviour, IAtackHandler
{
    public bool rotate = false;
    private Transform currentTarget = null;

    public Transform attackArea;

    public float attackAreaRadius = .6f;

    private float nextAttackTime;

    private Animator animator;

    bool isAttacking = false;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    public Transform canAttack()
    {
        //Debug.Log($"Time.time {Time.time} < nextAttackTime {nextAttackTime}");
        if (Time.time < nextAttackTime || isAttacking)
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
        else
        {
            Atack();
        }

        return currentTarget;
    }

    public void Atack()
    {
        if (currentTarget != null)
        {
            GetComponent<IAnimController>()?.Attack();
            StartCoroutine(SetNextAttackTimeAfterTransition());
            isAttacking = true;
        }
    }

    private IEnumerator SetNextAttackTimeAfterTransition()
    {
        // Aguarda a transição para o próximo estado
        yield return null; // Espera 1 frame

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        

        float animLength = stateInfo.length;
        float remainingTime = animLength / GetComponent<StatusController>().attackSpeed;

        nextAttackTime = Time.time + remainingTime;
        isAttacking = false;
    }

    public void OnHitFrame()
    {
        if (!GetComponent<StatusController>().attackInArea && currentTarget != null)
            currentTarget
                .GetComponent<IAnimController>()
                ?.GetDamage(GetComponent<StatusController>().damage);
        else
        {
            if (attackArea != null && currentTarget != null)
            {
                DetectEnemiesInArea(currentTarget);
            }
        }
    }

    private void DetectEnemiesInArea(Transform target)
    {
        Collider[] hits = Physics.OverlapSphere(
            target.transform.position,
            attackAreaRadius,
            GetComponent<StatusController>().targetLayer
        );

        foreach (var hit in hits)
        {
            Debug.Log("Inimigo atingido na área: " + hit.name);
            IAnimController enemy = hit.GetComponent<IAnimController>();
            if (enemy != null)
            {
                enemy.GetDamage(GetComponent<StatusController>().damage);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (attackArea != null)
        {
            // Define a cor do Gizmo (vermelho semi-transparente)
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);

            // Desenha a esfera preenchida
            Gizmos.DrawSphere(attackArea.transform.position, attackAreaRadius);

            // Desenha o contorno da esfera (opcional, para melhor visualização)
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackArea.transform.position, attackAreaRadius);
        }
    }
}
