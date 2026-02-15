using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField]
    private float speed = 6f;

    [SerializeField]
    private float turnSpeed = 10f;

    [SerializeField]
    private LayerMask enemyLayer;

    [SerializeField]
    private float lockOnAngle = 60f;

    private Transform currentTarget;

    [Header("Combat Settings")]
    [SerializeField]
    private float attackRotationSpeed = 15f;

    public bool IsAtacking { get; set; }

    private Rigidbody rb;
    private PlayerController animController;
    private PlayerLauncher launcher;

    private IAttackHandler attackHandler;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animController = GetComponent<PlayerController>();
        launcher = GetComponent<PlayerLauncher>();

        if (rb != null)
        {
            rb.useGravity = true;
            rb.isKinematic = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }
    }

    void OnAnimatorMove()
    {
        // Delega o controle do Root Motion para o PlayerLauncher
        // Isso permite que o launcher controle quando usar Root Motion
    }

    void Update()
    {
        Transform targetEnemy = GetComponent<MeleeAtack>().canAttack();

        if (targetEnemy != null)
        {
            currentTarget = targetEnemy;
            IsAtacking = true;
            attackHandler.HandleAttack();
            StartCoroutine(RotateTowardsTarget());
            GetComponent<IAnimController>()?.Attack();
        }
        
    }

    void FixedUpdate()
    {
        HandleMovement();
    }

    void HandleMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 inputDir = new Vector3(horizontal, 0f, vertical);

        // Só movimenta se não estiver no ar E não estiver atacando
        if (!launcher.IsLaunched() && !IsAtacking)
        {
            if (inputDir.sqrMagnitude > 0.01f)
            {
                animController.Run();
                rb.MovePosition(
                    transform.position
                        + inputDir.normalized
                            * GetComponent<StatusController>().speed
                            * Time.deltaTime
                );

                Quaternion targetRotation = Quaternion.LookRotation(inputDir);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    turnSpeed * Time.deltaTime
                );
            }
            else
            {
                animController.stopRun();
            }
        }
    }

    public void OnAttackFinished()
    {
        IsAtacking = false;
        currentTarget = null;
    }

    IEnumerator RotateTowardsTarget()
    {
        if (currentTarget == null)
        {
            IsAtacking = false;
            yield break;
        }

        float rotationTime = 0f;
        float maxRotationTime = 0.3f;

        while (rotationTime < maxRotationTime && currentTarget != null)
        {
            Vector3 direction = (currentTarget.position - transform.position).normalized;
            direction.y = 0;

            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    attackRotationSpeed * Time.deltaTime
                );
            }

            rotationTime += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(0.5f);

        IsAtacking = false;
        currentTarget = null;
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("PlatformLaucher") && !launcher.IsLaunched())
        {
            launcher.IniciarLancamento();
        }
    }

    void OnDrawGizmosSelected()
    {
        if (GetComponent<StatusController>() == null)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, GetComponent<StatusController>().attackRange);

        Vector3 forward = transform.forward * GetComponent<StatusController>().attackRange;
        Vector3 rightBound = Quaternion.Euler(0, lockOnAngle, 0) * forward;
        Vector3 leftBound = Quaternion.Euler(0, -lockOnAngle, 0) * forward;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + forward);
        Gizmos.DrawLine(transform.position, transform.position + rightBound);
        Gizmos.DrawLine(transform.position, transform.position + leftBound);
    }
}
