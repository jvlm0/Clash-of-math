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
    private float gravity = -25f;

    Vector3 velocity; // vertical + horizontal da física
    bool isLaunched;

    public float speed = 6f;
    public float turnSpeed = 10f;

    CharacterController controller;

    PlayerAnimController animController;

    Animator animator;

    float maxPossibleHeight;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animController = GetComponent<PlayerAnimController>();
        animator = GetComponent<Animator>();

        maxPossibleHeight = (verticalJumpSpeed * verticalJumpSpeed) / (2f * Mathf.Abs(gravity));
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

            float currentHeight = transform.position.y;

            // altura normalizada de 0 a 1
            float t = Mathf.InverseLerp(0f, maxPossibleHeight, currentHeight);

            // velocidade da animação baseada somente na altura
            // mais próximo do topo → animação mais lenta
            // mais próximo do chão → mais rápida
            float jumpAnimSpeed = Mathf.Lerp(2f, 0.2f, t);

            animator.SetFloat("JumpSpeed", jumpAnimSpeed);

            if (controller.isGrounded)
            {
                isLaunched = false;
                velocity.x = 0;
                velocity.z = 0;
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

        isLaunched = true;
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("PlatformLaucher"))
        {
            Vector3 launchDirection = -Vector3.forward;
            Launch(launchDirection);
            animController.jump();
        }
    }
}
