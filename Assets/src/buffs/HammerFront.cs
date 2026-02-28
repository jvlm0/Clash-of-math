using System.Numerics;
using UnityEngine;



public class HammerFront : MonoBehaviour
{
    public float damage = 100f;
    public float speed = 50f;
    public float distance = 20f;
    private Rigidbody rb;

    void Start ()
    {
        rb = GetComponent<Rigidbody>();
    
        rb.AddForce(transform.forward * speed, ForceMode.VelocityChange);

        Destroy(gameObject, speed / distance);
    }


    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("HammerFront Colidiu com: " + collision.gameObject.name);
        if (collision.gameObject.CompareTag("npc"))
        {
            collision.gameObject.GetComponent<IAnimController>()?.GetDamage(gameObject.GetComponentInParent<HammerFront>().damage);
        }
    }


    
}
