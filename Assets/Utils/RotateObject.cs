using UnityEngine;



public class RotateObject : MonoBehaviour
{
    public Transform RotateTransform;
    public float RotateSpeed = 1;


    void Update()
    {
        RotateTransform.Rotate(0,RotateSpeed*Time.deltaTime,0);
    }
}