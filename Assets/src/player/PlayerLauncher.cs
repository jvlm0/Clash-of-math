using System.Collections;
using UnityEngine;

public class PlayerLauncher : MonoBehaviour
{
    [Header("Configurações de Lançamento")]
    [SerializeField]
    private Transform posicaoAlvo;

    [SerializeField]
    private float horizontalJumpSpeed = 10f;

    [SerializeField]
    private float verticalJumpSpeed = 15f;

    [Header("Configurações de Animação (Animação Única)")]
    [Tooltip("NormalizedTime no qual o personagem sai do chão (evento OnJumpStartFinished)")]
    [SerializeField]
    private float takeoffNormalizedTime = 0.15f;

    [Tooltip("NormalizedTime no qual o personagem deve aterrissar")]
    [SerializeField]
    private float landingNormalizedTime = 0.85f;

    [SerializeField]
    private float minAirTime = 0.3f;

    [SerializeField]
    private float maxAirTime = 3.2f;

    [Header("Velocidade da Animação")]
    [Tooltip("JumpSpeed no início do voo (animação mais rápida)")]
    [SerializeField]
    private float jumpSpeedStart = 1f;

    [Tooltip("JumpSpeed no pico do voo (animação mais lenta)")]
    [SerializeField]
    private float jumpSpeedEnd = 0.05f;

    [Header("Ground Check")]
    [SerializeField]
    private float groundCheckDistance = 0.2f;
    [SerializeField] private float groundCheckRadius = 0.3f;

    [SerializeField]
    private LayerMask groundLayer;

    private Rigidbody rb;
    private Animator animator;
    private PlayerController animController;

    private bool isLaunched = false;
    private bool controllingAnimation = false;
    private float airTime;
    private float estimatedAirTime;
    private float gravity;
    private float capturedTakeoffTime;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        animController = GetComponent<PlayerController>();

        if (rb == null)
        {
            Debug.LogError("Rigidbody não encontrado no objeto!");
            return;
        }

        rb.useGravity = true;
        rb.isKinematic = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        gravity = Mathf.Abs(Physics.gravity.y);
        animator.applyRootMotion = true;
    }

    void Update()
    {
        if (isLaunched)
        {
            HandleLaunchedMovement();
        }
    }

    void OnAnimatorMove()
    {
        // Durante o controle do voo, ignoramos Root Motion
        if (controllingAnimation)
            return;

        // Nas fases de takeoff e landing, Root Motion normal
        rb.MovePosition(rb.position + animator.deltaPosition);
        rb.MoveRotation(rb.rotation * animator.deltaRotation);
    }

    void HandleLaunchedMovement()
    {
        airTime += Time.deltaTime;
        Debug.Log("controllingAnim atual " + controllingAnimation + "airtime " + airTime);
        if (controllingAnimation)
        {
            // Mapeia o progresso no ar (0 = decolagem, 1 = aterrissagem)
            float t = Mathf.Clamp01(airTime / estimatedAirTime);

            // Interpola JumpSpeed: rápido no início, lento no pico/final
            // Isso estica a animação para durar exatamente o tempo no ar
            float jumpSpeed = Mathf.Lerp(jumpSpeedStart, jumpSpeedEnd, t);
            animator.SetFloat("JumpSpeed", jumpSpeed);
            Debug.Log("Velocidade atual " + jumpSpeed);
        }

        if (airTime > 2f && IsGrounded())
        {
            FinishJump();
        }
    }

    bool IsGrounded()
    {
        Vector3 spherePos = transform.position + Vector3.down * groundCheckDistance;
        return Physics.CheckSphere(spherePos, groundCheckRadius, groundLayer);
    }

    void FinishJump()
    {
        Debug.Log("FinishJump");
        // Libera o controle — Root Motion volta a agir na fase de landing
        //controllingAnimation = false;
        isLaunched = false;
        airTime = 0f;

        // Reseta JumpSpeed para velocidade normal
        animator.SetFloat("JumpSpeed", 1f);

        // Para movimento horizontal residual
        Vector3 vel = rb.velocity;
        vel.x = 0;
        vel.z = 0;
        rb.velocity = vel;
        GetComponent<LightningPropagation>().Active();
        EquationController.instance?.SpawnFunctionMesh(transform.position);
        GameController.Instance.IsBattleStart = true;
    }

    public void IniciarLancamento()
    {
        if (isLaunched)
           return;

        posicaoAlvo = RoadSpawner.actualJumpPos;
        GetComponent<PlayerController>().TurnOffLayers();
        isLaunched = true;
        controllingAnimation = false;
        animController.jump();

        Debug.Log("Lançamento iniciado! Aguardando Animation Event OnJumpStartFinished()");
    }

    public void OnEndJump()
    {
        GetComponent<PlayerController>().TurnOnLayers();
    }


    // ─── Animation Event ──────────────────────────────────────────────────────
    // Coloque este evento no frame EXATO em que o pé sai do chão na animação.
    public void OnJumpStartFinished()
    {
        Debug.Log("OnJumpStartFinished chamado!");

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        capturedTakeoffTime = stateInfo.normalizedTime;

        // Ativa o controle de JumpSpeed durante o voo
        controllingAnimation = true;

        if (posicaoAlvo == null)
        {
            Debug.LogWarning("Posição alvo não definida! Usando lançamento para frente.");
            LancarParaFrente();
        }
        else
        {
            Debug.Log($"Lançando para alvo: {posicaoAlvo.name}");
            LancarParaAlvo();
        }
    }

    void LancarParaFrente()
    {
        Vector3 launchDirection = transform.forward;
        Vector3 horizontalVelocity = launchDirection.normalized * horizontalJumpSpeed;
        Vector3 velocity = new Vector3(
            horizontalVelocity.x,
            verticalJumpSpeed,
            horizontalVelocity.z
        );
        rb.velocity = velocity;

        Debug.Log($"Lançado para frente! Velocidade: {velocity}");
        CalcularTempoEstimado(launchDirection);
    }

    void LancarParaAlvo()
    {
        Vector3 direcaoParaAlvo = posicaoAlvo.position - transform.position;
        Vector3 direcaoHorizontal = new Vector3(direcaoParaAlvo.x, 0, direcaoParaAlvo.z);
        float distanciaHorizontal = direcaoHorizontal.magnitude;
        float distanciaVertical = direcaoParaAlvo.y;

        float tempoSubida = verticalJumpSpeed / gravity;
        float alturaMaxima = (verticalJumpSpeed * verticalJumpSpeed) / (2f * gravity);
        float alturaQueda = alturaMaxima - distanciaVertical;
        float tempoDescida = Mathf.Sqrt(2f * Mathf.Max(0, alturaQueda) / gravity);
        float tempoTotal = tempoSubida + tempoDescida;

        float velocidadeHorizontal = distanciaHorizontal / tempoTotal;
        Vector3 velocidadeHorizontalVec = direcaoHorizontal.normalized * velocidadeHorizontal;
        Vector3 velocidadeFinal = new Vector3(
            velocidadeHorizontalVec.x,
            verticalJumpSpeed,
            velocidadeHorizontalVec.z
        );
        rb.velocity = velocidadeFinal;

        if (direcaoHorizontal.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(direcaoHorizontal.normalized);

        estimatedAirTime = Mathf.Clamp(tempoTotal, minAirTime, maxAirTime);

        Debug.Log(
            $"Lançado para alvo! Velocidade: {velocidadeFinal}, Tempo estimado: {tempoTotal:F2}s"
        );
    }

    void CalcularTempoEstimado(Vector3 direction)
    {
        Vector3 horizontalVelocity = new Vector3(
            direction.normalized.x * horizontalJumpSpeed,
            0,
            direction.normalized.z * horizontalJumpSpeed
        );
        float horizontalSpeed = horizontalVelocity.magnitude;

        RaycastHit hit;
        if (
            Physics.Raycast(
                transform.position,
                horizontalVelocity.normalized,
                out hit,
                50f,
                groundLayer
            )
        )
        {
            float timeToReachPlatform = hit.distance / horizontalSpeed;
            estimatedAirTime = Mathf.Clamp(timeToReachPlatform, minAirTime, maxAirTime);
        }
        else
        {
            float theoreticalAirTime = (2f * verticalJumpSpeed) / gravity;
            estimatedAirTime = Mathf.Clamp(theoreticalAirTime, minAirTime, maxAirTime);
        }
    }

    public bool IsLaunched() => isLaunched;

    public void ResetarLancamento()
    {
        isLaunched = false;
        controllingAnimation = false;
        airTime = 0f;
        rb.velocity = Vector3.zero;
        animator.SetFloat("JumpSpeed", 1f);
    }

    void OnDrawGizmos()
    {
        if (posicaoAlvo != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, posicaoAlvo.position);
            Gizmos.DrawWireSphere(posicaoAlvo.position, 0.5f);
        }
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, Vector3.down * groundCheckDistance);
    }
}
