using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FunctionMeshCollisionDetector : MonoBehaviour
{
    public bool freezeEffect = true;
    private static HashSet<Collider> collidersInside = new HashSet<Collider>();

    private void OnTriggerEnter(Collider other)
    {
        if (!collidersInside.Contains(other))
        {
            if (freezeEffect)
            {
                other.GetComponent<FreezeController>()?.FreezeWithCoroutine();
            }

            other.GetComponentInChildren<IAnimController>().GetDamage(10f);
            
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
