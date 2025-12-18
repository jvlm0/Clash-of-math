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
    private float maxSegmentLength = 1f;

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

        int pointCount = resolution + 1;
        float xRange = currentXMax - currentXMin;

        if (Mathf.Abs(xRange) < 0.001f)
        {
            meshFilter.mesh = mesh;
            return;
        }

        float xStep = xRange / resolution;

        List<ValidPoint> allPoints = new List<ValidPoint>();

        for (int i = 0; i < pointCount; i++)
        {
            float x = currentXMin + i * xStep;
            float z = CalculateFunction(x);
            bool isValid = IsValidValue(z);
            allPoints.Add(new ValidPoint(x, z, isValid));
        }

        meshSegments = ProcessPointsWithDiscontinuities(allPoints);

        List<Vector3> verticesList = new List<Vector3>();
        List<Vector2> uvsList = new List<Vector2>();
        List<int> trianglesList = new List<int>();

        foreach (var segment in meshSegments)
        {
            if (segment.points.Count < 2)
                continue;

            if (segment.isDiscontinuity)
            {
                GenerateDiscontinuityMesh(segment, verticesList, uvsList, trianglesList);
            }
            else
            {
                GenerateSegmentMesh(
                    segment.points,
                    0,
                    segment.points.Count - 1,
                    verticesList,
                    uvsList,
                    trianglesList
                );
            }
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

                if (distance > discontinuityThreshold)
                {
                    if (drawDiscontinuities)
                    {
                        float searchRange = (points[i].x - lastPoint.x) / 4f;

                        ValidPoint refinedLastPoint = lastPoint;
                        ValidPoint refinedNextPoint = points[i];

                        // Refina o último ponto válido antes da descontinuidade
                        // CORREÇÃO: Adiciona verificação de limite
                        for (float offset = 0.01f; offset < searchRange; offset += 0.01f)
                        {
                            float testX = lastPoint.x + offset;
                            float testZ = CalculateFunction(testX);
                            
                            if (IsValidValue(testZ) && Mathf.Abs(testZ) <= discontinuityLimit)
                            {
                                Vector3 testP = new Vector3(testX, 0, testZ);
                                if (Vector3.Distance(p1, testP) < discontinuityThreshold)
                                {
                                    refinedLastPoint = new ValidPoint(testX, testZ, true);
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

                        // Refina o primeiro ponto válido depois da descontinuidade
                        // CORREÇÃO: Adiciona verificação de limite
                        for (float offset = 0.01f; offset < searchRange; offset += 0.01f)
                        {
                            float testX = points[i].x - offset;
                            float testZ = CalculateFunction(testX);
                            
                            if (IsValidValue(testZ) && Mathf.Abs(testZ) <= discontinuityLimit)
                            {
                                Vector3 testP = new Vector3(testX, 0, testZ);
                                if (Vector3.Distance(p2, testP) < discontinuityThreshold)
                                {
                                    refinedNextPoint = new ValidPoint(testX, testZ, true);
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

                        // CORREÇÃO: Clamp dos valores finais
                        refinedLastPoint = new ValidPoint(
                            refinedLastPoint.x,
                            Mathf.Clamp(refinedLastPoint.z, -discontinuityLimit, discontinuityLimit),
                            refinedLastPoint.isValid
                        );
                        
                        refinedNextPoint = new ValidPoint(
                            refinedNextPoint.x,
                            Mathf.Clamp(refinedNextPoint.z, -discontinuityLimit, discontinuityLimit),
                            refinedNextPoint.isValid
                        );

                        if (refinedLastPoint.x != lastPoint.x)
                        {
                            currentSegment.points[currentSegment.points.Count - 1] = refinedLastPoint;
                        }

                        segments.Add(currentSegment);

                        bool oppositeSides = (refinedLastPoint.z > 0) != (refinedNextPoint.z > 0);

                        if (oppositeSides)
                        {
                            bool goingUp = refinedLastPoint.z > 0;
                            bool comingFromUp = refinedNextPoint.z > 0;

                            MeshSegment discSegment = new MeshSegment();
                            discSegment.isDiscontinuity = true;

                            float zLimit1 = goingUp ? discontinuityLimit : -discontinuityLimit;

                            discSegment.points.Add(new ValidPoint(refinedLastPoint.x, refinedLastPoint.z, true));
                            discSegment.points.Add(new ValidPoint(refinedLastPoint.x, zLimit1, true));

                            segments.Add(discSegment);

                            MeshSegment discSegment2 = new MeshSegment();
                            discSegment2.isDiscontinuity = true;

                            float zLimit2 = comingFromUp ? discontinuityLimit : -discontinuityLimit;

                            discSegment2.points.Add(new ValidPoint(refinedNextPoint.x, zLimit2, true));
                            discSegment2.points.Add(new ValidPoint(refinedNextPoint.x, refinedNextPoint.z, true));

                            segments.Add(discSegment2);
                        }
                        else
                        {
                            MeshSegment discSegment = new MeshSegment();
                            discSegment.isDiscontinuity = true;

                            float midX = (refinedLastPoint.x + refinedNextPoint.x) / 2f;

                            discSegment.points.Add(new ValidPoint(midX, refinedLastPoint.z, true));
                            discSegment.points.Add(new ValidPoint(midX, refinedNextPoint.z, true));

                            segments.Add(discSegment);
                        }

                        currentSegment = new MeshSegment();

                        if (refinedNextPoint.x != points[i].x)
                        {
                            currentSegment.points.Add(refinedNextPoint);
                        }
                        currentSegment.points.Add(points[i]);
                    }
                    else
                    {
                        segments.Add(currentSegment);
                        currentSegment = new MeshSegment();
                        currentSegment.points.Add(points[i]);
                    }
                }
                else
                {
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
                        }
                    }

                    currentSegment.points.Add(points[i]);
                }
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

    void GenerateDiscontinuityMesh(
        MeshSegment segment,
        List<Vector3> vertices,
        List<Vector2> uvs,
        List<int> triangles
    )
    {
        if (segment.points.Count < 2)
            return;

        int baseVertexIndex = vertices.Count;

        Vector3 start = new Vector3(segment.points[0].x, 0, segment.points[0].z);
        Vector3 end = new Vector3(
            segment.points[segment.points.Count - 1].x,
            0,
            segment.points[segment.points.Count - 1].z
        );

        Vector3 lineDirection = (end - start).normalized;
        bool needsReverse = lineDirection.z < 0;

        Vector3 perpendicular = new Vector3(1, 0, 0).normalized * lineThickness;

        int pointCount = segment.points.Count;

        for (int i = 0; i < pointCount; i++)
        {
            int idx = needsReverse ? (pointCount - 1 - i) : i;
            float x = segment.points[idx].x;
            float z = segment.points[idx].z;

            Vector3 centerPoint = new Vector3(x, 0, z);

            vertices.Add(centerPoint + perpendicular);
            vertices.Add(centerPoint - perpendicular);

            float uvY = (float)i / (pointCount - 1);
            uvs.Add(new Vector2(0, uvY));
            uvs.Add(new Vector2(1, uvY));
        }

        for (int i = 0; i < pointCount - 1; i++)
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

        foreach (var segment in meshSegments)
        {
            if (segment.points.Count < 2)
                continue;

            if (segment.isDiscontinuity)
            {
                GenerateDiscontinuityCollider(segment);
            }
            else
            {
                GenerateSegmentColliders(segment);
            }
        }
    }

    void GenerateDiscontinuityCollider(MeshSegment segment)
    {
        if (segment.points.Count < 2)
            return;

        Vector3 pos1 = new Vector3(segment.points[0].x, 0, segment.points[0].z);
        Vector3 pos2 = new Vector3(
            segment.points[segment.points.Count - 1].x,
            0,
            segment.points[segment.points.Count - 1].z
        );

        float length = Vector3.Distance(pos1, pos2);
        if (length < 0.01f)
            return;

        Vector3 center = (pos1 + pos2) / 2f;

        GameObject colliderObj = new GameObject($"DiscCollider_{colliderObjects.Count}");
        colliderObj.transform.parent = transform;
        colliderObj.transform.localPosition = center;
        colliderObj.transform.localScale = Vector3.one;
        colliderObj.layer = gameObject.layer;

        colliderObj.AddComponent<FunctionMeshCollisionDetector>();

        CapsuleCollider capsule = colliderObj.AddComponent<CapsuleCollider>();
        capsule.radius = lineThickness * 1.5f;
        capsule.height = length + lineThickness * 2f;
        capsule.direction = 2;
        capsule.isTrigger = true;

        Vector3 direction = (pos2 - pos1).normalized;
        if (direction != Vector3.zero)
        {
            colliderObj.transform.localRotation = Quaternion.LookRotation(direction);
        }

        colliderObjects.Add(colliderObj);
    }

    void GenerateSegmentColliders(MeshSegment segment)
    {
        if (segment.points.Count < 2)
            return;

        float totalLength = 0f;
        for (int i = 1; i < segment.points.Count; i++)
        {
            Vector3 p1 = new Vector3(segment.points[i - 1].x, 0, segment.points[i - 1].z);
            Vector3 p2 = new Vector3(segment.points[i].x, 0, segment.points[i].z);
            totalLength += Vector3.Distance(p1, p2);
        }

        int numColliders = Mathf.Max(1, Mathf.CeilToInt(totalLength / maxSegmentLength));
        numColliders = Mathf.Min(numColliders, segment.points.Count - 1);

        float step = (float)(segment.points.Count - 1) / numColliders;

        for (int i = 0; i < numColliders; i++)
        {
            int startIdx = Mathf.RoundToInt(i * step);
            int endIdx = Mathf.RoundToInt((i + 1) * step);

            if (
                startIdx != endIdx
                && startIdx < segment.points.Count
                && endIdx < segment.points.Count
            )
            {
                CreateCapsuleCollider(segment.points, startIdx, endIdx);
            }
        }
    }

    void CreateCapsuleCollider(List<ValidPoint> points, int startIdx, int endIdx)
    {
        Vector3 pos1 = new Vector3(points[startIdx].x, 0, points[startIdx].z);
        Vector3 pos2 = new Vector3(points[endIdx].x, 0, points[endIdx].z);

        float curveLength = 0f;
        Vector3 centerSum = Vector3.zero;
        int pointCount = 0;

        for (int i = startIdx; i <= endIdx; i++)
        {
            Vector3 p = new Vector3(points[i].x, 0, points[i].z);
            centerSum += p;
            pointCount++;

            if (i > startIdx)
            {
                Vector3 prevP = new Vector3(points[i - 1].x, 0, points[i - 1].z);
                curveLength += Vector3.Distance(prevP, p);
            }
        }

        Vector3 center = pointCount > 0 ? centerSum / pointCount : (pos1 + pos2) / 2f;
        float length = curveLength > 0 ? curveLength : Vector3.Distance(pos1, pos2);

        if (length < 0.01f)
            return;

        GameObject colliderObj = new GameObject($"Collider_{colliderObjects.Count}");
        colliderObj.transform.parent = transform;
        colliderObj.transform.localPosition = center;
        colliderObj.transform.localScale = Vector3.one;
        colliderObj.layer = gameObject.layer;

        colliderObj.AddComponent<FunctionMeshCollisionDetector>();

        CapsuleCollider capsule = colliderObj.AddComponent<CapsuleCollider>();
        capsule.radius = lineThickness * 1.5f;
        capsule.height = length + lineThickness * 3f;
        capsule.direction = 2;
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

            CapsuleCollider capsule = obj.GetComponent<CapsuleCollider>();
            if (capsule != null)
            {
                Gizmos.matrix = obj.transform.localToWorldMatrix;
                Gizmos.DrawWireSphere(Vector3.zero, capsule.radius);

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