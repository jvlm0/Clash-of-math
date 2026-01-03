using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PortalExpressionsController : MonoBehaviour
{   
    [Header("Textos dos Portais")]
    [SerializeField] private List<string> bluePortalTexts = new List<string>();
    [SerializeField] private List<string> redPortalTexts = new List<string>();

    [Header("Cores dos Portais")]
    public Color blueColor = new Color(0.2f, 0.4f, 1f);
    public Color redColor = new Color(1f, 0.2f, 0.2f);



    
    private List<string> availableBlueTexts;
    private List<string> availableRedTexts;

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
    }

    void Start()
    {
        InitializeTextLists();
    }

    private void InitializeTextLists()
    {
        availableBlueTexts = new List<string>(bluePortalTexts);
        availableRedTexts = new List<string>(redPortalTexts);
        
        // Embaralhar as listas
        ShuffleList(availableBlueTexts);
        ShuffleList(availableRedTexts);
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
}