using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class FunctionCanvasGenerator : MonoBehaviour
{
    [Header("Configurações da Função")]
    [SerializeField]
    private int resolution = 100;

    [SerializeField]
    private float xMin = -5f;

    [SerializeField]
    private float xMax = 5f;

    [SerializeField]
    private float yMin = -5f;

    [SerializeField]
    private float yMax = 5f;

    [Header("Viewport Adaptativo")]
    [SerializeField]
    [Tooltip("Ativa o ajuste automático dos limites de Y baseado nos valores da função")]
    private bool useAdaptiveViewport = true;

    [SerializeField]
    [Tooltip("Limite absoluto máximo de Y (funções que chegam aqui usam este valor)")]
    private float absoluteYMax = 10f;

    [SerializeField]
    [Tooltip("Limite absoluto mínimo de Y (funções que chegam aqui usam este valor)")]
    private float absoluteYMin = -10f;

    [SerializeField]
    [Range(0f, 2f)]
    [Tooltip("Margem adicional em relação aos valores encontrados (0 = sem margem, 1 = 100% de margem)")]
    private float viewportPadding = 0.2f;

    [SerializeField]
    [Tooltip("Limites de Y mínimos para evitar viewport muito comprimido")]
    private float minViewportRange = 2f;

    private float activeYMin;
    private float activeYMax;

    [SerializeField]
    private float lineThickness = 2f;

    [SerializeField]
    private Color lineColor = Color.cyan;

    [SerializeField]
    [Tooltip("Comprimento máximo para subdividir segmentos longos")]
    private float maxSegmentLength = 1f;

    [SerializeField]
    [Tooltip("Se um segmento for maior que este valor, ele será pulado (descontinuidade)")]
    private float maxDrawSegmentLength = 10f;

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

    public enum AnimationDirection
    {
        FromLeft,
        FromRight,
        FromCenter,
        ToBoth,
    }

    private RectTransform rectTransform;
    private MathExpressionParser parser;
    private List<UILineRenderer> lineRenderers = new List<UILineRenderer>();
    private FunctionCanvasAxisRenderer axisRenderer;
    
    private bool isAnimating = false;
    private float currentXMin;
    private float currentXMax;
    private float animationProgress = 0f;

    private struct ValidPoint
    {
        public float x;
        public float y;
        public bool isValid;

        public ValidPoint(float x, float y, bool isValid)
        {
            this.x = x;
            this.y = y;
            this.isValid = isValid;
        }
    }

    private class GraphSegment
    {
        public List<ValidPoint> points = new List<ValidPoint>();
    }

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        parser = new MathExpressionParser(mathExpression);
        axisRenderer = GetComponent<FunctionCanvasAxisRenderer>();

        // Inicializa limites ativos
        activeYMin = yMin;
        activeYMax = yMax;

        if (animateOnStart)
        {
            StartPropagation();
        }
        else
        {
            currentXMin = xMin;
            currentXMax = xMax;
            GenerateGraph();
            Debug.Log($"Gráfico gerado usando: {mathExpression}");
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

    public void GetCurrentBounds(out float outXMin, out float outXMax, out float outYMin, out float outYMax)
    {
        outXMin = currentXMin;
        outXMax = currentXMax;
        outYMin = activeYMin;
        outYMax = activeYMax;
    }

    void GenerateGraph()
    {
        ClearLines();

        float xRange = currentXMax - currentXMin;

        if (Mathf.Abs(xRange) < 0.001f)
            return;

        List<ValidPoint> allPoints = new List<ValidPoint>();

        // Calcula pontos com base na resolução
        float piStep = Mathf.PI / resolution;
        float firstMultiple = Mathf.Ceil(currentXMin / piStep) * piStep;
        float lastMultiple = Mathf.Floor(currentXMax / piStep) * piStep;

        // Primeira passagem: coleta todos os pontos válidos
        List<float> validYValues = new List<float>();

        // Gera pontos nos múltiplos de pi
        for (float x = firstMultiple; x <= lastMultiple; x += piStep)
        {
            if (x >= currentXMin && x <= currentXMax)
            {
                float y = CalculateFunction(x);
                if (IsFiniteValue(y))
                {
                    validYValues.Add(y);
                }
            }
        }

        // Adiciona pontos nas extremidades
        float yStart = CalculateFunction(currentXMin);
        if (IsFiniteValue(yStart))
        {
            validYValues.Add(yStart);
        }

        float yEnd = CalculateFunction(currentXMax);
        if (IsFiniteValue(yEnd))
        {
            validYValues.Add(yEnd);
        }

        // Calcula viewport adaptativo
        if (useAdaptiveViewport && validYValues.Count > 0)
        {
            CalculateAdaptiveViewport(validYValues);
        }
        else
        {
            activeYMin = yMin;
            activeYMax = yMax;
        }

        // Segunda passagem: gera pontos usando os limites calculados
        for (float x = firstMultiple; x <= lastMultiple; x += piStep)
        {
            if (x >= currentXMin && x <= currentXMax)
            {
                float y = CalculateFunction(x);
                bool isValid = IsValidValue(y);
                allPoints.Add(new ValidPoint(x, y, isValid));
            }
        }

        // Adiciona pontos nas extremidades
        if (allPoints.Count == 0 || allPoints[0].x > currentXMin + 0.001f)
        {
            float y = CalculateFunction(currentXMin);
            bool isValid = IsValidValue(y);
            allPoints.Insert(0, new ValidPoint(currentXMin, y, isValid));
        }

        if (allPoints.Count == 0 || allPoints[allPoints.Count - 1].x < currentXMax - 0.001f)
        {
            float y = CalculateFunction(currentXMax);
            bool isValid = IsValidValue(y);
            allPoints.Add(new ValidPoint(currentXMax, y, isValid));
        }

        List<GraphSegment> segments = ProcessPointsWithDiscontinuities(allPoints);

        // Cria linhas para cada segmento
        foreach (var segment in segments)
        {
            if (segment.points.Count < 2)
                continue;

            CreateLineForSegment(segment.points);
        }

        if (useAdaptiveViewport)
        {
            Debug.Log($"Viewport adaptativo: Y [{activeYMin:F2}, {activeYMax:F2}]");
        }

        // Atualiza os eixos se o componente existir
        if (axisRenderer != null)
        {
            axisRenderer.UpdateAxis(currentXMin, currentXMax, activeYMin, activeYMax);
        }
    }

    List<GraphSegment> ProcessPointsWithDiscontinuities(List<ValidPoint> points)
    {
        List<GraphSegment> segments = new List<GraphSegment>();
        GraphSegment currentSegment = new GraphSegment();

        for (int i = 0; i < points.Count; i++)
        {
            if (!points[i].isValid)
            {
                if (currentSegment.points.Count > 0)
                {
                    segments.Add(currentSegment);
                    currentSegment = new GraphSegment();
                }
                continue;
            }

            if (currentSegment.points.Count > 0)
            {
                ValidPoint lastPoint = currentSegment.points[currentSegment.points.Count - 1];
                float distance = Vector2.Distance(
                    new Vector2(lastPoint.x, lastPoint.y),
                    new Vector2(points[i].x, points[i].y)
                );

                // Descontinuidade detectada
                if (distance > maxDrawSegmentLength)
                {
                    if (currentSegment.points.Count > 0)
                    {
                        segments.Add(currentSegment);
                    }
                    currentSegment = new GraphSegment();
                    currentSegment.points.Add(points[i]);
                    continue;
                }

                // Subdivisão de segmentos longos
                if (distance > maxSegmentLength)
                {
                    int subdivisions = Mathf.CeilToInt(distance / maxSegmentLength);

                    for (int j = 1; j < subdivisions; j++)
                    {
                        float t = (float)j / subdivisions;
                        float newX = Mathf.Lerp(lastPoint.x, points[i].x, t);
                        float newY = CalculateFunction(newX);

                        if (IsValidValue(newY))
                        {
                            currentSegment.points.Add(new ValidPoint(newX, newY, true));
                        }
                        else
                        {
                            if (currentSegment.points.Count > 0)
                            {
                                segments.Add(currentSegment);
                                currentSegment = new GraphSegment();
                            }
                            break;
                        }
                    }
                }

                currentSegment.points.Add(points[i]);
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

    void CreateLineForSegment(List<ValidPoint> points)
    {
        GameObject lineObj = new GameObject("GraphLine");
        lineObj.transform.SetParent(transform, false);
        
        // Adiciona RectTransform (necessário para UI)
        RectTransform lineRect = lineObj.AddComponent<RectTransform>();
        lineRect.anchorMin = new Vector2(0.5f, 0.5f);
        lineRect.anchorMax = new Vector2(0.5f, 0.5f);
        lineRect.sizeDelta = rectTransform.rect.size;
        lineRect.anchoredPosition = Vector2.zero;

        // Adiciona CanvasRenderer (necessário para MaskableGraphic)
        lineObj.AddComponent<CanvasRenderer>();

        UILineRenderer lineRenderer = lineObj.AddComponent<UILineRenderer>();
        lineRenderer.color = lineColor;
        lineRenderer.lineThickness = lineThickness;
        lineRenderer.raycastTarget = false; // Desabilita interação com mouse

        // Converte pontos do espaço matemático para espaço do Canvas
        List<Vector2> canvasPoints = new List<Vector2>();
        foreach (var point in points)
        {
            Vector2 canvasPos = MathToCanvasPosition(point.x, point.y);
            canvasPoints.Add(canvasPos);
        }

        lineRenderer.points = canvasPoints.ToArray();
        lineRenderers.Add(lineRenderer);
    }

    Vector2 MathToCanvasPosition(float mathX, float mathY)
    {
        Rect rect = rectTransform.rect;
        
        // Normaliza as coordenadas matemáticas para [0, 1]
        float normalizedX = Mathf.InverseLerp(xMin, xMax, mathX);
        float normalizedY = Mathf.InverseLerp(activeYMin, activeYMax, mathY);

        // Converte para coordenadas do Canvas
        float canvasX = Mathf.Lerp(rect.xMin, rect.xMax, normalizedX);
        float canvasY = Mathf.Lerp(rect.yMin, rect.yMax, normalizedY);

        return new Vector2(canvasX, canvasY);
    }

    void CalculateAdaptiveViewport(List<float> validYValues)
    {
        if (validYValues.Count == 0)
        {
            activeYMin = yMin;
            activeYMax = yMax;
            return;
        }

        float minY = float.MaxValue;
        float maxY = float.MinValue;
        bool hitAbsoluteLimit = false;

        // Encontra os valores mínimo e máximo, respeitando limites absolutos
        foreach (float y in validYValues)
        {
            // Verifica se atingiu os limites absolutos
            if (y <= absoluteYMin || y >= absoluteYMax)
            {
                hitAbsoluteLimit = true;
            }

            // Clamp aos limites absolutos
            float clampedY = Mathf.Clamp(y, absoluteYMin, absoluteYMax);
            
            if (clampedY < minY) minY = clampedY;
            if (clampedY > maxY) maxY = clampedY;
        }

        // Se atingiu os limites absolutos, usa eles
        if (hitAbsoluteLimit)
        {
            activeYMin = absoluteYMin;
            activeYMax = absoluteYMax;
        }
        else
        {
            // Calcula o range dos valores
            float range = maxY - minY;

            // Garante um range mínimo
            if (range < minViewportRange)
            {
                float center = (minY + maxY) / 2f;
                float halfRange = minViewportRange / 2f;
                minY = center - halfRange;
                maxY = center + halfRange;
                range = minViewportRange;
            }

            // Adiciona padding (margem)
            float padding = range * viewportPadding;
            activeYMin = minY - padding;
            activeYMax = maxY + padding;

            // Garante que não ultrapasse os limites absolutos
            activeYMin = Mathf.Max(activeYMin, absoluteYMin);
            activeYMax = Mathf.Min(activeYMax, absoluteYMax);
        }
    }

    bool IsFiniteValue(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    float CalculateFunction(float x)
    {
        return parser.Evaluate(x);
    }

    bool IsValidValue(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value) && value >= activeYMin && value <= activeYMax;
    }

    void ClearLines()
    {
        foreach (var line in lineRenderers)
        {
            if (line != null && line.gameObject != null)
            {
                if (Application.isPlaying)
                    Destroy(line.gameObject);
                else
                    DestroyImmediate(line.gameObject);
            }
        }
        lineRenderers.Clear();
    }


    public void UpdateGraph(string mathExpression)
    {
        parser = new MathExpressionParser(mathExpression);
        currentXMin = xMin;
        currentXMax = xMax;
        GenerateGraph();
        Debug.Log($"Gráfico atualizado com: {mathExpression}");
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

            GenerateGraph();
        }

        // Atalhos de teclado
        if (Input.GetKeyDown(KeyCode.Space))
        {
            parser = new MathExpressionParser(mathExpression);
            currentXMin = xMin;
            currentXMax = xMax;
            GenerateGraph();
            Debug.Log($"Gráfico atualizado com: {mathExpression}");
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

    void OnDestroy()
    {
        ClearLines();
    }
}

// Componente para desenhar linhas na UI
public class UILineRenderer : MaskableGraphic
{
    public Vector2[] points;
    public float lineThickness = 2f;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        if (points == null || points.Length < 2)
            return;

        for (int i = 0; i < points.Length - 1; i++)
        {
            Vector2 point1 = points[i];
            Vector2 point2 = points[i + 1];

            Vector2 direction = (point2 - point1).normalized;
            Vector2 perpendicular = new Vector2(-direction.y, direction.x) * lineThickness * 0.5f;

            int vertexIndex = vh.currentVertCount;

            // Cria um quad para o segmento
            vh.AddVert(point1 + perpendicular, color, Vector2.zero);
            vh.AddVert(point1 - perpendicular, color, Vector2.zero);
            vh.AddVert(point2 - perpendicular, color, Vector2.zero);
            vh.AddVert(point2 + perpendicular, color, Vector2.zero);

            vh.AddTriangle(vertexIndex, vertexIndex + 1, vertexIndex + 2);
            vh.AddTriangle(vertexIndex + 2, vertexIndex + 3, vertexIndex);
        }
    }
}