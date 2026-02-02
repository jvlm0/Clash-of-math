using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class BlinkFadeTMP : MonoBehaviour
{
    public float speed = 2f;
    private Image image;
    private Color imageOriginalColor;

    void Start()
    {
        image = GetComponent<Image>(); 
        imageOriginalColor = image.color;   
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
        
        
    }
}