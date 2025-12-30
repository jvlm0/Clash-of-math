using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField]
    private float speed = 6f;

    [SerializeField]
    private float turnSpeed = 10f;

    [Header("Jump Settings")]
    [SerializeField]
    private float horizontalJumpSpeed = 10f;

    [SerializeField]
    private float minAirTime = 0.3f;

    [SerializeField]
    private float maxAirTime = 3.2f;

    [SerializeField]
    private float verticalJumpSpeed = 15f;

    [SerializeField]
    private float jumpCenterY = 2.05f;

    [SerializeField]
    private float normalCenterY = 1.07f;

    [SerializeField]
    private float gravity = -25f;

    [SerializeField]
    private float minAnimSpeed = 0.2f;

    [Header("Combat Settings")]
    [SerializeField]
    private float attackRotationSpeed = 15f; // Velocidade de rotação durante ataque

    [SerializeField]
    private LayerMask enemyLayer; // Layer dos inimigos

    [SerializeField]
    private float lockOnAngle = 60f; // Ângulo máximo para considerar inimigo válido

    private Vector3 velocity;
    private bool isLaunched;
    private bool isAttacking; // Novo: controla se está atacando
    private Transform currentTarget; // Novo: inimigo atual sendo alvo

    private CharacterController controller;
    private PlayerController animController;
    private Animator animator;

    private float airTime;
    private float estimatedAirTime;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animController = GetComponent<PlayerController>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        HandleMovement();
        HandleAttack();
        HandleGravity();
        HandleLaunchedMovement();
    }

    void HandleMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 inputDir = new Vector3(-horizontal, 0f, -vertical);

        // Só movimenta se não estiver no ar E não estiver atacando
        if (!isLaunched && !isAttacking)
        {
            if (inputDir.sqrMagnitude > 0.01f)
            {
                animController.Run();
                controller.Move(inputDir.normalized * speed * Time.deltaTime);

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

    void HandleAttack()
    {
        if (Input.GetMouseButtonDown(0) && !isLaunched && !isAttacking)
        {
            // Busca o inimigo mais próximo no range
            Transform targetEnemy = GetComponent<MeleeAtack>().canAttack();

            if (targetEnemy != null)
            {
                currentTarget = targetEnemy;
                isAttacking = true;

                // Inicia a rotação suave em direção ao inimigo
                StartCoroutine(RotateTowardsTarget());

                GetComponent<IAnimController>()?.Attack();
                
            }

            // Executa o ataque (mesmo sem target)
            GetComponent<IAtackHandler>()?.Atack();
        }
    }

    // Busca o inimigo mais próximo dentro do range e ângulo de visão
    Transform FindNearestEnemyInRange()
    {
        Collider[] enemiesInRange = Physics.OverlapSphere(
            transform.position,
            GetComponent<StatusController>().attackRange,
            enemyLayer
        );

        Transform nearestEnemy = null;
        float nearestDistance = Mathf.Infinity;

        foreach (Collider enemy in enemiesInRange)
        {
            Vector3 directionToEnemy = enemy.transform.position - transform.position;
            float angleToEnemy = Vector3.Angle(transform.forward, directionToEnemy);

            // Verifica se está dentro do ângulo de lock-on
            if (angleToEnemy < lockOnAngle)
            {
                float distance = directionToEnemy.sqrMagnitude;

                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestEnemy = enemy.transform;
                }
            }
        }

        return nearestEnemy;
    }

    // Corrotina para rotacionar suavemente em direção ao alvo
    IEnumerator RotateTowardsTarget()
    {
        if (currentTarget == null)
        {
            isAttacking = false;
            yield break;
        }

        float rotationTime = 0f;
        float maxRotationTime = 0.3f; // Tempo máximo para girar (ajuste conforme necessário)

        Quaternion startRotation = transform.rotation;

        while (rotationTime < maxRotationTime && currentTarget != null)
        {
            Vector3 direction = (currentTarget.position - transform.position).normalized;
            direction.y = 0; // Mantém rotação apenas no plano horizontal

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

        // Reseta o estado de ataque após a animação
        // IMPORTANTE: Você pode chamar isso do Animation Event também
        yield return new WaitForSeconds(0.5f); // Ajuste baseado na duração da sua animação de ataque

        isAttacking = false;
        currentTarget = null;
    }

    // Método público que pode ser chamado por Animation Event ao fim do ataque
    public void OnAttackFinished()
    {
        isAttacking = false;
        currentTarget = null;
    }

    void HandleGravity()
    {
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(new Vector3(0, velocity.y, 0) * Time.deltaTime);
    }

    void HandleLaunchedMovement()
    {
        if (isLaunched)
        {
            controller.Move(new Vector3(velocity.x, 0, velocity.z) * Time.deltaTime);

            airTime += Time.deltaTime;

            float t = Mathf.Clamp01(airTime / estimatedAirTime);
            float jumpAnimSpeed = Mathf.Lerp(2f, minAnimSpeed, t);
            animator.SetFloat("JumpSpeed", jumpAnimSpeed);

            if (controller.isGrounded)
            {
                controller.center = new Vector3(0, normalCenterY, 0);
                animator.SetTrigger("FinishJump");
                EquationController.instance.SpawnFunctionMesh(transform.position);
                isLaunched = false;
                velocity.x = 0;
                velocity.z = 0;
                airTime = 0f;
            }
        }
    }

    void Launch(Vector3 direction)
    {
        velocity.x = direction.normalized.x * horizontalJumpSpeed;
        velocity.z = direction.normalized.z * horizontalJumpSpeed;
        velocity.y = verticalJumpSpeed;

        airTime = 0f;

        Vector3 horizontalVelocity = new Vector3(velocity.x, 0, velocity.z);
        float horizontalSpeed = horizontalVelocity.magnitude;

        RaycastHit hit;
        float maxDistance = 50f;

        if (
            Physics.Raycast(transform.position, horizontalVelocity.normalized, out hit, maxDistance)
        )
        {
            float horizontalDistance = hit.distance;
            float timeToReachPlatform = horizontalDistance / horizontalSpeed;
            estimatedAirTime = Mathf.Clamp(timeToReachPlatform, minAirTime, maxAirTime);
        }
        else
        {
            float theoreticalAirTime = (2f * verticalJumpSpeed) / Mathf.Abs(gravity);
            estimatedAirTime = Mathf.Clamp(theoreticalAirTime, minAirTime, maxAirTime);
        }

        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
        float jumpAnimDuration = 1f;

        foreach (AnimationClip clip in clips)
        {
            if (clip.name == "Jump")
            {
                jumpAnimDuration = clip.length;
                break;
            }
        }

        float perfectSpeed = jumpAnimDuration / estimatedAirTime;
        animator.SetFloat("JumpSpeed", perfectSpeed);
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("PlatformLaucher") && !isLaunched)
        {
            isLaunched = true;
            animController.jump();
        }
    }

    public void OnJumpStartFinished()
    {
        Vector3 launchDirection = -Vector3.forward;
        Launch(launchDirection);
        controller.center = new Vector3(0, jumpCenterY, 0);
    }

    // Visualização do range de ataque no editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, GetComponent<StatusController>().attackRange);

        // Mostra o cone de lock-on
        Vector3 forward = transform.forward * GetComponent<StatusController>().attackRange;
        Vector3 rightBound = Quaternion.Euler(0, lockOnAngle, 0) * forward;
        Vector3 leftBound = Quaternion.Euler(0, -lockOnAngle, 0) * forward;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + forward);
        Gizmos.DrawLine(transform.position, transform.position + rightBound);
        Gizmos.DrawLine(transform.position, transform.position + leftBound);
    }
}
