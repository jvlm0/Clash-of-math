using UnityEngine;



public class ObjectCollision : MonoBehaviour
{

    public GameObject functionPrefab;
    public Transform transformFunctionPos;

    void OnTriggerEnter(Collider other)
    {

        if (other.gameObject.CompareTag("Structure") || other.gameObject.CompareTag("Ground"))
        {
            Instantiate(functionPrefab, transformFunctionPos.position,Quaternion.identity);
        }

        Destroy(this.gameObject, .2f);
    }
}