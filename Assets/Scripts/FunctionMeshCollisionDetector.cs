using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FunctionMeshCollisionDetector : MonoBehaviour
{
    private static HashSet<Collider> collidersInside = new HashSet<Collider>();

    private void OnTriggerEnter(Collider other)
    {
        if (collidersInside.Count == 0)
        {
            
            Debug.Log("Trigger ENTER (como se fosse um só)");
        }

        collidersInside.Add(other);
    }

    private void OnTriggerExit(Collider other)
    {
        collidersInside.Remove(other);

        if (collidersInside.Count == 0)
        {
            
            Debug.Log("Trigger EXIT");
        }
    }



}
