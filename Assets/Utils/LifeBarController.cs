using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LifeBarController : MonoBehaviour
{
    public Camera cameraToLookAt;

    [SerializeField]
    private Transform lifeBarFill;
    private float totalLife;

    public void SetTotalLife(float life)
    {
        totalLife = life;
    }

    public void UpdateLifeBar(float currentLife)
    {
        float lifePercentage = currentLife / totalLife;
        lifePercentage = Mathf.Clamp01(lifePercentage);

        lifeBarFill.localScale = new Vector3(lifePercentage, 1f, 1f);
    }

    // Start is called before the first frame update
    void Start() { }

    // Update is called once per frame
    void Update()
    {
        // Rotaciona a barra de vida para olhar para a câmera
        transform.LookAt(cameraToLookAt.transform);

        Vector3 rotacao = transform.eulerAngles;
        rotacao.x = 0;
        transform.eulerAngles = rotacao;
    }
}
