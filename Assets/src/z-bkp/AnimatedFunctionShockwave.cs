using UnityEngine;

public class AnimatedFunctionShockwave : MonoBehaviour
{
    [Header("Referências")]
    public FunctionCurveMesh curveMeshPrefab;
    public Material curveMaterial;

    [Header("Animação")]
    public float speed = 2f;
    public float maxRadius = 20f;
    public float bandWidth = 2f; // Largura da "janela" visível

    [Header("Função")]
    public string functionExpression = "1/x";

    private FunctionCurveMesh currentCurve;
    private float currentRadius = 0f;

    void Start()
    {
        CreateCurveMesh();
    }

    void CreateCurveMesh()
    {
        if (currentCurve != null)
        {
            Destroy(currentCurve.gameObject);
        }

        GameObject curveObj = new GameObject("Function Curve");
        curveObj.transform.parent = transform;
        curveObj.transform.localPosition = Vector3.zero;

        currentCurve = curveObj.AddComponent<FunctionCurveMesh>();
        currentCurve.functionExpression = functionExpression;
        currentCurve.curveMaterial = curveMaterial;
        currentCurve.resolution = 200;
        currentCurve.lineWidth = 0.2f;
        currentCurve.addCollider = true;
        currentCurve.isTrigger = true;
    }

    void Update()
    {
        currentRadius += speed * Time.deltaTime;

        if (currentRadius > maxRadius)
        {
            currentRadius = 0f;
        }

        // Atualiza a "janela" visível da curva
        UpdateVisibleRange();
    }

    void UpdateVisibleRange()
    {
        if (currentCurve == null)
            return;

        // Define xMin e xMax baseado no raio atual ± bandWidth
        currentCurve.xMin = -currentRadius - bandWidth;
        currentCurve.xMax = currentRadius + bandWidth;

        // Evita x=0 para funções como 1/x
        if (currentCurve.xMin < 0 && currentCurve.xMax > 0)
        {
            // Pula a região perto de zero
            if (Mathf.Abs(currentCurve.xMin) < 0.5f)
                currentCurve.xMin = -0.5f;
            if (Mathf.Abs(currentCurve.xMax) < 0.5f)
                currentCurve.xMax = 0.5f;
        }

        // Força atualização da mesh
        currentCurve.OnValidate();
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, currentRadius);
    }
}
