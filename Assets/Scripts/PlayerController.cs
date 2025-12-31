using UnityEngine;

public class PlayerController : MonoBehaviour, IAnimController
{
    LifeBarController lifeBarController;
    private Animator animator;


    void Start()
    {
        animator = GetComponent<Animator>();
        lifeBarController = GetComponent<LifeBarController>();
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
        animator.SetTrigger(AnimContants.standingAttackTrigger);
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

}
