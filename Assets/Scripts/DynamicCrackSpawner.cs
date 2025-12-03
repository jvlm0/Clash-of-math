using UnityEngine;
using System.Collections.Generic;

public class DynamicCrackSpawner : MonoBehaviour
{
    [Header("References")]
    public GameObject crackPrefab; // Prefab com CenteredCrackController
    public Transform crackContainer;
    
    [Header("Spawn Settings")]
    public LayerMask groundLayer;
    public bool spawnOnClick = true;
    
    [Header("Crack Settings")]
    public float defaultDepth = 1.5f;
    public float defaultWidth = 0.05f;
    public float defaultLength = 0.6f;
    public bool randomizeAppearance = true;
    
    private List<GameObject> activeCracks = new List<GameObject>();
    
    void Update()
    {
        if (spawnOnClick && Input.GetMouseButtonDown(0))
        {
            SpawnCrackAtMouse();
        }
    }
    
    void SpawnCrackAtMouse()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, 1000f, groundLayer))
        {
            SpawnCrackAtPosition(hit.point, hit.collider.gameObject);
        }
    }
    
    public void SpawnCrackAtPosition(Vector3 worldPosition, GameObject targetObject)
    {
        // Cria instância
        GameObject crackObj = Instantiate(crackPrefab, crackContainer);
        crackObj.transform.position = targetObject.transform.position;
        crackObj.transform.rotation = targetObject.transform.rotation;
        crackObj.transform.localScale = targetObject.transform.localScale;
        
        // Pega o controlador
        CenteredCrackController controller = crackObj.GetComponent<CenteredCrackController>();
        
        if (controller != null)
        {
            // Define posição baseada no hit
            controller.SetPositionWorldSpace(worldPosition);
            
            // Configura aparência
            if (randomizeAppearance)
            {
                controller.RandomizeCrack();
            }
            else
            {
                controller.depth = defaultDepth;
                controller.width = defaultWidth;
                controller.length = defaultLength;
            }
            
            // Anima formação
            controller.StartCrackFormation();
        }
        
        activeCracks.Add(crackObj);
    }
    
    public void ClearAllCracks()
    {
        foreach (GameObject crack in activeCracks)
        {
            if (crack != null)
                Destroy(crack);
        }
        
        activeCracks.Clear();
    }
    
    public void SpawnRandomCracks(int count)
    {
        Collider groundCollider = GetComponent<Collider>();
        if (groundCollider == null) return;
        
        Bounds bounds = groundCollider.bounds;
        
        for (int i = 0; i < count; i++)
        {
            Vector3 randomPos = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                bounds.center.y,
                Random.Range(bounds.min.z, bounds.max.z)
            );
            
            SpawnCrackAtPosition(randomPos, gameObject);
        }
    }
}