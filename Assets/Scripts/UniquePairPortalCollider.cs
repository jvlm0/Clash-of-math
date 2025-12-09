using UnityEngine;

// Script para o prefab do portal
public class UniquePairPortalCollider : MonoBehaviour
{
    private bool colidiu = false;


    public void SetPortalState()
    {
        colidiu = true;
    }

    public bool HaveCollided()
    {
        return colidiu;
    }
}