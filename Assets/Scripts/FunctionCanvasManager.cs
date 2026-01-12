using System;
using UnityEngine;

public class FunctionCanvasManager : MonoBehaviour
{
    public FunctionCanvasGenerator enemyFunction;
    public FunctionCanvasGenerator playerFunction;

    float yMin, yMax;

    public void UpdateFunctions(
        FunctionCanvasGenerator f1,
        FunctionCanvasGenerator f2,
        string expression,
        string expression2 = "",
        bool inst = false
    )
    {
        // CALCULA os limites sem atualizar visualmente
        (float pYMin, float pYMax) = f1.CalculateYLimitsForExpression(expression);
        (float eYMin, float eYMax) = f2.CalculateYLimitsForExpression(expression2);

        Debug.Log($"Limite função player pYMin {pYMin} pYMax {pYMax}");
        Debug.Log($"Limite função inimigo eYMin {eYMin} eYMax {eYMax}");

        // Determina os limites unificados
        yMin = Mathf.Min(pYMin, eYMin);
        yMax = Mathf.Max(pYMax, eYMax);

        // Desabilita viewport adaptativo e define limites manualmente
        f1.useAdaptiveViewport = false;
        f2.useAdaptiveViewport = false;

        f1.SetYLimits(yMin, yMax);
        f2.SetYLimits(yMin, yMax);

        // Agora sim, atualiza os gráficos COM ou SEM transição
        if (!inst)
        {
            f1.UpdateGraph(expression);
            f2.UpdateGraph(expression2);
        }
        else
        {
            f1.UpdateGraphInstant(expression);
            f2.UpdateGraphInstant(expression2);
        }

        Debug.Log($"AdaptiveViewPort f1 {f1.useAdaptiveViewport}  f2 {f2.useAdaptiveViewport}");
    }

    public void UpdateEnemyFunction(string expression, string expression2 = "")
    {
        UpdateFunctions(enemyFunction, playerFunction, expression, expression2);
    }

    public void UpdatePlayerFunction(string expression, string expression2 = "", bool inst = false)
    {
        UpdateFunctions(playerFunction, enemyFunction, expression, expression2, inst);
    }
}