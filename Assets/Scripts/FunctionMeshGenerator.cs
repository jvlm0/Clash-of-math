using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class FunctionMeshGenerator : MonoBehaviour
{
    [Header("Configurações da Malha")]
    [SerializeField] private int resolution = 100;
    [SerializeField] private float xMin = -5f;
    [SerializeField] private float xMax = 5f;
    [SerializeField] private float lineThickness = 0.15f;
    
    [Header("Função Matemática")]
    [SerializeField] [TextArea(2, 5)] 
    public string mathExpression = "sin(x)";
    
    [Header("Animação de Propagação")]
    [SerializeField] private bool animateOnStart = true;
    [SerializeField] private float propagationSpeed = 2f; // Velocidade de propagação
    [SerializeField] private AnimationDirection direction = AnimationDirection.FromCenter;
    
    public enum AnimationDirection
    {
        FromLeft,      // Propaga da esquerda para direita
        FromRight,     // Propaga da direita para esquerda
        FromCenter,    // Propaga do centro para as bordas
        ToBoth         // Propaga simultaneamente para ambos os lados
    }
    
    private MeshFilter meshFilter;
    private Mesh mesh;
    private MathExpressionParser parser;
    
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
        
        // Inicializa baseado na direção
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

        // Usa currentXMin e currentXMax para animação
        int pointCount = resolution + 1;
        float xRange = currentXMax - currentXMin;
        
        // Evita divisão por zero
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
    }

    float CalculateFunction(float x)
    {
        return parser.Evaluate(x);
    }

    void Update()
    {
        // Atualiza animação de propagação
        if (isAnimating)
        {
            animationProgress += Time.deltaTime * propagationSpeed;
            
            // Calcula os limites baseado na direção
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
        
        // Teclas de controle
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
    }

    void OnDrawGizmos()
    {
        if (mesh == null) return;
        
        Gizmos.color = Color.cyan;
        Vector3[] vertices = mesh.vertices;
        
        for (int i = 0; i < vertices.Length; i++)
        {
            Gizmos.DrawSphere(transform.TransformPoint(vertices[i]), 0.02f);
        }
    }
}