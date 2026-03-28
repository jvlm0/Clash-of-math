using UnityEngine;



public class BuffCollision : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("hammerbelt"))
        {
             GetComponent<PlayerController>().EnableHammerbelt();
             Destroy(other.gameObject);
        }
        else if (other.gameObject.CompareTag("pistol"))
        {
            GetComponent<PlayerController>().SetAttackMode(PlayerController.AttackMode.DualGun);
            Debug.Log("Pistol buff coletado!");
            Destroy(other.gameObject);
        } else if (other.gameObject.CompareTag("ActiveBuff"))
        {

            
            var buff = other.gameObject.GetComponent<Buff>();
            GameController.Instance.ActivateBuff(buff, other.transform.position, other.transform.rotation);
            Destroy(other.gameObject);
        }

        //Destroy(other.gameObject);
    }
}


