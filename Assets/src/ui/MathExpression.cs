using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MathExpression : MonoBehaviour
{
    public GameObject opertationSymbol,
        expressionSymbol,
        interrerogationSymbol,
        parentesesSymbol,
        fractionSymbol;

    public static MathExpression Instance;

    public float scaleFactor = 0.8f;

    public Sprite blueExpressionSprite,
        redExpressionSprite,
        subSprite,
        multiplicationSprite,
        plusSprite;

    public int numberOfTerms = 0;

    private int currentTerm = 0;

    private GameObject lastInterrogationMark = null;

    // Rastreia se a expressão atual tem múltiplos termos (com + ou -)
    private bool hasMultipleTerms = false;

    // Índice do primeiro elemento da expressão completa (após o primeiro termo)
    private int expressionStartIndex = 0;

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

    void Start()
    {
        lastInterrogationMark = Instantiate(interrerogationSymbol, transform);
        expressionStartIndex = 0;
    }

    public void AddNext(string exp, bool isCorrect)
    {
        currentTerm++;

        if (exp[0] == '+' || exp[0] == '-')
        {
            GameObject go = Instantiate(opertationSymbol, transform);
            go.GetComponent<Image>().sprite = exp[0] == '+' ? plusSprite : subSprite;

            InstantiateExpresion(exp.Substring(1), isCorrect);
            
            // Marca que agora temos múltiplos termos
            hasMultipleTerms = true;
        }
        else
        {
            // Se é uma multiplicação e já temos múltiplos termos,
            // precisamos adicionar parênteses ao redor de TODA a expressão anterior
            if (currentTerm > 1)
            {
                if (hasMultipleTerms)
                {
                    AddParenthesesAroundExpression();
                }

                var go1 = Instantiate(opertationSymbol, transform);
                go1.GetComponent<Image>().sprite = multiplicationSprite;

                // Reset: após a multiplicação, começamos uma nova "sub-expressão"
                hasMultipleTerms = false;
                expressionStartIndex = transform.childCount;
            }

            InstantiateExpresion(exp, isCorrect);
            
        }

        if (lastInterrogationMark != null)
        {
            Destroy(lastInterrogationMark);
        }

        if (currentTerm < numberOfTerms)
        {
            lastInterrogationMark = Instantiate(interrerogationSymbol, transform);
        }
    }



    void InstantiateExpresion(string exp, bool isCorrect)
    {
        GameObject go;
        if (exp.Contains('/'))
        {
            go = Instantiate(fractionSymbol, transform);

            go.GetComponentsInChildren<TextMeshProUGUI>()[0].text = exp.Substring(0, exp.IndexOf('/') - 1);
            go.GetComponentsInChildren<TextMeshProUGUI>()[1].text = exp.Substring(exp.IndexOf('/') + 1);
        }
        else
        {
            go =  Instantiate(expressionSymbol, transform);
            go.GetComponentInChildren<TextMeshProUGUI>().text = exp;
        }

        if (!isCorrect)
        {
            go.GetComponent<Image>().sprite = redExpressionSprite;
        }
    }


    private void AddParenthesesAroundExpression()
    {
        // Cria o parêntese de abertura
        GameObject openParen = Instantiate(parentesesSymbol, transform);
        openParen.GetComponentInChildren<TextMeshProUGUI>().text = "(";
        openParen.GetComponent<Image>().color = Color.green;

        // Move o parêntese de abertura para o início da expressão
        openParen.transform.SetSiblingIndex(0);

        // Cria o parêntese de fechamento
        GameObject closeParen = Instantiate(parentesesSymbol, transform);
        closeParen.GetComponentInChildren<TextMeshProUGUI>().text = ")";
        closeParen.GetComponent<Image>().color = Color.green;

        // O parêntese de fechamento vai antes da interrogação (se existir)
        if (lastInterrogationMark != null)
        {
            closeParen.transform.SetSiblingIndex(lastInterrogationMark.transform.GetSiblingIndex());
        }

        for (int i = 1; i < transform.childCount - 2; i++)
        {
            Transform child = transform.GetChild(i);
            var rt = child.transform as RectTransform;
            rt.sizeDelta = new Vector2(rt.sizeDelta.x * scaleFactor, rt.sizeDelta.y * scaleFactor);

            if (child.childCount > 0)
            {
                var textRt = child.GetChild(0) as RectTransform;

                textRt.sizeDelta = new Vector2(
                    textRt.sizeDelta.x * scaleFactor,
                    textRt.sizeDelta.y * scaleFactor
                );
            }
        }
    }
}
