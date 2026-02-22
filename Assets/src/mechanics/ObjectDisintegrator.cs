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
    /// Desintegra o objeto convertendo Skinned Meshes em Mesh Renderers independentes.
    /// Se numberOfParts for menor que o total de renderers, agrupa os renderers em grupos,
    /// cada grupo formando uma parte com um único Rigidbody e um único Collider combinado.
    /// </summary>
    public void Disintegrate(int numberOfParts = -1)
    {
        GetComponent<CapsuleCollider>().enabled = false;
        GetComponent<MeleeAtack>().enabled = false;
        GetComponent<NpcController>().enabled = false;
        GetComponent<Animator>().enabled = false;
        if (isDisintegrated)
        {
            Debug.LogWarning("Objeto já foi desintegrado!");
            return;
        }

        // Coleta todos os renderers convertidos como pares (GameObject temporário com mesh + materiais)
        List<PartData> allPartData = CollectAllPartData();

        if (allPartData.Count == 0)
        {
            Debug.LogWarning("Nenhuma Skinned Mesh ou Mesh encontrada!");
            return;
        }

        // Desativa o objeto original
        DisableOriginalObject();

        // Se numberOfParts for inválido ou >= total de renderers, comportamento original (1 part por renderer)
        if (numberOfParts <= 0 || numberOfParts >= allPartData.Count)
        {
            foreach (PartData data in allPartData)
            {
                GameObject part = CreatePartFromData(data);
                createdParts.Add(part);
            }
        }
        else
        {
            // Agrupa os renderers em numberOfParts grupos
            List<List<PartData>> groups = GroupPartData(allPartData, numberOfParts);

            foreach (List<PartData> group in groups)
            {
                GameObject part = CreateGroupedPart(group);
                createdParts.Add(part);
            }
        }

        // Aplica física em cada parte
        ApplyPhysicsToAllParts();

        // Aplica força de explosão
        ApplyExplosionForce();

        isDisintegrated = true;

        if (autoDestroy)
        {
            StartCoroutine(DestroyPartsAfterDelay());
        }
    }

    /// <summary>
    /// Estrutura auxiliar para carregar os dados de cada renderer antes de criar os GameObjects finais
    /// </summary>
    private struct PartData
    {
        public string name;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public Mesh mesh;
        public Material[] materials;
        public UnityEngine.Rendering.ShadowCastingMode shadowCastingMode;
        public bool receiveShadows;
    }

    /// <summary>
    /// Coleta dados de todos os SkinnedMeshRenderers e MeshRenderers do objeto
    /// </summary>
    private List<PartData> CollectAllPartData()
    {
        List<PartData> result = new List<PartData>();

        // Skinned Mesh Renderers
        SkinnedMeshRenderer[] skinnedRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (SkinnedMeshRenderer skinnedRenderer in skinnedRenderers)
        {
            Mesh bakedMesh = new Mesh();
            skinnedRenderer.BakeMesh(bakedMesh);

            result.Add(new PartData
            {
                name = skinnedRenderer.gameObject.name,
                position = skinnedRenderer.transform.position,
                rotation = skinnedRenderer.transform.rotation,
                scale = skinnedRenderer.transform.lossyScale,
                mesh = bakedMesh,
                materials = skinnedRenderer.materials,
                shadowCastingMode = skinnedRenderer.shadowCastingMode,
                receiveShadows = skinnedRenderer.receiveShadows
            });
        }

        // Mesh Renderers normais
        MeshRenderer[] normalRenderers = GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer renderer in normalRenderers)
        {
            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            if (filter != null && filter.mesh != null)
            {
                result.Add(new PartData
                {
                    name = renderer.gameObject.name,
                    position = renderer.transform.position,
                    rotation = renderer.transform.rotation,
                    scale = renderer.transform.lossyScale,
                    mesh = filter.mesh,
                    materials = renderer.materials,
                    shadowCastingMode = renderer.shadowCastingMode,
                    receiveShadows = renderer.receiveShadows
                });
            }
        }

        Debug.Log($"Coletados {result.Count} renderers para desintegrar");
        return result;
    }

    /// <summary>
    /// Cria um GameObject independente a partir de um único PartData (comportamento original)
    /// </summary>
    private GameObject CreatePartFromData(PartData data)
    {
        GameObject part = new GameObject(data.name + "_Part");
        part.transform.position = data.position;
        part.transform.rotation = data.rotation;
        part.transform.localScale = data.scale;

        MeshFilter meshFilter = part.AddComponent<MeshFilter>();
        meshFilter.mesh = data.mesh;

        MeshRenderer meshRenderer = part.AddComponent<MeshRenderer>();
        meshRenderer.materials = data.materials;
        meshRenderer.shadowCastingMode = data.shadowCastingMode;
        meshRenderer.receiveShadows = data.receiveShadows;

        return part;
    }

    /// <summary>
    /// Agrupa uma lista de PartData em N grupos distribuídos o mais uniformemente possível
    /// </summary>
    private List<List<PartData>> GroupPartData(List<PartData> allData, int numberOfGroups)
    {
        List<List<PartData>> groups = new List<List<PartData>>();
        for (int i = 0; i < numberOfGroups; i++)
            groups.Add(new List<PartData>());

        for (int i = 0; i < allData.Count; i++)
            groups[i % numberOfGroups].Add(allData[i]);

        return groups;
    }

    /// <summary>
    /// Cria um único GameObject para um grupo de PartData.
    /// Cada PartData do grupo vira um filho com seu próprio MeshRenderer,
    /// e o pai recebe um único Rigidbody e um BoxCollider que engloba todos os bounds combinados.
    /// </summary>
    private GameObject CreateGroupedPart(List<PartData> group)
    {
        // Calcula o centro do grupo para posicionar o pai
        Vector3 groupCenter = Vector3.zero;
        foreach (PartData data in group)
            groupCenter += data.position;
        groupCenter /= group.Count;

        GameObject groupParent = new GameObject("GroupedPart");
        groupParent.transform.position = groupCenter;
        groupParent.transform.rotation = Quaternion.identity;

        Bounds combinedBounds = new Bounds(Vector3.zero, Vector3.zero);
        bool boundsInitialized = false;

        foreach (PartData data in group)
        {
            // Cria filho com o mesh visual
            GameObject child = new GameObject(data.name + "_Part");
            child.transform.SetParent(groupParent.transform, worldPositionStays: false);
            child.transform.position = data.position;
            child.transform.rotation = data.rotation;
            child.transform.localScale = data.scale;

            MeshFilter meshFilter = child.AddComponent<MeshFilter>();
            meshFilter.mesh = data.mesh;

            MeshRenderer meshRenderer = child.AddComponent<MeshRenderer>();
            meshRenderer.materials = data.materials;
            meshRenderer.shadowCastingMode = data.shadowCastingMode;
            meshRenderer.receiveShadows = data.receiveShadows;

            // Acumula os bounds em espaço local do pai para o collider combinado
            Bounds meshBounds = data.mesh.bounds;
            // Transforma os 8 cantos do bounds do filho para o espaço local do pai
            Vector3 localMeshCenter = groupParent.transform.InverseTransformPoint(
                child.transform.TransformPoint(meshBounds.center)
            );

            Vector3 size = meshBounds.size;
            Vector3[] corners = new Vector3[8]
            {
                new Vector3( size.x,  size.y,  size.z) * 0.5f,
                new Vector3(-size.x,  size.y,  size.z) * 0.5f,
                new Vector3( size.x, -size.y,  size.z) * 0.5f,
                new Vector3(-size.x, -size.y,  size.z) * 0.5f,
                new Vector3( size.x,  size.y, -size.z) * 0.5f,
                new Vector3(-size.x,  size.y, -size.z) * 0.5f,
                new Vector3( size.x, -size.y, -size.z) * 0.5f,
                new Vector3(-size.x, -size.y, -size.z) * 0.5f,
            };

            foreach (Vector3 corner in corners)
            {
                Vector3 worldCorner = child.transform.TransformPoint(meshBounds.center + corner);
                Vector3 localCorner = groupParent.transform.InverseTransformPoint(worldCorner);

                if (!boundsInitialized)
                {
                    combinedBounds = new Bounds(localCorner, Vector3.zero);
                    boundsInitialized = true;
                }
                else
                {
                    combinedBounds.Encapsulate(localCorner);
                }
            }
        }

        // Adiciona o BoxCollider combinado no pai
        BoxCollider box = groupParent.AddComponent<BoxCollider>();
        box.center = combinedBounds.center;
        box.size = combinedBounds.size;

        return groupParent;
    }

    /// <summary>
    /// Desativa todos os renderers do objeto original
    /// </summary>
    private void DisableOriginalObject()
    {
        SkinnedMeshRenderer[] skinnedRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (SkinnedMeshRenderer renderer in skinnedRenderers)
            renderer.enabled = false;

        MeshRenderer[] meshRenderers = GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer renderer in meshRenderers)
            renderer.enabled = false;
    }

    /// <summary>
    /// Adiciona Rigidbody em cada parte criada (o collider já foi adicionado no momento da criação)
    /// </summary>
    private void ApplyPhysicsToAllParts()
    {
        foreach (GameObject part in createdParts)
        {
            // Só adiciona Rigidbody se ainda não tiver (CreateGroupedPart não adiciona Rigidbody)
            if (part.GetComponent<Rigidbody>() == null)
            {
                Rigidbody rb = part.AddComponent<Rigidbody>();
                rb.mass = mass;
                rb.drag = drag;
                rb.angularDrag = angularDrag;
                rb.useGravity = true;
            }

            // Para partes não agrupadas (sem filhos), garante que tenha collider
            if (part.GetComponent<Collider>() == null)
            {
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
                    part.AddComponent<BoxCollider>();
                }
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
                rb.AddExplosionForce(
                    explosionForce,
                    explosionCenter,
                    explosionRadius,
                    upwardModifier,
                    forceMode
                );

                Vector3 randomTorque =
                    new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f))
                    * explosionForce
                    * 0.1f;

                rb.AddTorque(randomTorque, forceMode);
            }
        }
    }

    private IEnumerator DestroyPartsAfterDelay()
    {
        yield return new WaitForSeconds(destroyAfterSeconds);

        foreach (GameObject part in createdParts)
        {
            if (part != null)
                Destroy(part);
        }

        Destroy(gameObject);
    }

    public void DestroyParts()
    {
        foreach (GameObject part in createdParts)
        {
            if (part != null)
                Destroy(part);
        }

        Destroy(gameObject);
    }

    public void RestoreOriginal()
    {
        if (!isDisintegrated)
            return;

        foreach (GameObject part in createdParts)
        {
            if (part != null)
                Destroy(part);
        }
        createdParts.Clear();

        SkinnedMeshRenderer[] skinnedRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (SkinnedMeshRenderer renderer in skinnedRenderers)
            renderer.enabled = true;

        MeshRenderer[] meshRenderers = GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer renderer in meshRenderers)
            renderer.enabled = true;

        isDisintegrated = false;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && !isDisintegrated)
        {
            Disintegrate(); // sem argumento = comportamento original
        }

        if (Input.GetKeyDown(KeyCode.R) && isDisintegrated && !autoDestroy)
        {
            RestoreOriginal();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}