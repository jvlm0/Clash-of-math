using System;
using System.Data;
using UnityEngine;

public class ShockwaveShaderController : MonoBehaviour
{
    public Material shockwaveMaterial;
    public float speed = 2f;
    public float maxRadius = 20f;

    [Header("Função Matemática")]
    [Tooltip("Use 'x' como variável. Ex: x*x, sin(x), cos(x)*2, sqrt(abs(x))")]
    public string functionExpression = "x*x";

    private float radius = 0f;
    private Vector3 startPos;
    private float[] functionLUT; // Look-Up Table
    private int lutSize = 8192;
    private float lutRange = 30f; // alcance de -25 a +25

    [Header("Qualidade da Curva")]
    [Range(8, 128)]
    [Tooltip("Mais amostras = curva mais precisa, mas mais pesado")]
    public int sampleSteps = 32;

    void Start()
    {
        startPos = transform.position;
        shockwaveMaterial.SetVector("_Origin", startPos);
        shockwaveMaterial.SetInt("_SampleSteps", sampleSteps);
        GenerateFunctionLUT();
    }

    void GenerateFunctionLUT()
    {
        functionLUT = new float[lutSize];

        for (int i = 0; i < lutSize; i++)
        {
            float t = (float)i / (lutSize - 1); // 0 a 1
            float x = (t - 0.5f) * lutRange; // -range/2 a +range/2

            try
            {
                functionLUT[i] = EvaluateFunction(x);
            }
            catch (Exception e)
            {
                Debug.LogError($"Erro ao avaliar função em x={x}: {e.Message}");
                functionLUT[i] = 0;
            }
        }

        // Passa a LUT para o shader como textura
        Texture2D functionTexture = new Texture2D(lutSize, 1, TextureFormat.RFloat, false);
        functionTexture.wrapMode = TextureWrapMode.Clamp;
        functionTexture.filterMode = FilterMode.Bilinear;

        Color[] colors = new Color[lutSize];
        for (int i = 0; i < lutSize; i++)
        {
            colors[i] = new Color(functionLUT[i], 0, 0, 1);
        }

        functionTexture.SetPixels(colors);
        functionTexture.Apply();

        shockwaveMaterial.SetTexture("_FunctionLUT", functionTexture);
        shockwaveMaterial.SetFloat("_LUTRange", lutRange);
    }

    float EvaluateFunction(float x)
    {
        // Evita divisão por zero
        if (Mathf.Abs(x) < 0.01f)
            return Mathf.Sign(x) * 100f; // Retorna valor grande mas finito

        string expression = functionExpression.Replace(
            "x",
            x.ToString(System.Globalization.CultureInfo.InvariantCulture)
        );

        expression = expression.Replace("sin", "Math.Sin");
        expression = expression.Replace("cos", "Math.Cos");
        expression = expression.Replace("tan", "Math.Tan");
        expression = expression.Replace("sqrt", "Math.Sqrt");
        expression = expression.Replace("abs", "Math.Abs");
        expression = expression.Replace("pow", "Math.Pow");

        DataTable dt = new DataTable();

        try
        {
            var result = dt.Compute(expression, "");
            float value = Convert.ToSingle(result);

            // Clamp para evitar valores extremos
            return Mathf.Clamp(value, -50f, 50f);
        }
        catch
        {
            return 0f;
        }
    }

    void Update()
    {
        radius += speed * Time.deltaTime;
        shockwaveMaterial.SetFloat("_Radius", radius);

        // Aumenta espessura com o raio para compensar aliasing
        float adaptiveThickness = 0.1f + radius * 0.02f;
        shockwaveMaterial.SetFloat("_CurveThickness", adaptiveThickness);

        if (radius > maxRadius)
            radius = 0f;
    }

    void OnValidate()
    {
        if (Application.isPlaying && shockwaveMaterial != null)
        {
            shockwaveMaterial.SetInt("_SampleSteps", sampleSteps);
            GenerateFunctionLUT();
        }
    }
}
