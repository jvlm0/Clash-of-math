using UnityEngine;





public class Buff: MonoBehaviour
{   
    public float duration;
    public bool isActive;
    public Sprite activeSprite;
    public GameObject gamePrefab;


    public void ActivateBuff()
    {
        GetComponent<IBuffController>()?.ActivateBuff();
    }
}