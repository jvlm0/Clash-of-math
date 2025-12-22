using UnityEngine;

public class NpcController : MonoBehaviour, IAnimController
{
    
    private Animator animator;

    private float currentLife;

    private Transform currentTarget = null;

    private bool canAttack = true;

    void Start()
    {
        animator = GetComponent<Animator>();
        currentLife = GetComponent<StatusController>().hp;
    }

    public void Attack()
    {
        //if (canAttack)
        //{
            Debug.Log("NPC Atacando"); 
            animator.SetTrigger("Attack");
            animator.SetBool("IsWalking", false);
            animator.SetBool("IsRunning", false);
            animator.SetBool("IsIdle", false);
            canAttack = false;
        //}
    }

    public void Walk()
    {
        animator.SetBool("IsWalking", true);
        animator.SetBool("IsRunning", false);
        animator.SetBool("IsIdle", false);
    }

    public void Run()
    {
        animator.SetBool("IsRunning", true);
        animator.SetBool("IsWalking", false);
        animator.SetBool("IsIdle", false);
    }

    public void Death()
    {
        animator.SetTrigger("Death");
    }

    public void Idle()
    {
        animator.SetBool("IsWalking", false);
        animator.SetBool("IsRunning", false);
        animator.SetBool("IsIdle", true);
    }

    public void GetDamage(float damageAmount)
    {
        animator.SetTrigger("GetDamage");
        currentLife -= damageAmount;
        GetComponent<LifeBarController>().UpdateLifeBar(currentLife);
    }

    private void OnHitFrame()
    {
        GetComponent<IAtackHandler>()?.Atack();
    }

    private void OnAttackEnd()
    {
        canAttack = true;
        Debug.Log("NPC pode atacar novamente");
    }
}
