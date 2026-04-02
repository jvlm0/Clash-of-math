using System.Collections.Generic;
using UnityEngine;

public class PropPlacer : MonoBehaviour
{
    [Header("Referencias")]
    //[SerializeField] public FunctionMeshGenerator functionMeshGenerator;

    [Header("Plano de Referencia")]
    [Tooltip("O plano onde os props serao posicionados")]
    [SerializeField] private Transform plane;

    [Header("Lista de Props")]
    [SerializeField] private List<GameObject> propPrefabs = new List<GameObject>();

    [Header("Configuracoes")]
    [SerializeField] private float spacingMin = 1f;
    [SerializeField] private float clearanceFromFunction = 0.6f;
    [SerializeField] private int maxAttempts = 300;

    [Header("Gizmos")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color gizmoColor = new Color(0.2f, 1f, 0.4f, 0.8f);

    private List<Vector3> placedPositions = new List<Vector3>();

    public static PropPlacer Instance;

    private void Start()
    {
   // PlaceProps(FindObjectOfType<FunctionMeshGenerator>());ps();
        
    }


    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlaceProps(FunctionMeshGenerator functionMeshGenerator)
    {
        Debug.Log("Spawned");
        plane = GameController.Instance.currentLevelConfig.transform.GetChild(0);
        if (functionMeshGenerator == null) { Debug.LogError("[PropPlacer] FunctionMeshGenerator nao atribuido!"); return; }
        if (plane == null) { Debug.LogError("[PropPlacer] Plano nao atribuido!"); return; }
        if (propPrefabs == null || propPrefabs.Count == 0) { Debug.LogError("[PropPlacer] Lista de prefabs vazia!"); return; }

        List<Vector2> curvePoints = functionMeshGenerator.GetCurvePoints();
        if (curvePoints == null || curvePoints.Count == 0) { Debug.LogWarning("[PropPlacer] Pontos da funcao vazios."); return; }

        Renderer rend = plane.GetComponent<Renderer>();
        Vector3 bCenter = plane.position;
        Vector3 bSize = new Vector3(plane.localScale.x * 10f, 1f, plane.localScale.z * 10f);
        Bounds planeBounds = rend != null ? rend.bounds : new Bounds(bCenter, bSize);

        List<Vector3> funcPoints = new List<Vector3>();
        float planeY = plane.position.y;
        for (int i = 0; i < curvePoints.Count; i++)
            funcPoints.Add(new Vector3(curvePoints[i].x, planeY, curvePoints[i].y));

        float clearSq = clearanceFromFunction * clearanceFromFunction;
        float spaceSq = spacingMin * spacingMin;
        int placed = 0;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            float rx = Random.Range(planeBounds.min.x, planeBounds.max.x);
            float rz = Random.Range(planeBounds.min.z, planeBounds.max.z);
            Vector3 candidate = new Vector3(rx, planeY, rz);

            bool tooCloseToFunc = false;
            for (int fi = 0; fi < funcPoints.Count; fi++)
            {
                float dx = candidate.x - funcPoints[fi].x;
                float dz = candidate.z - funcPoints[fi].z;
                if (dx * dx + dz * dz < clearSq) { tooCloseToFunc = true; break; }
            }
            if (tooCloseToFunc) continue;

            bool tooCloseToOther = false;
            for (int pi = 0; pi < placedPositions.Count; pi++)
            {
                float dx = candidate.x - placedPositions[pi].x;
                float dz = candidate.z - placedPositions[pi].z;
                if (dx * dx + dz * dz < spaceSq) { tooCloseToOther = true; break; }
            }
            if (tooCloseToOther) continue;

            int idx = Random.Range(0, propPrefabs.Count);
            if (propPrefabs[idx] == null) continue;

            Instantiate(propPrefabs[idx], candidate, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), GameController.Instance.currentLevelConfig.transform);
            placedPositions.Add(candidate);
            placed++;
        }

        Debug.Log(string.Format("[PropPlacer] {0} props instanciados.", placed));
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;
        Gizmos.color = gizmoColor;
        for (int i = 0; i < placedPositions.Count; i++)
            Gizmos.DrawWireSphere(placedPositions[i], 0.2f);
    }
}
