using UnityEngine;

public class Skeleton1Controller : MonoBehaviour, IAnimController
{
    [SerializeField] 
    private float attackRange = 2.0f;
    [SerializeField]
    private float damage = 10.0f;
    [SerializeField]
    private float hp = 100.0f;
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    public void Attack()
    {
        animator.SetTrigger("Attack");
    }

    public void Walk()
    {
        animator.SetBool("isWalking", true);
        animator.SetBool("isRunning", false);
    }

    public void Run()
    {
        animator.SetBool("isRunning", true);
        animator.SetBool("isWalking", false);
    }

    public void Death()
    {
        animator.SetTrigger("Death");
    }

    public void Idle()
    {
        animator.SetBool("isWalking", false);
        animator.SetBool("isRunning", false);
    }

    public void GetDamage()
    {
        animator.SetTrigger("GetDamage");
    }


    private void OnHitFrame()
    {
        
    }


}
