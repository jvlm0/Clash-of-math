using UnityEngine;

public class ShotgunController : MonoBehaviour
{
    [Header("Configurações de Disparo")]
    [Tooltip("Prefab do projétil a ser disparado")]
    public GameObject projectilePrefab;
    
    [Tooltip("Ponto de origem do disparo")]
    public Transform firePoint;
    
    [Header("Configurações da Rajada")]
    [Tooltip("Número de projéteis por disparo")]
    [Range(1, 30)]
    public int projectileCount = 8;
    
    [Tooltip("Ângulo de abertura do leque (em graus)")]
    [Range(0f, 180f)]
    public float spreadAngle = 30f;
    
    [Header("Configurações dos Projéteis")]
    [Tooltip("Velocidade inicial dos projéteis")]
    public float projectileSpeed = 20f;
    
    [Tooltip("Distância máxima antes do projétil ser destruído")]
    public float maxDistance = 50f;
    
    [Tooltip("Tempo de vida do projétil (segundos)")]
    public float projectileLifetime = 5f;
    
    [Header("Configurações Opcionais")]
    [Tooltip("Adicionar pequena variação aleatória ao spread")]
    public bool randomSpread = false;
    
    [Range(0f, 10f)]
    public float randomSpreadAmount = 2f;
    
    [Tooltip("Força de recuo ao disparar")]
    public float recoilForce = 5f;
    
    [Header("Efeitos Visuais/Sonoros")]
    public ParticleSystem muzzleFlash;
    public AudioClip shootSound;
    
    private AudioSource audioSource;
    private Rigidbody rb;

    void Start()
    {
        // Configura componentes
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && shootSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        rb = GetComponent<Rigidbody>();
        
        // Validações
        if (firePoint == null)
        {
            Debug.LogWarning("FirePoint não configurado! Usando a posição do objeto.");
            firePoint = transform;
        }
        
        if (projectilePrefab == null)
        {
            Debug.LogError("Prefab do projétil não configurado!");
        }
    }

    void Update()
    {
        // Dispara ao pressionar o botão de fogo (pode customizar)
        //if (Input.GetButtonDown("Fire1"))
        //{
        //    Shoot();
        //}
    }

    /// <summary>
    /// Dispara uma rajada de projéteis em padrão de leque
    /// </summary>
    public void Shoot()
    {
        if (projectilePrefab == null)
        {
            Debug.LogError("Não é possível disparar sem um prefab de projétil!");
            return;
        }

        // Efeitos visuais e sonoros
        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
        }
        
        if (audioSource != null && shootSound != null)
        {
            audioSource.PlayOneShot(shootSound);
        }

        // Calcula o ângulo entre cada projétil
        float angleStep = spreadAngle / (projectileCount - 1);
        float startAngle = -spreadAngle / 2f;

        // Dispara cada projétil
        for (int i = 0; i < projectileCount; i++)
        {
            // Calcula o ângulo atual
            float currentAngle = startAngle + (angleStep * i);
            
            // Adiciona variação aleatória se habilitado
            if (randomSpread)
            {
                currentAngle += Random.Range(-randomSpreadAmount, randomSpreadAmount);
            }

            // Calcula a direção do projétil
            Quaternion rotation = firePoint.rotation * Quaternion.Euler(0f, currentAngle, 0f);
            Vector3 direction = rotation * Vector3.forward;

            // Instancia o projétil
            GameObject projectile = Instantiate(projectilePrefab, firePoint.position, rotation);
            
            // Configura o componente Rigidbody do projétil
            Rigidbody projectileRb = projectile.GetComponent<Rigidbody>();
            if (projectileRb != null)
            {
                projectileRb.velocity = direction * projectileSpeed;
            }
            else
            {
                Debug.LogWarning("Projétil sem Rigidbody! Adicionando componente.");
                projectileRb = projectile.AddComponent<Rigidbody>();
                projectileRb.velocity = direction * projectileSpeed;
            }

            // Adiciona o componente de projétil para controlar distância
            ProjectileBehavior behavior = projectile.GetComponent<ProjectileBehavior>();
            if (behavior == null)
            {
                behavior = projectile.AddComponent<ProjectileBehavior>();
            }

            behavior.Init(firePoint.transform.position, maxDistance);
        
        }

        // Aplica recuo ao jogador (se tiver Rigidbody)
        if (rb != null)
        {
            rb.AddForce(-firePoint.forward * recoilForce, ForceMode.Impulse);
        }
    }

    // Visualização no Editor
    void OnDrawGizmosSelected()
    {
        if (firePoint == null) return;

        Gizmos.color = Color.yellow;
        
        // Desenha o cone de disparo
        float angleStep = spreadAngle / Mathf.Max(1, projectileCount - 1);
        float startAngle = -spreadAngle / 2f;

        Vector3 previousDirection = Quaternion.Euler(0f, startAngle, 0f) * firePoint.forward;
        
        for (int i = 0; i <= projectileCount; i++)
        {
            float currentAngle = startAngle + (angleStep * i);
            Vector3 direction = Quaternion.Euler(0f, currentAngle, 0f) * firePoint.forward;
            
            // Desenha linha para cada projétil
            Gizmos.DrawRay(firePoint.position, direction * 5f);
            
            // Conecta os pontos para formar o cone
            if (i > 0)
            {
                Vector3 point1 = firePoint.position + previousDirection * 5f;
                Vector3 point2 = firePoint.position + direction * 5f;
                Gizmos.DrawLine(point1, point2);
            }
            
            previousDirection = direction;
        }

        // Desenha a distância máxima
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(firePoint.position, maxDistance);
    }
}

/// <summary>
/// Componente auxiliar para controlar o comportamento individual dos projéteis
/// </summary>
public class ProjectileBehavior : MonoBehaviour
{
    [Header("Configurações de Dano")]
    public int damage = 10;

    [Header("Efeitos de Impacto")]
    public GameObject impactEffect;

    [HideInInspector] public float maxDistance = 6f;
    [HideInInspector] public float lifetime;

    private float distanceTraveled = 0f;
    private Vector3 lastPosition;
    private bool initialized = false;

    // Chamado pelo ShotgunController logo apos AddComponent, antes do primeiro Update
    public void Init(Vector3 spawnPosition, float maxDist)
    {
        lastPosition = spawnPosition;
        maxDistance = maxDist;
        initialized = true;
    }

    void Update()
    {
        if (!initialized) return;
        distanceTraveled += Vector3.Distance(lastPosition, transform.position);
        lastPosition = transform.position;

        if (distanceTraveled >= maxDistance)
        {
            Debug.Log("distancia para destruir "+distanceTraveled);
            DestroyProjectile();
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        collision.gameObject.GetComponent<IAnimController>()?.GetDamage(damage);

        if (impactEffect != null)
        {
            GameObject effect = Instantiate(impactEffect, transform.position, Quaternion.LookRotation(collision.contacts[0].normal));
            Destroy(effect, 2f);
        }

        DestroyProjectile();
    }

    void DestroyProjectile()
    {
        Destroy(gameObject, .2f);
    }
}

/// <summary>
/// Interface para objetos que podem receber dano
/// </summary>
public interface IDamageable
{
    void TakeDamage(int damage);
}