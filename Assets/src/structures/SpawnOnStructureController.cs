using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnOnStructureController : MonoBehaviour
{
    public List<GameObject> slotsToSpawnOn = new List<GameObject>();

    public bool spawnZombies = true;
    public GameObject zombie;

    public static SpawnOnStructureController Instance;

    private float physicsDelayTime = 0.1f;

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
        if (spawnZombies)
        {
            StartCoroutine(SpawnZombies());
        }
        else
        {
            StartCoroutine(SpawnOnStructuresCoroutine());
        }
        
    }

    IEnumerator SpawnZombies()
    {
        yield return new WaitForEndOfFrame();

        foreach (var slot in slotsToSpawnOn)
        {
            var go = Instantiate(zombie, slot.transform.position, slot.transform.rotation);

            go.GetComponent<NpcController>().DisableIa();
            SetRigidbodiesActive(go, false);

            StartCoroutine(EnablePhysicsDelayed(go));
        }
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
                var go = Instantiate(
                    pair.prefab,
                    slotsToSpawnOn[j].transform.position,
                    slotsToSpawnOn[j].transform.rotation
                );

                go.GetComponent<NpcController>().DisableIa();
                SetRigidbodiesActive(go, false);

                StartCoroutine(EnablePhysicsDelayed(go));
            }
        }
    }

    private void SetRigidbodiesActive(GameObject obj, bool active)
    {
        Rigidbody[] rigidbodies = obj.GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody rb in rigidbodies)
        {
            rb.isKinematic = !active;
        }
    }

    private System.Collections.IEnumerator EnablePhysicsDelayed(GameObject obj)
    {
        yield return new WaitForSeconds(physicsDelayTime);
        Rigidbody[] rigidbodies = obj.GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody rb in rigidbodies)
        {
            rb.isKinematic = false;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.Sleep();
        }
    }
}
