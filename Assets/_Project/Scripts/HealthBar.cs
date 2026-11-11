using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HealthBar : MonoBehaviour
{

    [Header("UI Sliders")]
    public Slider healthSlider;
    public Slider easeSlider;
    public float lerpSpeed = 0.05f;

    [Header("Target")]
    [SerializeField] private Health targetHealth;

    public float maxHealth;
    public float currentHealth;
    private Coroutine easeRoutine;
    
    private void Awake(){
        if(targetHealth == null){
            GameObject player = GameObject.Find("player_controller") ?? GameObject.FindWithTag("Player") ?? GameObject.Find("player");
            if (player != null)
            {
                targetHealth = player.GetComponent<Health>() ?? player.GetComponentInChildren<Health>();    
            }
        }
    }

    private void OnEnable()
    {
        if (targetHealth != null)
        {
            targetHealth.OnHealthChanged += HandleHealthChanged;
        }
    }

    private void OnDisable()
    {
        if (targetHealth != null)
        {
            targetHealth.OnHealthChanged -= HandleHealthChanged;
        }
    }

    private void Start()
    {
        if (targetHealth != null)
        {
            HandleHealthChanged(targetHealth.CurrentHealth, targetHealth.MaxHealth); // Initial sync on boot
        }
    }
    

    private void HandleHealthChanged(float current, float max)
    {
        currentHealth = current;
        maxHealth = max;

        if (healthSlider != null)
        {
            healthSlider.maxValue = max;
            healthSlider.value = current;
        }

        if (easeSlider != null)
        {
            easeSlider.maxValue = max;
        }

        if(easeRoutine!=null) StopCoroutine(easeRoutine);
        easeRoutine = StartCoroutine(SmoothEaseBar());
    }

    private IEnumerator SmoothEaseBar()
    {
        while (Mathf.Abs(easeSlider.value - currentHealth) > 0.01f)
        {
            easeSlider.value = Mathf.Lerp(easeSlider.value, currentHealth, lerpSpeed);
            yield return null;
        }
        easeSlider.value = currentHealth;
        easeRoutine = null;
    }
}
