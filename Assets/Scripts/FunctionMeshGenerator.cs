using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class FunctionMeshGenerator : MonoBehaviour
{
    [Header("Configurações da Malha")]
    [SerializeField] private int resolution = 100;
    [SerializeField] private float xMin = -5f;
    [SerializeField] private float xMax = 5f;
    [SerializeField] private float lineThickness = 0.15f;
    [SerializeField] private bool useZClamp = false;
    [SerializeField] private float zMin = -100f;
    [SerializeField] private float zMax = 100f;
    
    [Header("Função Matemática")]
    [SerializeField] [TextArea(2, 5)] 
    public string mathExpression = "sin(x)";
    
    [Header("Animação de Propagação")]
    [SerializeField] private bool animateOnStart = true;
    [SerializeField] private float propagationSpeed = 2f;
    [SerializeField] private AnimationDirection direction = AnimationDirection.FromCenter;
    
    [Header("Configurações de Collider")]
    [SerializeField] private bool enableColliders = true;
    [SerializeField] private ColliderType colliderType = ColliderType.BoxColliders;
    [SerializeField] private int colliderResolution = 20; // Menos colliders = melhor performance
    [SerializeField] private float colliderHeight = 0.5f;
    [SerializeField] private bool showColliderGizmos = true;
    
    public enum AnimationDirection
    {
        FromLeft,
        FromRight,
        FromCenter,
        ToBoth
    }
    
    public enum ColliderType
    {
        BoxColliders,      // Vários BoxColliders ao longo da curva (RÁPIDO)
        SphereColliders,   // Esferas ao longo da curva (MUITO RÁPIDO)
        CapsuleColliders,  // Cápsulas ao longo da curva (BALANCEADO)
        MeshCollider       // MeshCollider tradicional (PESADO, não recomendado)
    }
    
    private MeshFilter meshFilter;
    private Mesh mesh;
    private MathExpressionParser parser;
    
    // Colliders dinâmicos
    private List<GameObject> colliderObjects = new List<GameObject>();
    
    // Variáveis para animação
    private bool isAnimating = false;
    private float currentXMin;
    private float currentXMax;
    private float animationProgress = 0f;

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

        int vertexCount = pointCount * 2;
        Vector3[] vertices = new Vector3[vertexCount];
        Vector2[] uvs = new Vector2[vertexCount];

        for (int i = 0; i < pointCount; i++)
        {
            float x = currentXMin + i * xStep;
            float z = CalculateFunction(x);
            
            Vector3 centerPoint = new Vector3(x, 0, z);
            
            Vector3 forward;
            if (i < pointCount - 1)
            {
                float nextX = currentXMin + (i + 1) * xStep;
                float nextZ = CalculateFunction(nextX);
                forward = new Vector3(nextX - x, 0, nextZ - z).normalized;
            }
            else
            {
                float prevX = currentXMin + (i - 1) * xStep;
                float prevZ = CalculateFunction(prevX);
                forward = new Vector3(x - prevX, 0, z - prevZ).normalized;
            }
            
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized * lineThickness;
            
            vertices[i * 2] = centerPoint + right;
            vertices[i * 2 + 1] = centerPoint - right;
            
            float uvX = (float)i / resolution;
            uvs[i * 2] = new Vector2(uvX, 1);
            uvs[i * 2 + 1] = new Vector2(uvX, 0);
        }

        int triangleCount = resolution * 6;
        int[] triangles = new int[triangleCount];
        
        int t = 0;
        for (int i = 0; i < resolution; i++)
        {
            int v = i * 2;
            
            triangles[t++] = v;
            triangles[t++] = v + 1;
            triangles[t++] = v + 2;
            
            triangles[t++] = v + 1;
            triangles[t++] = v + 3;
            triangles[t++] = v + 2;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        meshFilter.mesh = mesh;
        
        // Gera colliders após criar a malha
        if (enableColliders)
        {
            GenerateColliders();
        }
    }

    void GenerateColliders()
    {
        // Remove colliders antigos
        ClearColliders();
        
        float xRange = currentXMax - currentXMin;
        if (Mathf.Abs(xRange) < 0.001f) return;
        
        // Usa menos colliders para melhor performance
        int segmentCount = Mathf.Min(colliderResolution, resolution);
        float xStep = xRange / segmentCount;
        
        switch (colliderType)
        {
            case ColliderType.BoxColliders:
                GenerateBoxColliders(segmentCount, xStep);
                break;
                
            case ColliderType.SphereColliders:
                GenerateSphereColliders(segmentCount, xStep);
                break;
                
            case ColliderType.CapsuleColliders:
                GenerateCapsuleColliders(segmentCount, xStep);
                break;
                
            case ColliderType.MeshCollider:
                GenerateMeshCollider();
                break;
        }
    }
    
    void GenerateBoxColliders(int segmentCount, float xStep)
    {
        for (int i = 0; i < segmentCount; i++)
        {
            float x1 = currentXMin + i * xStep;
            float x2 = currentXMin + (i + 1) * xStep;
            float z1 = CalculateFunction(x1);
            float z2 = CalculateFunction(x2);
            
            Vector3 pos1 = new Vector3(x1, 0, z1);
            Vector3 pos2 = new Vector3(x2, 0, z2);
            
            Vector3 center = (pos1 + pos2) / 2f;
            float length = Vector3.Distance(pos1, pos2);
            
            GameObject colliderObj = new GameObject($"Collider_{i}");
            colliderObj.transform.parent = transform;
            colliderObj.transform.localPosition = center;
            colliderObj.transform.localScale = Vector3.one; // Importante: reseta a escala local
            colliderObj.layer = gameObject.layer;
            
            BoxCollider box = colliderObj.AddComponent<BoxCollider>();
            box.size = new Vector3(lineThickness * 2f, colliderHeight, length);
            
            // Rotaciona para alinhar com a curva
            Vector3 direction = (pos2 - pos1).normalized;
            if (direction != Vector3.zero)
            {
                colliderObj.transform.localRotation = Quaternion.LookRotation(direction);
            }
            
            colliderObjects.Add(colliderObj);
        }
    }
    
    void GenerateSphereColliders(int segmentCount, float xStep)
    {
        for (int i = 0; i <= segmentCount; i++)
        {
            float x = currentXMin + i * xStep;
            float z = CalculateFunction(x);
            
            Vector3 position = new Vector3(x, 0, z);
            
            GameObject colliderObj = new GameObject($"Collider_{i}");
            colliderObj.transform.parent = transform;
            colliderObj.transform.localPosition = position;
            colliderObj.transform.localScale = Vector3.one;
            colliderObj.layer = gameObject.layer;

            SphereCollider sphere = colliderObj.AddComponent<SphereCollider>();
            sphere.radius = lineThickness;
            
            colliderObjects.Add(colliderObj);
        }
    }
    
    void GenerateCapsuleColliders(int segmentCount, float xStep)
    {
        for (int i = 0; i < segmentCount; i++)
        {
            float x1 = currentXMin + i * xStep;
            float x2 = currentXMin + (i + 1) * xStep;
            float z1 = CalculateFunction(x1);
            float z2 = CalculateFunction(x2);
            
            Vector3 pos1 = new Vector3(x1, 0, z1);
            Vector3 pos2 = new Vector3(x2, 0, z2);
            
            Vector3 center = (pos1 + pos2) / 2f;
            float length = Vector3.Distance(pos1, pos2);
            
            GameObject colliderObj = new GameObject($"Collider_{i}");
            colliderObj.transform.parent = transform;
            colliderObj.transform.localPosition = center;
            colliderObj.transform.localScale = Vector3.one;
            colliderObj.layer = gameObject.layer;

            colliderObj.AddComponent<FunctionMeshCollisionDetector>();
            
            CapsuleCollider capsule = colliderObj.AddComponent<CapsuleCollider>();
            capsule.radius = lineThickness;
            capsule.height = length + lineThickness * 2f;
            capsule.direction = 2; // Z-axis
            capsule.isTrigger = true;       
            
            // Rotaciona para alinhar com a curva
            Vector3 direction = (pos2 - pos1).normalized;
            if (direction != Vector3.zero)
            {
                colliderObj.transform.localRotation = Quaternion.LookRotation(direction);
            }
            
            colliderObjects.Add(colliderObj);
        }
    }
    
    void GenerateMeshCollider()
    {
        GameObject colliderObj = new GameObject("MeshCollider");
        colliderObj.transform.parent = transform;
        colliderObj.transform.localPosition = Vector3.zero;
        colliderObj.transform.localRotation = Quaternion.identity;
        colliderObj.layer = gameObject.layer;
        
        MeshCollider meshCollider = colliderObj.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;
        meshCollider.convex = false; // Para malhas não-convexas
        
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
        
        // Aplica clamp se ativado
        if (useZClamp)
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
        if (!showColliderGizmos) return;
        
        // Desenha os colliders em verde semi-transparente
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        
        foreach (GameObject obj in colliderObjects)
        {
            if (obj == null) continue;
            
            BoxCollider box = obj.GetComponent<BoxCollider>();
            if (box != null)
            {
                Gizmos.matrix = obj.transform.localToWorldMatrix;
                Gizmos.DrawCube(box.center, box.size);
            }
            
            SphereCollider sphere = obj.GetComponent<SphereCollider>();
            if (sphere != null)
            {
                Gizmos.DrawSphere(obj.transform.position, sphere.radius);
            }
            
            CapsuleCollider capsule = obj.GetComponent<CapsuleCollider>();
            if (capsule != null)
            {
                Gizmos.matrix = obj.transform.localToWorldMatrix;
                Gizmos.DrawSphere(Vector3.zero, capsule.radius);
            }
        }
        
        Gizmos.matrix = Matrix4x4.identity;
    }
    
    void OnDestroy()
    {
        ClearColliders();
    }
}