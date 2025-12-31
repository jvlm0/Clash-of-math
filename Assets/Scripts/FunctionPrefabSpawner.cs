using System.Collections.Generic;
using UnityEngine;

public class FunctionPrefabSpawner : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField]
    [Tooltip("Referência ao script FunctionMeshGenerator")]
    private FunctionMeshGenerator functionGenerator;

    [Header("Configurações de Spawn")]
    [SerializeField]
    private GameObject prefabToSpawn;

    [SerializeField]
    [Range(1, 500)]
    private int numberOfPrefabs = 10;

    [SerializeField]
    private SpawnMode spawnMode = SpawnMode.EvenlySpaced;

    [SerializeField]
    [Tooltip("Distância entre prefabs (apenas para modo ByDistance)")]
    private float spawnDistance = 1f;

    [SerializeField]
    [Tooltip("Tolerância para arredondamento de posição (0 = sem arredondamento)")]
    private float snapTolerance = 0.1f;

    [SerializeField]
    private bool spawnOnStart = false;

    [SerializeField]
    [Tooltip("Atualizar posições durante animação da função")]
    private bool updateDuringAnimation = true;

    [Header("Posicionamento")]
    [SerializeField]
    private float heightOffset = 0.5f;

    [SerializeField]
    private bool alignToFunctionSlope = true;

    [SerializeField]
    [Tooltip("Rotação adicional em graus (Y)")]
    private float additionalRotation = 0f;

    [Header("Variação")]
    [SerializeField]
    private bool randomizeScale = false;

    [SerializeField]
    private Vector2 scaleRange = new Vector2(0.8f, 1.2f);

    [SerializeField]
    private bool randomizeRotation = false;

    [SerializeField]
    private Vector2 rotationRange = new Vector2(-15f, 15f);

    [Header("Organização")]
    [SerializeField]
    private bool useParentObject = true;

    [SerializeField]
    private string parentName = "SpawnedPrefabs";

    public enum SpawnMode
    {
        EvenlySpaced,      // Distribui uniformemente pelos índices dos pontos
        EveryNthPoint,     // A cada N pontos
        RandomPoints,      // Pontos aleatórios
        ByDistance         // A cada X unidades de distância ao longo da curva
    }

    private List<GameObject> spawnedObjects = new List<GameObject>();
    private Transform parentTransform;

    void Start()
    {
        if (functionGenerator == null)
        {
            functionGenerator = GetComponent<FunctionMeshGenerator>();
            if (functionGenerator == null)
            {
                Debug.LogError("FunctionMeshGenerator não encontrado! Adicione uma referência.");
                return;
            }
        }

        if (useParentObject)
        {
            GameObject parent = new GameObject(parentName);
            parent.transform.parent = transform;
            parent.transform.localPosition = Vector3.zero;
            parentTransform = parent.transform;
        }

        if (spawnOnStart)
        {
            // Aguarda um frame para garantir que a malha foi gerada
            Invoke(nameof(SpawnPrefabs), 0.1f);
        }
    }

    void Update()
    {
        if (updateDuringAnimation && spawnedObjects.Count > 0)
        {
            UpdatePrefabPositions();
        }

        // Atalhos do teclado
        if (Input.GetKeyDown(KeyCode.S))
        {
            SpawnPrefabs();
        }

        if (Input.GetKeyDown(KeyCode.X))
        {
            ClearPrefabs();
        }
    }

    public void SpawnPrefabs()
    {
        if (prefabToSpawn == null)
        {
            Debug.LogError("Nenhum prefab atribuído para spawnar!");
            return;
        }

        ClearPrefabs();

        // Acessa os segmentos da malha através de reflexão
        var meshSegmentsField = typeof(FunctionMeshGenerator).GetField("meshSegments", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (meshSegmentsField == null)
        {
            Debug.LogError("Não foi possível acessar meshSegments!");
            return;
        }

        var meshSegments = meshSegmentsField.GetValue(functionGenerator) as System.Collections.IList;
        
        if (meshSegments == null || meshSegments.Count == 0)
        {
            Debug.LogWarning("Nenhum segmento de malha disponível. Gere a malha primeiro!");
            return;
        }

        int totalSpawned = 0;

        // Spawna prefabs em cada segmento
        foreach (var segment in meshSegments)
        {
            var pointsProperty = segment.GetType().GetField("points");
            var points = pointsProperty.GetValue(segment) as System.Collections.IList;

            if (points == null || points.Count < 2)
                continue;

            List<int> selectedIndices;
            
            if (spawnMode == SpawnMode.ByDistance)
            {
                selectedIndices = SelectPointsByDistance(points);
            }
            else
            {
                selectedIndices = SelectPointIndices(points.Count);
            }

            foreach (int index in selectedIndices)
            {
                var point = points[index];
                var xField = point.GetType().GetField("x");
                var zField = point.GetType().GetField("z");
                
                float x = (float)xField.GetValue(point);
                float z = (float)zField.GetValue(point);

                SpawnPrefabAt(x, z, index, points);
                totalSpawned++;
            }
        }

        Debug.Log($"{totalSpawned} prefabs instanciados ao longo da função");
    }

    List<int> SelectPointIndices(int totalPoints)
    {
        List<int> indices = new List<int>();

        switch (spawnMode)
        {
            case SpawnMode.EvenlySpaced:
                if (numberOfPrefabs >= totalPoints)
                {
                    // Se pediu mais prefabs que pontos, usa todos
                    for (int i = 0; i < totalPoints; i++)
                    {
                        indices.Add(i);
                    }
                }
                else if (numberOfPrefabs == 1)
                {
                    indices.Add(totalPoints / 2);
                }
                else
                {
                    float step = (float)(totalPoints - 1) / (numberOfPrefabs - 1);
                    for (int i = 0; i < numberOfPrefabs; i++)
                    {
                        indices.Add(Mathf.RoundToInt(i * step));
                    }
                }
                break;

            case SpawnMode.EveryNthPoint:
                int interval = Mathf.Max(1, totalPoints / numberOfPrefabs);
                for (int i = 0; i < totalPoints; i += interval)
                {
                    indices.Add(i);
                }
                break;

            case SpawnMode.RandomPoints:
                // Cria lista de índices disponíveis e embaralha
                List<int> availableIndices = new List<int>();
                for (int i = 0; i < totalPoints; i++)
                {
                    availableIndices.Add(i);
                }

                int count = Mathf.Min(numberOfPrefabs, totalPoints);
                for (int i = 0; i < count; i++)
                {
                    int randomIndex = Random.Range(0, availableIndices.Count);
                    indices.Add(availableIndices[randomIndex]);
                    availableIndices.RemoveAt(randomIndex);
                }

                indices.Sort();
                break;
        }

        return indices;
    }

    List<int> SelectPointsByDistance(System.Collections.IList points)
    {
        List<int> indices = new List<int>();
        
        if (points.Count < 2)
            return indices;

        // Sempre inclui o primeiro ponto
        indices.Add(0);
        
        float accumulatedDistance = 0f;
        float targetDistance = spawnDistance;
        
        for (int i = 1; i < points.Count; i++)
        {
            var currentPoint = points[i];
            var prevPoint = points[i - 1];
            
            float currentX = (float)currentPoint.GetType().GetField("x").GetValue(currentPoint);
            float currentZ = (float)currentPoint.GetType().GetField("z").GetValue(currentPoint);
            float prevX = (float)prevPoint.GetType().GetField("x").GetValue(prevPoint);
            float prevZ = (float)prevPoint.GetType().GetField("z").GetValue(prevPoint);
            
            Vector3 p1 = new Vector3(prevX, 0, prevZ);
            Vector3 p2 = new Vector3(currentX, 0, currentZ);
            float segmentDistance = Vector3.Distance(p1, p2);
            
            accumulatedDistance += segmentDistance;
            
            // Verifica se atingiu a distância alvo
            if (accumulatedDistance >= targetDistance)
            {
                // Se está dentro da tolerância de arredondamento
                if (snapTolerance > 0f)
                {
                    float distanceError = accumulatedDistance - targetDistance;
                    
                    // Se o erro for pequeno o suficiente, spawna aqui
                    if (distanceError <= snapTolerance)
                    {
                        indices.Add(i);
                        accumulatedDistance = 0f;
                    }
                    // Se passou muito da distância, volta um ponto
                    else if (distanceError > spawnDistance * 0.5f && i > 0)
                    {
                        indices.Add(i - 1);
                        // Recalcula a distância acumulada a partir do ponto anterior
                        accumulatedDistance = segmentDistance;
                    }
                    else
                    {
                        indices.Add(i);
                        accumulatedDistance = 0f;
                    }
                }
                else
                {
                    // Sem arredondamento, spawna no ponto mais próximo
                    indices.Add(i);
                    accumulatedDistance = 0f;
                }
            }
        }
        
        return indices;
    }

    Vector3 GetPointPosition(object point)
    {
        float x = (float)point.GetType().GetField("x").GetValue(point);
        float z = (float)point.GetType().GetField("z").GetValue(point);
        return new Vector3(x, 0, z);
    }

    void SpawnPrefabAt(float x, float z, int currentIndex, System.Collections.IList points)
    {
        Vector3 position = new Vector3(x, heightOffset, z);
        
        GameObject instance = Instantiate(prefabToSpawn, transform.TransformPoint(position), Quaternion.identity);

        if (useParentObject && parentTransform != null)
        {
            instance.transform.parent = parentTransform;
        }
        else
        {
            instance.transform.parent = transform;
        }

        // Alinha com a inclinação da função usando pontos adjacentes
        if (alignToFunctionSlope && points.Count > 1)
        {
            int nextIndex = Mathf.Min(currentIndex + 1, points.Count - 1);
            int prevIndex = Mathf.Max(currentIndex - 1, 0);

            if (nextIndex != currentIndex || prevIndex != currentIndex)
            {
                var nextPoint = points[nextIndex];
                var prevPoint = points[prevIndex];

                float nextX = (float)nextPoint.GetType().GetField("x").GetValue(nextPoint);
                float nextZ = (float)nextPoint.GetType().GetField("z").GetValue(nextPoint);
                float prevX = (float)prevPoint.GetType().GetField("x").GetValue(prevPoint);
                float prevZ = (float)prevPoint.GetType().GetField("z").GetValue(prevPoint);

                Vector3 direction = new Vector3(nextX - prevX, 0, nextZ - prevZ).normalized;
                
                if (direction.magnitude > 0.001f)
                {
                    instance.transform.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0, additionalRotation, 0);
                }
            }
        }
        else
        {
            instance.transform.rotation = Quaternion.Euler(0, additionalRotation, 0);
        }

        // Aplica variações aleatórias
        if (randomizeScale)
        {
            float scale = Random.Range(scaleRange.x, scaleRange.y);
            instance.transform.localScale *= scale;
        }

        if (randomizeRotation)
        {
            float randomRot = Random.Range(rotationRange.x, rotationRange.y);
            instance.transform.Rotate(0, randomRot, 0);
        }

        spawnedObjects.Add(instance);
    }

    void UpdatePrefabPositions()
    {
        // Recalcula as posições durante a animação
        SpawnPrefabs();
    }

    public void ClearPrefabs()
    {
        foreach (GameObject obj in spawnedObjects)
        {
            if (obj != null)
            {
                if (Application.isPlaying)
                    Destroy(obj);
                else
                    DestroyImmediate(obj);
            }
        }
        spawnedObjects.Clear();
    }

    void OnDrawGizmosSelected()
    {
        if (spawnedObjects.Count == 0)
            return;

        Gizmos.color = Color.yellow;
        
        foreach (GameObject obj in spawnedObjects)
        {
            if (obj != null)
            {
                Gizmos.DrawWireSphere(obj.transform.position, 0.2f);
            }
        }
    }

    void OnDestroy()
    {
        ClearPrefabs();
    }
}