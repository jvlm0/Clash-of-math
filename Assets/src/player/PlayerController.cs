using UnityEngine;

public class PlayerController : MonoBehaviour, IAnimController
{
    LifeBarController lifeBarController;
    private Animator animator;


    public GameObject hammerBelt;
    public GameObject leftPistol;
    public GameObject rightPistol;
    public GameObject meeeleHammer;
  



    public enum AttackMode
    {
        HammerBelt,
        OnePistol,
        TwoPistol,
        Meele
    }

    private int baseLayer;
    void Start()
    {
        animator = GetComponent<Animator>();
        lifeBarController = GetComponent<LifeBarController>();

        baseLayer = animator.GetLayerIndex("Base");

        
        EnablePistols();

       

    }


    public void GetAnimDuration(string animName)
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.IsName(animName))
        {
            float animDuration = stateInfo.length;
            Debug.Log("Duração da animação " + animName + ": " + animDuration + " segundos");
        }
        else
        {
            Debug.Log("Animação " + animName + " não está sendo reproduzida atualmente.");
        }
    }

    public void Walk()
    {
        animator.SetBool(AnimContants.walkBool, true);
    }

    public void Idle()
    {
        animator.SetBool(AnimContants.walkBool, false);
    }

    public void stopWalk()
    {
        animator.SetBool(AnimContants.walkBool, false);
    }

    public void stopRun()
    {
        animator.SetBool(AnimContants.runBool, false);
    }

    public void jump()
    {
        animator.SetTrigger(AnimContants.jumpTrigger);
    }

    public void Death()
    {
        animator.SetTrigger(AnimContants.deathTrigger);
    }

    public void Attack()
    {
        animator.SetFloat("AttackSpeed", GetComponent<StatusController>().attackSpeed); 
        animator.SetTrigger("Attack");

        //GetComponent<IGunController>()?.Attack();

        
    }

    public void Run()
    {
        animator.SetBool(AnimContants.runBool, true);
    }

    public void GetDamage(float damageAmount)
    {
        Debug.Log("Player received damage: " + damageAmount);
        lifeBarController.UpdateLifeBar(damageAmount);
    }

    public void SetAttackMode(AttackMode attackMode)
    {
        int i = -1;
        if (attackMode == AttackMode.OnePistol)
        {
            i = animator.GetLayerIndex("OnePistol");
            animator.SetLayerWeight(i, 1f);
        } 
        else if (attackMode == AttackMode.TwoPistol)
        {
            i = animator.GetLayerIndex("DualGun");
            animator.SetLayerWeight(i, 1f);  
        }
        else if (attackMode == AttackMode.HammerBelt)
        {
            
        }
        else if (attackMode == AttackMode.Meele)
        {
            i = animator.GetLayerIndex("Meele");
            animator.SetLayerWeight(i, 1f);
        }

        
            
        DisableOthers(i);
    }


    private void DisableOthers(int a)
    {
        int end = animator.layerCount;          

        for (int i = 0; i < end; i++)
        {
            if (i == a || i == baseLayer) continue;

            animator.SetLayerWeight(i, 0);
        }
    }



    public void MeeleState()
    {
        hammerBelt.SetActive(false);
        leftPistol.SetActive(false);
        rightPistol.SetActive(false);
        meeeleHammer.SetActive(true);

        SetAttackMode(AttackMode.Meele);
    }


    public void EnablePistols()
    {
        hammerBelt.SetActive(false);
        leftPistol.SetActive(true);
        rightPistol.SetActive(true);
        meeeleHammer.SetActive(false);

        SetAttackMode(AttackMode.TwoPistol);
    }


    public void EnableHammerbelt()
    {
        hammerBelt.SetActive(true);
        leftPistol.SetActive(false);
        rightPistol.SetActive(false);
        meeeleHammer.SetActive(false);

        SetAttackMode(AttackMode.HammerBelt);
    }

}
