
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public class RewriteProjectileBehavior : Editor
{
    [MenuItem("Tools/Shotgun/Rewrite ProjectileBehavior")]
    static void Rewrite()
    {
        string path = Application.dataPath + "/src/shotgun/ShotgunController.cs";
        string original = File.ReadAllText(path);

        // Novo bloco do ProjectileBehavior — usa Init() para evitar race condition do Awake
        string oldClass = original.Substring(original.IndexOf("public class ProjectileBehavior"));

        string newClass =
@"public class ProjectileBehavior : MonoBehaviour
{
    [Header(""Configurações de Dano"")]
    public int damage = 10;

    [Header(""Efeitos de Impacto"")]
    public GameObject impactEffect;

    [HideInInspector] public float maxDistance;
    [HideInInspector] public float lifetime;

    private float distanceTraveled = 0f;
    private Vector3 lastPosition;
    private bool initialized = false;

    // Chamado pelo ShotgunController logo apos AddComponent, antes do primeiro Update
    public void Init(Vector3 spawnPosition, float maxDist, float life)
    {
        lastPosition = spawnPosition;
        maxDistance = maxDist;
        lifetime = life;
        initialized = true;
    }

    void Update()
    {
        if (!initialized) return;

        distanceTraveled += Vector3.Distance(lastPosition, transform.position);
        lastPosition = transform.position;

        if (distanceTraveled >= maxDistance)
        {
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
        Destroy(gameObject, .1f);
    }
}

/// <summary>
/// Interface para objetos que podem receber dano
/// </summary>
public interface IDamageable
{
    void TakeDamage(int damage);
}";

        // Substitui a classe antiga pela nova
        int classStart = original.IndexOf("public class ProjectileBehavior");
        string newContent = original.Substring(0, classStart) + newClass;
        File.WriteAllText(path, newContent, System.Text.Encoding.UTF8);

        // Agora corrige o Shoot() para usar Init() em vez de atribuição direta
        string updated = File.ReadAllText(path);

        string oldInit =
@"            behavior.maxDistance = maxDistance;
            behavior.startPosition = firePoint.position;
            behavior.lifetime = projectileLifetime;";

        string newInit =
@"            behavior.Init(firePoint.position, maxDistance, projectileLifetime);";

        if (updated.Contains(oldInit))
        {
            updated = updated.Replace(oldInit, newInit);
            File.WriteAllText(path, updated, System.Text.Encoding.UTF8);
            Debug.Log("Init() substituído com sucesso.");
        }
        else
        {
            Debug.LogWarning("Trecho de atribuição não encontrado — verifique manualmente.");
        }

        AssetDatabase.ImportAsset("Assets/src/shotgun/ShotgunController.cs");
        AssetDatabase.Refresh();
        Debug.Log("✅ ProjectileBehavior reescrito com Init() — maxDistance agora funciona corretamente.");
    }
}
#endif
