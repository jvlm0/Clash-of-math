using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EquationController : MonoBehaviour
{
    public string currentEquation = "";
    public static EquationController instance;
    public GameObject functionMeshPrefab;

    public FunctionCanvasManager playerFunctionManager;

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
    void Start() { }

    // Update is called once per frame
    void Update() { }

    public void AppendEquation(string equation)
    {
        
        if (equation.StartsWith('+') || equation.StartsWith('-'))
        {
            currentEquation += equation;
        }
        else if (currentEquation == "")
        {
            currentEquation = equation;
        }
        else
        {
            currentEquation = "(" + currentEquation + ")" + equation;
        }
        

        

        playerFunctionManager.UpdatePlayerFunction(currentEquation);

        
    }

    public void SpawnFunctionMesh(Vector3 position)
    {
        SpawnController.Instance.SpawnPlayerMesh(currentEquation);
    }
}
