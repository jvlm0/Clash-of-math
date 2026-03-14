using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Gera uma interseção de rua no estilo da imagem de referência.
/// O plano base (chão) e o plano central (rua horizontal) ficam sob o mesmo parent "RoadParent".
/// Os 4 faixas laterais ficam no parent "CrossingParent".
/// 
/// Execute pelo menu: Tools > Build Intersection
/// Ou adicione o componente a um GameObject e clique em "Build" no Inspector.
/// </summary>
public class IntersectionBuilder : MonoBehaviour
{
    [Header("Material")]
    public Material roadMaterial;       // Material branco para as pistas
    public Material groundMaterial;     // Material do chão (xadrez / cinza)

    [Header("Road Dimensions")]
    public float roadWidth      = 2f;
    public float roadLength     = 8f;
    public float crossingWidth  = 1.5f;
    public float crossingLength = 2.5f;

    [Header("Crossing Offsets")]
    [Tooltip("Distância do centro até cada faixa lateral (eixo X)")]
    public float sideOffsetX = 3.5f;
    [Tooltip("Distância do centro até cada faixa frontal (eixo Z)")]
    public float sideOffsetZ = 3.5f;

    // ----------------------------------------------------------------

    [ContextMenu("Build Intersection")]
    public void Build()
    {
        // Limpa filhos antigos
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);

        // ── Parent compartilhado: chão + pista central ──────────────
        GameObject roadParent = CreateEmpty("RoadParent", transform, Vector3.zero);

        // Chão (plano cinza / xadrez)
        CreatePlane("Ground",      roadParent.transform,
                    new Vector3(0, 0, 0),
                    new Vector3(10f, 1f, 10f),
                    Quaternion.identity,
                    groundMaterial);

        // Pista central horizontal (branca, longa, sobre o chão)
        CreatePlane("RoadCenter", roadParent.transform,
                    new Vector3(0, 0.001f, 0),
                    new Vector3(roadLength * 0.1f, 1f, roadWidth * 0.1f),
                    Quaternion.identity,
                    roadMaterial);

        // ── Faixas de pedestres / cruzamentos ao redor ──────────────
        GameObject crossParent = CreateEmpty("CrossingParent", transform, Vector3.zero);

        // Frente-esquerda
        CreatePlane("Crossing_FL", crossParent.transform,
                    new Vector3(-sideOffsetX,  0.001f,  sideOffsetZ),
                    new Vector3(crossingWidth * 0.1f, 1f, crossingLength * 0.1f),
                    Quaternion.Euler(0, -15f, 0),
                    roadMaterial);

        // Frente-direita
        CreatePlane("Crossing_FR", crossParent.transform,
                    new Vector3( sideOffsetX,  0.001f,  sideOffsetZ),
                    new Vector3(crossingWidth * 0.1f, 1f, crossingLength * 0.1f),
                    Quaternion.Euler(0, 10f, 0),
                    roadMaterial);

        // Trás-esquerda
        CreatePlane("Crossing_BL", crossParent.transform,
                    new Vector3(-sideOffsetX,  0.001f, -sideOffsetZ),
                    new Vector3(crossingWidth * 0.1f, 1f, crossingLength * 0.1f),
                    Quaternion.Euler(0, 5f, 0),
                    roadMaterial);

        // Trás-direita
        CreatePlane("Crossing_BR", crossParent.transform,
                    new Vector3( sideOffsetX,  0.001f, -sideOffsetZ),
                    new Vector3(crossingWidth * 0.1f, 1f, crossingLength * 0.1f),
                    Quaternion.Euler(0, -10f, 0),
                    roadMaterial);

        Debug.Log("[IntersectionBuilder] Interseção criada com sucesso!");
    }

    // ── Helpers ─────────────────────────────────────────────────────

    private static GameObject CreateEmpty(string name, Transform parent, Vector3 localPos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        return go;
    }

    private static void CreatePlane(string name, Transform parent,
                                    Vector3 localPos, Vector3 localScale,
                                    Quaternion localRot, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale    = localScale;
        go.transform.localRotation = localRot;

        if (mat != null)
            go.GetComponent<Renderer>().sharedMaterial = mat;

        // Remove collider desnecessário (opcional)
        var col = go.GetComponent<Collider>();
        if (col != null) DestroyImmediate(col);
    }
}

// ── Menu Editor ─────────────────────────────────────────────────────
#if UNITY_EDITOR
[CustomEditor(typeof(IntersectionBuilder))]
public class IntersectionBuilderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        GUILayout.Space(8);
        if (GUILayout.Button("▶  Build Intersection", GUILayout.Height(36)))
            ((IntersectionBuilder)target).Build();
    }
}
#endif
