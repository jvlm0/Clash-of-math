using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PortalCollider : MonoBehaviour
{
    [Header("Componentes")]
    [SerializeField]
    private GameObject textObject;

    // Start is called before the first frame update
    void Start() { }

    // Update is called once per frame
    void Update() { }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (GetComponentInParent<UniquePairPortalCollider>().HaveCollided())
            {
                return;
            }
            Debug.Log("Colidiu com: " + other.name);

            GetComponentInParent<UniquePairPortalCollider>().SetPortalState();
            string expresion = textObject.GetComponent<TextMeshProUGUI>().text;
            Debug.Log("Equação coletada: " + expresion);
            EquationController.instance.AppendEquation(expresion);
            Destroy(gameObject.transform.parent.gameObject, 0.2f);
        }
    }
}
