using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class CrackController : MonoBehaviour
{
    [Header("References")]
    public Material crackMaterial;
    
    [Header("Animation")]
    public bool animateOnStart = true;
    public float formationTime = 2.0f;
    public AnimationCurve depthCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Runtime Controls")]
    [Range(0f, 5f)] public float crackDepth = 1.0f;
    [Range(0.5f, 2f)] public float crackWidth = 1.0f;
    
    private MeshRenderer meshRenderer;
    private float currentDepth = 0f;
    private float targetDepth = 0f;
    
    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        
        if (crackMaterial == null)
        {
            crackMaterial = meshRenderer.material;
        }
        
        if (animateOnStart)
        {
            AnimateCrackFormation();
        }
    }
    
    void Update()
    {
        // Atualiza propriedades do shader
        if (crackMaterial != null)
        {
            crackMaterial.SetFloat("_CrackDepth", currentDepth);
            crackMaterial.SetFloat("_CrackWidth", crackWidth);
        }
    }
    
    public void AnimateCrackFormation()
    {
        targetDepth = crackDepth;
        currentDepth = 0f;
        StartCoroutine(AnimateDepth());
    }
    
    private System.Collections.IEnumerator AnimateDepth()
    {
        float elapsed = 0f;
        
        while (elapsed < formationTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / formationTime;
            currentDepth = targetDepth * depthCurve.Evaluate(t);
            yield return null;
        }
        
        currentDepth = targetDepth;
    }
    
    public void SetDepth(float depth)
    {
        crackDepth = depth;
        currentDepth = depth;
    }
    
    public void SetWidth(float width)
    {
        crackWidth = width;
    }
    
    public void SetEmission(bool enabled, Color color, float strength)
    {
        if (crackMaterial != null)
        {
            crackMaterial.SetFloat("_UseEmission", enabled ? 1f : 0f);
            crackMaterial.SetColor("_EmissionColor", color);
            crackMaterial.SetFloat("_EmissionStrength", strength);
        }
    }
}
