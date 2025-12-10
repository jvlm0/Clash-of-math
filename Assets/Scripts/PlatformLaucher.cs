using UnityEngine;

public class PlatformLaucher : MonoBehaviour
{
    private CharacterController controller;
    public float gravity = -25f;

    Vector3 velocity;
    bool isLaunched;

    void Launch(Vector3 direction, float horizontalSpeed, float verticalSpeed)
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
            float horizontalSpeed = 10f;
            float verticalSpeed = 15f;
            Launch(launchDirection, horizontalSpeed, verticalSpeed);
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
