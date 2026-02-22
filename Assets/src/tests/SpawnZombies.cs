using UnityEngine;


public class SpawnZombies : MonoBehaviour
{
    public GameObject zombie;
    public float spawnInterval = 3f;


    private float spawnTime = 0;

    void Update()
    {
        if (spawnTime >= spawnInterval)
        {
            spawnTime = 0;
            Instantiate(zombie, transform.position, Quaternion.identity);
        }

        spawnTime+=Time.deltaTime;
    }
}