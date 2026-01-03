using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;

// Script para o prefab do portal
public class Portal : MonoBehaviour
{
    [Header("Componentes")]
    [SerializeField] private MeshRenderer quadRenderer;
    [SerializeField] private GameObject textMesh;
    [SerializeField] private MeshRenderer pilarRenderer;
    [SerializeField] private Image troopIcon;
    [SerializeField] private TextMeshProUGUI expressionText;
    [SerializeField] private TextMeshProUGUI troopCountText;
    
    public bool isTextPortal = true;
    
    private Material quadMaterial;

    
    
    private void Awake()
    {
        if (quadRenderer != null)
        {
            quadMaterial = quadRenderer.material;
        }
    }


    public void Start()
    {
    
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
            expressionText.text = text;
            textMesh.SetActive(true);
            troopIcon.gameObject.SetActive(false);
            troopCountText.gameObject.SetActive(false);
        }
    }


    public void SetTroopInfo(Sprite icon, int count)
    {
        if (troopIcon != null && troopCountText != null)
        {
            troopIcon.sprite = icon;
            troopCountText.text = count.ToString();
            textMesh.SetActive(false);
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




    private void ConfigurePortal(Color color, bool isBlue)
    {
        SetColor(color);
        string text = PortalExpressionsController.Instance.GetRandomText(isBlue);
        SetText(text);
        
        
    }

    public void ConfigureRedPortal()
    {
        ConfigurePortal(PortalExpressionsController.Instance.redColor, false);
    }

    public void ConfigureBluePortal()
    {
        ConfigurePortal(PortalExpressionsController.Instance.blueColor, true);
    }
}