using UnityEngine;

/// <summary>
/// Attach to each child part of the structure (Rigidbody + Collider + Tag "Structure").
/// Tracks displacement and tilt from its original transform.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class StructurePart : MonoBehaviour
{
    [HideInInspector] public StructureController parentStructure;

    private Vector3    initialLocalPosition;
    private Quaternion initialLocalRotation;

    void Start()
    {
        initialLocalPosition = transform.localPosition;
        initialLocalRotation = transform.localRotation;
    }

    /// <summary>Distance moved from original local position.</summary>
    public float GetDisplacement() =>
        Vector3.Distance(transform.localPosition, initialLocalPosition);

    /// <summary>Degrees rotated from original local rotation.</summary>
    public float GetTiltAngle() =>
        Quaternion.Angle(transform.localRotation, initialLocalRotation);
}