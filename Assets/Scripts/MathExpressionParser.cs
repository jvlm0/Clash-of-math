using UnityEngine;
using System;
using System.Collections.Generic;
using System.Globalization;

public class MathExpressionParser
{
    private string expression;
    private int position;
    private char currentChar;

    public MathExpressionParser(string expr)
    {
        expression = expr.Replace(" ", "").ToLower();
        position = 0;
        currentChar = expression.Length > 0 ? expression[0] : '\0';
    }

    private void Advance()
    {
        position++;
        currentChar = position < expression.Length ? expression[position] : '\0';
    }

    private void SkipWhitespace()
    {
        while (currentChar == ' ')
            Advance();
    }

    // Analisa números (incluindo decimais)
    private float ParseNumber()
    {
        string numStr = "";
        while (char.IsDigit(currentChar) || currentChar == '.' || currentChar == ',')
        {
            numStr += currentChar == ',' ? '.' : currentChar;
            Advance();
        }
        return float.Parse(numStr, CultureInfo.InvariantCulture);
    }

    // Analisa funções matemáticas
    private float ParseFunction(float x)
    {
        string funcName = "";
        while (char.IsLetter(currentChar))
        {
            funcName += currentChar;
            Advance();
        }

        SkipWhitespace();
        
        // Espera um '(' após o nome da função
        if (currentChar == '(')
        {
            Advance();
            float arg = ParseExpression(x);
            
            if (currentChar == ')')
                Advance();
            
            // Aplica a função
            switch (funcName)
            {
                case "sin": return Mathf.Sin(arg);
                case "cos": return Mathf.Cos(arg);
                case "tan": return Mathf.Tan(arg);
                case "sqrt": return Mathf.Sqrt(arg);
                case "abs": return Mathf.Abs(arg);
                case "log": return Mathf.Log(arg);
                case "ln": return Mathf.Log(arg);
                case "exp": return Mathf.Exp(arg);
                case "floor": return Mathf.Floor(arg);
                case "ceil": return Mathf.Ceil(arg);
                case "round": return Mathf.Round(arg);
                default:
                    Debug.LogWarning($"Função desconhecida: {funcName}");
                    return 0;
            }
        }
        
        Debug.LogWarning($"Esperado '(' após função {funcName}");
        return 0;
    }

    // Analisa fatores (números, variáveis, funções, parênteses)
    private float ParseFactor(float x)
    {
        SkipWhitespace();
        
        // Números negativos
        if (currentChar == '-')
        {
            Advance();
            return -ParseFactor(x);
        }
        
        // Números positivos explícitos
        if (currentChar == '+')
        {
            Advance();
            return ParseFactor(x);
        }
        
        // Números
        if (char.IsDigit(currentChar))
        {
            return ParseNumber();
        }
        
        // Variável x
        if (currentChar == 'x')
        {
            Advance();
            return x;
        }
        
        // Constantes
        if (currentChar == 'e' && (position + 1 >= expression.Length || !char.IsLetter(expression[position + 1])))
        {
            Advance();
            return Mathf.Exp(1); // e = 2.71828...
        }
        
        if (expression.Substring(position).StartsWith("pi"))
        {
            Advance();
            Advance();
            return Mathf.PI;
        }
        
        // Funções
        if (char.IsLetter(currentChar))
        {
            return ParseFunction(x);
        }
        
        // Parênteses
        if (currentChar == '(')
        {
            Advance();
            float result = ParseExpression(x);
            if (currentChar == ')')
                Advance();
            return result;
        }
        
        Debug.LogWarning($"Caractere inesperado: {currentChar}");
        return 0;
    }

    // Analisa potências (x^2, 2^x, etc.)
    private float ParsePower(float x)
    {
        float result = ParseFactor(x);
        
        while (currentChar == '^' || currentChar == '²' || currentChar == '³')
        {
            if (currentChar == '²')
            {
                Advance();
                result = result * result;
            }
            else if (currentChar == '³')
            {
                Advance();
                result = result * result * result;
            }
            else // ^
            {
                Advance();
                float exponent = ParseFactor(x);
                result = Mathf.Pow(result, exponent);
            }
        }
        
        return result;
    }

    // Analisa multiplicação implícita (2x, 3sin(x), etc.) e divisão/multiplicação explícita
    private float ParseTerm(float x)
    {
        float result = ParsePower(x);
        
        while (true)
        {
            SkipWhitespace();
            
            // Multiplicação explícita
            if (currentChar == '*')
            {
                Advance();
                result *= ParsePower(x);
            }
            // Divisão
            else if (currentChar == '/')
            {
                Advance();
                result /= ParsePower(x);
            }
            // Multiplicação implícita (2x, 3sin(x), etc.)
            else if (char.IsDigit(currentChar) || currentChar == 'x' || char.IsLetter(currentChar) || currentChar == '(')
            {
                result *= ParsePower(x);
            }
            else
            {
                break;
            }
        }
        
        return result;
    }

    // Analisa adição e subtração
    private float ParseExpression(float x)
    {
        float result = ParseTerm(x);
        
        while (currentChar == '+' || currentChar == '-')
        {
            char op = currentChar;
            Advance();
            
            if (op == '+')
                result += ParseTerm(x);
            else
                result -= ParseTerm(x);
        }
        
        return result;
    }

    // Método público para avaliar a expressão
    public float Evaluate(float x)
    {
        try
        {
            position = 0;
            currentChar = expression.Length > 0 ? expression[0] : '\0';
            return ParseExpression(x);
        }
        catch (Exception e)
        {
            Debug.LogError($"Erro ao avaliar expressão '{expression}': {e.Message}");
            return 0;
        }
    }

    // Método estático para uso rápido
    public static float Eval(string expr, float x)
    {
        MathExpressionParser parser = new MathExpressionParser(expr);
        return parser.Evaluate(x);
    }
}