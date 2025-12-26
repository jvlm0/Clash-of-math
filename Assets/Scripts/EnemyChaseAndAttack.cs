using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class EnemyChaseAndAttack : MonoBehaviour
{
    [Header("Detecção")]
    public List<Transform> enemyList = new List<Transform>();
    //public float detectionRadius = 15f;
    public float targetUpdateRate = 0.3f;

    [Header("Ataque")]
    public float attackRange = 1.5f;
    public float attackCooldown = 1.2f;

    private NavMeshAgent agent;
    private Transform currentTarget;
    private float updateTimer;
    private float attackTimer;

    private NpcController npcController;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.stoppingDistance = attackRange;
        npcController = GetComponent<NpcController>();

        agent.speed = GetComponent<StatusController>().speed;
    }

    void Update()
    {
        updateTimer += Time.deltaTime;
        attackTimer += Time.deltaTime;

        // Atualiza alvo mais próximo em intervalos
        if (updateTimer >= targetUpdateRate)
        {
            updateTimer = 0f;
            FindNearestEnemy();
        }

        if (currentTarget == null)
        {
            agent.isStopped = true;
            agent.ResetPath();
            return;
        }

        // Calcula distância real até o alvo
        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);

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
                Attack();
                Debug.Log("Npc atacando");
            }
        }
        else
        {
            // Persegue o alvo
            agent.isStopped = false;
            agent.SetDestination(currentTarget.position);
            npcController.Run();

            Debug.Log($"Npc correndo - Distância: {distanceToTarget:F2}");
        }
    }

    void FindNearestEnemy()
    {
        // Remove inimigos nulos da lista (caso tenham sido destruídos)
        enemyList.RemoveAll(enemy => enemy == null);

        if (enemyList.Count == 0)
        {
            currentTarget = null;
            return;
        }

        float minDistance = Mathf.Infinity;
        Transform nearest = null;

        foreach (Transform enemy in enemyList)
        {
            float dist = Vector3.Distance(transform.position, enemy.position);

            // Verifica se está dentro do raio de detecção
            if (dist < minDistance)
            {
                minDistance = dist;
                nearest = enemy;
            }
        }

        currentTarget = nearest;
    }

    void Attack()
    {
        npcController.Attack();
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

    // Método público para adicionar inimigos dinamicamente
    public void AddEnemy(Transform enemy)
    {
        if (!enemyList.Contains(enemy))
        {
            enemyList.Add(enemy);
        }
    }

    // Método público para remover inimigos
    public void RemoveEnemy(Transform enemy)
    {
        enemyList.Remove(enemy);
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

        // Desenha linhas para cada inimigo na lista
        if (enemyList != null)
        {
            Gizmos.color = Color.cyan;
            foreach (Transform enemy in enemyList)
            {
                if (enemy != null)
                {
                    Gizmos.DrawLine(transform.position, enemy.position);
                }
            }
        }

        // Destaca o alvo atual
        if (currentTarget != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, currentTarget.position);
            Gizmos.DrawWireSphere(currentTarget.position, 0.5f);
        }
    }
}