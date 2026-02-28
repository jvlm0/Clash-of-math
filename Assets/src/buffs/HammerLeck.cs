using UnityEngine;

public class HammerLeck : MonoBehaviour
{
    public float spinSpeed;
    public float damage = 60f;

    public bool stop = false;

    void Start ()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).gameObject.AddComponent<HammerLeckCollision>();
        }
    }

    void Update()
    {
        if (!stop)
            transform.Rotate(Vector3.right * spinSpeed * Time.deltaTime);



        
    }

    
}



public class HammerLeckCollision : MonoBehaviour
{
    public void OnCollisionEnter(Collision collision)
    {
        Debug.Log("HammerLeck Colidiu com: " + collision.gameObject.name);
        if (collision.gameObject.CompareTag("npc"))
        {
            collision.gameObject.GetComponent<IAnimController>()?.GetDamage(gameObject.GetComponentInParent<HammerLeck>().damage);
        }

        if (collision.gameObject.CompareTag("Ground"))
        {
            gameObject.GetComponentInParent<HammerLeck>().stop = true;
            Destroy(transform.parent.gameObject, 1f);
        }
    }
}