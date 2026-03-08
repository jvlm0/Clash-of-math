using UnityEngine;

public class LightningPropagation : MonoBehaviour
{
    public float speed;
    public float scaleFactor;
    bool active;
    float finalScale, initialScale, initialEmission, finalEmission;
    ParticleSystem ps;
    public GameObject effect;

    void Update()
    {
        if (!active) return;

        float currentScale = effect.transform.localScale.x;
        
        // Progresso de 0 a 1
        float t = Mathf.InverseLerp(initialScale, finalScale, currentScale);

        // Emission acompanha a escala proporcionalmente
        var emission = ps.emission;
        emission.rateOverTime = new ParticleSystem.MinMaxCurve(Mathf.Lerp(initialEmission, finalEmission, t));

        if (t >= 1f)
        {
            active = false;
            effect.SetActive(false);
            return;
        }

        effect.transform.localScale += new Vector3(Time.deltaTime * speed, Time.deltaTime * speed, Time.deltaTime * speed);
    }

    public void Active()
    {
        ps = effect.GetComponent<ParticleSystem>();

        initialScale = effect.transform.localScale.x;
        initialEmission = ps.emission.rateOverTime.constant;

        finalScale = initialScale * (1f + scaleFactor);
        finalEmission = initialEmission * (1f + scaleFactor);

        effect.SetActive(true);
        active = true;
    }
}