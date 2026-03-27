using UnityEngine;
using System.Collections.Generic;

public class StackPrefabs : MonoBehaviour
{
    [Header("Configurações dos Prefabs")]
    [SerializeField] private List<GameObject> prefabList = new List<GameObject>();

    [SerializeField] private Vector3 prefabInitialScale = Vector3.one;

    [Header("Configurações de Slot")]
    [SerializeField] private GameObject slotPrefab; // Prefab a ser instanciado na posição do 'slot'
    [SerializeField] private Vector3 slotInitialScale = Vector3.one; // Escala inicial do slot no primeiro nível

    [Header("Configurações de Escala")]
    [SerializeField] private Vector3 scaleMultiplier = new Vector3(0.8f, 0.8f, 0.8f);

    [Header("Configurações de Empilhamento")]
    [SerializeField] private float spacing = 0.01f;
    [SerializeField] private bool usePhysicsDelay = true;
    [SerializeField] private float physicsDelayTime = 0.1f;

    [Header("Ponto Inicial")]
    [SerializeField] private Transform startPoint;

    private List<GameObject> stackedObjects = new List<GameObject>();
    private List<GameObject> spawnedSlotObjects = new List<GameObject>(); // Objetos instanciados nos slots

    public int levels = 1;

    void Start()
    {
        StackObjects(Random.Range(2, 4));
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.Space))
        {
            StackObjects(levels);
        }
    }

    public void StackObjects(int numberOfLevels)
    {
        if (prefabList == null || prefabList.Count == 0)
        {
            Debug.LogError("Lista de prefabs está vazia!");
            return;
        }

        ClearStack();

        Vector3 currentPosition = startPoint != null ? startPoint.position : transform.position;
        Vector3 currentScale = prefabInitialScale;
        Vector3 currentSlotScale = slotInitialScale;

        for (int i = 0; i < numberOfLevels; i++)
        {
            GameObject randomPrefab = prefabList[Random.Range(0, prefabList.Count)];
            GameObject obj = Instantiate(randomPrefab, currentPosition, transform.rotation, transform);
            obj.transform.Rotate(new Vector3(0, 90, 0));
            obj.name = $"{randomPrefab.name}_Level_{i + 1}";

            SetRigidbodiesActive(obj, false);
            obj.transform.localScale = currentScale;

            float bottomOffset = GetBottomOffset(obj);
            obj.transform.position = new Vector3(currentPosition.x, currentPosition.y - bottomOffset, currentPosition.z);

            stackedObjects.Add(obj);

            // Spawna o prefab do slot na posição do filho chamado 'slot', na hierarquia global
            SpawnSlotPrefab(obj, currentSlotScale, i);

            float objectHeight = GetObjectHeight(obj);
            currentPosition.y += objectHeight + spacing;

            currentScale = new Vector3(
                currentScale.x * scaleMultiplier.x,
                currentScale.y * scaleMultiplier.y,
                currentScale.z * scaleMultiplier.z
            );

            // Reduz a escala do slot pelo mesmo fator de scaleMultiplier
            currentSlotScale = new Vector3(
                currentSlotScale.x * scaleMultiplier.x,
                currentSlotScale.y * scaleMultiplier.y,
                currentSlotScale.z * scaleMultiplier.z
            );
        }

        if (usePhysicsDelay)
        {
            StartCoroutine(EnablePhysicsWithDelay());
        }
        else
        {
            EnableAllPhysics();
        }

        Debug.Log($"Empilhados {numberOfLevels} níveis com sucesso!");
    }

    /// <summary>
    /// Instancia o slotPrefab na posição do filho 'slot' dentro do stackedObject,
    /// mas mantém o objeto na hierarquia global da cena.
    /// </summary>
    private void SpawnSlotPrefab(GameObject stackedObj, Vector3 slotScale, int levelIndex)
    {
        if (slotPrefab == null) return;

        Transform slotTransform = FindSlotRecursive(stackedObj.transform);
        if (slotTransform == null)
        {
            Debug.LogWarning($"Nenhum filho chamado 'slot' encontrado em {stackedObj.name}.");
            return;
        }

        // Instancia na posição/rotação do slot, mas sem pai (hierarquia global)
        GameObject slotObj = Instantiate(slotPrefab, slotTransform.position, slotTransform.rotation, null);
        slotObj.name = $"{slotPrefab.name}_Slot_Level_{levelIndex + 1}";
        slotObj.transform.localScale = slotScale;

        slotObj.GetComponent<NpcController>().DisableIa();
        Debug.Log("zumbi spawnado");
        // Desativa física imediatamente, igual aos stackedObjects
        SetRigidbodiesActive(slotObj, false);

        spawnedSlotObjects.Add(slotObj);
    }

    /// <summary>
    /// Busca recursivamente um filho chamado 'slot' dentro de um Transform.
    /// </summary>
    private Transform FindSlotRecursive(Transform parent)
    {
        foreach (Transform child in parent)
        {
            if (child.name.ToLower() == "slot")
                return child;

            Transform found = FindSlotRecursive(child);
            if (found != null) return found;
        }
        return null;
    }

    private System.Collections.IEnumerator EnablePhysicsWithDelay()
    {
        yield return new WaitForSeconds(physicsDelayTime);
        EnableAllPhysics();
    }

    private float GetObjectHeight(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds combinedBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                combinedBounds.Encapsulate(renderers[i].bounds);
            return combinedBounds.size.y;
        }

        Collider[] colliders = obj.GetComponentsInChildren<Collider>();
        if (colliders.Length > 0)
        {
            Bounds combinedBounds = colliders[0].bounds;
            for (int i = 1; i < colliders.Length; i++)
                combinedBounds.Encapsulate(colliders[i].bounds);
            return combinedBounds.size.y;
        }

        Debug.LogWarning($"Não foi possível determinar a altura de {obj.name}. Usando valor padrão de 1.");
        return 1f;
    }

    private float GetBottomOffset(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds combinedBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                combinedBounds.Encapsulate(renderers[i].bounds);
            return obj.transform.position.y - combinedBounds.min.y;
        }

        Collider[] colliders = obj.GetComponentsInChildren<Collider>();
        if (colliders.Length > 0)
        {
            Bounds combinedBounds = colliders[0].bounds;
            for (int i = 1; i < colliders.Length; i++)
                combinedBounds.Encapsulate(colliders[i].bounds);
            return obj.transform.position.y - combinedBounds.min.y;
        }

        return 0f;
    }

    public void ClearStack()
    {
        foreach (GameObject obj in stackedObjects)
        {
            if (obj != null) Destroy(obj);
        }
        stackedObjects.Clear();

        // Destroi também os objetos instanciados nos slots
        foreach (GameObject slotObj in spawnedSlotObjects)
        {
            if (slotObj != null) Destroy(slotObj);
        }
        spawnedSlotObjects.Clear();
    }

    public void AddLevel()
    {
        if (prefabList == null || prefabList.Count == 0)
        {
            Debug.LogError("Lista de prefabs está vazia!");
            return;
        }

        Vector3 topPosition = startPoint != null ? startPoint.position : transform.position;
        Vector3 currentScale = prefabInitialScale;
        Vector3 currentSlotScale = slotInitialScale;

        int currentLevel = stackedObjects.Count;
        for (int i = 0; i < currentLevel; i++)
        {
            currentScale = new Vector3(
                currentScale.x * scaleMultiplier.x,
                currentScale.y * scaleMultiplier.y,
                currentScale.z * scaleMultiplier.z
            );
            currentSlotScale = new Vector3(
                currentSlotScale.x * scaleMultiplier.x,
                currentSlotScale.y * scaleMultiplier.y,
                currentSlotScale.z * scaleMultiplier.z
            );
        }

        if (stackedObjects.Count > 0)
        {
            GameObject lastObj = stackedObjects[stackedObjects.Count - 1];
            if (lastObj != null)
            {
                float height = GetObjectHeight(lastObj);
                topPosition = new Vector3(
                    lastObj.transform.position.x,
                    lastObj.transform.position.y + height + spacing,
                    lastObj.transform.position.z
                );
            }
        }

        GameObject randomPrefab = prefabList[Random.Range(0, prefabList.Count)];
        GameObject newObj = Instantiate(randomPrefab, topPosition, Quaternion.identity, transform);
        newObj.name = $"{randomPrefab.name}_Level_{currentLevel + 1}";
        newObj.transform.localScale = currentScale;

        SetRigidbodiesActive(newObj, false);

        float bottomOffset = GetBottomOffset(newObj);
        newObj.transform.position = new Vector3(topPosition.x, topPosition.y - bottomOffset, topPosition.z);

        stackedObjects.Add(newObj);

        SpawnSlotPrefab(newObj, currentSlotScale, currentLevel);

        StartCoroutine(EnablePhysicsDelayed(newObj, spawnedSlotObjects.Count > 0 ? spawnedSlotObjects[spawnedSlotObjects.Count - 1] : null));

        Debug.Log($"Adicionado nível {currentLevel + 1} com escala {currentScale}");
    }

    private void SetRigidbodiesActive(GameObject obj, bool active)
    {
        Rigidbody[] rigidbodies = obj.GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody rb in rigidbodies)
            rb.isKinematic = !active;
    }

    public void EnableAllPhysics()
    {
        // Ativa física dos objetos empilhados
        foreach (GameObject obj in stackedObjects)
        {
            if (obj != null)
            {
                Rigidbody[] rigidbodies = obj.GetComponentsInChildren<Rigidbody>();
                foreach (Rigidbody rb in rigidbodies)
                {
                    rb.isKinematic = false;
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.Sleep();
                }
            }
        }

        // Ativa física dos objetos instanciados nos slots
        foreach (GameObject slotObj in spawnedSlotObjects)
        {
            if (slotObj != null)
            {
                Rigidbody[] rigidbodies = slotObj.GetComponentsInChildren<Rigidbody>();
                foreach (Rigidbody rb in rigidbodies)
                {
                    rb.isKinematic = false;
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.Sleep();
                }
            }
        }
    }

    public void DisableAllPhysics()
    {
        foreach (GameObject obj in stackedObjects)
        {
            if (obj != null) SetRigidbodiesActive(obj, false);
        }
    }

    private System.Collections.IEnumerator EnablePhysicsDelayed(GameObject obj, GameObject slotObj = null)
    {
        yield return new WaitForSeconds(physicsDelayTime);

        Rigidbody[] rigidbodies = obj.GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody rb in rigidbodies)
        {
            rb.isKinematic = false;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.Sleep();
        }

        if (slotObj != null)
        {
            Rigidbody[] slotRigidbodies = slotObj.GetComponentsInChildren<Rigidbody>();
            foreach (Rigidbody rb in slotRigidbodies)
            {
                rb.isKinematic = false;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.Sleep();
            }
        }
    }

    public int GetCurrentLevelCount()
    {
        return stackedObjects.Count;
    }
}