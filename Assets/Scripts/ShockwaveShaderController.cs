using UnityEngine;

public class ShockwaveShaderController : MonoBehaviour
{
    public Material shockwaveMaterial;

    public float speed = 2f;
    public float maxRadius = 20f;

    private float radius = 0f;
    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
        shockwaveMaterial.SetVector("_Origin", startPos);
    }

    void Update()
    {
        radius += speed * Time.deltaTime;

        shockwaveMaterial.SetFloat("_Radius", radius);
        shockwaveMaterial.SetFloat("_TimeOffset", Time.time);

        if (radius > maxRadius)
            radius = 0f;
    }
}
