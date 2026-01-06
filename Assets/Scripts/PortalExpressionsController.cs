using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;

public class PortalExpressionsController : MonoBehaviour
{   
    [Header("Textos dos Portais")]
    [SerializeField] private List<string> bluePortalTexts = new List<string>();
    [SerializeField] private List<string> redPortalTexts = new List<string>();

    [Header("Cores dos Portais")]
    public Color blueColor = new Color(0.2f, 0.4f, 1f);
    public Color redColor = new Color(1f, 0.2f, 0.2f);


    [Header("Tropas dos portais")]
    [SerializeField] private List<GameObjectSpritePair> portalTroopPairs = new List<GameObjectSpritePair>();
    private Dictionary<GameObject, Sprite> portalPrefabs;
    
    private List<string> availableBlueTexts;
    private List<string> availableRedTexts;
    private List<GameObject> availablePrefabs;
    private List<Sprite> availableSprites;

    public static PortalExpressionsController Instance;


    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        portalPrefabs = new Dictionary<GameObject, Sprite>();
        foreach (var e in portalTroopPairs)
        {
            portalPrefabs[e.key] = e.value;
        }

    }

    void Start()
    {
        InitializeLists();
    }

    private void InitializeLists()
    {
        availableBlueTexts = new List<string>(bluePortalTexts);
        availableRedTexts = new List<string>(redPortalTexts);
        availablePrefabs = new List<GameObject>(portalPrefabs.Keys);

        // Embaralhar as listas
        ShuffleList(availableBlueTexts);
        ShuffleList(availableRedTexts);
        ShuffleList(availablePrefabs);
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

    public string GetRandomText(bool isBlue)
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

    public (GameObject prefab, Sprite sprite) GetRandomTroop()
    {
        // Se a lista disponível estiver vazia, reinicializar
        if (availablePrefabs.Count == 0)
        {
            availablePrefabs.AddRange(portalPrefabs.Keys);
            ShuffleList(availablePrefabs);
        }
        
        // Pegar e remover o primeiro prefab da lista
        GameObject prefab = availablePrefabs[0];
        availablePrefabs.RemoveAt(0);
        Sprite sprite = portalPrefabs[prefab];
        
        return (prefab, sprite);
    }
}