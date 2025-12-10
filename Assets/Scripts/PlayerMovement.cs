using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 6f;
    public float turnSpeed = 10f;

    CharacterController controller;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 direction = new Vector3(-horizontal, 0f, -vertical);

        // MOVIMENTO
        if (direction.sqrMagnitude > 0.01f)
        {
            controller.Move(direction.normalized * speed * Time.deltaTime);

            // ROTAÇÃO SUAVE
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }
    }
}
