using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LifeBarController : MonoBehaviour
{
    public Camera cameraToLookAt;

    [SerializeField]
    private Transform lifeBarFill;
    private float totalLife;
    private float currentLife;

    [SerializeField] private bool isIncavas = false;


    public void UpdateLifeBar(float damage)
    {   
        currentLife -= damage;
        if (currentLife < 0)
            currentLife = 0;    
        float lifePercentage = currentLife / totalLife;

        lifeBarFill.localScale = new Vector3(lifePercentage, 1f, 1f);
    }

    // Start is called before the first frame update
    void Start()
    {
        totalLife = GetComponentInParent<StatusController>().hp;
        currentLife = totalLife;
    }

    // Update is called once per frame
    void Update()
    {
        if (isIncavas)
            return;
        // Rotaciona a barra de vida para olhar para a câmera
        transform.LookAt(cameraToLookAt.transform);

        Vector3 rotacao = transform.eulerAngles;
        rotacao.x = 0;
        transform.eulerAngles = rotacao;
    }
}
