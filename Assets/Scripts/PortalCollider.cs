using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PortalCollider : MonoBehaviour
{
    [Header("Componentes")]
    [SerializeField]
    private GameObject textObject;
    private static int countPortal;

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
            
            PortalExpressionsController.Instance.GetEnemyEquationOnIPortal(++countPortal);
            Debug.Log("Colidiu com: " + other.name);
            if (GetComponent<Portal>().isTextPortal)
            {
                GetComponentInParent<UniquePairPortalCollider>().SetPortalState();
                string expresion = textObject.GetComponent<TextMeshProUGUI>().text;
                Debug.Log("Equação coletada: " + expresion);
                EquationController.instance.AppendEquation(expresion);
                
                
            } 
            else
            {
                SpawnTroopsController.Instance.playerPrefabCountPairs.Add(new PrefabCountPair
                {
                    prefab = GetComponent<Portal>().troopPrefab,
                    count = GetComponent<Portal>().troopCountText.text != "" ? int.Parse(GetComponent<Portal>().troopCountText.text) : 0
                });
            }
            Destroy(gameObject.transform.parent.gameObject, 0.2f);
        }
    }
}
