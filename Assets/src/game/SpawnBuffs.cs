using UnityEngine;
using UnityEngine.UIElements;

public class SpawnBuffs : MonoBehaviour
{
    public GameObject[] buffs;

    public static SpawnBuffs instance;

    public int prob = 10;

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

    public void SpawnBuff(Vector3 pos)
    {
        if (Random.Range(0, 100) > 100 - prob)
        {
            int index = Random.Range(0, buffs.Length);
            GameObject buff = Instantiate(buffs[index], pos, Quaternion.identity);
        } //buff.transform.SetParent(transform);
    }
}
