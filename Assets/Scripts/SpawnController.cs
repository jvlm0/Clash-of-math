using System.Collections.Generic;
using UnityEngine;

public class SpawnController : MonoBehaviour
{
    public List<PrefabCountPair> playerPrefabCountPairs = new List<PrefabCountPair>();

    public List<PrefabCountPair> enemyPrefabCountPairs = new List<PrefabCountPair>();

    public GameObject functionMeshPrefab;

    public static SpawnController Instance;

    public Transform transformFunctionPos; 

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

    public void SpawnEnemyMesh(string expression)
    {
        GameObject functionMesh = Instantiate(functionMeshPrefab, transformFunctionPos.position,Quaternion.identity);
        functionMesh.GetComponent<FunctionMeshGenerator>().mathExpression = expression;

        functionMesh.GetComponent<FunctionPrefabSpawner>().prefabCountPairs = enemyPrefabCountPairs;
    }

    public void SpawnPlayerMesh(string expression)
    {
        GameObject functionMesh = Instantiate(functionMeshPrefab, transformFunctionPos.position,Quaternion.identity);
        functionMesh.GetComponent<FunctionMeshGenerator>().mathExpression = expression;

        functionMesh.GetComponent<FunctionPrefabSpawner>().prefabCountPairs = playerPrefabCountPairs;
    }
}
