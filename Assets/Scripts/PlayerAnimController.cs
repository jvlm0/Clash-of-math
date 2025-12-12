using UnityEngine;

public class PlayerAnimController : MonoBehaviour
{
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    public void walk()
    {
        animator.SetBool(AnimContants.walkBool, true);
    }

    public void idle()
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

    public void die()
    {
        animator.SetTrigger(AnimContants.deathTrigger);
    }

    public void attack()
    {
        animator.SetTrigger(AnimContants.standingAttackTrigger);
    }

    public void run()
    {
        animator.SetBool(AnimContants.runBool, true);
    }  
}
