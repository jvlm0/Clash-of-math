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

    [Header("Função no canvas")]
    public FunctionCanvasManager functionCanvasManager; 

    [Header("Função malha prefab")]
    public GameObject functionMeshPrefab;

    public Transform transformFunctionPos;

    [Header("Tropas dos portais")]
    [SerializeField] private List<GameObjectSpritePair> portalTroopPairs = new List<GameObjectSpritePair>();
    private Dictionary<GameObject, Sprite> portalPrefabs;
    
    private List<string> availableBlueTexts;
    private List<string> availableRedTexts;
    private List<GameObject> availablePrefabs;
    private List<Sprite> availableSprites;


    public List<PortalPairExpression> portalPairExpressions = new List<PortalPairExpression>();

    public List<PortalInfo> enemyPortals = new List<PortalInfo>();

    public static PortalExpressionsController Instance;



    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
           
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

        InitializeLists();

    }

    void Start()
    {
        
    }


    public void InitPairPortals(Portal leftPortal, Portal rightPortal, bool rightIsBlue, bool isExpressionPortal)
    {
        PortalPairExpression portalPair = new PortalPairExpression();
        bool enemyChoice = Random.Range(0,2) == 1 ? true : false;
        
        portalPair.bluePortal = new PortalInfo();
        portalPair.redPortal = new PortalInfo();

        if (isExpressionPortal)
        {   
            portalPair.bluePortal.isExpressionPortal = true;
            portalPair.redPortal.isExpressionPortal = true;
            portalPair.bluePortal.portalText = GetRandomText(true);
            portalPair.redPortal.portalText = GetRandomText(false);

            if (rightIsBlue)
            {
                rightPortal.InitPortal(rightIsBlue, portalPair.bluePortal.portalText, null, 0, null);
                leftPortal.InitPortal(!rightIsBlue, portalPair.redPortal.portalText, null, 0, null);
            } else
            {
                rightPortal.InitPortal(rightIsBlue, portalPair.redPortal.portalText, null, 0, null);
                leftPortal.InitPortal(!rightIsBlue, portalPair.bluePortal.portalText, null, 0, null);
            }
            
        }
        else {
            portalPair.bluePortal.isExpressionPortal = false;
            portalPair.redPortal.isExpressionPortal = false;
            (portalPair.bluePortal.portalPrefab, portalPair.bluePortal.troopSprite) = GetRandomTroop();
            (portalPair.redPortal.portalPrefab, portalPair.redPortal.troopSprite) = GetRandomTroop();

            if (rightIsBlue)
            {
                rightPortal.InitPortal(rightIsBlue, "", portalPair.bluePortal.portalPrefab, Random.Range(7,12), portalPair.bluePortal.troopSprite);
                leftPortal.InitPortal(!rightIsBlue, "", portalPair.redPortal.portalPrefab, Random.Range(4,8), portalPair.redPortal.troopSprite);
            } 
            else
            {
                rightPortal.InitPortal(rightIsBlue, "", portalPair.redPortal.portalPrefab, Random.Range(4,8), portalPair.redPortal.troopSprite);
                leftPortal.InitPortal(!rightIsBlue, "", portalPair.bluePortal.portalPrefab, Random.Range(7,12), portalPair.bluePortal.troopSprite);
            }
        }

        if (enemyChoice)
        {
            enemyPortals.Add(portalPair.bluePortal);
        } 
        else
        {
            enemyPortals.Add(portalPair.redPortal);
        }
    }


    public string AppendExpression(string currentExpression, string expToAppend)
    {
        if (expToAppend[0] == '+' || expToAppend[0] == '-')
        {
            currentExpression+=expToAppend;
        } 
        else
        {     
            if (currentExpression == "") 
            {
                currentExpression = expToAppend;
            }
            else 
            {
                currentExpression = $"({currentExpression}){expToAppend}";
            }    
        }

        return currentExpression;
    }

    public void GetEnemyEquationOnIPortal(int iPortal, bool draw = true)
    {
        string r = "";

        iPortal++;

        if (iPortal >  enemyPortals.Count) return;

        for (int i = 0;  i < iPortal; i++)
        {
            if (enemyPortals[i].isExpressionPortal)
            {
                r = AppendExpression(r, enemyPortals[i].portalText);
            }    
        }
        Debug.Log($"Atual equação do inimigo no portal {iPortal}: {r}");
        if (r != "" && draw) {
            functionCanvasManager.UpdateEnemyFunction(r);   
        }

        if (!draw)
        {
            GameObject functionMesh = Instantiate(functionMeshPrefab, transformFunctionPos.position, Quaternion.identity);
            FunctionMeshGenerator meshGenerator = functionMesh.GetComponent<FunctionMeshGenerator>();
            meshGenerator.mathExpression = r;
        }

    }


    public void DrawEnemyMeshFuction()
    {
        GetEnemyEquationOnIPortal(enemyPortals.Count-1, false);
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