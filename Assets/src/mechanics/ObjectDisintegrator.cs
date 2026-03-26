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

    [Header("Grafo de Adjacência (Cache do Prefab)")]
    [SerializeField]
    private DisintegrationGraph graphCache;

    [Tooltip("Distância máxima entre bounds para considerar duas partes adjacentes")]
    [SerializeField]
    private float adjacencyThreshold = 0.05f;

    private List<GameObject> createdParts = new List<GameObject>();
    private bool isDisintegrated = false;

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

    void Awake()
    {
        if (graphCache != null && !graphCache.isBuilt)
        {
            BuildAdjacencyGraph();
        }
    }

    // ─────────────────────────────────────────────
    // CONSTRUÇÃO DO GRAFO (apenas na 1ª instância)
    // ─────────────────────────────────────────────

    private void BuildAdjacencyGraph()
    {
        List<PartData> parts = CollectAllPartData();
        int n = parts.Count;

        graphCache.Reset();
        for (int i = 0; i < n; i++)
            graphCache.adjacency.Add(new DisintegrationGraph.IntList());

        // Armazena também os centros mundo para usar no corte por distância
        for (int i = 0; i < n; i++)
        {
            Bounds bi = GetWorldBounds(parts[i]);
            for (int j = i + 1; j < n; j++)
            {
                Bounds bj = GetWorldBounds(parts[j]);
                Bounds biExpanded = bi;
                biExpanded.Expand(adjacencyThreshold);
                if (biExpanded.Intersects(bj))
                    graphCache.AddNeighbor(i, j);
            }
        }

        // Salva os centros mundo de cada part para usar no corte
        graphCache.partCenters = new List<Vector3>();
        foreach (PartData p in parts)
            graphCache.partCenters.Add(GetWorldBounds(p).center);

        graphCache.isBuilt = true;

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(graphCache);
#endif
        Debug.Log($"[DisintegrationGraph] Grafo construído com {n} nós.");
    }

    private Bounds GetWorldBounds(PartData data)
    {
        // Transforma o bounds local da mesh para o espaço mundo usando posição/escala/rotação da parte
        Bounds local = data.mesh.bounds;
        Vector3 worldCenter =
            data.position + data.rotation * Vector3.Scale(local.center, data.scale);
        Vector3 worldSize = new Vector3(
            local.size.x * data.scale.x,
            local.size.y * data.scale.y,
            local.size.z * data.scale.z
        );
        return new Bounds(worldCenter, worldSize);
    }

    // ─────────────────────────────────────────────
    // DESINTEGRAÇÃO
    // ─────────────────────────────────────────────

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

        List<PartData> allPartData = CollectAllPartData();

        if (allPartData.Count == 0)
        {
            Debug.LogWarning("Nenhuma Skinned Mesh ou Mesh encontrada!");
            return;
        }

        // Garante que o grafo está construído (segurança caso Awake não tenha rodado)
        if (graphCache != null && !graphCache.isBuilt)
            BuildAdjacencyGraph();

        DisableOriginalObject();

        if (numberOfParts <= 0 || numberOfParts >= allPartData.Count)
        {
            foreach (PartData data in allPartData)
                createdParts.Add(CreatePartFromData(data));
        }
        else
        {
            List<List<PartData>> groups = GroupPartData(allPartData, numberOfParts);
            foreach (List<PartData> group in groups)
                createdParts.Add(CreateGroupedPart(group));
        }

        ApplyPhysicsToAllParts();
        ApplyExplosionForce();

        isDisintegrated = true;

        if (autoDestroy)
            StartCoroutine(DestroyPartsAfterDelay());
    }

    // ─────────────────────────────────────────────
    // AGRUPAMENTO VIA BFS NO GRAFO DE ADJACÊNCIA
    // ─────────────────────────────────────────────

    private List<List<PartData>> GroupPartData(List<PartData> allData, int numberOfParts)
    {
        int n = allData.Count;

        // Copia grafo
        List<HashSet<int>> adj = new List<HashSet<int>>();
        for (int i = 0; i < n; i++)
        {
            adj.Add(new HashSet<int>());
            if (graphCache != null && i < graphCache.adjacency.Count)
                foreach (int nb in graphCache.GetNeighbors(i))
                    if (nb < n)
                        adj[i].Add(nb);
        }

        // Girvan-Newman: remove arestas de maior betweenness até ter numberOfParts componentes
        while (CountComponents(adj, n) < numberOfParts)
        {
            // Calcula betweenness de todas as arestas
            Dictionary<(int, int), float> betweenness = ComputeEdgeBetweenness(adj, n);

            if (betweenness.Count == 0)
                break;

            // Acha a aresta de maior betweenness
            float maxB = -1f;
            (int, int) bestEdge = (-1, -1);
            foreach (var kv in betweenness)
            {
                if (kv.Value > maxB)
                {
                    maxB = kv.Value;
                    bestEdge = kv.Key;
                }
            }

            if (bestEdge.Item1 == -1)
                break;

            adj[bestEdge.Item1].Remove(bestEdge.Item2);
            adj[bestEdge.Item2].Remove(bestEdge.Item1);
            Debug.Log(
                $"[Disintegrate] Cortou {allData[bestEdge.Item1].name} ↔ {allData[bestEdge.Item2].name} (betweenness {maxB:F2})"
            );
        }

        // BFS final
        bool[] visited = new bool[n];
        List<List<PartData>> groups = new List<List<PartData>>();

        for (int i = 0; i < n; i++)
        {
            if (visited[i])
                continue;
            List<PartData> group = new List<PartData>();
            Queue<int> queue = new Queue<int>();
            queue.Enqueue(i);
            visited[i] = true;
            while (queue.Count > 0)
            {
                int curr = queue.Dequeue();
                group.Add(allData[curr]);
                foreach (int nb in adj[curr])
                    if (!visited[nb])
                    {
                        visited[nb] = true;
                        queue.Enqueue(nb);
                    }
            }
            groups.Add(group);
        }

        Debug.Log($"[Disintegrate] {groups.Count} grupos após Girvan-Newman.");
        return groups;
    }

    private int CountComponents(List<HashSet<int>> adj, int n)
    {
        bool[] visited = new bool[n];
        int count = 0;
        for (int i = 0; i < n; i++)
        {
            if (visited[i])
                continue;
            count++;
            Queue<int> q = new Queue<int>();
            q.Enqueue(i);
            visited[i] = true;
            while (q.Count > 0)
            {
                int curr = q.Dequeue();
                foreach (int nb in adj[curr])
                    if (!visited[nb])
                    {
                        visited[nb] = true;
                        q.Enqueue(nb);
                    }
            }
        }
        return count;
    }

    private Dictionary<(int, int), float> ComputeEdgeBetweenness(List<HashSet<int>> adj, int n)
    {
        Dictionary<(int, int), float> betweenness = new Dictionary<(int, int), float>();

        // Inicializa todas as arestas com 0
        for (int i = 0; i < n; i++)
            foreach (int j in adj[i])
                if (j > i)
                    betweenness[(i, j)] = 0f;

        // BFS a partir de cada nó — algoritmo de Brandes
        for (int s = 0; s < n; s++)
        {
            Stack<int> stack = new Stack<int>();
            Dictionary<int, List<int>> predecessors = new Dictionary<int, List<int>>();
            float[] sigma = new float[n]; // número de caminhos mínimos passando por cada nó
            int[] dist = new int[n];

            for (int i = 0; i < n; i++)
            {
                sigma[i] = 0;
                dist[i] = -1;
                predecessors[i] = new List<int>();
            }
            sigma[s] = 1;
            dist[s] = 0;

            Queue<int> queue = new Queue<int>();
            queue.Enqueue(s);

            while (queue.Count > 0)
            {
                int v = queue.Dequeue();
                stack.Push(v);
                foreach (int w in adj[v])
                {
                    if (dist[w] < 0)
                    {
                        queue.Enqueue(w);
                        dist[w] = dist[v] + 1;
                    }
                    if (dist[w] == dist[v] + 1)
                    {
                        sigma[w] += sigma[v];
                        predecessors[w].Add(v);
                    }
                }
            }

            // Acumula dependências (back-propagation)
            float[] delta = new float[n];
            while (stack.Count > 0)
            {
                int w = stack.Pop();
                foreach (int v in predecessors[w])
                {
                    float contrib = (sigma[v] / sigma[w]) * (1f + delta[w]);
                    delta[v] += contrib;

                    // Acumula na aresta (v, w)
                    var edge = v < w ? (v, w) : (w, v);
                    if (betweenness.ContainsKey(edge))
                        betweenness[edge] += contrib;
                }
            }
        }

        return betweenness;
    }


    // ─────────────────────────────────────────────
    // COLETA DE DADOS
    // ─────────────────────────────────────────────

    private List<PartData> CollectAllPartData()
    {
        List<PartData> result = new List<PartData>();

        SkinnedMeshRenderer[] skinnedRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (SkinnedMeshRenderer smr in skinnedRenderers)
        {
            Mesh bakedMesh = new Mesh();
            smr.BakeMesh(bakedMesh);
            result.Add(
                new PartData
                {
                    name = smr.gameObject.name,
                    position = smr.transform.position,
                    rotation = smr.transform.rotation,
                    scale = smr.transform.lossyScale,
                    mesh = bakedMesh,
                    materials = smr.materials,
                    shadowCastingMode = smr.shadowCastingMode,
                    receiveShadows = smr.receiveShadows,
                }
            );
        }

        MeshRenderer[] normalRenderers = GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer mr in normalRenderers)
        {
            MeshFilter filter = mr.GetComponent<MeshFilter>();
            if (filter != null && filter.mesh != null)
            {
                result.Add(
                    new PartData
                    {
                        name = mr.gameObject.name,
                        position = mr.transform.position,
                        rotation = mr.transform.rotation,
                        scale = mr.transform.lossyScale,
                        mesh = filter.mesh,
                        materials = mr.materials,
                        shadowCastingMode = mr.shadowCastingMode,
                        receiveShadows = mr.receiveShadows,
                    }
                );
            }
        }

        return result;
    }

    // ─────────────────────────────────────────────
    // CRIAÇÃO DAS PARTES
    // ─────────────────────────────────────────────

    private GameObject CreatePartFromData(PartData data)
    {
        GameObject part = new GameObject(data.name + "_Part");
        part.transform.position = data.position;
        part.transform.rotation = data.rotation;
        part.transform.localScale = data.scale;

        part.AddComponent<MeshFilter>().mesh = data.mesh;

        MeshRenderer mr = part.AddComponent<MeshRenderer>();
        mr.materials = data.materials;
        mr.shadowCastingMode = data.shadowCastingMode;
        mr.receiveShadows = data.receiveShadows;

        return part;
    }

    private GameObject CreateGroupedPart(List<PartData> group)
    {
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
            GameObject child = new GameObject(data.name + "_Part");
            child.transform.SetParent(groupParent.transform, worldPositionStays: false);
            child.transform.position = data.position;
            child.transform.rotation = data.rotation;
            child.transform.localScale = data.scale;

            child.AddComponent<MeshFilter>().mesh = data.mesh;

            MeshRenderer mr = child.AddComponent<MeshRenderer>();
            mr.materials = data.materials;
            mr.shadowCastingMode = data.shadowCastingMode;
            mr.receiveShadows = data.receiveShadows;

            // Acumula bounds em espaço local do pai
            Vector3 meshSize = data.mesh.bounds.size;
            Vector3[] corners = new Vector3[8]
            {
                new Vector3(meshSize.x, meshSize.y, meshSize.z) * 0.5f,
                new Vector3(-meshSize.x, meshSize.y, meshSize.z) * 0.5f,
                new Vector3(meshSize.x, -meshSize.y, meshSize.z) * 0.5f,
                new Vector3(-meshSize.x, -meshSize.y, meshSize.z) * 0.5f,
                new Vector3(meshSize.x, meshSize.y, -meshSize.z) * 0.5f,
                new Vector3(-meshSize.x, meshSize.y, -meshSize.z) * 0.5f,
                new Vector3(meshSize.x, -meshSize.y, -meshSize.z) * 0.5f,
                new Vector3(-meshSize.x, -meshSize.y, -meshSize.z) * 0.5f,
            };

            foreach (Vector3 corner in corners)
            {
                Vector3 worldCorner = child.transform.TransformPoint(
                    data.mesh.bounds.center + corner
                );
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

        BoxCollider box = groupParent.AddComponent<BoxCollider>();
        box.center = combinedBounds.center;
        box.size = combinedBounds.size;

        return groupParent;
    }

    // ─────────────────────────────────────────────
    // FÍSICA
    // ─────────────────────────────────────────────

    private void ApplyPhysicsToAllParts()
    {
        foreach (GameObject part in createdParts)
        {
            if (part.GetComponent<Rigidbody>() == null)
            {
                Rigidbody rb = part.AddComponent<Rigidbody>();
                rb.mass = mass;
                rb.drag = drag;
                rb.angularDrag = angularDrag;
                rb.useGravity = true;
            }

            if (part.GetComponent<Collider>() == null)
            {
                MeshFilter mf = part.GetComponent<MeshFilter>();
                BoxCollider box = part.AddComponent<BoxCollider>();
                if (mf != null)
                {
                    box.center = mf.mesh.bounds.center;
                    box.size = mf.mesh.bounds.size;
                }
            }
        }
    }

    private void ApplyExplosionForce()
    {
        Vector3 explosionCenter = transform.position;
        foreach (GameObject part in createdParts)
        {
            Rigidbody rb = part.GetComponent<Rigidbody>();
            if (rb == null)
                continue;

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

    private void DisableOriginalObject()
    {
        foreach (SkinnedMeshRenderer smr in GetComponentsInChildren<SkinnedMeshRenderer>())
            smr.enabled = false;
        foreach (MeshRenderer mr in GetComponentsInChildren<MeshRenderer>())
            mr.enabled = false;
    }

    // ─────────────────────────────────────────────
    // UTILITÁRIOS
    // ─────────────────────────────────────────────

    private IEnumerator DestroyPartsAfterDelay()
    {
        yield return new WaitForSeconds(destroyAfterSeconds);
        foreach (GameObject part in createdParts)
            if (part != null)
                Destroy(part);
        Destroy(gameObject);
    }

    public void DestroyParts()
    {
        foreach (GameObject part in createdParts)
            if (part != null)
                Destroy(part);
        Destroy(gameObject);
    }

    public void RestoreOriginal()
    {
        if (!isDisintegrated)
            return;

        foreach (GameObject part in createdParts)
            if (part != null)
                Destroy(part);
        createdParts.Clear();

        foreach (SkinnedMeshRenderer smr in GetComponentsInChildren<SkinnedMeshRenderer>())
            smr.enabled = true;
        foreach (MeshRenderer mr in GetComponentsInChildren<MeshRenderer>())
            mr.enabled = true;

        isDisintegrated = false;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && !isDisintegrated)
            Disintegrate();
        if (Input.GetKeyDown(KeyCode.R) && isDisintegrated && !autoDestroy)
            RestoreOriginal();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
