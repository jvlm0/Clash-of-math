using UnityEngine;
using System.Collections.Generic;

public class StackPrefabs : MonoBehaviour
{
    [Header("Configurações dos Prefabs")]
    [SerializeField] private List<GameObject> prefabList = new List<GameObject>();
    
    [Header("Configurações de Escala")]
    [SerializeField] private Vector3 scaleMultiplier = new Vector3(0.8f, 0.8f, 0.8f);
    
    [Header("Configurações de Empilhamento")]
    [SerializeField] private float spacing = 0.01f; // Pequeno espaçamento para evitar sobreposição de colliders
    [SerializeField] private bool usePhysicsDelay = true; // Delay antes de ativar física
    [SerializeField] private float physicsDelayTime = 0.1f; // Tempo de delay em segundos
    
    [Header("Ponto Inicial")]
    [SerializeField] private Transform startPoint; // Se null, usa a posição deste objeto
    
    private List<GameObject> stackedObjects = new List<GameObject>();

    
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


    /// <summary>
    /// Empilha prefabs aleatórios da lista com escala decrescente
    /// </summary>
    /// <param name="numberOfLevels">Número de níveis a empilhar</param>
    public void StackObjects(int numberOfLevels)
    {
        if (prefabList == null || prefabList.Count == 0)
        {
            Debug.LogError("Lista de prefabs está vazia!");
            return;
        }
        
        ClearStack();
        
        Vector3 currentPosition = startPoint != null ? startPoint.position : transform.position;
        Vector3 currentScale = Vector3.one;
        
        for (int i = 0; i < numberOfLevels; i++)
        {
            // Escolhe um prefab aleatório da lista
            GameObject randomPrefab = prefabList[Random.Range(0, prefabList.Count)];
            
            // Instancia o prefab
            GameObject obj = Instantiate(randomPrefab, currentPosition, transform.rotation, transform);
            //obj.transform.Rotate(new Vector3(0, 90, 0));
            obj.name = $"{randomPrefab.name}_Level_{i + 1}";
            
            // Desativa todos os Rigidbodies para posicionamento
            SetRigidbodiesActive(obj, false);
            
            // Aplica a escala
            obj.transform.localScale = currentScale;
            
            // Ajusta a posição para que a base do objeto fique em currentPosition
            float bottomOffset = GetBottomOffset(obj);
            obj.transform.position = new Vector3(currentPosition.x, currentPosition.y - bottomOffset, currentPosition.z);
            
            // Adiciona à lista de objetos empilhados
            stackedObjects.Add(obj);
            
            // Calcula a altura do objeto para posicionar o próximo
            float objectHeight = GetObjectHeight(obj);
            
            // Atualiza a posição para o próximo objeto (empilha para cima)
            currentPosition.y += objectHeight + spacing;
            
            // Atualiza a escala para o próximo nível (multiplica componente por componente)
            currentScale = new Vector3(
                currentScale.x * scaleMultiplier.x,
                currentScale.y * scaleMultiplier.y,
                currentScale.z * scaleMultiplier.z
            );
        }
        
        // Ativa todos os Rigidbodies após terminar o empilhamento
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
    /// Ativa física após um pequeno delay para estabilização
    /// </summary>
    private System.Collections.IEnumerator EnablePhysicsWithDelay()
    {
        yield return new WaitForSeconds(physicsDelayTime);
        EnableAllPhysics();
    }
    
    private float GetObjectHeight(GameObject obj)
    {
        // Tenta obter todos os Renderers (incluindo filhos)
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            // Calcula os bounds combinados de todos os renderers
            Bounds combinedBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                combinedBounds.Encapsulate(renderers[i].bounds);
            }
            return combinedBounds.size.y;
        }
        
        // Se não tiver Renderer, tenta obter todos os Colliders
        Collider[] colliders = obj.GetComponentsInChildren<Collider>();
        if (colliders.Length > 0)
        {
            Bounds combinedBounds = colliders[0].bounds;
            for (int i = 1; i < colliders.Length; i++)
            {
                combinedBounds.Encapsulate(colliders[i].bounds);
            }
            return combinedBounds.size.y;
        }
        
        // Valor padrão se não encontrar nenhum componente
        Debug.LogWarning($"Não foi possível determinar a altura de {obj.name}. Usando valor padrão de 1.");
        return 1f;
    }
    
    /// <summary>
    /// Calcula a distância do pivot do objeto até sua base (ponto mais baixo)
    /// </summary>
    private float GetBottomOffset(GameObject obj)
    {
        // Tenta obter todos os Renderers (incluindo filhos)
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            // Calcula os bounds combinados de todos os renderers
            Bounds combinedBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                combinedBounds.Encapsulate(renderers[i].bounds);
            }
            // Retorna a distância do pivot até o ponto mais baixo
            return obj.transform.position.y - combinedBounds.min.y;
        }
        
        // Se não tiver Renderer, tenta obter todos os Colliders
        Collider[] colliders = obj.GetComponentsInChildren<Collider>();
        if (colliders.Length > 0)
        {
            Bounds combinedBounds = colliders[0].bounds;
            for (int i = 1; i < colliders.Length; i++)
            {
                combinedBounds.Encapsulate(colliders[i].bounds);
            }
            return obj.transform.position.y - combinedBounds.min.y;
        }
        
        return 0f;
    }
    
    /// <summary>
    /// Limpa todos os objetos empilhados
    /// </summary>
    public void ClearStack()
    {
        foreach (GameObject obj in stackedObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        stackedObjects.Clear();
    }
    
    /// <summary>
    /// Adiciona um novo nível ao topo da pilha existente
    /// </summary>
    public void AddLevel()
    {
        if (prefabList == null || prefabList.Count == 0)
        {
            Debug.LogError("Lista de prefabs está vazia!");
            return;
        }
        
        Vector3 topPosition = startPoint != null ? startPoint.position : transform.position;
        Vector3 currentScale = Vector3.one;
        
        // Calcula a escala baseada no número de níveis existentes
        int currentLevel = stackedObjects.Count;
        for (int i = 0; i < currentLevel; i++)
        {
            currentScale = new Vector3(
                currentScale.x * scaleMultiplier.x,
                currentScale.y * scaleMultiplier.y,
                currentScale.z * scaleMultiplier.z
            );
        }
        
        // Encontra o objeto mais alto
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
        
        // Escolhe um prefab aleatório
        GameObject randomPrefab = prefabList[Random.Range(0, prefabList.Count)];
        
        // Instancia e configura
        GameObject newObj = Instantiate(randomPrefab, topPosition, Quaternion.identity, transform);
        newObj.name = $"{randomPrefab.name}_Level_{currentLevel + 1}";
        newObj.transform.localScale = currentScale;
        
        // Desativa Rigidbodies temporariamente
        SetRigidbodiesActive(newObj, false);
        
        // Ajusta a posição para que a base do objeto fique em topPosition
        float bottomOffset = GetBottomOffset(newObj);
        newObj.transform.position = new Vector3(topPosition.x, topPosition.y - bottomOffset, topPosition.z);
        
        stackedObjects.Add(newObj);
        
        // Reativa física após um frame
        StartCoroutine(EnablePhysicsDelayed(newObj));
        
        Debug.Log($"Adicionado nível {currentLevel + 1} com escala {currentScale}");
    }
    
    /// <summary>
    /// Ativa ou desativa todos os Rigidbodies de um objeto
    /// </summary>
    private void SetRigidbodiesActive(GameObject obj, bool active)
    {
        Rigidbody[] rigidbodies = obj.GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody rb in rigidbodies)
        {
            rb.isKinematic = !active;
        }
    }
    
    /// <summary>
    /// Ativa a física de todos os objetos empilhados
    /// </summary>
    public void EnableAllPhysics()
    {
        foreach (GameObject obj in stackedObjects)
        {
            if (obj != null)
            {
                Rigidbody[] rigidbodies = obj.GetComponentsInChildren<Rigidbody>();
                foreach (Rigidbody rb in rigidbodies)
                {
                    rb.isKinematic = false;
                    // Reseta velocidades para evitar movimentos iniciais
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    // Coloca o Rigidbody para "dormir" inicialmente
                    rb.Sleep();
                }
            }
        }
    }
    
    /// <summary>
    /// Desativa a física de todos os objetos empilhados
    /// </summary>
    public void DisableAllPhysics()
    {
        foreach (GameObject obj in stackedObjects)
        {
            if (obj != null)
            {
                SetRigidbodiesActive(obj, false);
            }
        }
    }
    
    /// <summary>
    /// Ativa física com um pequeno delay
    /// </summary>
    private System.Collections.IEnumerator EnablePhysicsDelayed(GameObject obj)
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
    }
    
    /// <summary>
    /// Retorna o número atual de níveis empilhados
    /// </summary>
    public int GetCurrentLevelCount()
    {
        return stackedObjects.Count;
    }
}