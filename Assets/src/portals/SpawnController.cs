using System.Collections.Generic;
using UnityEngine;

public class SpawnController : MonoBehaviour
{
    public List<PrefabCountPair> playerPrefabCountPairs = new List<PrefabCountPair>();

    public List<PrefabCountPair> enemyPrefabCountPairs = new List<PrefabCountPair>();

    public GameObject functionEnemyMeshPrefab;

    public GameObject functionPlayerMeshPrefab;

    public static SpawnController Instance;

    public Transform transformFunctionPos;


    public LayerMask playerLayerMask;

    public LayerMask enemyLayerMask;

    public Camera cameraToLookAt;

    public int enemyPrefabLayer;
    public int playerPrefabLayer;


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

    public void SpawnMesh(GameObject functionMeshPrefab, string expression, List<PrefabCountPair> prefabCountPairs, LayerMask layerMask, int layer)
    {
        GameObject functionMesh = Instantiate(functionMeshPrefab, transformFunctionPos.position,Quaternion.identity);

        functionMesh.GetComponent<FunctionMeshGenerator>().mathExpression = expression;

        
        functionMesh.GetComponent<FunctionPrefabSpawner>().prefabCountPairs = prefabCountPairs;
        functionMesh.GetComponent<FunctionPrefabSpawner>().targetLayer = layerMask;
        functionMesh.GetComponent<FunctionPrefabSpawner>().cameraToLookAt = cameraToLookAt;
        functionMesh.GetComponent<FunctionPrefabSpawner>().prefabsLayer = layer;
    }

    public void SpawnEnemyMesh(string expression)
    {
        SpawnMesh(functionEnemyMeshPrefab, expression, enemyPrefabCountPairs, playerLayerMask, enemyPrefabLayer);
    }

    public void SpawnPlayerMesh(string expression)
    {
        SpawnMesh(functionPlayerMeshPrefab, expression, playerPrefabCountPairs, enemyLayerMask, playerPrefabLayer);
    }
}
