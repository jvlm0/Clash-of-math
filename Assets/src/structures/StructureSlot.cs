using UnityEngine;




public class StructureSlot : MonoBehaviour
{
    
    void Awake()
    {
        SpawnOnStructureController.Instance.slotsToSpawnOn.Add(this.gameObject);
        
    }
    void Start()
    {
        
    }




}