using UnityEngine;
using UnityEngine.AI;

public class NpcController : MonoBehaviour, IAnimController
{
    
    private Animator animator;

    private float currentLife;

    private Transform currentTarget = null;

    private bool canAttack = true;

    public bool isGrounded = false;

    private Vector3 velocity;

    private float gravity = -9.81f;

    public float AnimSpeed = 1;

    private Rigidbody rb;

    private bool IsDeath = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        currentLife = GetComponent<StatusController>().hp;
    }

    void Update()
    {
        //HandleGravity();
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

        animator.SetFloat("Speed", AnimSpeed*GetComponent<StatusController>().speed);
    }

    public void Death()
    {
        if (IsDeath) return;

        SpawnBuffs.instance.SpawnBuff(transform.position+Vector3.up);
        animator.SetTrigger("Death");

        IsDeath = true;
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
        GetComponentInChildren<LifeBarController>().UpdateLifeBar(damageAmount);
    }

    public void DisableIa()
    {
        if (GetComponent<NavMeshAgent>() != null)
        {
            GetComponent<NavMeshAgent>().enabled = false;
        }
    }

    public void EnableIa()
    {
        if (GetComponent<NavMeshAgent>() != null)
        {
            GetComponent<NavMeshAgent>().enabled = true;
        }
    }

    private void OnAttackEnd()
    {
        canAttack = true;
        Debug.Log("NPC pode atacar novamente");
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ground"))
        {
            isGrounded = true;
            EnableIa();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Ground"))
        {
            isGrounded = false;
        }
    }
}
