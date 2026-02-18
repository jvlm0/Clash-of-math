using System.Collections;
using UnityEngine;




public class DualGunController: MonoBehaviour, IGunController
{
    public ShotgunController gun1;
    public ShotgunController gun2;

    public float fireInterval = .5f;





    public void Attack()
    {
        gun1.Shoot();
        StartCoroutine(delayGun2());
    }

    public IEnumerator delayGun2()
    {
        yield return new WaitForSeconds(fireInterval);
        gun2.Shoot();
    }

    void OnFireFrame()
    {
        Attack();
    }

}