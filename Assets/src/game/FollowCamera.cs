using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 10f, -7f); // ajuste conforme necessário
    public float smoothTime = 0.2f;

    private Vector3 velocity;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = target.position + offset;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desired,
            ref velocity,
            smoothTime
        );

        // Rotação fixa — não usa LookAt
        // Ajuste esses valores para bater com o ângulo da sua cena
        //transform.rotation = Quaternion.Euler(50f, 0f, 0f);
    }
}