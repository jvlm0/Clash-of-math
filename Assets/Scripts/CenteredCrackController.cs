using UnityEngine;
using System.Collections;

[RequireComponent(typeof(MeshRenderer))]
public class CenteredCrackController : MonoBehaviour
{
    [Header("Material")]
    public Material crackMaterial;
    private MaterialPropertyBlock propertyBlock;
    private MeshRenderer meshRenderer;
    
    [Header("Position")]
    [Range(0f, 1f)] public float centerX = 0.5f;
    [Range(0f, 1f)] public float centerY = 0.5f;
    [Range(0f, 360f)] public float angle = 0f;
    
    [Header("Shape")]
    [Range(0f, 2f)] public float length = 0.6f;
    [Range(0f, 0.5f)] public float width = 0.05f;
    [Range(0f, 3f)] public float depth = 1.0f;
    [Range(0f, 1f)] public float roughness = 0.3f;
    
    [Header("Branches")]
    [Range(0, 6)] public int branchCount = 2;
    [Range(0f, 90f)] public float branchAngle = 45f;
    [Range(0f, 1f)] public float branchLength = 0.5f;
    
    [Header("Animation")]
    public bool animateOnStart = true;
    public float formationDuration = 2.0f;
    public AnimationCurve formationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [Range(0f, 1f)] public float formationProgress = 1.0f;
    
    [Header("Random")]
    public bool randomizeOnStart = false;
    public float randomSeed = 1.0f;
    
    // Property IDs
    private static readonly int CrackCenterID = Shader.PropertyToID("_CrackCenter");
    private static readonly int CrackAngleID = Shader.PropertyToID("_CrackAngle");
    private static readonly int CrackLengthID = Shader.PropertyToID("_CrackLength");
    private static readonly int CrackWidthID = Shader.PropertyToID("_CrackWidth");
    private static readonly int CrackDepthID = Shader.PropertyToID("_CrackDepth");
    private static readonly int RoughnessID = Shader.PropertyToID("_Roughness");
    private static readonly int BranchCountID = Shader.PropertyToID("_BranchCount");
    private static readonly int BranchAngleID = Shader.PropertyToID("_BranchAngle");
    private static readonly int BranchLengthID = Shader.PropertyToID("_BranchLength");
    private static readonly int FormationProgressID = Shader.PropertyToID("_FormationProgress");
    private static readonly int AnimationSeedID = Shader.PropertyToID("_AnimationSeed");
    
    private bool isAnimating = false;
    
    void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        propertyBlock = new MaterialPropertyBlock();
        
        if (crackMaterial != null)
        {
            meshRenderer.sharedMaterial = crackMaterial;
        }
    }
    
    void Start()
    {
        if (randomizeOnStart)
        {
            RandomizeCrack();
        }
        
        if (animateOnStart)
        {
            StartCrackFormation();
        }
        else
        {
            UpdateMaterialProperties();
        }
    }
    
    void UpdateMaterialProperties()
    {
        meshRenderer.GetPropertyBlock(propertyBlock);
        
        propertyBlock.SetVector(CrackCenterID, new Vector4(centerX, centerY, 0, 0));
        propertyBlock.SetFloat(CrackAngleID, angle);
        propertyBlock.SetFloat(CrackLengthID, length);
        propertyBlock.SetFloat(CrackWidthID, width);
        propertyBlock.SetFloat(CrackDepthID, depth);
        propertyBlock.SetFloat(RoughnessID, roughness);
        propertyBlock.SetInt(BranchCountID, branchCount);
        propertyBlock.SetFloat(BranchAngleID, branchAngle);
        propertyBlock.SetFloat(BranchLengthID, branchLength);
        propertyBlock.SetFloat(FormationProgressID, formationProgress);
        propertyBlock.SetFloat(AnimationSeedID, randomSeed);
        
        meshRenderer.SetPropertyBlock(propertyBlock);
    }
    
    public void StartCrackFormation()
    {
        if (!isAnimating)
        {
            StartCoroutine(AnimateFormation());
        }
    }
    
    private IEnumerator AnimateFormation()
    {
        isAnimating = true;
        float elapsed = 0f;
        
        while (elapsed < formationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / formationDuration;
            formationProgress = formationCurve.Evaluate(t);
            
            UpdateMaterialProperties();
            yield return null;
        }
        
        formationProgress = 1f;
        UpdateMaterialProperties();
        isAnimating = false;
    }
    
    public void SetPosition(float x, float y)
    {
        centerX = Mathf.Clamp01(x);
        centerY = Mathf.Clamp01(y);
        UpdateMaterialProperties();
    }
    
    public void SetPositionWorldSpace(Vector3 worldPos)
    {
        // Converte posição do mundo para UV
        Bounds bounds = GetComponent<MeshRenderer>().bounds;
        Vector3 localPos = worldPos - bounds.min;
        
        centerX = localPos.x / bounds.size.x;
        centerY = localPos.z / bounds.size.z;
        
        UpdateMaterialProperties();
    }
    
    public void SetAngle(float newAngle)
    {
        angle = newAngle % 360f;
        UpdateMaterialProperties();
    }
    
    public void RandomizeCrack()
    {
        centerX = Random.Range(0.3f, 0.7f);
        centerY = Random.Range(0.3f, 0.7f);
        angle = Random.Range(0f, 360f);
        length = Random.Range(0.4f, 0.8f);
        width = Random.Range(0.03f, 0.08f);
        branchCount = Random.Range(1, 5);
        branchAngle = Random.Range(30f, 60f);
        randomSeed = Random.Range(0.5f, 2.0f);
        
        UpdateMaterialProperties();
    }
    
    public void Reset()
    {
        formationProgress = 0f;
        UpdateMaterialProperties();
    }
    
    // Para testing no Inspector
    void OnValidate()
    {
        if (Application.isPlaying && meshRenderer != null)
        {
            UpdateMaterialProperties();
        }
    }
    
    // Debug: desenha gizmo mostrando posição
    void OnDrawGizmosSelected()
    {
        Bounds bounds = GetComponent<MeshRenderer>().bounds;
        Vector3 worldPos = bounds.min + new Vector3(
            centerX * bounds.size.x,
            bounds.max.y,
            centerY * bounds.size.z
        );
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(worldPos, 0.1f);
        
        // Desenha direção
        Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;
        Gizmos.DrawLine(worldPos, worldPos + direction * length);
    }
}
