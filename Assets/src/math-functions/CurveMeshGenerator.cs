using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Gera um mesh procedural que representa uma função matemática f(x).
/// A curva é uma "faixa" com espessura, ideal para ondas de choque.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CurveMeshGenerator : MonoBehaviour
{
    [Header("Configuração da Função")]
    public float xStart = -5f;
    public float xEnd = 5f;
    public float xStep = 0.1f;

    [Header("Aparência da Linha")]
    public float lineWidth = 0.15f;

    [Header("Transformações")]
    public float verticalScale = 1f;   // Escala vertical da função
    public float horizontalScale = 1f; // Escala horizontal (para onda expandir)

    // Referência do Mesh
    Mesh mesh;

    void Awake()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
    }

    /// <summary>
    /// Gera a curva usando a função passada como parâmetro.
    /// </summary>
    public void GenerateCurveMesh(Func<float, float> function)
    {
        List<Vector3> points = SampleFunction(function);
        BuildMesh(points);
    }

    /// <summary>
    /// Amostra a função f(x) criando uma lista de pontos.
    /// </summary>
    private List<Vector3> SampleFunction(Func<float, float> f)
    {
        List<Vector3> pts = new List<Vector3>();

        for (float x = xStart; x <= xEnd; x += xStep)
        {
            float y = f(x);
            pts.Add(new Vector3(
                x * horizontalScale,
                0,  // Y = 0 (no chão)
                y * verticalScale
            ));
        }

        return pts;
    }

    /// <summary>
    /// Constrói um mesh do tipo "faixa" (duas linhas paralelas formando uma fita).
    /// </summary>
    private void BuildMesh(List<Vector3> points)
    {
        if (points.Count < 2) return;

        int vertCount = points.Count * 2;
        int triCount = (points.Count - 1) * 6;

        Vector3[] vertices = new Vector3[vertCount];
        int[] triangles = new int[triCount];
        Vector2[] uvs = new Vector2[vertCount];

        // Criar dois vértices por ponto (esquerda e direita)
        for (int i = 0; i < points.Count; i++)
        {
            Vector3 p = points[i];

            // Direção da curva naquele ponto (tangente)
            Vector3 forward;

            if (i < points.Count - 1)
                forward = (points[i + 1] - p).normalized;
            else
                forward = (p - points[i - 1]).normalized;

            // Perpendicular no plano XZ (usa o eixo Y como "up")
            // MUDANÇA CRÍTICA: Cross com Vector3.up para obter perpendicular no plano horizontal
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized * lineWidth;

            vertices[i * 2]     = p + right; // lado direito
            vertices[i * 2 + 1] = p - right; // lado esquerdo
            
            // UVs para texturização
            float uvX = (float)i / (points.Count - 1);
            uvs[i * 2] = new Vector2(uvX, 1);
            uvs[i * 2 + 1] = new Vector2(uvX, 0);
        }

        // Criar triângulos conectando os segmentos
        // Ordem invertida para normais apontarem para baixo (Y-) - visível de cima
        int t = 0;
        for (int i = 0; i < points.Count - 1; i++)
        {
            int v = i * 2;

            // Triângulo 1 (sentido horário visto de cima = normal para baixo)
            triangles[t++] = v;
            triangles[t++] = v + 1;
            triangles[t++] = v + 2;

            // Triângulo 2
            triangles[t++] = v + 1;
            triangles[t++] = v + 3;
            triangles[t++] = v + 2;
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    /// <summary>
    /// Exemplo interno para debug: gera f(x) = x² ao iniciar.
    /// </summary>
    void Start()
    {
        // Para ver algo de imediato: x²
        GenerateCurveMesh(x => x * x);
    }
}