using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField]
    private float horizontalJumpSpeed = 10f;

    [SerializeField]
    float minAirTime = 0.3f;

    [SerializeField]
    float maxAirTime = 3.2f;

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

    Vector3 velocity; // vertical + horizontal da física
    bool isLaunched;

    public float speed = 6f;
    public float turnSpeed = 10f;

    CharacterController controller;

    PlayerAnimController animController;

    Animator animator;

    float airTime; // tempo que está no ar
    float estimatedAirTime; // tempo estimado baseado na distância/velocidade

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animController = GetComponent<PlayerAnimController>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        // ============================
        // 1. Movimento normal
        // ============================
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 inputDir = new Vector3(-horizontal, 0f, -vertical);

        if (!isLaunched) // só movimenta se não estiver no ar pelo Launch
        {
            if (inputDir.sqrMagnitude > 0.01f)
            {
                animController.run();
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

        // ============================
        // 2. Gravidade funcionando sempre
        // ============================
        if (controller.isGrounded && velocity.y < 0)
        {
            // valor pequeno negativo para segurar no chão
            velocity.y = -2f;
        }

        velocity.y += gravity * Time.deltaTime;

        // ============================
        // 3. Movimento vertical (gravidade)
        // ============================
        controller.Move(new Vector3(0, velocity.y, 0) * Time.deltaTime);

        // ============================
        // 4. Movimento quando lançado
        // ============================
        if (isLaunched)
        {
            
            controller.Move(new Vector3(velocity.x, 0, velocity.z) * Time.deltaTime);

            airTime += Time.deltaTime;

            // Atualiza a velocidade da animação baseado no progresso real
            float t = Mathf.Clamp01(airTime / estimatedAirTime);
            float jumpAnimSpeed = Mathf.Lerp(2f, minAnimSpeed, t);
            animator.SetFloat("JumpSpeed", jumpAnimSpeed);

            if (controller.isGrounded)
            {
                controller.center = new Vector3(0,normalCenterY,0);
                animator.SetTrigger("FinishJump");
                isLaunched = false;
                velocity.x = 0;
                velocity.z = 0;
                airTime = 0f;
            }
        }
    }

    // ============================
    // Lançamento (trampolim)
    // ============================
    void Launch(Vector3 direction)
    {
        // horizontal baseado na direção passada
        velocity.x = direction.normalized.x * horizontalJumpSpeed;
        velocity.z = direction.normalized.z * horizontalJumpSpeed;

        // força vertical inicial
        velocity.y = verticalJumpSpeed;

        airTime = 0f;

        // ← SOLUÇÃO: Detecta a plataforma de destino para calcular tempo real
        // Faz um raycast ou spherecast na direção do pulo
        Vector3 horizontalVelocity = new Vector3(velocity.x, 0, velocity.z);
        float horizontalSpeed = horizontalVelocity.magnitude;

        RaycastHit hit;
        float maxDistance = 50f; // distância máxima de busca
        
        if (Physics.Raycast(transform.position, horizontalVelocity.normalized, out hit, maxDistance))
        {
            // Calcula tempo baseado na distância até a plataforma
            float horizontalDistance = hit.distance;
            float timeToReachPlatform = horizontalDistance / horizontalSpeed;
            
            // Usa o tempo real até a plataforma
            estimatedAirTime = Mathf.Clamp(timeToReachPlatform, minAirTime, maxAirTime);
        }
        else
        {
            // Se não encontrar plataforma, usa o tempo máximo teórico de voo
            float theoreticalAirTime = (2f * verticalJumpSpeed) / Mathf.Abs(gravity);
            estimatedAirTime = Mathf.Clamp(theoreticalAirTime, minAirTime, maxAirTime);
        }

        // Opcional: Ajusta JumpSpeed uma única vez no início
        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
        float jumpAnimDuration = 1f;
        
        foreach (AnimationClip clip in clips)
        {
            if (clip.name == "Jump") // ← AJUSTE O NOME DA SUA ANIMAÇÃO
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
            Vector3 launchDirection = -Vector3.forward;
            //Launch(launchDirection);
            isLaunched = true;
            animController.jump();
        }
    }

    public void OnJumpStartFinished()
    {
        // Aqui você faz o Launch de verdade!
        Vector3 launchDirection = -Vector3.forward;
        Launch(launchDirection);
        controller.center = new Vector3(0,jumpCenterY,0);
    }
}