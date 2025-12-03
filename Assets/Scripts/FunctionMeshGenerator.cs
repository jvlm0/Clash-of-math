using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class FunctionMeshGenerator : MonoBehaviour
{
    [Header("Configurações da Malha")]
    [SerializeField] private int resolution = 100; // Quantidade de pontos da função
    [SerializeField] private float xMin = -5f; // X mínimo
    [SerializeField] private float xMax = 5f; // X máximo
    [SerializeField] private float lineThickness = 0.15f; // Espessura da linha
    
    [Header("Parâmetros da Função")]
    [SerializeField] private float amplitude = 2f;
    [SerializeField] private float frequency = 1f;
    
    private MeshFilter meshFilter;
    private Mesh mesh;

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        
        // Cria um material padrão se não houver um
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer.sharedMaterial == null)
        {
            renderer.material = new Material(Shader.Find("Standard"));
            renderer.material.color = Color.cyan;
            Debug.Log("Material padrão criado automaticamente!");
        }
        
        GenerateMesh();
        Debug.Log($"Malha gerada com {mesh.vertexCount} vértices!");
    }

    void GenerateMesh()
    {
        mesh = new Mesh();
        mesh.name = "Function Graph";

        // Quantidade de pontos ao longo da função
        int pointCount = resolution + 1;
        float xRange = xMax - xMin;
        float xStep = xRange / resolution;

        // Cada ponto terá 2 vértices (esquerda e direita da linha)
        int vertexCount = pointCount * 2;
        Vector3[] vertices = new Vector3[vertexCount];
        Vector2[] uvs = new Vector2[vertexCount];

        // Gera os vértices ao longo da função
        for (int i = 0; i < pointCount; i++)
        {
            // Posição X ao longo da função
            float x = xMin + i * xStep;
            
            // Calcula Z (valor da função)
            float z = CalculateFunction(x);
            
            // Ponto central da função
            Vector3 centerPoint = new Vector3(x, 0, z);
            
            // Calcula a tangente (direção da curva)
            Vector3 forward;
            if (i < pointCount - 1)
            {
                float nextX = xMin + (i + 1) * xStep;
                float nextZ = CalculateFunction(nextX);
                forward = new Vector3(nextX - x, 0, nextZ - z).normalized;
            }
            else
            {
                float prevX = xMin + (i - 1) * xStep;
                float prevZ = CalculateFunction(prevX);
                forward = new Vector3(x - prevX, 0, z - prevZ).normalized;
            }
            
            // Calcula a perpendicular no plano XZ (espessura da linha)
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized * lineThickness;
            
            // Vértices da esquerda e direita
            vertices[i * 2] = centerPoint + right;      // Lado direito
            vertices[i * 2 + 1] = centerPoint - right;  // Lado esquerdo
            
            // UVs
            float uvX = (float)i / resolution;
            uvs[i * 2] = new Vector2(uvX, 1);
            uvs[i * 2 + 1] = new Vector2(uvX, 0);
        }

        // Gera triângulos
        int triangleCount = resolution * 6;
        int[] triangles = new int[triangleCount];
        
        int t = 0;
        for (int i = 0; i < resolution; i++)
        {
            int v = i * 2;
            
            // Primeiro triângulo do quad
            triangles[t++] = v;
            triangles[t++] = v + 1;
            triangles[t++] = v + 2;
            
            // Segundo triângulo do quad
            triangles[t++] = v + 1;
            triangles[t++] = v + 3;
            triangles[t++] = v + 2;
        }

        // Atribui ao mesh
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        meshFilter.mesh = mesh;
    }

    // ============================================
    // DEFINA SUA FUNÇÃO AQUI!
    // ============================================
    // x é a coordenada no eixo X (controlada por xMin e xMax)
    // O valor retornado é a posição no eixo Z
    // 
    // A função será desenhada no plano XZ (chão)
    // com Y = 0 (altura constante)
    // ============================================
    
    float CalculateFunction(float x)
    {
        // ===== FUNÇÕES BÁSICAS (descomente a que quiser usar) =====
        
        // Função linear: f(x) = x
        // return x;
        
        // Função linear com multiplicador: f(x) = 2x
        // return 2 * x;
        
        // Função quadrática: f(x) = x²
        //return x * x;
        
        // Função quadrática com coeficiente: f(x) = 0.5x²
        // return 0.5f * x * x;
        
        // Função cúbica: f(x) = x³
        // return x * x * x;
        
        // Parábola invertida: f(x) = -x²
        // return -x * x;
        
        // ===== FUNÇÕES TRIGONOMÉTRICAS =====
        
        // Onda senoidal: f(x) = sin(x)
         return Mathf.Sin(x * frequency) * amplitude * x*x;
        
        // Onda cosseno: f(x) = cos(x)
        // return Mathf.Cos(x * frequency) * amplitude;
        
        // Combinação de ondas: f(x) = sin(x) + sin(2x)
        // return Mathf.Sin(x * frequency) * amplitude + Mathf.Sin(x * frequency * 2) * amplitude * 0.5f;
        
        // ===== FUNÇÕES MAIS COMPLEXAS =====
        
        // Função exponencial (cuidado com valores grandes)
        // return Mathf.Exp(x * 0.1f) * 0.5f;
        
        // Função logarítmica (cuidado com x <= 0)
        // return x > 0 ? Mathf.Log(x + 1) * amplitude : 0;
        
        // Ondulação: f(x) = sin(x²)
        // return Mathf.Sin(x * x * 0.5f) * amplitude;
        
        // Gaussiana: f(x) = e^(-x²)
        // return Mathf.Exp(-x * x * 0.5f) * amplitude;
    }

    // Permite atualizar a malha em tempo de execução
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            GenerateMesh();
        }
    }

    // Visualiza a grade no editor
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