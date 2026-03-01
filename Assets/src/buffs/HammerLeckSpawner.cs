using UnityEngine;

/// <summary>
/// Gera N martelos em leque ao redor do eixo Y do objeto pai.
/// A aura e serializada diretamente no Inspector - sem busca por nome.
/// Apos a geracao adiciona HammerLeck ao root e injeta todas as refs.
/// </summary>
public class HammerLeckSpawner : MonoBehaviour, IBuffController
{
    [Header("Prefab")]
    public GameObject hammerPrefab;

    [Header("Aura")]
    [Tooltip("Prefab da aura - arrastar direto no Inspector, sem depender de nome")]
    public GameObject auraPrefab;
    public float auraOffsetDown = 0.5f;

    [Header("Quantidade e Layout")]
    [Min(1)]
    public int hammerCount = 3;
    public float fanAngle = 30f;
    public float hammerLength = 2f;

    [Header("HammerLeck Config")]
    public float spinSpeed = 360f;
    public float damage = 60f;
    public float impactRadius = 3f;
    public LayerMask impactLayers = ~0;
    public float auraScaleMultiplier = 3f;
    public float auraScaleDuration = 0.4f;

    public void Spawn(Vector3 position, Quaternion rotation)
    {
        GameObject root = new GameObject("HammerLeckRoot");
        root.transform.position = position;
        root.transform.rotation = rotation;

        // Posicao local da cabeca do martelo central
        Vector3 headLocalPos = Vector3.up * hammerLength;

        // --- Gera os martelos em leque ---
        float halfFan = (hammerCount > 1) ? fanAngle * 0.5f : 0f;
        float step = (hammerCount > 1) ? fanAngle / (hammerCount - 1) : 0f;

        for (int i = 0; i < hammerCount; i++)
        {
            float angle = (hammerCount > 1) ? -halfFan + step * i : 0f;

            // Todos os cabos partem da mesma origem (Vector3.zero).
            // Apenas a rotacao muda, abrindo as cabecas em leque.
            Quaternion rot = Quaternion.AngleAxis(angle, Vector3.forward);

            GameObject hammer = Instantiate(hammerPrefab, root.transform);
            hammer.transform.localPosition = Vector3.zero;
            hammer.transform.localRotation = rot;
            hammer.name = "martelo_" + i;
        }

        // --- Instancia a aura e posiciona na cabeca central com offset ---
        Transform auraInstance = null;
        if (auraPrefab != null)
        {
            GameObject auraGO = Instantiate(auraPrefab, root.transform);
            auraGO.transform.localPosition = headLocalPos + Vector3.down * auraOffsetDown;
            auraGO.transform.localRotation = Quaternion.identity;
            auraInstance = auraGO.transform;
        }
        else
        {
            Debug.LogWarning("[HammerLeckSpawner] auraPrefab nao atribuido no Inspector.");
        }

        // --- Adiciona e configura HammerLeck ---
        HammerLeck hl = root.AddComponent<HammerLeck>();
        hl.spinSpeed = spinSpeed;
        hl.damage = damage;
        hl.impactRadius = impactRadius;
        hl.impactLayers = impactLayers;
        hl.auraScaleMultiplier = auraScaleMultiplier;
        hl.auraScaleDuration = auraScaleDuration;
        hl.impactOffset = headLocalPos;

        // Injeta a referencia da aura diretamente - sem busca por nome
        hl.SetAura(auraInstance);
        int layer = gameObject.layer;
        SetLayerRecursive(root.transform, layer);
        Debug.Log("[HammerLeckSpawner] Gerado com " + hammerCount + " martelos.");
    }

    private void SetLayerRecursive(Transform t, int layer)
    {
        t.gameObject.layer = layer;
        foreach (Transform child in t)
            SetLayerRecursive(child, layer);
    }

    void Start()
    {
        var player = GameController.Instance.playerTarget;
        Spawn(player.position + player.up * 1f + player.forward * 1f, player.rotation);
    }

    private void OnDrawGizmosSelected()
    {
        if (hammerCount <= 0)
            return;

        float halfFan = (hammerCount > 1) ? fanAngle * 0.5f : 0f;
        float step = (hammerCount > 1) ? fanAngle / (hammerCount - 1) : 0f;
        Vector3 headRef = transform.position + transform.up * hammerLength;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < hammerCount; i++)
        {
            float angle = (hammerCount > 1) ? -halfFan + step * i : 0f;
            Quaternion rot = transform.rotation * Quaternion.AngleAxis(angle, Vector3.forward);
            Vector3 dir = rot * Vector3.up;
            Vector3 base_ = headRef - dir * hammerLength;
            Gizmos.DrawLine(base_, headRef);
            Gizmos.DrawWireSphere(base_, 0.05f);
        }

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawSphere(headRef, impactRadius);
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.9f);
        Gizmos.DrawWireSphere(headRef, impactRadius);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(headRef + Vector3.down * auraOffsetDown, 0.15f);
    }

    public void ActivateBuff()
    {
        throw new System.NotImplementedException();
    }

}
