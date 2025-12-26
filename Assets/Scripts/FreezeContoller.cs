using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FreezeController : MonoBehaviour
{
    [Header("Freeze Settings")]
    [SerializeField] private Material freezeMaterial;
    [SerializeField] private float freezeDuration = 2f;
    [SerializeField] private float unfreezeDuration = 1.5f;
    [SerializeField] private bool includeChildren = true;
    
    [Header("Visual Effects")]
    [SerializeField] private ParticleSystem iceParticles;
    [SerializeField] private AudioClip freezeSound;
    [SerializeField] private AudioClip unfreezeSound;

    private int timeToUnfreeze;
    
    private class RendererData
    {
        public Renderer renderer;
        public Material[] originalMaterials;
        public Material[] freezeMaterials;
    }
    
    private List<RendererData> rendererDataList = new List<RendererData>();
    private AudioSource audioSource;
    private float currentFreezeAmount = 0f;
    private bool isFrozen = false;
    
    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        
        if (audioSource == null && (freezeSound != null || unfreezeSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Coletar todos os renderers
        CollectRenderers();
    }
    
    private void CollectRenderers()
    {
        rendererDataList.Clear();
        
        Renderer[] renderers;
        
        if (includeChildren)
        {
            // Pegar todos os renderers incluindo os filhos
            renderers = GetComponentsInChildren<Renderer>();
        }
        else
        {
            // Pegar apenas o renderer deste objeto
            Renderer singleRenderer = GetComponent<Renderer>();
            renderers = singleRenderer != null ? new Renderer[] { singleRenderer } : new Renderer[0];
        }
        
        foreach (Renderer rend in renderers)
        {
            if (rend != null)
            {
                RendererData data = new RendererData
                {
                    renderer = rend,
                    originalMaterials = rend.materials
                };
                rendererDataList.Add(data);
            }
        }
        
        Debug.Log($"FreezeController: Found {rendererDataList.Count} renderer(s)");
    }
    
    public void Freeze()
    {
        if (!isFrozen)
        {
            StartCoroutine(FreezeCoroutine());
            
            var animator = GetComponent<Animator>();
            if (animator != null)
                animator.speed = 0f;
            
            var enemyScript = GetComponent<EnemyChaseAndAttack>();
            if (enemyScript != null)
                enemyScript.FreezeStopNpc();
        }
    }
    
    public void Unfreeze()
    {
        if (isFrozen)
        {
            StartCoroutine(UnfreezeCoroutine());
            
            var animator = GetComponent<Animator>();
            if (animator != null)
                animator.speed = 1f;
            
            var enemyScript = GetComponent<EnemyChaseAndAttack>();
            if (enemyScript != null)
                enemyScript.ContinueUnfreezeNpc();
        }
    }
    
    public void ToggleFreeze()
    {
        if (isFrozen)
            Unfreeze();
        else
            Freeze();
    }

    public void FreezeWithCoroutine()
    {  
        Freeze();
        timeToUnfreeze++;
        StartCoroutine(WaitToUnfreeze(2f));
    }

    private IEnumerator WaitToUnfreeze(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        timeToUnfreeze--;
        Debug.Log("Time to unfreeze: " + timeToUnfreeze);
        if (timeToUnfreeze == 0)
        {
            Unfreeze();
        }
    }   
    
    private IEnumerator FreezeCoroutine()
    {
        isFrozen = true;
        
        // Tocar som de congelamento
        if (audioSource != null && freezeSound != null)
        {
            audioSource.PlayOneShot(freezeSound);
        }
        
        // Ativar partículas
        if (iceParticles != null)
        {
            iceParticles.Play();
        }
        
        // Aplicar material de gelo a todos os renderers
        if (freezeMaterial != null)
        {
            foreach (RendererData data in rendererDataList)
            {
                if (data.renderer != null)
                {
                    Material[] freezeMats = new Material[data.originalMaterials.Length];
                    
                    for (int i = 0; i < freezeMats.Length; i++)
                    {
                        freezeMats[i] = new Material(freezeMaterial);
                        
                        // Copiar propriedades do material original
                        CopyMaterialProperties(data.originalMaterials[i], freezeMats[i]);
                    }
                    
                    data.freezeMaterials = freezeMats;
                    data.renderer.materials = freezeMats;
                    
                    Debug.Log($"Applied freeze material to: {data.renderer.gameObject.name}");
                }
            }
        }
        
        // Animar o valor de congelamento
        float elapsed = 0f;
        while (elapsed < freezeDuration)
        {
            elapsed += Time.deltaTime;
            currentFreezeAmount = Mathf.Lerp(0f, 1f, elapsed / freezeDuration);
            
            // Atualizar shader
            UpdateFreezeAmount(currentFreezeAmount);
            
            yield return null;
        }
        
        currentFreezeAmount = 1f;
        UpdateFreezeAmount(currentFreezeAmount);
        
        // Parar partículas após congelamento completo
        if (iceParticles != null)
        {
            iceParticles.Stop();
        }
    }
    
    /// <summary>
    /// Copia texturas e propriedades relevantes do material original para o material de gelo
    /// </summary>
    private void CopyMaterialProperties(Material original, Material freeze)
    {
        // Primeiro, tentar copiar a textura principal (mainTexture) diretamente
        if (original.mainTexture != null && freeze.HasProperty("_MainTex"))
        {
            freeze.SetTexture("_MainTex", original.mainTexture);
            Debug.Log($"Copied mainTexture: {original.mainTexture.name} to _MainTex");
            
            // Copiar tiling e offset da textura principal
            freeze.SetTextureScale("_MainTex", original.mainTextureScale);
            freeze.SetTextureOffset("_MainTex", original.mainTextureOffset);
        }
        else
        {
            // Se não tem mainTexture, procurar qualquer propriedade de textura
            Shader originalShader = original.shader;
            int propertyCount = originalShader.GetPropertyCount();
            
            for (int i = 0; i < propertyCount; i++)
            {
                if (originalShader.GetPropertyType(i) == UnityEngine.Rendering.ShaderPropertyType.Texture)
                {
                    string propName = originalShader.GetPropertyName(i);
                    Texture tex = original.GetTexture(propName);
                    
                    if (tex != null && freeze.HasProperty("_MainTex"))
                    {
                        freeze.SetTexture("_MainTex", tex);
                        Debug.Log($"Copied texture from {propName} to _MainTex");
                        
                        // Tentar copiar tiling e offset
                        try
                        {
                            freeze.SetTextureScale("_MainTex", original.GetTextureScale(propName));
                            freeze.SetTextureOffset("_MainTex", original.GetTextureOffset(propName));
                        }
                        catch { }
                        
                        break;
                    }
                }
            }
        }
        
        // Copiar cor principal
        if (freeze.HasProperty("_Color"))
        {
            // Tentar color do material original
            Color originalColor = original.color;
            
            // Se a cor for preta (0,0,0) ou muito escura, usar branco por padrão
            // Isso acontece quando o shader não usa cor base
            if (originalColor.r < 0.1f && originalColor.g < 0.1f && originalColor.b < 0.1f)
            {
                originalColor = Color.white;
                Debug.Log("Original color was black, using white instead");
            }
            
            freeze.SetColor("_Color", originalColor);
            Debug.Log($"Set color: {originalColor}");
        }
    }
    
    private IEnumerator UnfreezeCoroutine()
    {
        // Tocar som de descongelamento
        if (audioSource != null && unfreezeSound != null)
        {
            audioSource.PlayOneShot(unfreezeSound);
        }
        
        // Animar o valor de descongelamento
        float elapsed = 0f;
        while (elapsed < unfreezeDuration)
        {
            elapsed += Time.deltaTime;
            currentFreezeAmount = Mathf.Lerp(1f, 0f, elapsed / unfreezeDuration);
            
            // Atualizar shader
            UpdateFreezeAmount(currentFreezeAmount);
            
            yield return null;
        }
        
        currentFreezeAmount = 0f;
        UpdateFreezeAmount(currentFreezeAmount);
        
        // Restaurar materiais originais
        foreach (RendererData data in rendererDataList)
        {
            if (data.renderer != null && data.originalMaterials != null)
            {
                data.renderer.materials = data.originalMaterials;
            }
        }
        
        // Limpar materiais de gelo instanciados
        CleanupFreezeMaterials();
        
        isFrozen = false;
    }
    
    private void UpdateFreezeAmount(float amount)
    {
        foreach (RendererData data in rendererDataList)
        {
            if (data.renderer != null && data.freezeMaterials != null)
            {
                foreach (Material mat in data.freezeMaterials)
                {
                    if (mat != null && mat.HasProperty("_FreezeAmount"))
                    {
                        mat.SetFloat("_FreezeAmount", amount);
                    }
                }
            }
        }
    }
    
    private void CleanupFreezeMaterials()
    {
        foreach (RendererData data in rendererDataList)
        {
            if (data.freezeMaterials != null)
            {
                foreach (Material mat in data.freezeMaterials)
                {
                    if (mat != null)
                    {
                        Destroy(mat);
                    }
                }
                data.freezeMaterials = null;
            }
        }
    }
    
    // Para testar no editor
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            FreezeWithCoroutine();
        }
    }
    
    private void OnDestroy()
    {
        // Limpar todos os materiais instanciados
        CleanupFreezeMaterials();
    }
    
    // Método para recoletar renderers em runtime (útil se objetos mudarem)
    public void RefreshRenderers()
    {
        CollectRenderers();
    }
    
    // Método de debug para visualizar os renderers encontrados
    [ContextMenu("Debug: Show All Renderers")]
    private void DebugShowRenderers()
    {
        Debug.Log("=== FREEZE CONTROLLER DEBUG ===");
        Debug.Log($"Total renderers found: {rendererDataList.Count}");
        
        foreach (RendererData data in rendererDataList)
        {
            if (data.renderer != null)
            {
                Debug.Log($"- Renderer: {data.renderer.gameObject.name}");
                Debug.Log($"  Materials count: {data.originalMaterials.Length}");
                
                for (int i = 0; i < data.originalMaterials.Length; i++)
                {
                    Material mat = data.originalMaterials[i];
                    Debug.Log($"  Material {i}: {mat.name}");
                    
                    if (mat.HasProperty("_MainTex"))
                    {
                        Texture tex = mat.mainTexture;
                        Debug.Log($"    MainTex: {(tex != null ? tex.name : "null")}");
                    }
                    
                    if (mat.HasProperty("_Color"))
                    {
                        Debug.Log($"    Color: {mat.color}");
                    }
                }
            }
        }
    }
}