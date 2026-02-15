using System.Collections;
using UnityEngine;

public class PlayerMeeleAttackHandler : MonoBehaviour, IAttackHandler
{
    public void HandleAttack()
    {
        //if (Input.GetMouseButtonDown(0) && !launcher.IsLaunched() && !isAttacking)
        //{

        GetComponent<IAnimController>()?.Attack();

        //GetComponent<IAtackHandler>()?.Atack();
        //}
    }
}
