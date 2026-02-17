using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectDisintegrator : MonoBehaviour
{
    [Header("Configurações de Desintegração")]
    [SerializeField]
    private float explosionForce = 500f;

    [SerializeField]
    private float explosionRadius = 5f;

    [SerializeField]
    private float upwardModifier = 1f;

    [SerializeField]
    private ForceMode forceMode = ForceMode.Impulse;

    [Header("Configurações de Física")]
    [SerializeField]
    private float mass = 1f;

    [SerializeField]
    private float drag = 0.5f;

    [SerializeField]
    private float angularDrag = 0.5f;

    [Header("Configurações de Tempo")]
    [SerializeField]
    private float destroyAfterSeconds = 3f;

    [SerializeField]
    private bool autoDestroy = true;

    private List<GameObject> createdParts = new List<GameObject>();
    private bool isDisintegrated = false;

    /// <summary>
    /// Desintegra o objeto convertendo Skinned Meshes em Mesh Renderers independentes
    /// </summary>
    public void Disintegrate()
    {
        GetComponent<CapsuleCollider>().enabled = false;
        if (isDisintegrated)
        {
            Debug.LogWarning("Objeto já foi desintegrado!");
            return;
        }

        // Coleta e converte todas as Skinned Meshes
        ConvertSkinnedMeshesToParts();

        if (createdParts.Count == 0)
        {
            Debug.LogWarning("Nenhuma Skinned Mesh encontrada!");
            return;
        }

        // Desativa o objeto original
        DisableOriginalObject();

        // Aplica física em cada parte
        ApplyPhysicsToAllParts();

        // Aplica força de explosão
        ApplyExplosionForce();

        isDisintegrated = true;

        // Destrói as partes após um tempo
        if (autoDestroy)
        {
            StartCoroutine(DestroyPartsAfterDelay());
        }
    }

    /// <summary>
    /// Converte cada Skinned Mesh Renderer em um objeto independente com Mesh Renderer
    /// </summary>
    private void ConvertSkinnedMeshesToParts()
    {
        createdParts.Clear();

        // Busca todas as Skinned Mesh Renderers nos filhos
        SkinnedMeshRenderer[] skinnedRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();

        foreach (SkinnedMeshRenderer skinnedRenderer in skinnedRenderers)
        {
            // Cria um novo GameObject para esta parte
            GameObject part = new GameObject(skinnedRenderer.gameObject.name + "_Part");
            part.transform.position = skinnedRenderer.transform.position;
            part.transform.rotation = skinnedRenderer.transform.rotation;
            part.transform.localScale = skinnedRenderer.transform.lossyScale;

            // Adiciona MeshFilter e copia a malha "bakada"
            MeshFilter meshFilter = part.AddComponent<MeshFilter>();
            Mesh bakedMesh = new Mesh();
            skinnedRenderer.BakeMesh(bakedMesh);
            meshFilter.mesh = bakedMesh;

            // Adiciona MeshRenderer e copia os materiais
            MeshRenderer meshRenderer = part.AddComponent<MeshRenderer>();
            meshRenderer.materials = skinnedRenderer.materials;

            // Copia as propriedades de renderização
            meshRenderer.shadowCastingMode = skinnedRenderer.shadowCastingMode;
            meshRenderer.receiveShadows = skinnedRenderer.receiveShadows;

            createdParts.Add(part);
        }

        // Também processa Mesh Renderers normais se houver
        MeshRenderer[] normalRenderers = GetComponentsInChildren<MeshRenderer>();

        foreach (MeshRenderer renderer in normalRenderers)
        {
            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            if (filter != null && filter.mesh != null)
            {
                // Cria cópia do objeto com mesh normal
                GameObject part = new GameObject(renderer.gameObject.name + "_Part");
                part.transform.position = renderer.transform.position;
                part.transform.rotation = renderer.transform.rotation;
                part.transform.localScale = renderer.transform.lossyScale;

                MeshFilter meshFilter = part.AddComponent<MeshFilter>();
                meshFilter.mesh = filter.mesh;

                MeshRenderer meshRenderer = part.AddComponent<MeshRenderer>();
                meshRenderer.materials = renderer.materials;
                meshRenderer.shadowCastingMode = renderer.shadowCastingMode;
                meshRenderer.receiveShadows = renderer.receiveShadows;

                createdParts.Add(part);
            }
        }

        Debug.Log($"Criadas {createdParts.Count} partes para desintegrar");
    }

    /// <summary>
    /// Desativa todos os renderers do objeto original
    /// </summary>
    private void DisableOriginalObject()
    {
        // Desativa Skinned Mesh Renderers
        SkinnedMeshRenderer[] skinnedRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (SkinnedMeshRenderer renderer in skinnedRenderers)
        {
            renderer.enabled = false;
        }

        // Desativa Mesh Renderers
        MeshRenderer[] meshRenderers = GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer renderer in meshRenderers)
        {
            renderer.enabled = false;
        }
    }

    /// <summary>
    /// Adiciona Rigidbody e Collider em cada parte criada
    /// </summary>
    private void ApplyPhysicsToAllParts()
    {
        foreach (GameObject part in createdParts)
        {
            Rigidbody rb = part.AddComponent<Rigidbody>();
            rb.mass = mass;
            rb.drag = drag;
            rb.angularDrag = angularDrag;
            rb.useGravity = true;

            // Calcula os bounds da mesh para ajustar o collider
            MeshFilter meshFilter = part.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                Bounds bounds = meshFilter.mesh.bounds;

                BoxCollider box = part.AddComponent<BoxCollider>();
                box.center = bounds.center;
                box.size = bounds.size;
            }
            else
            {
                // Fallback se não encontrar mesh
                part.AddComponent<BoxCollider>();
            }
        }
    }

    /// <summary>
    /// Aplica força de explosão em todas as partes
    /// </summary>
    private void ApplyExplosionForce()
    {
        Vector3 explosionCenter = transform.position;

        foreach (GameObject part in createdParts)
        {
            Rigidbody rb = part.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Aplica força de explosão
                rb.AddExplosionForce(
                    explosionForce,
                    explosionCenter,
                    explosionRadius,
                    upwardModifier,
                    forceMode
                );

                // Adiciona torque aleatório para rotação
                Vector3 randomTorque =
                    new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f))
                    * explosionForce
                    * 0.1f;

                rb.AddTorque(randomTorque, forceMode);
            }
        }
    }

    /// <summary>
    /// Destrói todas as partes após um delay
    /// </summary>
    private IEnumerator DestroyPartsAfterDelay()
    {
        yield return new WaitForSeconds(destroyAfterSeconds);

        foreach (GameObject part in createdParts)
        {
            if (part != null)
            {
                Destroy(part);
            }
        }

        // Destrói o objeto pai também
        Destroy(gameObject);
    }

    /// <summary>
    /// Método público para destruir as partes manualmente
    /// </summary>
    public void DestroyParts()
    {
        foreach (GameObject part in createdParts)
        {
            if (part != null)
            {
                Destroy(part);
            }
        }

        Destroy(gameObject);
    }

    /// <summary>
    /// Restaura o objeto ao estado original (apenas se ainda não foi destruído)
    /// </summary>
    public void RestoreOriginal()
    {
        if (!isDisintegrated)
            return;

        // Destrói as partes criadas
        foreach (GameObject part in createdParts)
        {
            if (part != null)
            {
                Destroy(part);
            }
        }
        createdParts.Clear();

        // Reativa os renderers originais
        SkinnedMeshRenderer[] skinnedRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (SkinnedMeshRenderer renderer in skinnedRenderers)
        {
            renderer.enabled = true;
        }

        MeshRenderer[] meshRenderers = GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer renderer in meshRenderers)
        {
            renderer.enabled = true;
        }

        isDisintegrated = false;
    }

    // Método de teste - pressione 'D' para desintegrar
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && !isDisintegrated)
        {
            Disintegrate();
        }

        // Pressione 'R' para restaurar (se ainda não foi destruído)
        if (Input.GetKeyDown(KeyCode.R) && isDisintegrated && !autoDestroy)
        {
            RestoreOriginal();
        }
    }

    // Visualização no Editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
