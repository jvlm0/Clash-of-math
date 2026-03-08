using UnityEngine;



public class BuffCollision : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("hammerbelt"))
        {
             GetComponent<PlayerController>().EnableHammerbelt();
        }
        else if (other.gameObject.CompareTag("pistol"))
        {
            GetComponent<PlayerController>().EnablePistols();
            Debug.Log("Pistol buff coletado!");
        } else if (other.gameObject.CompareTag("ActiveBuff"))
        {

            
            var buff = other.gameObject.GetComponent<Buff>();
            GameController.Instance.ActivateBuff(buff, other.transform.position, other.transform.rotation);
        }

        //Destroy(other.gameObject);
    }
}


