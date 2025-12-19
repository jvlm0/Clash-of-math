using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class FunctionMeshGenerator : MonoBehaviour
{
    [Header("Configurações da Malha")]
    [SerializeField]
    private int resolution = 100;

    [SerializeField]
    private float xMin = -5f;

    [SerializeField]
    private float xMax = 5f;

    [SerializeField]
    private float lineThickness = 0.15f;

    [SerializeField]
    private bool useZClamp = false;

    [SerializeField]
    private float zMin = -100f;

    [SerializeField]
    private float zMax = 100f;

    [SerializeField]
    [Tooltip("Comprimento máximo para subdividir segmentos longos")]
    private float maxSegmentLength = 1f;

    [SerializeField]
    [Tooltip("Se um segmento for maior que este valor, ele será pulado (descontinuidade)")]
    private float maxDrawSegmentLength = 10f;

    [SerializeField]
    private float discontinuityThreshold = 5f;

    [Header("Descontinuidades")]
    [SerializeField]
    private bool drawDiscontinuities = true;

    [SerializeField]
    private float discontinuityLimit = 50f;

    [Header("Função Matemática")]
    [SerializeField]
    [TextArea(2, 5)]
    public string mathExpression = "tan(x)";

    [Header("Animação de Propagação")]
    [SerializeField]
    private bool animateOnStart = true;

    [SerializeField]
    private float propagationSpeed = 2f;

    [SerializeField]
    private AnimationDirection direction = AnimationDirection.FromCenter;

    [Header("Configurações de Collider")]
    [SerializeField]
    private bool enableColliders = true;

    [SerializeField]
    [Range(1, 100)]
    [Tooltip("Resolução dos colliders - quanto maior, mais precisos mas mais pesados (1 = baixa, 100 = alta)")]
    private int colliderResolution = 10;

    [SerializeField]
    [Tooltip("Multiplicador do raio do collider em relação à espessura da linha")]
    private float colliderRadiusMultiplier = 1.2f;

    [SerializeField]
    private bool showColliderGizmos = true;

    [SerializeField]
    [Tooltip("Atualizar colliders durante animação (pode causar lag)")]
    private bool updateCollidersWhileAnimating = false;

    public enum AnimationDirection
    {
        FromLeft,
        FromRight,
        FromCenter,
        ToBoth,
    }

    private MeshFilter meshFilter;
    private Mesh mesh;
    private MathExpressionParser parser;

    private List<GameObject> colliderObjects = new List<GameObject>();
    private List<MeshSegment> meshSegments = new List<MeshSegment>();

    private bool isAnimating = false;
    private float currentXMin;
    private float currentXMax;
    private float animationProgress = 0f;

    private struct ValidPoint
    {
        public float x;
        public float z;
        public bool isValid;

        public ValidPoint(float x, float z, bool isValid)
        {
            this.x = x;
            this.z = z;
            this.isValid = isValid;
        }
    }

    private class MeshSegment
    {
        public List<ValidPoint> points = new List<ValidPoint>();
        public bool isDiscontinuity = false;
    }

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();

        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer.sharedMaterial == null)
        {
            renderer.material = new Material(Shader.Find("Standard"));
            renderer.material.color = Color.cyan;
        }

        parser = new MathExpressionParser(mathExpression);

        if (animateOnStart)
        {
            StartPropagation();
        }
        else
        {
            currentXMin = xMin;
            currentXMax = xMax;
            GenerateMesh();
            Debug.Log($"Malha gerada com {mesh.vertexCount} vértices usando: {mathExpression}");
        }
    }

    public void StartPropagation()
    {
        isAnimating = true;
        animationProgress = 0f;

        switch (direction)
        {
            case AnimationDirection.FromLeft:
                currentXMin = xMin;
                currentXMax = xMin;
                break;

            case AnimationDirection.FromRight:
                currentXMin = xMax;
                currentXMax = xMax;
                break;

            case AnimationDirection.FromCenter:
            case AnimationDirection.ToBoth:
                float center = (xMin + xMax) / 2f;
                currentXMin = center;
                currentXMax = center;
                break;
        }

        Debug.Log($"Iniciando propagação da função: {mathExpression}");
    }

    public void ResetPropagation()
    {
        StartPropagation();
    }

    void GenerateMesh()
    {
        mesh = new Mesh();
        mesh.name = "Function Graph";

        float xRange = currentXMax - currentXMin;

        if (Mathf.Abs(xRange) < 0.001f)
        {
            meshFilter.mesh = mesh;
            return;
        }

        List<ValidPoint> allPoints = new List<ValidPoint>();

        // Calcula quantos múltiplos de pi cabem no intervalo
        float piStep = Mathf.PI / resolution; // Subdivide π em partes menores

        // Determina o primeiro e último múltiplo de pi no intervalo
        float firstMultiple = Mathf.Ceil(currentXMin / piStep) * piStep;
        float lastMultiple = Mathf.Floor(currentXMax / piStep) * piStep;

        // Gera pontos nos múltiplos de pi
        for (float x = firstMultiple; x <= lastMultiple; x += piStep)
        {
            // Garante que x está dentro do intervalo
            if (x >= currentXMin && x <= currentXMax)
            {
                float z = CalculateFunction(x);
                bool isValid = IsValidValue(z);
                allPoints.Add(new ValidPoint(x, z, isValid));
            }
        }

        // Adiciona pontos nas extremidades se necessário
        if (allPoints.Count == 0 || allPoints[0].x > currentXMin + 0.001f)
        {
            float z = CalculateFunction(currentXMin);
            bool isValid = IsValidValue(z);
            allPoints.Insert(0, new ValidPoint(currentXMin, z, isValid));
        }

        if (allPoints.Count == 0 || allPoints[allPoints.Count - 1].x < currentXMax - 0.001f)
        {
            float z = CalculateFunction(currentXMax);
            bool isValid = IsValidValue(z);
            allPoints.Add(new ValidPoint(currentXMax, z, isValid));
        }

        meshSegments = ProcessPointsWithDiscontinuities(allPoints);

        List<Vector3> verticesList = new List<Vector3>();
        List<Vector2> uvsList = new List<Vector2>();
        List<int> trianglesList = new List<int>();

        foreach (var segment in meshSegments)
        {
            if (segment.points.Count < 2)
                continue;

            GenerateSegmentMesh(
                segment.points,
                0,
                segment.points.Count - 1,
                verticesList,
                uvsList,
                trianglesList
            );
        }

        if (verticesList.Count > 0)
        {
            mesh.vertices = verticesList.ToArray();
            mesh.triangles = trianglesList.ToArray();
            mesh.uv = uvsList.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }

        meshFilter.mesh = mesh;

        // Só gera colliders se não estiver animando ou se updateCollidersWhileAnimating estiver ativo
        if (enableColliders && (!isAnimating || updateCollidersWhileAnimating))
        {
            GenerateColliders();
        }
    }

    List<MeshSegment> ProcessPointsWithDiscontinuities(List<ValidPoint> points)
    {
        List<MeshSegment> segments = new List<MeshSegment>();
        MeshSegment currentSegment = new MeshSegment();

        for (int i = 0; i < points.Count; i++)
        {
            if (!points[i].isValid)
            {
                if (currentSegment.points.Count > 0)
                {
                    segments.Add(currentSegment);
                    currentSegment = new MeshSegment();
                }
                continue;
            }

            if (currentSegment.points.Count > 0)
            {
                ValidPoint lastPoint = currentSegment.points[currentSegment.points.Count - 1];
                Vector3 p1 = new Vector3(lastPoint.x, 0, lastPoint.z);
                Vector3 p2 = new Vector3(points[i].x, 0, points[i].z);
                float distance = Vector3.Distance(p1, p2);

                // Se a distância exceder maxDrawSegmentLength, cria uma descontinuidade
                if (distance > maxDrawSegmentLength)
                {
                    // Finaliza o segmento atual
                    if (currentSegment.points.Count > 0)
                    {
                        segments.Add(currentSegment);
                    }

                    // Inicia novo segmento com o ponto atual
                    currentSegment = new MeshSegment();
                    currentSegment.points.Add(points[i]);
                    continue;
                }

                // Se a distância for maior que maxSegmentLength mas menor que maxDrawSegmentLength,
                // subdivide o segmento
                if (distance > maxSegmentLength)
                {
                    int subdivisions = Mathf.CeilToInt(distance / maxSegmentLength);

                    for (int j = 1; j < subdivisions; j++)
                    {
                        float t = (float)j / subdivisions;
                        float newX = Mathf.Lerp(lastPoint.x, points[i].x, t);
                        float newZ = CalculateFunction(newX);

                        if (IsValidValue(newZ))
                        {
                            currentSegment.points.Add(new ValidPoint(newX, newZ, true));
                        }
                        else
                        {
                            // Se encontrar valor inválido durante subdivisão, quebra o segmento
                            if (currentSegment.points.Count > 0)
                            {
                                segments.Add(currentSegment);
                                currentSegment = new MeshSegment();
                            }
                            break;
                        }
                    }
                }

                currentSegment.points.Add(points[i]);
            }
            else
            {
                currentSegment.points.Add(points[i]);
            }
        }

        if (currentSegment.points.Count > 0)
        {
            segments.Add(currentSegment);
        }

        return segments;
    }

    void GenerateSegmentMesh(
        List<ValidPoint> points,
        int startIdx,
        int endIdx,
        List<Vector3> vertices,
        List<Vector2> uvs,
        List<int> triangles
    )
    {
        int segmentLength = endIdx - startIdx + 1;
        int baseVertexIndex = vertices.Count;

        for (int i = 0; i < segmentLength; i++)
        {
            int pointIdx = startIdx + i;
            float x = points[pointIdx].x;
            float z = points[pointIdx].z;

            Vector3 centerPoint = new Vector3(x, 0, z);

            Vector3 forward;
            if (i < segmentLength - 1)
            {
                int nextIdx = pointIdx + 1;
                float nextX = points[nextIdx].x;
                float nextZ = points[nextIdx].z;
                forward = new Vector3(nextX - x, 0, nextZ - z).normalized;
            }
            else
            {
                int prevIdx = pointIdx - 1;
                float prevX = points[prevIdx].x;
                float prevZ = points[prevIdx].z;
                forward = new Vector3(x - prevX, 0, z - prevZ).normalized;
            }

            if (forward.magnitude < 0.001f)
            {
                forward = Vector3.forward;
            }

            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized * lineThickness;

            vertices.Add(centerPoint + right);
            vertices.Add(centerPoint - right);

            float uvX = (float)i / (segmentLength - 1);
            uvs.Add(new Vector2(uvX, 1));
            uvs.Add(new Vector2(uvX, 0));
        }

        for (int i = 0; i < segmentLength - 1; i++)
        {
            int v = baseVertexIndex + i * 2;

            triangles.Add(v);
            triangles.Add(v + 1);
            triangles.Add(v + 2);

            triangles.Add(v + 1);
            triangles.Add(v + 3);
            triangles.Add(v + 2);
        }
    }

    bool IsValidValue(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    void GenerateColliders()
    {
        ClearColliders();

        int totalColliders = 0;

        foreach (var segment in meshSegments)
        {
            if (segment.points.Count < 2)
                continue;
            totalColliders += GenerateSegmentColliders(segment);
        }

        Debug.Log($"Colliders gerados: {totalColliders} (Resolução: {colliderResolution})");
    }

    int GenerateSegmentColliders(MeshSegment segment)
    {
        if (segment.points.Count < 2)
            return 0;

        // Calcula quantos colliders criar baseado na resolução
        // colliderResolution = 1: 1 collider por segmento
        // colliderResolution = 100: número máximo de colliders (um para cada par de pontos)
        int maxPossibleColliders = segment.points.Count - 1;
        int numColliders = Mathf.Max(1, Mathf.RoundToInt(maxPossibleColliders * (colliderResolution / 100f)));
        
        // Garante pelo menos 1 collider
        numColliders = Mathf.Clamp(numColliders, 1, maxPossibleColliders);

        if (numColliders == 1)
        {
            // Cria um único collider para todo o segmento
            CreateBoxColliderForSegment(segment.points, 0, segment.points.Count - 1);
            return 1;
        }

        // Distribui os colliders uniformemente ao longo do segmento
        float step = (float)(segment.points.Count - 1) / numColliders;
        int collidersCreated = 0;

        for (int i = 0; i < numColliders; i++)
        {
            int startIdx = Mathf.RoundToInt(i * step);
            int endIdx = Mathf.RoundToInt((i + 1) * step);

            // Garante que o último collider chegue até o final
            if (i == numColliders - 1)
            {
                endIdx = segment.points.Count - 1;
            }

            if (startIdx < endIdx && startIdx < segment.points.Count && endIdx < segment.points.Count)
            {
                CreateBoxColliderForSegment(segment.points, startIdx, endIdx);
                collidersCreated++;
            }
        }

        return collidersCreated;
    }

    void CreateBoxColliderForSegment(List<ValidPoint> points, int startIdx, int endIdx)
    {
        if (startIdx >= endIdx)
            return;

        // Calcula o centro e dimensões do segmento
        Vector3 minBounds = new Vector3(float.MaxValue, 0, float.MaxValue);
        Vector3 maxBounds = new Vector3(float.MinValue, 0, float.MinValue);

        List<Vector3> segmentPoints = new List<Vector3>();

        for (int i = startIdx; i <= endIdx; i++)
        {
            Vector3 point = new Vector3(points[i].x, 0, points[i].z);
            segmentPoints.Add(point);

            minBounds.x = Mathf.Min(minBounds.x, point.x);
            minBounds.z = Mathf.Min(minBounds.z, point.z);
            maxBounds.x = Mathf.Max(maxBounds.x, point.x);
            maxBounds.z = Mathf.Max(maxBounds.z, point.z);
        }

        // Expande os bounds para incluir a espessura da linha
        float expansion = lineThickness * colliderRadiusMultiplier;
        minBounds.x -= expansion;
        minBounds.z -= expansion;
        maxBounds.x += expansion;
        maxBounds.z += expansion;

        Vector3 center = (minBounds + maxBounds) / 2f;
        Vector3 size = maxBounds - minBounds;

        // Garante tamanho mínimo
        size.x = Mathf.Max(size.x, lineThickness * 2f * colliderRadiusMultiplier);
        size.y = lineThickness * 2f * colliderRadiusMultiplier;
        size.z = Mathf.Max(size.z, lineThickness * 2f * colliderRadiusMultiplier);

        GameObject colliderObj = new GameObject($"Collider_{colliderObjects.Count}");
        colliderObj.transform.parent = transform;
        colliderObj.transform.localPosition = center;
        colliderObj.transform.localScale = Vector3.one;
        colliderObj.layer = gameObject.layer;

        colliderObj.AddComponent<FunctionMeshCollisionDetector>();

        BoxCollider box = colliderObj.AddComponent<BoxCollider>();
        box.size = size;
        box.isTrigger = true;

        // Calcula a rotação baseada na direção do segmento
        if (segmentPoints.Count >= 2)
        {
            Vector3 direction = (segmentPoints[segmentPoints.Count - 1] - segmentPoints[0]).normalized;
            if (direction != Vector3.zero)
            {
                colliderObj.transform.localRotation = Quaternion.LookRotation(direction);
            }
        }

        colliderObjects.Add(colliderObj);
    }

    void ClearColliders()
    {
        foreach (GameObject obj in colliderObjects)
        {
            if (obj != null)
            {
                if (Application.isPlaying)
                    Destroy(obj);
                else
                    DestroyImmediate(obj);
            }
        }
        colliderObjects.Clear();
    }

    float CalculateFunction(float x)
    {
        float z = parser.Evaluate(x);

        if (useZClamp && IsValidValue(z))
        {
            z = Mathf.Clamp(z, zMin, zMax);
        }

        return z;
    }

    void Update()
    {
        if (isAnimating)
        {
            animationProgress += Time.deltaTime * propagationSpeed;

            switch (direction)
            {
                case AnimationDirection.FromLeft:
                    currentXMax = Mathf.Lerp(xMin, xMax, animationProgress);
                    if (currentXMax >= xMax)
                    {
                        currentXMax = xMax;
                        isAnimating = false;
                        // Gera colliders finais após animação completa
                        if (enableColliders && !updateCollidersWhileAnimating)
                        {
                            GenerateColliders();
                        }
                        Debug.Log("Propagação completa!");
                    }
                    break;

                case AnimationDirection.FromRight:
                    currentXMin = Mathf.Lerp(xMax, xMin, animationProgress);
                    if (currentXMin <= xMin)
                    {
                        currentXMin = xMin;
                        isAnimating = false;
                        if (enableColliders && !updateCollidersWhileAnimating)
                        {
                            GenerateColliders();
                        }
                        Debug.Log("Propagação completa!");
                    }
                    break;

                case AnimationDirection.FromCenter:
                    float center = (xMin + xMax) / 2f;
                    float halfRange = (xMax - xMin) / 2f;
                    float expansion = halfRange * animationProgress;

                    currentXMin = center - expansion;
                    currentXMax = center + expansion;

                    if (currentXMin <= xMin && currentXMax >= xMax)
                    {
                        currentXMin = xMin;
                        currentXMax = xMax;
                        isAnimating = false;
                        if (enableColliders && !updateCollidersWhileAnimating)
                        {
                            GenerateColliders();
                        }
                        Debug.Log("Propagação completa!");
                    }
                    break;

                case AnimationDirection.ToBoth:
                    currentXMin = Mathf.Lerp((xMin + xMax) / 2f, xMin, animationProgress);
                    currentXMax = Mathf.Lerp((xMin + xMax) / 2f, xMax, animationProgress);

                    if (currentXMin <= xMin && currentXMax >= xMax)
                    {
                        currentXMin = xMin;
                        currentXMax = xMax;
                        isAnimating = false;
                        if (enableColliders && !updateCollidersWhileAnimating)
                        {
                            GenerateColliders();
                        }
                        Debug.Log("Propagação completa!");
                    }
                    break;
            }

            GenerateMesh();
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            parser = new MathExpressionParser(mathExpression);
            currentXMin = xMin;
            currentXMax = xMax;
            GenerateMesh();
            Debug.Log($"Malha atualizada com: {mathExpression}");
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetPropagation();
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            isAnimating = !isAnimating;
            Debug.Log(isAnimating ? "Animação retomada" : "Animação pausada");
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            enableColliders = !enableColliders;
            if (!enableColliders)
            {
                ClearColliders();
            }
            else
            {
                GenerateColliders();
            }
            Debug.Log($"Colliders: {(enableColliders ? "Ativados" : "Desativados")}");
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            drawDiscontinuities = !drawDiscontinuities;
            GenerateMesh();
            Debug.Log(
                $"Desenhar Descontinuidades: {(drawDiscontinuities ? "Ativado" : "Desativado")}"
            );
        }
    }

    void OnDrawGizmos()
    {
        if (!showColliderGizmos)
            return;

        Gizmos.color = new Color(0, 1, 0, 0.3f);

        foreach (GameObject obj in colliderObjects)
        {
            if (obj == null)
                continue;

            BoxCollider box = obj.GetComponent<BoxCollider>();
            if (box != null)
            {
                Gizmos.matrix = obj.transform.localToWorldMatrix;
                Gizmos.DrawWireCube(Vector3.zero, box.size);
            }
        }

        Gizmos.matrix = Matrix4x4.identity;
    }

    void OnDestroy()
    {
        ClearColliders();
    }
}