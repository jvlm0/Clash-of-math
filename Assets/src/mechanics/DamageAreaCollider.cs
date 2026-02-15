using UnityEngine;



public class DamageAreaCollider : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {   
        Debug.Log("Dano em área atingiu: " + other.name);
        if (((1 << other.gameObject.layer) & GetComponentInParent<StatusController>().targetLayer) != 0)
        {
            Debug.Log("Objeto pertence a layer: " + other.name);
            other.GetComponent<IAnimController>()
            ?.GetDamage(GetComponentInParent<StatusController>().damage);
        }
    }
}