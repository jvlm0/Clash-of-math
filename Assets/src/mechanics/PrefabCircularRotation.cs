using UnityEngine;

/// <summary>
/// Rotação circular com instanciação automática de prefabs
/// </summary>
public class PrefabCircularRotation : MonoBehaviour
{
    [Header("Configurações do Prefab")]
    [Tooltip("Prefab que será instanciado")]
    public GameObject prefab;
    
    [Tooltip("Quantidade de objetos para instanciar")]
    [Range(1, 50)]
    public int quantidade = 6;
    
    [Header("Configurações de Rotação")]
    [Tooltip("Velocidade de rotação em graus por segundo")]
    public float velocidade = 30f;
    
    [Tooltip("Raio do círculo")]
    public float raio = 5f;
    
    [Header("Opções Visuais")]
    [Tooltip("Os objetos olham para o centro?")]
    public bool olharParaCentro = true;
    
    [Tooltip("Offset de rotação adicional")]
    public float offsetRotacao = 0f;
    
    [Tooltip("Escala dos objetos instanciados")]
    public float escala = 1f;

    [Header("Debug")]
    [Tooltip("Mostrar gizmos no editor")]
    public bool mostrarGizmos = true;

    private Transform centro;
    private GameObject[] objetosInstanciados;
    private float angulo = 0f;
    private bool objetosCriados = false;

    void Start()
    {
        CriarObjetos();
    }

    /// <summary>
    /// Cria e posiciona os objetos em círculo
    /// </summary>
    void CriarObjetos()
    {
        if (prefab == null)
        {
            Debug.LogError("Prefab não foi atribuído no Inspector!");
            return;
        }

        // Cria o ponto central
        if (centro == null)
        {
            GameObject centroObj = new GameObject("Centro_Rotacao");
            centroObj.transform.position = transform.position;
            centroObj.transform.parent = transform;
            centro = centroObj.transform;
        }

        // Limpa objetos antigos se existirem
        LimparObjetos();

        // Cria array para armazenar os objetos
        objetosInstanciados = new GameObject[quantidade];

        // Calcula o ângulo entre cada objeto
        float anguloInicial = 360f / quantidade;

        // Instancia e posiciona cada objeto
        for (int i = 0; i < quantidade; i++)
        {
            // Instancia o prefab
            GameObject obj = Instantiate(prefab, transform);
            obj.name = prefab.name + "_" + i;
            
            // Define a escala
            obj.transform.localScale = Vector3.one * escala;

            // Calcula a posição inicial
            float ang = i * anguloInicial * Mathf.Deg2Rad;
            Vector3 posicao = centro.position + new Vector3(
                Mathf.Cos(ang) * raio,
                0f,
                Mathf.Sin(ang) * raio
            );
            
            obj.transform.position = posicao;

            // Define a rotação inicial
            if (olharParaCentro)
            {
                obj.transform.LookAt(centro.position);
                obj.transform.Rotate(0f, offsetRotacao, 0f, Space.Self);
            }
            else
            {
                obj.transform.rotation = Quaternion.Euler(0f, i * anguloInicial + offsetRotacao, 0f);
            }

            // Armazena no array
            objetosInstanciados[i] = obj;
        }

        objetosCriados = true;
        Debug.Log($"{quantidade} objetos criados e posicionados em círculo!");
    }

    void Update()
    {
        if (!objetosCriados || objetosInstanciados == null) return;

        // Incrementa o ângulo
        angulo += velocidade * Time.deltaTime;
        
        // Mantém entre 0 e 360
        if (angulo >= 360f)
            angulo -= 360f;

        // Rotaciona todos os objetos
        RotacionarObjetos();
    }

    /// <summary>
    /// Rotaciona os objetos ao redor do centro
    /// </summary>
    void RotacionarObjetos()
    {
        float passoAngular = 360f / quantidade;

        for (int i = 0; i < objetosInstanciados.Length; i++)
        {
            if (objetosInstanciados[i] == null) continue;

            // Calcula ângulo atual deste objeto
            float anguloAtual = (angulo + i * passoAngular) * Mathf.Deg2Rad;

            // Calcula e aplica nova posição
            Vector3 novaPosicao = centro.position + new Vector3(
                Mathf.Cos(anguloAtual) * raio,
                0f,
                Mathf.Sin(anguloAtual) * raio
            );
            
            objetosInstanciados[i].transform.position = novaPosicao;

            // Aplica rotação
            if (olharParaCentro)
            {
                objetosInstanciados[i].transform.LookAt(centro.position);
                objetosInstanciados[i].transform.Rotate(0f, offsetRotacao, 0f, Space.Self);
            }
            else
            {
                float anguloGraus = (angulo + i * passoAngular);
                objetosInstanciados[i].transform.rotation = Quaternion.Euler(0f, anguloGraus + offsetRotacao, 0f);
            }
        }
    }

    /// <summary>
    /// Limpa todos os objetos instanciados
    /// </summary>
    void LimparObjetos()
    {
        if (objetosInstanciados != null)
        {
            foreach (GameObject obj in objetosInstanciados)
            {
                if (obj != null)
                {
                    if (Application.isPlaying)
                        Destroy(obj);
                    else
                        DestroyImmediate(obj);
                }
            }
        }
        objetosCriados = false;
    }

    /// <summary>
    /// Recria os objetos (útil para quando mudar parâmetros no Inspector)
    /// </summary>
    public void RecriarObjetos()
    {
        CriarObjetos();
    }

    /// <summary>
    /// Para a rotação
    /// </summary>
    public void PararRotacao()
    {
        enabled = false;
    }

    /// <summary>
    /// Continua a rotação
    /// </summary>
    public void ContinuarRotacao()
    {
        enabled = true;
    }

    /// <summary>
    /// Inverte a direção da rotação
    /// </summary>
    public void InverterDirecao()
    {
        velocidade *= -1f;
    }

    /// <summary>
    /// Muda a velocidade
    /// </summary>
    public void MudarVelocidade(float novaVelocidade)
    {
        velocidade = novaVelocidade;
    }

    /// <summary>
    /// Muda o raio
    /// </summary>
    public void MudarRaio(float novoRaio)
    {
        raio = novoRaio;
    }

    void OnDestroy()
    {
        LimparObjetos();
    }

    // Desenha gizmos no editor
    void OnDrawGizmos()
    {
        if (!mostrarGizmos) return;

        Vector3 centroPosicao = Application.isPlaying && centro != null ? 
            centro.position : transform.position;

        // Desenha o círculo
        Gizmos.color = Color.cyan;
        int segmentos = 50;
        float anguloStep = 360f / segmentos;

        Vector3 pontoAnterior = centroPosicao + new Vector3(raio, 0, 0);

        for (int i = 1; i <= segmentos; i++)
        {
            float ang = i * anguloStep * Mathf.Deg2Rad;
            Vector3 novoPonto = centroPosicao + new Vector3(
                Mathf.Cos(ang) * raio,
                0f,
                Mathf.Sin(ang) * raio
            );

            Gizmos.DrawLine(pontoAnterior, novoPonto);
            pontoAnterior = novoPonto;
        }

        // Desenha o centro
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(centroPosicao, 0.3f);

        // Desenha as posições dos objetos
        Gizmos.color = Color.yellow;
        float anguloObj = 360f / quantidade;
        
        for (int i = 0; i < quantidade; i++)
        {
            float ang = i * anguloObj * Mathf.Deg2Rad;
            Vector3 pos = centroPosicao + new Vector3(
                Mathf.Cos(ang) * raio,
                0f,
                Mathf.Sin(ang) * raio
            );
            Gizmos.DrawWireSphere(pos, 0.2f);
        }
    }

    // Validação no Inspector
    void OnValidate()
    {
        // Garante valores mínimos
        if (quantidade < 1) quantidade = 1;
        if (raio < 0.5f) raio = 0.5f;
        if (escala < 0.1f) escala = 0.1f;
    }
}