using UnityEngine;
using System.Collections.Generic;

public class PortalSpawner : MonoBehaviour
{
    [Header("Configurações de Portais")]
    [SerializeField] private GameObject portalPrefab;
    [SerializeField] private int numberOfPortalPairs = 5;
    
    
    
    
    [Header("Posicionamento")]
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform endPoint;
    [SerializeField] private float distanceBetweenPairPortals = 2f;
    [SerializeField] private float verticalOffset = 0f; // Ajuste vertical adicional se necessário
    [SerializeField] private float horizontalOffset = 3f; // Deslocamento horizontal aleatório dos pares
    
    
    

    private float portalHeight = 0f;
    
    private void Start()
    {
        SpawnPortals();
        
    }
    
    [ContextMenu("Gerar Portais")]
    public void SpawnPortals()
    {
        // Limpar portais existentes
        ClearExistingPortals();
        
        // Validações
        if (!ValidateSetup())
        {
            return;
        }
        
        // Calcular altura do portal
        CalculatePortalHeight();
        
        // Inicializar listas de textos disponíveis
        
        
        // Calcular posições
        float startZ = startPoint.position.z;
        float endZ = endPoint.position.z;
        float totalDistance = Mathf.Abs(endZ - startZ);
        
        
        if (numberOfPortalPairs <= 1)
        {
            SpawnPortalPair(startPoint.position, 0);
        }
        else
        {
            float spacing = totalDistance / (numberOfPortalPairs - 1);
            int direction = startZ > endZ ? -1 : 1;
            
            
            
            for (int i = 0; i < numberOfPortalPairs; i++)
            {
                float zPosition = startZ + (spacing * i * direction);
                
                // Calcular Y considerando a altura do portal
                float yPosition = startPoint.position.y + (portalHeight / 2f) + verticalOffset;
                
                // Calcular deslocamento horizontal aleatório (primeiro par sempre no centro)
                float xOffset = 0f;
                if (i > 0 && horizontalOffset > 0f)
                {
                    xOffset = Random.Range(-horizontalOffset, horizontalOffset);
                }
                
                Vector3 position = new Vector3(startPoint.position.x + xOffset, yPosition, zPosition);
                
                
                SpawnPortalPair(position, i);
            }
        }
    }
    
    private bool ValidateSetup()
    {
        if (portalPrefab == null)
        {
            Debug.LogError("Portal Prefab não está atribuído!");
            return false;
        }
        
        if (startPoint == null || endPoint == null)
        {
            Debug.LogError("Start Point ou End Point não estão atribuídos!");
            return false;
        }
        
    
        
        return true;
    }
    
    private void CalculatePortalHeight()
    {
        // Instanciar temporariamente um portal para obter sua altura
        GameObject tempPortal = Instantiate(portalPrefab);
        Portal portalScript = tempPortal.GetComponent<Portal>();
        
        if (portalScript != null)
        {
            portalHeight = portalScript.GetPortalHeight();
        }
        
        // Destruir o portal temporário
        if (Application.isPlaying)
        {
            Destroy(tempPortal);
        }
        else
        {
            DestroyImmediate(tempPortal);
        }
    }
    
    
    
    private void SpawnPortalPair(Vector3 centerPosition, int pairIndex)
    {
        // Decidir aleatoriamente qual portal (esquerdo ou direito) será azul ou vermelho
        bool leftIsBlue = Random.Range(0, 2) == 0;
        bool isExpressionPortal = Random.Range(0f, 1f) < 0.7f;
        
        // Calcular posições dos portais do par (separados no eixo X)
        Vector3 leftPosition = centerPosition - Vector3.right * (distanceBetweenPairPortals / 2f);
        Vector3 rightPosition = centerPosition + Vector3.right * (distanceBetweenPairPortals / 2f);
        
        GameObject portalParent = new GameObject($"Portal_Pair{pairIndex}");
        portalParent.transform.position = centerPosition;
        portalParent.transform.parent = this.transform;
        portalParent.AddComponent<UniquePairPortalCollider>();

        // Criar portais
        GameObject leftPortal = Instantiate(portalPrefab, leftPosition, portalPrefab.transform.rotation, portalParent.transform);
        GameObject rightPortal = Instantiate(portalPrefab, rightPosition, portalPrefab.transform.rotation, portalParent.transform);
        
        leftPortal.name = $"Portal_Pair{pairIndex}_Left_{(leftIsBlue ? "Blue" : "Red")}";
        rightPortal.name = $"Portal_Pair{pairIndex}_Right_{(leftIsBlue ? "Red" : "Blue")}";
        
        // Configurar portais
        Portal leftPortalScript = leftPortal.GetComponent<Portal>();
        Portal rightPortalScript = rightPortal.GetComponent<Portal>();


        PortalExpressionsController.Instance.InitPairPortals(leftPortalScript, rightPortalScript, !leftIsBlue, isExpressionPortal);
    }


    private void ClearExistingPortals()
    {
        // Remover todos os portais filhos
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            if (Application.isPlaying)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
            else
            {
                DestroyImmediate(transform.GetChild(i).gameObject);
            }
        }
    }
}