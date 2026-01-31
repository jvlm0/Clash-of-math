using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class BlinkFadeTMP : MonoBehaviour
{
    public float speed = 2f;
    private TextMeshProUGUI tmp;
    private Image image;
    private Color originalColor, imageOriginalColor;

    void Start()
    {
        image = GetComponent<Image>(); 
        tmp = GetComponentInChildren<TextMeshProUGUI>();
        imageOriginalColor = image.color;   
        originalColor = tmp.color;
    }

    void Update()
    {


        float alpha = Mathf.Abs(Mathf.Sin(Time.time * speed));
        
        image.color = new Color(
            imageOriginalColor.r,
            imageOriginalColor.g,
            imageOriginalColor.b,
            alpha
        );
        
        tmp.color = new Color(
            originalColor.r,
            originalColor.g,
            originalColor.b,
            alpha
        );
    }
}