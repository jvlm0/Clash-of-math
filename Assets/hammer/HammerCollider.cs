using UnityEngine;





public class HammerCollider: MonoBehaviour
{
    void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Colidiu com: " + collision.gameObject.name);
        collision.gameObject.GetComponent<IAnimController>()?.GetDamage(50);
    }
}