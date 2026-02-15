using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(RectTransform))]
public class FunctionCanvasAxisRenderer : MonoBehaviour
{
    [Header("Configurações dos Eixos")]
    [SerializeField]
    private Color axisColor = Color.white;

    [SerializeField]
    private float axisThickness = 2f;

    [SerializeField]
    private Color gridColor = new Color(1f, 1f, 1f, 0.2f);

    [SerializeField]
    private float gridThickness = 1f;

    [SerializeField]
    private bool showGrid = true;

    [Header("Configurações de Marcações")]
    [SerializeField]
    private float tickLength = 10f;

    [SerializeField]
    private Color tickColor = Color.white;

    [SerializeField]
    private float tickThickness = 2f;

    [Header("Configurações de Labels")]
    [SerializeField]
    private GameObject labelPrefab;

    [SerializeField]
    private Color labelColor = Color.white;

    [SerializeField]
    private int labelFontSize = 14;

    [SerializeField]
    [Tooltip("Distância dos labels do eixo X")]
    private float labelOffsetX = 25f;

    [SerializeField]
    [Tooltip("Distância dos labels do eixo Y")]
    private float labelOffsetY = 35f;

    [SerializeField]
    [Tooltip("Número aproximado de marcações em cada eixo")]
    private int targetTickCount = 10;

    [SerializeField]
    [Tooltip("Formato dos números (F0 = inteiro, F1 = 1 decimal, F2 = 2 decimais)")]
    private string numberFormat = "F1";

    private RectTransform rectTransform;
    private FunctionCanvasGenerator functionGenerator;

    private List<GameObject> axisObjects = new List<GameObject>();
    private List<GameObject> gridObjects = new List<GameObject>();
    private List<GameObject> tickObjects = new List<GameObject>();
    private List<GameObject> labelObjects = new List<GameObject>();

    private float currentXMin, currentXMax;
    private float currentYMin, currentYMax;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        functionGenerator = GetComponent<FunctionCanvasGenerator>();

        if (functionGenerator == null)
        {
            Debug.LogError("FunctionCanvasAxisRenderer requer FunctionCanvasGenerator no mesmo GameObject!");
            enabled = false;
            return;
        }

        // Cria prefab de label se não existir
        if (labelPrefab == null)
        {
            CreateDefaultLabelPrefab();
        }
    }

    void CreateDefaultLabelPrefab()
    {
        labelPrefab = new GameObject("LabelPrefab");
        labelPrefab.SetActive(false);

        RectTransform rect = labelPrefab.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(100, 30);

        TextMeshProUGUI text = labelPrefab.AddComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = labelFontSize;
        text.color = labelColor;

        labelPrefab.transform.SetParent(transform, false);
    }

    public void UpdateAxis(float xMin, float xMax, float yMin, float yMax)
    {
        currentXMin = xMin;
        currentXMax = xMax;
        currentYMin = yMin;
        currentYMax = yMax;

        ClearAllAxisElements();
        DrawAxis();
        if (showGrid)
        {
            DrawGrid();
        }
        DrawTicks();
        DrawLabels();
    }

    void DrawAxis()
    {
        Rect rect = rectTransform.rect;

        // Calcula posição do eixo X (y = 0)
        float xAxisY = MathToCanvasY(0f);
        // Clamp para manter dentro do canvas
        xAxisY = Mathf.Clamp(xAxisY, rect.yMin, rect.yMax);

        // Calcula posição do eixo Y (x = 0)
        float yAxisX = MathToCanvasX(0f);
        // Clamp para manter dentro do canvas
        yAxisX = Mathf.Clamp(yAxisX, rect.xMin, rect.xMax);

        // Desenha eixo X
        CreateLine("Axis_X", 
            new Vector2(rect.xMin, xAxisY), 
            new Vector2(rect.xMax, xAxisY), 
            axisColor, 
            axisThickness, 
            axisObjects);

        // Desenha eixo Y
        CreateLine("Axis_Y", 
            new Vector2(yAxisX, rect.yMin), 
            new Vector2(yAxisX, rect.yMax), 
            axisColor, 
            axisThickness, 
            axisObjects);
    }

    void DrawGrid()
    {
        Rect rect = rectTransform.rect;

        // Calcula intervalos para as linhas de grade
        float xInterval = CalculateNiceInterval(currentXMax - currentXMin, targetTickCount);
        float yInterval = CalculateNiceInterval(currentYMax - currentYMin, targetTickCount);

        // Linhas verticais da grade (paralelas ao eixo Y)
        float startX = Mathf.Ceil(currentXMin / xInterval) * xInterval;
        for (float x = startX; x <= currentXMax; x += xInterval)
        {
            if (Mathf.Abs(x) > 0.001f) // Não desenha no eixo Y principal
            {
                float canvasX = MathToCanvasX(x);
                CreateLine($"Grid_V_{x}", 
                    new Vector2(canvasX, rect.yMin), 
                    new Vector2(canvasX, rect.yMax), 
                    gridColor, 
                    gridThickness, 
                    gridObjects);
            }
        }

        // Linhas horizontais da grade (paralelas ao eixo X)
        float startY = Mathf.Ceil(currentYMin / yInterval) * yInterval;
        for (float y = startY; y <= currentYMax; y += yInterval)
        {
            if (Mathf.Abs(y) > 0.001f) // Não desenha no eixo X principal
            {
                float canvasY = MathToCanvasY(y);
                CreateLine($"Grid_H_{y}", 
                    new Vector2(rect.xMin, canvasY), 
                    new Vector2(rect.xMax, canvasY), 
                    gridColor, 
                    gridThickness, 
                    gridObjects);
            }
        }
    }

    void DrawTicks()
    {
        Rect rect = rectTransform.rect;

        float xAxisY = Mathf.Clamp(MathToCanvasY(0f), rect.yMin, rect.yMax);
        float yAxisX = Mathf.Clamp(MathToCanvasX(0f), rect.xMin, rect.xMax);

        // Calcula intervalos
        float xInterval = CalculateNiceInterval(currentXMax - currentXMin, targetTickCount);
        float yInterval = CalculateNiceInterval(currentYMax - currentYMin, targetTickCount);

        // Marcações no eixo X
        float startX = Mathf.Ceil(currentXMin / xInterval) * xInterval;
        for (float x = startX; x <= currentXMax; x += xInterval)
        {
            float canvasX = MathToCanvasX(x);
            CreateLine($"Tick_X_{x}", 
                new Vector2(canvasX, xAxisY - tickLength / 2f), 
                new Vector2(canvasX, xAxisY + tickLength / 2f), 
                tickColor, 
                tickThickness, 
                tickObjects);
        }

        // Marcações no eixo Y
        float startY = Mathf.Ceil(currentYMin / yInterval) * yInterval;
        for (float y = startY; y <= currentYMax; y += yInterval)
        {
            float canvasY = MathToCanvasY(y);
            CreateLine($"Tick_Y_{y}", 
                new Vector2(yAxisX - tickLength / 2f, canvasY), 
                new Vector2(yAxisX + tickLength / 2f, canvasY), 
                tickColor, 
                tickThickness, 
                tickObjects);
        }
    }

    void DrawLabels()
    {
        Rect rect = rectTransform.rect;

        float xAxisY = Mathf.Clamp(MathToCanvasY(0f), rect.yMin, rect.yMax);
        float yAxisX = Mathf.Clamp(MathToCanvasX(0f), rect.xMin, rect.xMax);

        // Calcula intervalos
        float xInterval = CalculateNiceInterval(currentXMax - currentXMin, targetTickCount);
        float yInterval = CalculateNiceInterval(currentYMax - currentYMin, targetTickCount);

        // Labels no eixo X
        float startX = Mathf.Ceil(currentXMin / xInterval) * xInterval;
        for (float x = startX; x <= currentXMax; x += xInterval)
        {
            if (Mathf.Abs(x) < 0.001f) continue; // Pula o zero

            float canvasX = MathToCanvasX(x);
            
            // Posiciona abaixo do eixo X, ou no fundo se o eixo estiver muito em cima
            float labelY = xAxisY - labelOffsetX;
            if (xAxisY > rect.yMax - 50f)
            {
                labelY = rect.yMin + 15f;
            }
            else if (xAxisY < rect.yMin + 50f)
            {
                labelY = xAxisY + labelOffsetX;
            }

            CreateLabel($"Label_X_{x}", 
                new Vector2(canvasX, labelY), 
                FormatNumber(x));
        }

        // Labels no eixo Y
        float startY = Mathf.Ceil(currentYMin / yInterval) * yInterval;
        for (float y = startY; y <= currentYMax; y += yInterval)
        {
            if (Mathf.Abs(y) < 0.001f) continue; // Pula o zero

            float canvasY = MathToCanvasY(y);
            
            // Posiciona à esquerda do eixo Y, ou na borda se o eixo estiver muito à esquerda
            float labelX = yAxisX - labelOffsetY;
            if (yAxisX < rect.xMin + 50f)
            {
                labelX = yAxisX + labelOffsetY;
            }
            else if (yAxisX > rect.xMax - 50f)
            {
                labelX = rect.xMax - 30f;
            }

            CreateLabel($"Label_Y_{y}", 
                new Vector2(labelX, canvasY), 
                FormatNumber(y));
        }
    }

    float CalculateNiceInterval(float range, int targetCount)
    {
        if (range <= 0) return 1f;

        float roughInterval = range / targetCount;
        float magnitude = Mathf.Pow(10f, Mathf.Floor(Mathf.Log10(roughInterval)));

        float[] niceNumbers = { 1f, 2f, 5f, 10f };
        float normalizedInterval = roughInterval / magnitude;

        float niceInterval = 1f;
        foreach (float nice in niceNumbers)
        {
            if (normalizedInterval <= nice)
            {
                niceInterval = nice;
                break;
            }
        }

        return niceInterval * magnitude;
    }

    string FormatNumber(float value)
    {
        // Remove zeros desnecessários
        if (Mathf.Abs(value) < 0.001f)
            return "0";

        string formatted = value.ToString(numberFormat);
        
        // Remove trailing zeros e ponto decimal se não necessário
        if (formatted.Contains(".") || formatted.Contains(","))
        {
            formatted = formatted.TrimEnd('0').TrimEnd('.').TrimEnd(',');
        }

        return formatted;
    }

    void CreateLine(string name, Vector2 start, Vector2 end, Color color, float thickness, List<GameObject> targetList)
    {
        GameObject lineObj = new GameObject(name);
        lineObj.transform.SetParent(transform, false);

        RectTransform lineRect = lineObj.AddComponent<RectTransform>();
        lineRect.anchorMin = new Vector2(0.5f, 0.5f);
        lineRect.anchorMax = new Vector2(0.5f, 0.5f);
        lineRect.sizeDelta = rectTransform.rect.size;
        lineRect.anchoredPosition = Vector2.zero;

        lineObj.AddComponent<CanvasRenderer>();

        UILineRenderer lineRenderer = lineObj.AddComponent<UILineRenderer>();
        lineRenderer.color = color;
        lineRenderer.lineThickness = thickness;
        lineRenderer.points = new Vector2[] { start, end };
        lineRenderer.raycastTarget = false;

        targetList.Add(lineObj);
    }

    void CreateLabel(string name, Vector2 position, string text)
    {
        if (labelPrefab == null) return;

        GameObject labelObj = Instantiate(labelPrefab, transform);
        labelObj.name = name;
        labelObj.SetActive(true);

        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchoredPosition = position;

        TextMeshProUGUI labelText = labelObj.GetComponent<TextMeshProUGUI>();
        if (labelText != null)
        {
            labelText.text = text;
            labelText.color = labelColor;
            labelText.fontSize = labelFontSize;
        }

        labelObjects.Add(labelObj);
    }

    float MathToCanvasX(float mathX)
    {
        Rect rect = rectTransform.rect;
        float normalizedX = Mathf.InverseLerp(currentXMin, currentXMax, mathX);
        return Mathf.Lerp(rect.xMin, rect.xMax, normalizedX);
    }

    float MathToCanvasY(float mathY)
    {
        Rect rect = rectTransform.rect;
        float normalizedY = Mathf.InverseLerp(currentYMin, currentYMax, mathY);
        return Mathf.Lerp(rect.yMin, rect.yMax, normalizedY);
    }

    void ClearAllAxisElements()
    {
        ClearObjectList(axisObjects);
        ClearObjectList(gridObjects);
        ClearObjectList(tickObjects);
        ClearObjectList(labelObjects);
    }

    void ClearObjectList(List<GameObject> list)
    {
        foreach (GameObject obj in list)
        {
            if (obj != null)
            {
                if (Application.isPlaying)
                    Destroy(obj);
                else
                    DestroyImmediate(obj);
            }
        }
        list.Clear();
    }

    void OnDestroy()
    {
        ClearAllAxisElements();
        
        if (labelPrefab != null && labelPrefab.transform.parent == transform)
        {
            if (Application.isPlaying)
                Destroy(labelPrefab);
            else
                DestroyImmediate(labelPrefab);
        }
    }
}