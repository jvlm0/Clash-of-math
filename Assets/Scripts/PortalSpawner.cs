using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class PortalSpawner : MonoBehaviour
{
    [Header("Configurações de Portais")]
    [SerializeField] private GameObject portalPrefab;
    [SerializeField] private int numberOfPortalPairs = 5;
    
    [Header("Cores dos Portais")]
    [SerializeField] private Color blueColor = new Color(0.2f, 0.4f, 1f);
    [SerializeField] private Color redColor = new Color(1f, 0.2f, 0.2f);
    
    [Header("Posicionamento")]
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform endPoint;
    [SerializeField] private float distanceBetweenPairPortals = 2f;
    [SerializeField] private float verticalOffset = 0f; // Ajuste vertical adicional se necessário
    [SerializeField] private float horizontalOffset = 3f; // Deslocamento horizontal aleatório dos pares
    
    [Header("Textos dos Portais")]
    [SerializeField] private List<string> bluePortalTexts = new List<string>();
    [SerializeField] private List<string> redPortalTexts = new List<string>();
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    
    private List<string> availableBlueTexts;
    private List<string> availableRedTexts;
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
        InitializeTextLists();
        
        // Calcular posições
        float startZ = startPoint.position.z;
        float endZ = endPoint.position.z;
        float totalDistance = Mathf.Abs(endZ - startZ);
        
        if (showDebugInfo)
        {
            Debug.Log($"=== SPAWNING PORTALS ===");
            Debug.Log($"Start Z: {startZ}, End Z: {endZ}");
            Debug.Log($"Total Distance: {totalDistance}");
            Debug.Log($"Number of Pairs: {numberOfPortalPairs}");
            Debug.Log($"Portal Height: {portalHeight}");
        }
        
        if (numberOfPortalPairs <= 1)
        {
            SpawnPortalPair(startPoint.position, 0);
        }
        else
        {
            float spacing = totalDistance / (numberOfPortalPairs - 1);
            int direction = startZ > endZ ? -1 : 1;
            
            if (showDebugInfo)
            {
                Debug.Log($"Spacing: {spacing}, Direction: {direction}");
            }
            
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
                
                if (showDebugInfo)
                {
                    Debug.Log($"Pair {i}: Position = {position}, X Offset = {xOffset}");
                }
                
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
        
        if (bluePortalTexts.Count == 0 || redPortalTexts.Count == 0)
        {
            Debug.LogWarning("As listas de textos estão vazias! Usando textos padrão.");
            if (bluePortalTexts.Count == 0)
            {
                bluePortalTexts.Add("Portal Azul");
            }
            if (redPortalTexts.Count == 0)
            {
                redPortalTexts.Add("Portal Vermelho");
            }
        }
        
        if (numberOfPortalPairs <= 0)
        {
            Debug.LogError("Number of Portal Pairs deve ser maior que 0!");
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
    
    private void InitializeTextLists()
    {
        availableBlueTexts = new List<string>(bluePortalTexts);
        availableRedTexts = new List<string>(redPortalTexts);
        
        // Embaralhar as listas
        ShuffleList(availableBlueTexts);
        ShuffleList(availableRedTexts);
    }
    
    private void SpawnPortalPair(Vector3 centerPosition, int pairIndex)
    {
        // Decidir aleatoriamente qual portal (esquerdo ou direito) será azul ou vermelho
        bool leftIsBlue = Random.Range(0, 2) == 0;
        
        // Calcular posições dos portais do par (separados no eixo X)
        Vector3 leftPosition = centerPosition - Vector3.right * (distanceBetweenPairPortals / 2f);
        Vector3 rightPosition = centerPosition + Vector3.right * (distanceBetweenPairPortals / 2f);
        
        // Criar portais
        GameObject leftPortal = Instantiate(portalPrefab, leftPosition, portalPrefab.transform.rotation, transform);
        GameObject rightPortal = Instantiate(portalPrefab, rightPosition, portalPrefab.transform.rotation, transform);
        
        leftPortal.name = $"Portal_Pair{pairIndex}_Left_{(leftIsBlue ? "Blue" : "Red")}";
        rightPortal.name = $"Portal_Pair{pairIndex}_Right_{(leftIsBlue ? "Red" : "Blue")}";
        
        // Configurar portais
        Portal leftPortalScript = leftPortal.GetComponent<Portal>();
        Portal rightPortalScript = rightPortal.GetComponent<Portal>();
        
        if (leftPortalScript == null || rightPortalScript == null)
        {
            Debug.LogError("O prefab do portal não possui o componente Portal!");
            return;
        }
        
        if (leftIsBlue)
        {
            ConfigurePortal(leftPortalScript, blueColor, true);
            ConfigurePortal(rightPortalScript, redColor, false);
        }
        else
        {
            ConfigurePortal(leftPortalScript, redColor, false);
            ConfigurePortal(rightPortalScript, blueColor, true);
        }
    }
    
    private void ConfigurePortal(Portal portal, Color color, bool isBlue)
    {
        portal.SetColor(color);
        
        string text = GetRandomText(isBlue);
        portal.SetText(text);
        
        if (showDebugInfo)
        {
            Debug.Log($"Configured {portal.gameObject.name}: Color={color}, Text={text}");
        }
    }
    
    private string GetRandomText(bool isBlue)
    {
        List<string> sourceList = isBlue ? availableBlueTexts : availableRedTexts;
        List<string> originalList = isBlue ? bluePortalTexts : redPortalTexts;
        
        // Se a lista disponível estiver vazia, reinicializar
        if (sourceList.Count == 0)
        {
            sourceList.AddRange(originalList);
            ShuffleList(sourceList);
        }
        
        // Pegar e remover o primeiro texto da lista
        string text = sourceList[0];
        sourceList.RemoveAt(0);
        
        return text;
    }
    
    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
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