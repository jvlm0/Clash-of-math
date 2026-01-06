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
    private TextMeshProUGUI troopCountText;

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

    private void ConfigurePortal(bool isBlue)
    {
        
        string text = PortalExpressionsController.Instance.GetRandomText(isBlue);
        SetText(text);
        isTextPortal = true;
    }

    private void ConfigureTroopPortal()
    {
        
        isTextPortal = false;
        (troopPrefab, troopIcon.sprite) = PortalExpressionsController.Instance.GetRandomTroop();
        int troopCount = Random.Range(1, 6); // Exemplo: número aleatório entre 1 e 5
        troopCountText.text = troopCount.ToString();

        troopCountText.gameObject.SetActive(true);
        troopIcon.gameObject.SetActive(true);
        expressionText.gameObject.SetActive(false);
        
    }


    public void InitPortal(bool isBlue, bool isExpressionPortal = true)
    {   

        if (isBlue)
        {
            SetColor(PortalExpressionsController.Instance.blueColor);
        } else {   
            SetColor(PortalExpressionsController.Instance.redColor);
        }

        if (isExpressionPortal)
        {
            ConfigurePortal(true);
        } else {
            ConfigureTroopPortal();
        }
    }
}
