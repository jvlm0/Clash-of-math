using UnityEngine;

/// <summary>
/// Script simples para o prefab do projétil
/// Anexe este script a uma esfera ou outro objeto 3D para usar como projétil
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class SimpleProjectile : MonoBehaviour
{
    [Header("Configurações Visuais")]
    public TrailRenderer trail;
    public Light projectileLight;
    
    void Start()
    {
        // Configura o Rigidbody
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.useGravity = true; // Mude para false se quiser projéteis retos
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        
        // Configura trail se existir
        if (trail != null)
        {
            trail.time = 0.5f;
            trail.startWidth = 0.1f;
            trail.endWidth = 0.01f;
        }
    }
}

/// <summary>
/// Exemplo de alvo que recebe dano
/// </summary>
public class Target : MonoBehaviour, IDamageable
{
    [Header("Configurações de Vida")]
    public int maxHealth = 100;
    private int currentHealth;
    
    [Header("Feedback Visual")]
    public Color damageColor = Color.red;
    public float damageFlashDuration = 0.1f;
    
    private Renderer targetRenderer;
    private Color originalColor;

    void Start()
    {
        currentHealth = maxHealth;
        targetRenderer = GetComponent<Renderer>();
        
        if (targetRenderer != null)
        {
            originalColor = targetRenderer.material.color;
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        
        Debug.Log($"{gameObject.name} recebeu {damage} de dano. Vida: {currentHealth}/{maxHealth}");
        
        // Feedback visual
        if (targetRenderer != null)
        {
            StartCoroutine(DamageFlash());
        }
        
        // Verifica se morreu
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    System.Collections.IEnumerator DamageFlash()
    {
        targetRenderer.material.color = damageColor;
        yield return new WaitForSeconds(damageFlashDuration);
        targetRenderer.material.color = originalColor;
    }

    void Die()
    {
        Debug.Log($"{gameObject.name} foi destruído!");
        // Adicione efeitos de explosão, pontos, etc.
        Destroy(gameObject);
    }
}