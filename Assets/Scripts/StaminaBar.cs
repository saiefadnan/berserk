using UnityEngine;
using UnityEngine.UI;

public class StaminaBar : MonoBehaviour
{
    [Header("UI Sliders")]
    public Slider staminaSlider;

    [Header("Target")]
    [SerializeField] private Stamina targetStamina;

    private void Awake()
    {
        if (staminaSlider == null)
            staminaSlider = GetComponent<Slider>();

        if (targetStamina == null)
        {
            GameObject player = GameObject.Find("player_controller") ??
                                GameObject.FindWithTag("Player") ??
                                GameObject.Find("player");
            if (player != null)
                targetStamina = player.GetComponent<Stamina>() ?? player.GetComponentInChildren<Stamina>();
        }
    }

    private void OnEnable()
    {
        if (targetStamina != null)
            targetStamina.OnStaminaChanged += HandleStaminaChanged;
    }

    private void OnDisable()
    {
        if (targetStamina != null)
            targetStamina.OnStaminaChanged -= HandleStaminaChanged;
    }

    private void Start()
    {
        if (targetStamina != null)
            HandleStaminaChanged(targetStamina.CurrentStamina, targetStamina.MaxStamina);
    }

    private void HandleStaminaChanged(float current, float max)
    {
        if (staminaSlider != null)
        {
            staminaSlider.maxValue = max;
            staminaSlider.value = current;
        }
    }
}
