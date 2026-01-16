using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyChaseAndAttack : MonoBehaviour
{
    [Header("Detecção")]
    public float detectionRadius = 50f;
    public float targetUpdateRate = 0.3f;

    [Header("Ataque")]
    private float attackRange;
    public float attackCooldown = 1.2f;

    private NavMeshAgent agent;
    private Transform currentTarget;
    private float updateTimer;
    private float attackTimer;

    private NpcController npcController;

    private float distanceToTarget;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        attackRange = GetComponent<StatusController>().attackRange;

        agent.stoppingDistance = attackRange;
        npcController = GetComponent<NpcController>();
        agent.stoppingDistance = attackRange;
        agent.speed = GetComponent<StatusController>().speed;
    }

    void Update()
    {

        if (!GameController.Instance.IsBattleStart)
        {
            return;
        }

        updateTimer += Time.deltaTime;
        attackTimer += Time.deltaTime;

        FindNearestEnemy();
        // Calcula distância real até o alvo
        if (currentTarget == null)
        {
            return;
        }

        distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);
        // Atualiza alvo mais próximo em intervalos
        if (updateTimer >= targetUpdateRate)
        {
            updateTimer = 0f;
            if (distanceToTarget > attackRange)
                FindNearestEnemy();
            else
            {
                currentTarget = GetComponent<MeleeAtack>().canAttack();
            }
        }

        if (currentTarget == null)
        {
            agent.isStopped = true;
            agent.ResetPath();
            return;
        }

        // Se está no range de ataque
        if (distanceToTarget <= attackRange)
        {
            agent.isStopped = true;
            agent.ResetPath(); // Limpa o path

            // Gira para o alvo
            FaceTarget();

            // Ataca se passou o cooldown
            if (attackTimer >= attackCooldown)
            {
                attackTimer = 0f;
                npcController.Attack();
                Debug.Log("Npc atacando");
            }
        }
        else
        {
            // Persegue o alvo
            agent.isStopped = false;
            agent.SetDestination(currentTarget.position);
            npcController.Run();

            //Debug.Log($"Npc correndo - Distância: {distanceToTarget:F2}");
        }
    }

    void FindNearestEnemy()
    {
        // Se já temos um alvo válido dentro do alcance, mantém ele
        if (currentTarget != null)
        {
            float distToCurrent = Vector3.Distance(transform.position, currentTarget.position);
            if (distToCurrent <= attackRange)
            {
                return;
            }
        }

        // Detecta todos os colliders na layer de inimigos dentro do raio
        Collider[] enemiesInRange = Physics.OverlapSphere(
            transform.position,
            detectionRadius,
            GetComponent<StatusController>().targetLayer
        );

        if (enemiesInRange.Length == 0)
        {
            currentTarget = null;
            return;
        }

        float minDistance = Mathf.Infinity;
        Transform nearest = null;

        foreach (Collider enemyCollider in enemiesInRange)
        {
            float dist = Vector3.Distance(transform.position, enemyCollider.transform.position);

            if (dist < minDistance)
            {
                minDistance = dist;
                nearest = enemyCollider.transform;
            }
        }

        currentTarget = nearest;
    }

    void FaceTarget()
    {
        Vector3 direction = (currentTarget.position - transform.position).normalized;
        direction.y = 0f;

        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                lookRotation,
                Time.deltaTime * 8f
            );
        }
    }

    public void FreezeStopNpc()
    {
        agent.isStopped = true;
        agent.speed = 0f;
    }

    public void ContinueUnfreezeNpc()
    {
        agent.isStopped = false;
        agent.speed = GetComponent<StatusController>().speed;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        //Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);


        // Destaca o alvo atual
        if (currentTarget != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, currentTarget.position);
            Gizmos.DrawWireSphere(currentTarget.position, 0.5f);
        }
    }
}
