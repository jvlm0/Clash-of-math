using UnityEngine;

public class PlatformLaucher : MonoBehaviour
{
    private CharacterController controller;

    [SerializeField]
    private float horizontalSpeed = 10f;

    [SerializeField]
    private float verticalSpeed = 15f;

    [SerializeField]
    private float gravity = -25f;

    Vector3 velocity;
    bool isLaunched;

    void Launch(Vector3 direction)
    {
        velocity = direction.normalized * horizontalSpeed;
        velocity.y = verticalSpeed;
        isLaunched = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        other.TryGetComponent<CharacterController>(out controller);
        if (controller != null)
        {
            Vector3 launchDirection = -Vector3.forward;

            Launch(launchDirection);
        }
    }

    void Update()
    {
        if (isLaunched && controller != null)
        {
            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);

            // detectar quando tocou o chão
            if (controller.isGrounded)
            {
                isLaunched = false;
            }
        }
    }
}
