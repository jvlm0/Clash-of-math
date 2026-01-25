using UnityEngine;

public class ObjectLauncher : MonoBehaviour
{
    [Header("Configurações de Lançamento")]
    [SerializeField] private Transform targetPosition; // Posição de destino
    [SerializeField] private float launchSpeed = 10f; // Velocidade do lançamento
    [SerializeField] private float arcHeight = 3f; // Altura do arco da trajetória
    
    private bool isLaunching = false;
    private float launchTimer = 0f;
    private Vector3 startPosition;
    private float totalDistance;
    private float launchDuration;
    
    void Update()
    {
        // Detecta o pressionamento da tecla espaço
        if (Input.GetKeyDown(KeyCode.Space) && !isLaunching)
        {
            LaunchObject();
        }
        
        // Atualiza a trajetória durante o lançamento
        if (isLaunching)
        {
            UpdateTrajectory();
        }
    }
    
    void LaunchObject()
    {
        if (targetPosition == null)
        {
            Debug.LogWarning("Target Position não foi atribuída!");
            return;
        }
        
        isLaunching = true;
        launchTimer = 0f;
        startPosition = transform.position;
        
        // Calcula a distância total e a duração baseada na velocidade
        totalDistance = Vector3.Distance(startPosition, targetPosition.position);
        launchDuration = totalDistance / launchSpeed;
    }
    
    void UpdateTrajectory()
    {
        launchTimer += Time.deltaTime;
        float progress = launchTimer / launchDuration;
        
        if (progress >= 1f)
        {
            // Lançamento completo
            transform.position = targetPosition.position;
            isLaunching = false;
            launchTimer = 0f;
            return;
        }
        
        // Calcula a posição ao longo da trajetória parabólica
        Vector3 currentPos = Vector3.Lerp(startPosition, targetPosition.position, progress);
        
        // Adiciona altura em arco (parábola)
        float arc = arcHeight * Mathf.Sin(progress * Mathf.PI);
        currentPos.y += arc;
        
        transform.position = currentPos;
    }
    
    // Método opcional para visualizar a trajetória no editor
    void OnDrawGizmos()
    {
        if (targetPosition != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, targetPosition.position);
            Gizmos.DrawWireSphere(targetPosition.position, 0.3f);
            
            // Desenha a trajetória em arco
            Vector3 previousPoint = transform.position;
            int segments = 20;
            
            for (int i = 1; i <= segments; i++)
            {
                float t = i / (float)segments;
                Vector3 point = Vector3.Lerp(transform.position, targetPosition.position, t);
                point.y += arcHeight * Mathf.Sin(t * Mathf.PI);
                
                Gizmos.color = Color.green;
                Gizmos.DrawLine(previousPoint, point);
                previousPoint = point;
            }
        }
    }
}