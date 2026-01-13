using System.Collections.Generic;
using UnityEngine;

public class SpawnTroopsController : MonoBehaviour
{
    public List<PrefabCountPair> playerPrefabCountPairs = new List<PrefabCountPair>();

    public List<PrefabCountPair> enemyPlayerPrefabCountPairs = new List<PrefabCountPair>();

    public static SpawnTroopsController Instance;

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
}
