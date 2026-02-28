using UnityEngine;
using System.Collections;

public class HammerLeck : MonoBehaviour
{
    [Header("Movimento")]
    public float spinSpeed = 360f;

    [Header("Dano")]
    public float damage = 60f;

    [Header("Zona de Impacto")]
    public float impactRadius = 3f;
    public Vector3 impactOffset = Vector3.zero; // centro da esfera relativo ao objeto
    public LayerMask impactLayers = ~0;         // layers que recebem dano de impacto

    [Header("Aura")]
    public float auraScaleMultiplier = 3f;
    public float auraScaleDuration = 0.4f;
    public string auraChildName = "Lightning aura";

    private bool stopped = false;
    private Transform auraTransform;
    private Vector3 auraOriginalScale;

    void Start()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();

        rb.isKinematic = true;
        rb.useGravity = false;

        foreach (MeshRenderer mr in GetComponentsInChildren<MeshRenderer>())
        {
            if (mr.gameObject.name == "Martelo_Cabeca")
            {
                BoxCollider col = mr.GetComponent<BoxCollider>();
                if (col == null)
                    col = mr.gameObject.AddComponent<BoxCollider>();

                col.isTrigger = true;

                HammerLeckCollision hlc = mr.GetComponent<HammerLeckCollision>();
                if (hlc == null)
                    hlc = mr.gameObject.AddComponent<HammerLeckCollision>();

                hlc.owner = this;
                Debug.Log("[HammerLeck] Collider trigger configurado em: " + mr.gameObject.name + " (pai: " + mr.transform.parent?.name + ")");
            }
        }

        Transform auraT = transform.Find(auraChildName);
        if (auraT != null)
        {
            auraTransform = auraT;
            auraOriginalScale = auraT.localScale;
        }
    }

    void Update()
    {
        if (!stopped)
            transform.Rotate(Vector3.right * spinSpeed * Time.deltaTime);
    }

    public void OnHitGround()
    {
        if (stopped) return;
        stopped = true;

        if (auraTransform != null)
            StartCoroutine(PulseAura());

        // Centro da esfera = posicao do objeto + offset configuravel no Inspector
        Vector3 sphereCenter = transform.position + transform.TransformDirection(impactOffset);

        // OverlapSphere filtrando apenas as layers definidas no Inspector
        Collider[] hits = Physics.OverlapSphere(sphereCenter, impactRadius, impactLayers);
        foreach (var col in hits)
        {
            col.GetComponent<IAnimController>()?.GetDamage(damage * 0.5f);
            Debug.Log("[HammerLeck] Impacto de explosao em: " + col.gameObject.name);
        }

        Destroy(gameObject, auraScaleDuration + 0.6f);
    }

    private IEnumerator PulseAura()
    {
        float elapsed = 0f;
        Vector3 targetScale = auraOriginalScale * auraScaleMultiplier;

        while (elapsed < auraScaleDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / auraScaleDuration;
            auraTransform.localScale = Vector3.Lerp(auraOriginalScale, targetScale, t);
            yield return null;
        }

        auraTransform.localScale = targetScale;
    }

    // Gizmo mostra a esfera no editor na posicao exata com offset
    private void OnDrawGizmosSelected()
    {
        Vector3 sphereCenter = transform.position + transform.TransformDirection(impactOffset);
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.35f);
        Gizmos.DrawSphere(sphereCenter, impactRadius);
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.9f);
        Gizmos.DrawWireSphere(sphereCenter, impactRadius);
    }
}


public class HammerLeckCollision : MonoBehaviour
{
    [HideInInspector] public HammerLeck owner;

    private void OnTriggerEnter(Collider other)
    {
        if (owner == null)
            owner = GetComponentInParent<HammerLeck>();

        Debug.Log("HammerLeck Colidiu com: " + other.gameObject.name);

        if (other.CompareTag("npc"))
        {
            other.GetComponent<IAnimController>()?.GetDamage(owner != null ? owner.damage : 60f);
        }

        if (other.CompareTag("Ground"))
        {
            owner?.OnHitGround();
        }
    }
}
