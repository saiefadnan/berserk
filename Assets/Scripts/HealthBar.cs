using UnityEngine;
using UnityEngine.UI;


public class HealthBar : MonoBehaviour
{
    public Slider HealthSlider;
    public Slider EaseSlider;
    public float MaxHealth = 100f;
    public float CurrentHealth;
    public float lerpSpeed = 0.05f;

    void Start()
    {
        CurrentHealth = MaxHealth;
    }
    // Update is called once per frame
    void Update()
    {
        if(HealthSlider.value!=CurrentHealth){
            HealthSlider.value=CurrentHealth;
        }
        if(HealthSlider.value!=EaseSlider.value){
           EaseSlider.value=Mathf.Lerp(EaseSlider.value,CurrentHealth,lerpSpeed);
        }
    }
}
