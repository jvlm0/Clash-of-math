using System;
using System.Collections.Generic;
using System.Data;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class FunctionCurveMesh : MonoBehaviour
{
    [Header("Orientação")]
    [Tooltip("Plano XZ (horizontal) ou XY (vertical)")]
    public bool verticalPlane = false;
    
    [Header("Função Matemática")]
    [Tooltip("Use 'x' como variável. Ex: 1/x, sin(x), x*x")]
    public string functionExpression = "1/x";
    
    [Header("Configurações da Curva")]
    [Range(10, 500)]
    public int resolution = 100;
    
    [Tooltip("Adiciona pontos extras onde a função varia muito")]
    public bool adaptiveSampling = true;
    
    [Range(0.1f, 5f)]
    [Tooltip("Distância máxima entre pontos consecutivos")]
    public float maxSegmentLength = 1f;
    
    public float xMin = -10f;
    public float xMax = 10f;
    
    [Range(0.01f, 1f)]
    public float lineWidth = 0.1f;
    
    [Header("Limites de Y")]
    public float yMin = -20f;
    public float yMax = 20f;
    public bool clampY = true;
    
    [Header("Material")]
    public Material curveMaterial;
    
    [Header("Colisão")]
    public bool addCollider = true;
    public bool isTrigger = false;
    
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;
    
    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        GenerateMesh();
        
        if (addCollider)
        {
            meshCollider = gameObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = meshFilter.mesh;
            meshCollider.convex = false;
            meshCollider.isTrigger = isTrigger;
        }
        
        if (curveMaterial != null)
        {
            GetComponent<MeshRenderer>().material = curveMaterial;
        }
    }
    
    void GenerateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "Function Curve";
        
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        
        // Calcula pontos da função
        List<Vector2> points = new List<Vector2>();
        float step = (xMax - xMin) / resolution;
        
        // Primeira passagem: pontos regulares
        List<Vector2> initialPoints = new List<Vector2>();
        for (int i = 0; i <= resolution; i++)
        {
            float x = xMin + i * step;
            float y = EvaluateFunction(x);
            
            // Clamp Y se necessário
            if (clampY)
            {
                y = Mathf.Clamp(y, yMin, yMax);
            }
            
            // Pula valores inválidos (NaN, Infinity)
            if (float.IsNaN(y) || float.IsInfinity(y))
            {
                continue;
            }
            
            initialPoints.Add(new Vector2(x, y));
        }
        
        // Segunda passagem: amostragem adaptativa
        if (adaptiveSampling && initialPoints.Count > 1)
        {
            points.Add(initialPoints[0]);
            
            for (int i = 0; i < initialPoints.Count - 1; i++)
            {
                Vector2 p1 = initialPoints[i];
                Vector2 p2 = initialPoints[i + 1];
                float dist = Vector2.Distance(p1, p2);
                
                // Se a distância for muito grande, adiciona pontos intermediários
                if (dist > maxSegmentLength)
                {
                    int subdivisions = Mathf.CeilToInt(dist / maxSegmentLength);
                    for (int j = 1; j < subdivisions; j++)
                    {
                        float t = (float)j / subdivisions;
                        float x = Mathf.Lerp(p1.x, p2.x, t);
                        float y = EvaluateFunction(x);
                        
                        if (clampY) y = Mathf.Clamp(y, yMin, yMax);
                        if (!float.IsNaN(y) && !float.IsInfinity(y))
                        {
                            points.Add(new Vector2(x, y));
                        }
                    }
                }
                
                points.Add(p2);
            }
        }
        else
        {
            points = initialPoints;
        }
        
        // Se não há pontos válidos, retorna
        if (points.Count < 2)
        {
            Debug.LogWarning("Função não gerou pontos válidos!");
            return;
        }
        
        // Gera mesh de linha com largura
        for (int i = 0; i < points.Count; i++)
        {
            Vector2 point = points[i];
            Vector2 perpendicular;
            
            // Calcula vetor perpendicular
            if (i == 0)
            {
                Vector2 forward = (points[i + 1] - points[i]).normalized;
                perpendicular = new Vector2(-forward.y, forward.x);
            }
            else if (i == points.Count - 1)
            {
                Vector2 forward = (points[i] - points[i - 1]).normalized;
                perpendicular = new Vector2(-forward.y, forward.x);
            }
            else
            {
                Vector2 forward = (points[i + 1] - points[i - 1]).normalized;
                perpendicular = new Vector2(-forward.y, forward.x);
            }
            
            // Cria dois vértices (lados da linha)
            Vector3 center, offset;
            
            if (verticalPlane)
            {
                // Plano XY (vertical) - Y é o valor da função
                center = new Vector3(point.x, point.y, 0);
                offset = new Vector3(-perpendicular.y, perpendicular.x, 0) * lineWidth * 0.5f;
            }
            else
            {
                // Plano XZ (horizontal) - Z é o valor da função
                center = new Vector3(point.x, 0, point.y);
                offset = new Vector3(-perpendicular.y, 0, perpendicular.x) * lineWidth * 0.5f;
            }
            
            vertices.Add(center - offset);
            vertices.Add(center + offset);
            
            // UVs
            float u = (float)i / points.Count;
            uvs.Add(new Vector2(u, 0));
            uvs.Add(new Vector2(u, 1));
            
            // Triângulos (conecta com o próximo segmento)
            if (i < points.Count - 1)
            {
                int baseIndex = i * 2;
                
                // Primeiro triângulo
                triangles.Add(baseIndex);
                triangles.Add(baseIndex + 2);
                triangles.Add(baseIndex + 1);
                
                // Segundo triângulo
                triangles.Add(baseIndex + 1);
                triangles.Add(baseIndex + 2);
                triangles.Add(baseIndex + 3);
            }
        }
        
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        meshFilter.mesh = mesh;
    }
    
    float EvaluateFunction(float x)
    {
        // Proteção contra divisão por zero
        if (functionExpression.Contains("/x") && Mathf.Abs(x) < 0.001f)
        {
            return x > 0 ? yMax : yMin;
        }
        
        try
        {
            string expression = functionExpression.Trim().ToLower();
            
            // Casos especiais primeiro (mais eficiente)
            if (expression == "x") return x;
            if (expression == "x*x" || expression == "x^2") return x * x;
            if (expression == "x*x*x" || expression == "x^3") return x * x * x;
            
            // Funções trigonométricas
            if (expression == "sin(x)") return Mathf.Sin(x);
            if (expression == "cos(x)") return Mathf.Cos(x);
            if (expression == "tan(x)") return Mathf.Tan(x);
            
            // Funções matemáticas comuns
            if (expression == "sqrt(x)") return Mathf.Sqrt(Mathf.Abs(x));
            if (expression == "abs(x)") return Mathf.Abs(x);
            if (expression == "ln(x)" || expression == "log(x)") 
                return x > 0 ? Mathf.Log(x) : float.NaN;
            
            // Divisão
            if (expression == "1/x") return 1f / x;
            if (expression.StartsWith("1/(") && expression.EndsWith(")"))
            {
                string inner = expression.Substring(3, expression.Length - 4);
                float denominator = EvaluateParsedExpression(inner, x);
                return Mathf.Abs(denominator) > 0.001f ? 1f / denominator : float.NaN;
            }
            
            // Parser genérico
            return EvaluateParsedExpression(expression, x);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Erro ao avaliar função '{functionExpression}' em x={x}: {e.Message}");
            return float.NaN;
        }
    }
    
    float EvaluateParsedExpression(string expr, float x)
    {
        // Substitui x pelo valor
        string expression = expr.Replace("x", x.ToString(System.Globalization.CultureInfo.InvariantCulture));
        
        // Suporte a funções matemáticas para DataTable
        expression = expression.Replace("sin(", "Math.Sin(");
        expression = expression.Replace("cos(", "Math.Cos(");
        expression = expression.Replace("tan(", "Math.Tan(");
        expression = expression.Replace("sqrt(", "Math.Sqrt(");
        expression = expression.Replace("abs(", "Math.Abs(");
        expression = expression.Replace("ln(", "Math.Log(");
        expression = expression.Replace("log(", "Math.Log10(");
        expression = expression.Replace("exp(", "Math.Exp(");
        
        // Potências: x^2 -> Math.Pow(x, 2)
        expression = System.Text.RegularExpressions.Regex.Replace(
            expression, 
            @"(\d+\.?\d*)\s*\^\s*(\d+\.?\d*)", 
            "Math.Pow($1, $2)"
        );
        
        try
        {
            DataTable dt = new DataTable();
            var result = dt.Compute(expression, "");
            return Convert.ToSingle(result);
        }
        catch
        {
            // Se DataTable falhar, tenta avaliação direta de expressões simples
            return EvaluateSimpleExpression(expression);
        }
    }
    
    float EvaluateSimpleExpression(string expr)
    {
        // Remove espaços
        expr = expr.Replace(" ", "");
        
        // Tenta avaliar expressões matemáticas simples
        // Isso é um fallback básico
        try
        {
            // Avalia usando reflexão (limitado mas funciona para casos simples)
            var culture = System.Globalization.CultureInfo.InvariantCulture;
            
            if (float.TryParse(expr, System.Globalization.NumberStyles.Float, culture, out float result))
            {
                return result;
            }
            
            return float.NaN;
        }
        catch
        {
            return float.NaN;
        }
    }
    
    // Atualiza a mesh quando valores mudam no Inspector
    public void OnValidate()
    {
        if (Application.isPlaying && meshFilter != null)
        {
            GenerateMesh();
            
            if (addCollider && meshCollider != null)
            {
                meshCollider.sharedMesh = meshFilter.mesh;
            }
        }
    }
    
    // Debug visual
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        
        Gizmos.color = Color.yellow;
        float step = (xMax - xMin) / 20;
        
        for (float x = xMin; x <= xMax; x += step)
        {
            float y = EvaluateFunction(x);
            if (!float.IsNaN(y) && !float.IsInfinity(y))
            {
                Vector3 pos = verticalPlane 
                    ? new Vector3(x, y, 0) 
                    : new Vector3(x, 0, y);
                Gizmos.DrawWireSphere(transform.position + pos, 0.1f);
            }
        }
    }
}