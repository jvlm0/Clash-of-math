using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;





public class UiBuffButton : MonoBehaviour, IPointerClickHandler
{
    public Image BuffTimerImage;
    public Buff buff;

    public Image BuffImage;

    float duration, currtentTime;

    private void Start()
    {
        gameObject.SetActive(false);
    }


    public void ActiveButtonBuff(Buff buffp)
    {
        gameObject.SetActive(true);
        BuffTimerImage.fillAmount = 1f;
        buff = buffp;
        duration = buff.duration;
        currtentTime = 0f;
        BuffImage.sprite = buff.activeSprite;

        
    }

    void Update()
    {
        currtentTime += Time.deltaTime;
        BuffTimerImage.fillAmount = 1f - (currtentTime / duration);
        if (currtentTime >= duration)
        {
            gameObject.SetActive(false);
        }
    }


    public void renewBuff()
    {
        currtentTime = 0f;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        GameController.Instance.UseBuff(buff);      
    }
}