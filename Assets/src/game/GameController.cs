using System.Collections.Generic;
using UnityEngine;




public class GameController : MonoBehaviour
{

    public bool IsBattleStart = false;

    public Camera cameraToLookAt;
    public Transform playerTarget;
    public UiBuffButton[] uiBuffButton;
    public static GameController Instance;
    Dictionary<Buff, UiBuffButton> activeBuffs = new Dictionary<Buff, UiBuffButton>();

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


    public void ActivateBuff(Buff buff, Vector3 position, Quaternion rotation)
    {
        if (activeBuffs.ContainsKey(buff))
        {
            activeBuffs[buff].renewBuff();
            return;
        }



        UiBuffButton button = GetAvailableButton();
        if (button != null)
        {
            button.ActiveButtonBuff(buff);
            activeBuffs.Add(buff, button);
        }
    }


    public UiBuffButton GetAvailableButton() 
    {
        foreach (var button in uiBuffButton)
        {
            if (!button.gameObject.activeInHierarchy)
            {
                return button;
            }
        }
        return null;
    }


    public void DeactivateBuff(Buff buff)
    {
        if (activeBuffs.ContainsKey(buff))
        {
            activeBuffs[buff].gameObject.SetActive(false);
            activeBuffs.Remove(buff);
        }
    }




}