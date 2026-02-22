using UnityEngine;




public class GameController : MonoBehaviour
{

    public bool IsBattleStart = false;

    public Camera cameraToLookAt;
    public Transform playerTarget;


    public static GameController Instance;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
}