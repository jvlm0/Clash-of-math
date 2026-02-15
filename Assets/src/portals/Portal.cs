using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Script para o prefab do portal
public class Portal : MonoBehaviour
{
    [Header("Componentes")]
    [SerializeField]
    private MeshRenderer quadRenderer;

    
    [SerializeField]
    private MeshRenderer pilarRenderer;

    [SerializeField]
    private Image troopIcon;

    [SerializeField]
    private TextMeshProUGUI expressionText;

    [SerializeField]
    public TextMeshProUGUI troopCountText;

    public bool isTextPortal = true;

    private Material quadMaterial;

    public GameObject troopPrefab;

    private void Awake()
    {
        if (quadRenderer != null)
        {
            quadMaterial = quadRenderer.material;
        }
    }

    public void Start() { }

    public void SetColor(Color color)
    {
        if (quadMaterial != null)
        {
            quadMaterial.color = color;
        }
    }

    public void SetText(string text)
    {
        if (expressionText != null)
        {
            expressionText.text = text;
            expressionText.gameObject.SetActive(true);
            troopIcon.gameObject.SetActive(false);
            troopCountText.gameObject.SetActive(false);
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
            return quadRenderer.bounds.size.x + pilarRenderer.bounds.size.x * 2;
        }
        return 0f;
    }

    private void ConfigurePortal(string text)
    {
        SetText(text);
        isTextPortal = true;
    }

    private void ConfigureTroopPortal(GameObject troopPrefab, int troopCount, Sprite troopSprite)
    {
        
        isTextPortal = false;
        this.troopPrefab = troopPrefab;
        troopIcon.sprite = troopSprite;
        troopCountText.text = troopCount.ToString();

        troopCountText.gameObject.SetActive(true);
        troopIcon.gameObject.SetActive(true);
        expressionText.gameObject.SetActive(false);
        
    }


    public void InitPortal(bool isBlue, string text = "", GameObject troopPrefab = null, int troopCount = 0, Sprite troopSprite = null)
    {   

        if (isBlue)
        {
            SetColor(PortalExpressionsController.Instance.blueColor);
        } else {   
            SetColor(PortalExpressionsController.Instance.redColor);
        }

        if (text != "")
        {
            ConfigurePortal(text);
        } else {
            ConfigureTroopPortal(troopPrefab, troopCount, troopSprite);
        }
    }
}
