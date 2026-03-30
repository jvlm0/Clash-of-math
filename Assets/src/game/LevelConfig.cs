using UnityEngine;




public class LevelConfig : MonoBehaviour
{
    public PortalPairExpression[] portals;
    public int numberPrefabsToSpawn = 10;
    public int minHeightOfStructures = 2;
    public int maxHeightOfStructures = 5;
    public int numberOfSpawns = 3;
    public float timeToSpawn = 3f;
    public int numberOfWaves = 3;



    void Start()
    {
        EquationController.instance.currentEquation = "";
        GameController.Instance.currentLevelConfig = this;
        GetComponentInChildren<PortalSpawner>().Init(portals);

        PortalExpressionsController.Instance.enemyPortals.Clear();
    }
}