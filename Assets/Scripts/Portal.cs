using UnityEngine;
using TMPro;
using System.Collections.Generic;

// Script para o prefab do portal
public class Portal : MonoBehaviour
{
    [Header("Componentes")]
    [SerializeField] private MeshRenderer quadRenderer;
    [SerializeField] private GameObject textMesh;
    [SerializeField] private MeshRenderer pilarRenderer;
    
    private Material quadMaterial;
    
    private void Awake()
    {
        if (quadRenderer != null)
        {
            quadMaterial = quadRenderer.material;
        }
    }
    
    public void SetColor(Color color)
    {
        if (quadMaterial != null)
        {
            quadMaterial.color = color;
        }
    }
    
    public void SetText(string text)
    {
        if (textMesh != null)
        {
            textMesh.GetComponent<TextMeshProUGUI>().text = text;
        }
    }


     // Método para obter a altura do portal
    public float GetPortalHeight()
    {
        if (pilarRenderer != null)
        {
            return pilarRenderer.bounds.size.y;
        }
        return 0f;
    }

    public float GetPortalWidth()
    {
        if (quadRenderer != null)
        {
            return quadRenderer.bounds.size.x + pilarRenderer.bounds.size.x*2;
        }
        return 0f;
    }
}