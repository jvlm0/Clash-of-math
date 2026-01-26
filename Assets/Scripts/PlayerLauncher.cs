using System.Collections;
using UnityEngine;

public class PlayerLauncher : MonoBehaviour
{
    [Header("Configurações de Lançamento")]
    [SerializeField] private Transform posicaoAlvo;
    [SerializeField] private float horizontalJumpSpeed = 10f;
    [SerializeField] private float verticalJumpSpeed = 15f;
    
    [Header("Configurações de Animação")]
    [SerializeField] private float minAirTime = 0.3f;
    [SerializeField] private float maxAirTime = 3.2f;
    [SerializeField] private float minAnimSpeed = 0.2f;
    
    [Header("Ground Check")]
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private LayerMask groundLayer;
    
    private Rigidbody rb;
    private Animator animator;
    private PlayerController animController;
    
    private bool isLaunched = false;
    private bool applyRootMotion = true;
    private float airTime;
    private float estimatedAirTime;
    private float gravity;

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
        
        // Configurações do Rigidbody
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        
        gravity = Mathf.Abs(Physics.gravity.y);
        
        // IMPORTANTE: Manter Apply Root Motion ATIVADO
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
        // Durante o salto, ignoramos o Root Motion e usamos física
        if (isLaunched && !applyRootMotion)
        {
            // Não aplica o Root Motion durante o voo
            return;
        }
        
        // Fora do salto, aplica normalmente o Root Motion
        if (!isLaunched)
        {
            // Movimento via Root Motion (para idle, run, etc)
            rb.MovePosition(rb.position + animator.deltaPosition);
            rb.MoveRotation(rb.rotation * animator.deltaRotation);
        }
    }

    void HandleLaunchedMovement()
    {
        airTime += Time.deltaTime;

        // Atualiza a velocidade da animação baseado no tempo no ar
        float t = Mathf.Clamp01(airTime / estimatedAirTime);
        float jumpAnimSpeed = Mathf.Lerp(2f, minAnimSpeed, t);
        animator.SetFloat("JumpSpeed", jumpAnimSpeed);

        // Verifica se aterrissou
        if (IsGrounded())
        {
            FinishJump();
        }
    }

    bool IsGrounded()
    {
        return Physics.Raycast(
            transform.position,
            Vector3.down,
            groundCheckDistance,
            groundLayer
        );
    }

    void FinishJump()
    {
        animator.SetTrigger("FinishJump");
        
        // Reativa o Root Motion para o landing
        applyRootMotion = true;
        
        EquationController.instance.SpawnFunctionMesh(transform.position);
        GameController.Instance.IsBattleStart = true;
        
        isLaunched = false;
        airTime = 0f;
        
        // Para o movimento horizontal suavemente
        Vector3 vel = rb.velocity;
        vel.x = 0;
        vel.z = 0;
        rb.velocity = vel;
    }

    public void IniciarLancamento()
    {
        if (isLaunched) return;
        
        isLaunched = true;
        applyRootMotion = true; // Mantém Root Motion para animação inicial
        animController.jump();
        
        Debug.Log("Lançamento iniciado! Aguardando Animation Event OnJumpStartFinished()");
    }

    // Chamado pelo Animation Event quando a animação de início do pulo termina
    public void OnJumpStartFinished()
    {
        Debug.Log("OnJumpStartFinished chamado!");
        
        // DESATIVA Root Motion apenas durante o voo
        applyRootMotion = false;
        
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
        Vector3 velocity = new Vector3(horizontalVelocity.x, verticalJumpSpeed, horizontalVelocity.z);
        
        rb.velocity = velocity;
        
        Debug.Log($"Lançado para frente! Velocidade: {velocity}, Magnitude: {velocity.magnitude}");
        
        CalcularTempoEstimado(launchDirection);
    }

    void LancarParaAlvo()
    {
        Vector3 direcaoParaAlvo = (posicaoAlvo.position - transform.position);
        Vector3 direcaoHorizontal = new Vector3(direcaoParaAlvo.x, 0, direcaoParaAlvo.z);
        float distanciaHorizontal = direcaoHorizontal.magnitude;
        float distanciaVertical = direcaoParaAlvo.y;

        // Calcula o tempo de voo
        float velocidadeVerticalInicial = verticalJumpSpeed;
        float tempoSubida = velocidadeVerticalInicial / gravity;
        float alturaMaxima = (velocidadeVerticalInicial * velocidadeVerticalInicial) / (2 * gravity);
        
        float alturaQueda = alturaMaxima - distanciaVertical;
        float tempoDescida = Mathf.Sqrt(2 * Mathf.Max(0, alturaQueda) / gravity);
        float tempoTotal = tempoSubida + tempoDescida;

        // Calcula a velocidade horizontal necessária
        float velocidadeHorizontal = distanciaHorizontal / tempoTotal;
        Vector3 velocidadeHorizontalVec = direcaoHorizontal.normalized * velocidadeHorizontal;

        // Aplica a velocidade
        Vector3 velocidadeFinal = new Vector3(velocidadeHorizontalVec.x, verticalJumpSpeed, velocidadeHorizontalVec.z);
        rb.velocity = velocidadeFinal;
        
        Debug.Log($"Lançado para alvo! Velocidade: {velocidadeFinal}, Distância: {distanciaHorizontal:F2}m, Tempo estimado: {tempoTotal:F2}s");
        
        // Rotaciona o personagem para a direção do alvo
        if (direcaoHorizontal.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(direcaoHorizontal.normalized);
        }
        
        estimatedAirTime = Mathf.Clamp(tempoTotal, minAirTime, maxAirTime);
        SincronizarAnimacao();
    }

    void CalcularTempoEstimado(Vector3 direction)
    {
        Vector3 horizontalVelocity = new Vector3(direction.normalized.x * horizontalJumpSpeed, 0, direction.normalized.z * horizontalJumpSpeed);
        float horizontalSpeed = horizontalVelocity.magnitude;

        RaycastHit hit;
        float maxDistance = 50f;

        if (Physics.Raycast(transform.position, horizontalVelocity.normalized, out hit, maxDistance, groundLayer))
        {
            float horizontalDistance = hit.distance;
            float timeToReachPlatform = horizontalDistance / horizontalSpeed;
            estimatedAirTime = Mathf.Clamp(timeToReachPlatform, minAirTime, maxAirTime);
        }
        else
        {
            float theoreticalAirTime = (2f * verticalJumpSpeed) / gravity;
            estimatedAirTime = Mathf.Clamp(theoreticalAirTime, minAirTime, maxAirTime);
        }

        SincronizarAnimacao();
    }

    void SincronizarAnimacao()
    {
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

    public bool IsLaunched()
    {
        return isLaunched;
    }

    public void ResetarLancamento()
    {
        isLaunched = false;
        applyRootMotion = true;
        airTime = 0f;
        rb.velocity = Vector3.zero;
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