using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attach to the ROOT (parent) GameObject — the one WITHOUT a Rigidbody.
/// Monitors all StructurePart children every frame.
/// No hit notification needed: ANY cause of movement triggers fall detection.
/// </summary>
public class StructureController : MonoBehaviour
{
    [Header("Fall Detection — Tilt")]
    [Tooltip("Degrees a part must rotate from its original orientation to count as fallen")]
    public float tiltAngleThreshold = 40f;

    [Header("Fall Detection — Displacement")]
    [Tooltip("Unity units a part must move from its original position to count as fallen")]
    public float displacementThreshold = 1.5f;

    [Tooltip("How many parts must fail before the whole structure is considered fallen")]
    public int partsNeededToFall = 1;

    [Header("Destruction")]
    [Tooltip("Seconds to wait after fall is detected before destroying parts")]
    public float destroyDelay = 1.2f;

    [Tooltip("Optional effect prefab instantiated at each part on destruction")]
    public GameObject destroyEffectPrefab;

    // ── Events ────────────────────────────────────────────────────────────────
    public System.Action OnStructureFallen;
    public System.Action OnStructureDestroyed;

    // ── Internal ──────────────────────────────────────────────────────────────
    private List<StructurePart> parts = new List<StructurePart>();
    private bool hasFallen    = false;
    private bool isDestroying = false;

    // ─────────────────────────────────────────────────────────────────────────
    void Awake()
    {
        foreach (StructurePart part in GetComponentsInChildren<StructurePart>())
        {
            part.parentStructure = this;
            parts.Add(part);
        }

        if (parts.Count == 0)
            Debug.LogWarning($"[StructureController] '{name}' found no StructurePart children!");
    }

    void Update()
    {
        if (hasFallen || isDestroying) return;

        int failedParts = 0;

        foreach (StructurePart part in parts)
        {
            if (part == null) continue;

            if (part.GetTiltAngle()    >= tiltAngleThreshold ||
                part.GetDisplacement() >= displacementThreshold)
            {
                failedParts++;
            }
        }

        if (failedParts >= partsNeededToFall)
            TriggerFall();
    }

    // ─────────────────────────────────────────────────────────────────────────
    private void TriggerFall()
    {
        if (hasFallen) return;
        hasFallen = true;

        Debug.Log($"[StructureController] '{name}' has fallen!");
        OnStructureFallen?.Invoke();
        StartCoroutine(DestroyParts());
    }

    private IEnumerator DestroyParts()
    {
        isDestroying = true;
        yield return new WaitForSeconds(destroyDelay);

        foreach (StructurePart part in parts)
        {
            if (part == null) continue;

            if (destroyEffectPrefab != null)
                Instantiate(destroyEffectPrefab, part.transform.position, Quaternion.identity);

            Destroy(part.gameObject);
        }

        OnStructureDestroyed?.Invoke();
        Destroy(gameObject, 0.1f);
    }

    // ── Gizmos ────────────────────────────────────────────────────────────────
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.3f);
        foreach (StructurePart part in parts)
            if (part != null)
                Gizmos.DrawWireSphere(part.transform.position, displacementThreshold);
    }
}