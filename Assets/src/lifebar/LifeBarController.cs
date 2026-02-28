using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LifeBarController : MonoBehaviour
{
    public Camera cameraToLookAt;

    public float burstTime;

    [SerializeField]
    private Transform lifeBarFill;
    private float totalLife;
    private float currentLife;

    [SerializeField]
    private bool isIncavas = false;

    private float burstDamage;
    private float burstTimeCount;

    public void UpdateLifeBar(float damage)
    {
        burstDamage += damage;
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
        transform.LookAt(GameController.Instance.cameraToLookAt.transform);

        Vector3 rotacao = transform.eulerAngles;
        rotacao.x = 0;
        transform.eulerAngles = rotacao;
        if (burstDamage >= totalLife * 0.5f && currentLife == 0)
        {
            GetComponentInParent<NpcController>()?.Death();
            GetComponentInParent<ObjectDisintegrator>()?.Disintegrate(6);
            Destroy(gameObject, .1f);
        }
        else if (burstDamage >= totalLife * 0.4f && currentLife == 0)
        {
            GetComponentInParent<NpcController>()?.Death();
            GetComponentInParent<ObjectDisintegrator>()?.Disintegrate(4);
            Destroy(gameObject, .1f);
        }
        else if (burstDamage >= totalLife * 0.3f && currentLife == 0)
        {
            GetComponentInParent<ObjectDisintegrator>()?.Disintegrate(3);
            
            GetComponentInParent<NpcController>()?.Death();

            Destroy(gameObject, .1f);

        }

        if (burstTimeCount >= burstTime)
        {
            burstTimeCount = 0;
            burstDamage = 0;
        }

        burstTimeCount += Time.deltaTime;
    }
}
