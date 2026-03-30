using System.Collections.Generic;
using UnityEngine;

public class PortalSpawner : MonoBehaviour
{
    [Header("Configurações de Portais")]
    [SerializeField]
    private GameObject portalPrefab;

    [SerializeField]
    private int numberOfPortalPairs = 5;

    public string[] portals;

    [Header("Posicionamento")]
    [SerializeField]
    private Transform startPoint;

    [SerializeField]
    private Transform endPoint;

    [SerializeField]
    private float distanceBetweenPairPortals = 2f;

    [SerializeField]
    private float verticalOffset = 0f; // Ajuste vertical adicional se necessário

    [SerializeField]
    private float horizontalOffset = 3f; // Deslocamento horizontal aleatório dos pares

    [SerializeField]
    private float scaleFactor = 1f;
    private int numberOfExpressions = 0;

    private float portalHeight = 0f;


    public void Init(PortalPairExpression[] portals)
    {
        SpawnPortals(portals);
        PortalExpressionsController.Instance.GetEnemyEquationOnIPortal(0);
        PortalExpressionsController.Instance.DrawEnemyMeshFuction(transform.parent);
        Debug.Log("PortalExpressionsController.Instance.GetEnemyEquationOnIPortal(0);");

        MathExpression.Instance.numberOfTerms = numberOfExpressions;
    }

    [ContextMenu("Gerar Portais")]
    public void SpawnPortals(PortalPairExpression[] portals)
    {
        ClearExistingPortals();

        if (!ValidateSetup())
            return;

        CalculatePortalHeight();

        // Usa posição LOCAL em relação ao próprio PortalSpawner
        // para não ser afetado por escala ou posição global do segmento
        Vector3 localStart = transform.InverseTransformPoint(startPoint.position);
        Vector3 localEnd = transform.InverseTransformPoint(endPoint.position);

        float totalDistance = Vector3.Distance(localStart, localEnd);
        Vector3 localDir = (localEnd - localStart).normalized;

        float yPosition = localStart.y + (portalHeight / 2f) + verticalOffset;

        float spacing = totalDistance / (numberOfPortalPairs - 1);

        for (int i = 0; i < portals.Length; i++)
        {
            Vector3 localPos = localStart + localDir * (spacing * i);
            localPos.y = yPosition;

            // Offset horizontal no eixo perpendicular LOCAL
            float xOffset = 0f;
            if (i > 0 && horizontalOffset > 0f)
                xOffset = Random.Range(-horizontalOffset, horizontalOffset);

            // Perpendicular ao dir no plano XZ local
            Vector3 localPerp = new Vector3(-localDir.z, 0f, localDir.x);
            localPos += localPerp * xOffset;

            SpawnPortalPair(transform.TransformPoint(localPos), i, portals[i]);
        }
    }

    private bool ValidateSetup()
    {
        if (portalPrefab == null)
        {
            Debug.LogError("Portal Prefab não está atribuído!");
            return false;
        }

        if (startPoint == null || endPoint == null)
        {
            Debug.LogError("Start Point ou End Point não estão atribuídos!");
            return false;
        }

        return true;
    }

    private void CalculatePortalHeight()
    {
        // Instanciar temporariamente um portal para obter sua altura
        GameObject tempPortal = Instantiate(portalPrefab);
        Portal portalScript = tempPortal.GetComponent<Portal>();

        if (portalScript != null)
        {
            portalHeight = portalScript.GetPortalHeight();
        }

        // Destruir o portal temporário
        if (Application.isPlaying)
        {
            Destroy(tempPortal);
        }
        else
        {
            DestroyImmediate(tempPortal);
        }
    }

    // Substitua o método SpawnPortalPair inteiro por este:

    private void SpawnPortalPair(Vector3 centerPosition, int pairIndex, PortalPairExpression portalPair)
    {
        bool leftIsBlue = Random.Range(0, 2) == 0;
        //bool isExpressionPortal = Random.Range(0f, 1f) < 0.7f;

        if (portalPair.isExpressionPortal)
            numberOfExpressions++;

        // Direção world start → end (jogador avança nesse sentido)
        Vector3 worldDir = (endPoint.position - startPoint.position).normalized;
        worldDir.y = 0f;
        if (worldDir == Vector3.zero)
            worldDir = transform.forward;

        // Portal deve olhar CONTRA o jogador, ou seja, -worldDir
        Quaternion portalRotation = Quaternion.LookRotation(-worldDir, Vector3.up);

        // Eixo direito baseado na direção real de avanço (não invertida)
        Vector3 rightDir = Quaternion.LookRotation(worldDir, Vector3.up) * Vector3.right;

        Vector3 leftPosition = centerPosition - rightDir * (distanceBetweenPairPortals / 2f);
        Vector3 rightPosition = centerPosition + rightDir * (distanceBetweenPairPortals / 2f);

        // Parent neutro — só agrupa, sem rotação própria
        GameObject portalParent = new GameObject($"Portal_Pair{pairIndex}");
        portalParent.transform.position = centerPosition;
        portalParent.transform.rotation = portalRotation;
        portalParent.transform.parent = this.transform;
        portalParent.AddComponent<UniquePairPortalCollider>();

        // Portais instanciados com a rotação final correta (contra o jogador)
        GameObject leftPortal = Instantiate(
            portalPrefab,
            leftPosition,
            portalRotation,
            portalParent.transform
        );
        GameObject rightPortal = Instantiate(
            portalPrefab,
            rightPosition,
            portalRotation,
            portalParent.transform
        );

        leftPortal.transform.localScale = Vector3.one * scaleFactor;
        rightPortal.transform.localScale = Vector3.one * scaleFactor;

        leftPortal.name = $"Portal_Pair{pairIndex}_Left_{(leftIsBlue ? "Blue" : "Red")}";
        rightPortal.name = $"Portal_Pair{pairIndex}_Right_{(leftIsBlue ? "Red" : "Blue")}";

        Portal leftPortalScript = leftPortal.GetComponent<Portal>();
        Portal rightPortalScript = rightPortal.GetComponent<Portal>();

        PortalExpressionsController.Instance.InitPairPortals(
            leftPortalScript,
            rightPortalScript,
            !leftIsBlue,
            portalPair.isExpressionPortal,
            portalPair
        );
    }

    private void ClearExistingPortals()
    {
        // Remover todos os portais filhos
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            if (Application.isPlaying)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
            else
            {
                DestroyImmediate(transform.GetChild(i).gameObject);
            }
        }
    }
}
