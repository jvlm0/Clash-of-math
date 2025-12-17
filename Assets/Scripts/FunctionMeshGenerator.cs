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
    private float maxSegmentLength = 1f; // Limita o comprimento máximo de cada segmento

    [SerializeField]
    private float discontinuityThreshold = 5f; // Detecta descontinuidades (assíntotas)

    [Header("Função Matemática")]
    [SerializeField]
    [TextArea(2, 5)]
    public string mathExpression = "sin(x)";

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
    private int colliderResolution = 20;

    [SerializeField]
    private bool showColliderGizmos = true;

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
    private List<ValidPoint> subdividedPoints = new List<ValidPoint>(); // Armazena pontos para colliders

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

        int pointCount = resolution + 1;
        float xRange = currentXMax - currentXMin;

        if (Mathf.Abs(xRange) < 0.001f)
        {
            meshFilter.mesh = mesh;
            return;
        }

        float xStep = xRange / resolution;

        // Calcula todos os pontos iniciais
        List<ValidPoint> allPoints = new List<ValidPoint>();

        for (int i = 0; i < pointCount; i++)
        {
            float x = currentXMin + i * xStep;
            float z = CalculateFunction(x);
            bool isValid = IsValidValue(z);
            allPoints.Add(new ValidPoint(x, z, isValid));
        }

        // Subdivide segmentos muito longos
        allPoints = SubdivideLongSegments(allPoints);
        subdividedPoints = allPoints; // Armazena para uso nos colliders

        List<Vector3> verticesList = new List<Vector3>();
        List<Vector2> uvsList = new List<Vector2>();
        List<int> trianglesList = new List<int>();

        // Processa pontos válidos em sequências contínuas
        for (int i = 0; i < allPoints.Count - 1; i++)
        {
            if (!allPoints[i].isValid || !allPoints[i + 1].isValid)
                continue;

            int startIdx = i;
            int endIdx = i;

            while (endIdx < allPoints.Count - 1 && allPoints[endIdx + 1].isValid)
            {
                endIdx++;
            }

            GenerateSegmentMesh(allPoints, startIdx, endIdx, verticesList, uvsList, trianglesList);

            i = endIdx;
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

        if (enableColliders)
        {
            GenerateColliders();
        }
    }

    List<ValidPoint> SubdivideLongSegments(List<ValidPoint> points)
    {
        List<ValidPoint> result = new List<ValidPoint>();

        for (int i = 0; i < points.Count; i++)
        {
            result.Add(points[i]);

            // Se não é o último ponto e ambos são válidos
            if (i < points.Count - 1 && points[i].isValid && points[i + 1].isValid)
            {
                Vector3 p1 = new Vector3(points[i].x, 0, points[i].z);
                Vector3 p2 = new Vector3(points[i + 1].x, 0, points[i + 1].z);
                float distance = Vector3.Distance(p1, p2);

                // Detecta descontinuidade (assíntota)
                if (distance > discontinuityThreshold)
                {
                    // Marca o ponto atual como fim de segmento
                    result[result.Count - 1] = new ValidPoint(points[i].x, points[i].z, false);
                    continue;
                }

                // Se o segmento é muito longo, subdivide
                if (distance > maxSegmentLength)
                {
                    int subdivisions = Mathf.CeilToInt(distance / maxSegmentLength);

                    for (int j = 1; j < subdivisions; j++)
                    {
                        float t = (float)j / subdivisions;
                        float newX = Mathf.Lerp(points[i].x, points[i + 1].x, t);
                        float newZ = CalculateFunction(newX);

                        if (IsValidValue(newZ))
                        {
                            // Verifica se não está criando uma descontinuidade
                            Vector3 lastPoint = new Vector3(
                                result[result.Count - 1].x,
                                0,
                                result[result.Count - 1].z
                            );
                            Vector3 newPoint = new Vector3(newX, 0, newZ);

                            if (Vector3.Distance(lastPoint, newPoint) < discontinuityThreshold)
                            {
                                result.Add(new ValidPoint(newX, newZ, true));
                            }
                            else
                            {
                                break;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
        }

        return result;
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

            // Garante que forward não é zero
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

        if (subdividedPoints.Count < 2)
            return;

        // Identifica segmentos contínuos (sem descontinuidades)
        List<List<int>> continuousSegments = new List<List<int>>();
        List<int> currentSegment = new List<int>();

        for (int i = 0; i < subdividedPoints.Count; i++)
        {
            if (subdividedPoints[i].isValid)
            {
                currentSegment.Add(i);
            }
            else
            {
                if (currentSegment.Count > 1)
                {
                    continuousSegments.Add(new List<int>(currentSegment));
                }
                currentSegment.Clear();
            }
        }

        // Adiciona o último segmento se existir
        if (currentSegment.Count > 1)
        {
            continuousSegments.Add(currentSegment);
        }

        // Cria colliders para cada segmento contínuo
        foreach (var segment in continuousSegments)
        {
            GenerateCollidersForSegment(segment);
        }
    }

    void GenerateCollidersForSegment(List<int> segmentIndices)
    {
        if (segmentIndices.Count < 2)
            return;

        // Calcula comprimento total do segmento
        float totalLength = 0f;
        for (int i = 1; i < segmentIndices.Count; i++)
        {
            int idx1 = segmentIndices[i - 1];
            int idx2 = segmentIndices[i];

            Vector3 p1 = new Vector3(subdividedPoints[idx1].x, 0, subdividedPoints[idx1].z);
            Vector3 p2 = new Vector3(subdividedPoints[idx2].x, 0, subdividedPoints[idx2].z);
            totalLength += Vector3.Distance(p1, p2);
        }

        // Calcula quantos colliders criar baseado no comprimento
        int numColliders = Mathf.Max(1, Mathf.CeilToInt(totalLength / maxSegmentLength));
        numColliders = Mathf.Min(numColliders, segmentIndices.Count - 1);

        // Divide os índices uniformemente
        float step = (float)(segmentIndices.Count - 1) / numColliders;

        for (int i = 0; i < numColliders; i++)
        {
            int startIdx = segmentIndices[Mathf.RoundToInt(i * step)];
            int endIdx = segmentIndices[Mathf.RoundToInt((i + 1) * step)];

            if (startIdx != endIdx)
            {
                CreateCapsuleCollider(startIdx, endIdx);
            }
        }
    }

    void CreateCapsuleCollider(int startIdx, int endIdx)
    {
        if (!subdividedPoints[startIdx].isValid || !subdividedPoints[endIdx].isValid)
            return;

        Vector3 pos1 = new Vector3(subdividedPoints[startIdx].x, 0, subdividedPoints[startIdx].z);
        Vector3 pos2 = new Vector3(subdividedPoints[endIdx].x, 0, subdividedPoints[endIdx].z);

        // Calcula o centro e comprimento real seguindo a curva
        float curveLength = 0f;
        Vector3 centerSum = Vector3.zero;
        int pointCount = 0;

        for (int i = startIdx; i <= endIdx; i++)
        {
            if (subdividedPoints[i].isValid)
            {
                Vector3 p = new Vector3(subdividedPoints[i].x, 0, subdividedPoints[i].z);
                centerSum += p;
                pointCount++;

                if (i > startIdx && subdividedPoints[i - 1].isValid)
                {
                    Vector3 prevP = new Vector3(
                        subdividedPoints[i - 1].x,
                        0,
                        subdividedPoints[i - 1].z
                    );
                    curveLength += Vector3.Distance(prevP, p);
                }
            }
        }

        Vector3 center = pointCount > 0 ? centerSum / pointCount : (pos1 + pos2) / 2f;
        float length = curveLength > 0 ? curveLength : Vector3.Distance(pos1, pos2);

        // Pula segmentos muito pequenos
        if (length < 0.01f)
            return;

        GameObject colliderObj = new GameObject($"Collider_{colliderObjects.Count}");
        colliderObj.transform.parent = transform;
        colliderObj.transform.localPosition = center;
        colliderObj.transform.localScale = Vector3.one;
        colliderObj.layer = gameObject.layer;

        colliderObj.AddComponent<FunctionMeshCollisionDetector>();

        CapsuleCollider capsule = colliderObj.AddComponent<CapsuleCollider>();
        capsule.radius = lineThickness * 1.5f; // Aumenta mais o raio para cobrir melhor
        capsule.height = length + lineThickness * 3f; // Aumenta a altura também
        capsule.direction = 2; // Z-axis
        capsule.isTrigger = true;

        Vector3 direction = (pos2 - pos1).normalized;
        if (direction != Vector3.zero)
        {
            colliderObj.transform.localRotation = Quaternion.LookRotation(direction);
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
                        Debug.Log("Propagação completa!");
                    }
                    break;

                case AnimationDirection.FromRight:
                    currentXMin = Mathf.Lerp(xMax, xMin, animationProgress);
                    if (currentXMin <= xMin)
                    {
                        currentXMin = xMin;
                        isAnimating = false;
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

            CapsuleCollider capsule = obj.GetComponent<CapsuleCollider>();
            if (capsule != null)
            {
                Gizmos.matrix = obj.transform.localToWorldMatrix;
                Gizmos.DrawWireSphere(Vector3.zero, capsule.radius);

                // Desenha a cápsula completa
                Vector3 point1 = Vector3.forward * (capsule.height / 2f - capsule.radius);
                Vector3 point2 = Vector3.back * (capsule.height / 2f - capsule.radius);
                Gizmos.DrawWireSphere(point1, capsule.radius);
                Gizmos.DrawWireSphere(point2, capsule.radius);
            }
        }

        Gizmos.matrix = Matrix4x4.identity;
    }

    void OnDestroy()
    {
        ClearColliders();
    }
}
