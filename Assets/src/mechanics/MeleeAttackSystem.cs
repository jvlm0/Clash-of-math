using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Sistema de controle de ataque melee single target.
/// Mantém o alvo atual se ele estiver no range, caso contrário seleciona o mais próximo.
/// </summary>
public static class MeleeAttackSystem
{
    /// <summary>
    /// Determina o alvo do próximo ataque.
    /// </summary>
    /// <param name="attacker">Transform do atacante</param>
    /// <param name="currentTarget">Alvo atual (pode ser null)</param>
    /// <param name="attackRange">Range de ataque</param>
    /// <param name="targetLayer">Layer dos alvos (opcional)</param>
    /// <returns>Novo alvo ou null se não houver alvos disponíveis</returns>
    public static Transform GetAttackTarget(
        Transform attacker, 
        Transform currentTarget, 
        float attackRange, 
        LayerMask targetLayer = default)
    {
        if (attacker == null)
        {
            Debug.LogWarning("Attacker é null!");
            return null;
        }

        // Se há um alvo atual, verifica se ainda está no range
        if (currentTarget != null && IsTargetValid(currentTarget, attacker, attackRange))
        {
            return currentTarget;
        }

        // Caso contrário, busca o alvo mais próximo
        return FindClosestTarget(attacker, attackRange, targetLayer);
    }

    /// <summary>
    /// Verifica se o alvo ainda é válido (ativo e dentro do range).
    /// </summary>
    private static bool IsTargetValid(Transform target, Transform attacker, float range)
    {
        if (target == null || !target.gameObject.activeInHierarchy)
            return false;

        float distSqr = (target.position - attacker.position).sqrMagnitude;
        return distSqr <= range * range;
    }

    /// <summary>
    /// Encontra o alvo mais próximo dentro do range.
    /// </summary>
    private static Transform FindClosestTarget(Transform attacker, float range, LayerMask targetLayer)
    {
        Collider[] hits;

        if (targetLayer != default)
        {
            hits = Physics.OverlapSphere(attacker.position, range, targetLayer);
        }
        else
        {
            hits = Physics.OverlapSphere(attacker.position, range);
        }

        Transform closest = null;
        float closestDistSqr = float.MaxValue;

        foreach (var hit in hits)
        {
            // Ignora o próprio atacante
            if (hit.transform == attacker || hit.transform.IsChildOf(attacker))
                continue;

            float distSqr = (hit.transform.position - attacker.position).sqrMagnitude;
            
            if (distSqr < closestDistSqr)
            {
                closestDistSqr = distSqr;
                closest = hit.transform;
            }
        }

        return closest;
    }

    /// <summary>
    /// Versão alternativa usando Tags para identificar alvos.
    /// </summary>
    public static Transform GetAttackTargetByTag(
        Transform attacker, 
        Transform currentTarget, 
        float attackRange, 
        string targetTag)
    {
        if (attacker == null)
        {
            Debug.LogWarning("Attacker é null!");
            return null;
        }

        // Se há um alvo atual com a tag correta, verifica se ainda está no range
        if (currentTarget != null && 
            currentTarget.CompareTag(targetTag) && 
            IsTargetValid(currentTarget, attacker, attackRange))
        {
            return currentTarget;
        }

        // Caso contrário, busca o alvo mais próximo com a tag
        return FindClosestTargetByTag(attacker, attackRange, targetTag);
    }

    /// <summary>
    /// Encontra o alvo mais próximo com uma tag específica.
    /// </summary>
    private static Transform FindClosestTargetByTag(Transform attacker, float range, string tag)
    {
        var hits = Physics.OverlapSphere(attacker.position, range);
        
        Transform closest = null;
        float closestDistSqr = float.MaxValue;

        foreach (var hit in hits)
        {
            if (!hit.CompareTag(tag))
                continue;

            if (hit.transform == attacker || hit.transform.IsChildOf(attacker))
                continue;

            float distSqr = (hit.transform.position - attacker.position).sqrMagnitude;
            
            if (distSqr < closestDistSqr)
            {
                closestDistSqr = distSqr;
                closest = hit.transform;
            }
        }

        return closest;
    }
}