using System.Collections;
using UnityEngine;




public class DualGunController: MonoBehaviour, IGunController
{
    public ParticleSystem gun1;
    public ParticleSystem gun2;

    public float diffFire = .5f;

    public void Attack()
    {
        gun1.Play();
    }

    public IEnumerator delayGun2()
    {
        yield return new WaitForSeconds(diffFire);
        gun2.Play();
    }


    

}