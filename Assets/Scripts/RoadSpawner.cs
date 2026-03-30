using UnityEngine;

/// <summary>
/// Spawna segmentos de estrada em runtime usando um prefab.
/// Cada novo segmento é rotacionado +90° em Y e posicionado
/// usando offsets em X e Z que invertem conforme a rotação.
///
/// Padrão de spawn (top-down):
///   #0  rot  0° → posição inicial,   avança em +Z na próxima chamada
///   #1  rot 90° → desloca em +Z,     avança em +X na próxima chamada
///   #2  rot180° → desloca em +X,     avança em -Z na próxima chamada
///   #3  rot270° → desloca em -Z,     avança em -X na próxima chamada
///
/// SETUP:
///   1. Adicione RoadSpawner a um GameObject vazio.
///   2. Atribua o prefab (plano branco + xadrez como filhos do mesmo parent).
///   3. Ajuste spawnDistance (distância entre centros) e heightIncrement.
///   4. Play → Espaço para testar.
/// </summary>
public class RoadSpawner : MonoBehaviour
{
    [Header("Prefab")]
    [Tooltip("Prefab com plano branco e xadrez como filhos do mesmo parent.")]
    public GameObject segmentPrefab;

    [Header("Spawn Settings")]
    [Tooltip("Distância entre o centro de um segmento e o próximo.")]
    public float mainDistance    = 462f;

    [Tooltip("Deslocamento no eixo perpendicular / offset lateral (aprox. 77 nas medições)")]
    public float sideOffset      = 77f;

    [Tooltip("Altura adicional a cada novo segmento.")]
    public float heightIncrement = 1f;

    public static Transform actualJumpPos; 

    // ── Estado ────────────────────────────────────────────────────
    private GameObject _lastSegment;
    private int        _stepIndex  = 0;
    private int        _spawnCount       = 0;

    // ─────────────────────────────────────────────────────────────
    private void Start()
    {
        if (segmentPrefab == null)
        {
            Debug.LogError("[RoadSpawner] segmentPrefab não atribuído!");
            return;
        }

        _lastSegment = Instantiate(segmentPrefab, Vector3.zero, Quaternion.identity);
        _lastSegment.name = "Segment_0";
        _spawnCount = 1;

        var startPos = _lastSegment.transform.Find("playerStart");
        GameController.Instance.player.transform.position = startPos.position;
        GameController.Instance.player.transform.rotation = startPos.rotation;
        
        var functionPos = _lastSegment.transform.Find("functionPos");
        PortalExpressionsController.Instance.transformFunctionPos = functionPos;

        SpawnController.Instance.transformFunctionPos = functionPos;

        actualJumpPos = functionPos;

        
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            SpawnNext();
    }

    // ── API pública ───────────────────────────────────────────────
    /// <summary>
    /// Gera o próximo segmento. Chame aqui ao atingir um objetivo no jogo.
    /// </summary>
    public void SpawnNext()
    {
        if (_lastSegment == null) return;

        Vector3 delta = DeltaForStep(_stepIndex);
        _stepIndex    = (_stepIndex + 1) % 4;

        float rotY     = _stepIndex * 90f;
        Vector3 newPos = _lastSegment.transform.position
                       + delta
                       + Vector3.up * heightIncrement;

        Quaternion newRot = Quaternion.Euler(0f, rotY, 0f);

        _lastSegment      = Instantiate(segmentPrefab, newPos, newRot);
        _lastSegment.name = $"Segment_{_spawnCount}";

        var functionPos = _lastSegment.transform.Find("functionPos");
        PortalExpressionsController.Instance.transformFunctionPos = functionPos;

        SpawnController.Instance.transformFunctionPos = functionPos;

        Debug.Log($"[RoadSpawner] Segment_{_spawnCount} | pos={newPos} | rotY={rotY}°");
        _spawnCount++;
    }

    // ─────────────────────────────────────────────────────────────
    /// <summary>
    /// Converte rotação Y em vetor de deslocamento no plano XZ.
    /// Cada 90° gira o eixo de avanço:
    ///   0°  → +Z (forward)
    ///   90° → +X (right)
    ///  180° → -Z (back)
    ///  270° → -X (left)
    /// </summary>
private Vector3 DeltaForStep(int step)
    {
        switch (step % 4)
        {
            case 0: return new Vector3( mainDistance,  0f,  sideOffset);   // 0→1
            case 1: return new Vector3( sideOffset,    0f, -mainDistance);  // 1→2
            case 2: return new Vector3(-mainDistance,  0f, -sideOffset);    // 2→3
            case 3: return new Vector3(-sideOffset,    0f,  mainDistance);  // 3→0
            default: return Vector3.zero;
        }
    }
}