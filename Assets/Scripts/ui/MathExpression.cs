using System;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;




public class MathExpression : MonoBehaviour
{
    public GameObject multiplicationSymbol, plusSymbol, expressionSymbol, interrerogationSymbol;

    public static MathExpression Instance;

    public int numberOfTerms = 0;

    private int currentTerm = 0;

    private GameObject lastInterrogationMark = null;


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
    }


    public void AddNext(string exp, bool isCorrect)
    {
        currentTerm++;
        if (exp[0] == '+' || exp[0] == '-')
        {
            GameObject go = Instantiate(plusSymbol, transform);
            go.GetComponentInChildren<TextMeshProUGUI>().text = exp[0].ToString();

            GameObject go2 = Instantiate(expressionSymbol, transform);
            go2.GetComponentInChildren<TextMeshProUGUI>().text = exp.Substring(1);

            if (!isCorrect)
            {
                go.GetComponent<Image>().color = Color.red;
            }
        }
        else
        {
            if (currentTerm > 1)
            {
                Instantiate(multiplicationSymbol, transform);
            }
            
            GameObject go = Instantiate(expressionSymbol, transform);
            go.GetComponentInChildren<TextMeshProUGUI>().text = exp;

            if (!isCorrect)
            {
                go.GetComponent<Image>().color = Color.red;
            }
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

    // This class can be expanded with methods to evaluate the expression,
    // display it in the UI, or interact with other game systems as needed.
}