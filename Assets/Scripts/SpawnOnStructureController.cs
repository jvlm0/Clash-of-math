using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class SpawnOnStructureController : MonoBehaviour
{
    public List<GameObject> slotsToSpawnOn = new List<GameObject>();


    public static SpawnOnStructureController Instance;

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


    public void SpawnOnStructures()
    {

        StartCoroutine(SpawnOnStructuresCoroutine());
    } 


    IEnumerator SpawnOnStructuresCoroutine()
    {
        yield return new WaitForEndOfFrame();
        var prefabCountPairs = SpawnController.Instance.enemyPrefabCountPairs;

        ShuffleUtils.ShuffleList(slotsToSpawnOn);

        int j = 0;
        for (int i = 0; i < prefabCountPairs.Count; i++)
        {
            PrefabCountPair pair = prefabCountPairs[i];
            for (; j < pair.count; j++)
            {
                Debug.Log($"Tamanho da lista de slots: {slotsToSpawnOn.Count} ");
                Instantiate(pair.prefab, slotsToSpawnOn[j].transform.position, slotsToSpawnOn[j].transform.rotation);
            }
        }
    }
}