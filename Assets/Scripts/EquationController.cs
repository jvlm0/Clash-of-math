using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EquationController : MonoBehaviour
{
    private string currentEquation = "";
    public static EquationController instance;
    public GameObject functionMeshPrefab;

    //private string[] operators = { "+", "-", "*", "/", "^" };

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void AppendEquation(string equation)
    {
        
        if (equation.StartsWith('+') || equation.StartsWith('-'))
        {
            currentEquation += equation;
        } else if (currentEquation == "")
        {
            currentEquation = equation;
        }
        else
        {
            currentEquation += "*" + equation;
        }
        Debug.Log("Equação atual: " + currentEquation);

        
    }

    public void SpawnFunctionMesh(Vector3 position)
    {
        GameObject functionMesh = Instantiate(functionMeshPrefab, position, Quaternion.identity);

        FunctionMeshGenerator meshGenerator = functionMesh.GetComponent<FunctionMeshGenerator>();

        meshGenerator.mathExpression = currentEquation;

    }   
}
