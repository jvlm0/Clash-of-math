using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    public Transform target;
    public Vector3 offset;
    public float smoothTime = 0.2f;
    Vector3 velocity;

    void LateUpdate()
    {
        Vector3 desired = target.position + offset;
        transform.position = Vector3.SmoothDamp(
            transform.position,
            desired,
            ref velocity,
            smoothTime
        );
        transform.LookAt(target);
    }
}
