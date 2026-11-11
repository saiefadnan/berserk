using System;
using UnityEngine;

public class Stamina : MonoBehaviour
{
    [Header("Stamina Tuning")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float regenRate = 28f;       // recovered per second
    [SerializeField] private float regenDelay = 0.8f;     // delay after spending before regen begins

    private float currentStamina;
    private float lastUseTime;

    public float CurrentStamina => currentStamina;
    public float MaxStamina => maxStamina;
    public bool IsExhausted => currentStamina <= 0.5f;

    // Event-driven: HUD listens without polling Update()
    public event Action<float, float> OnStaminaChanged;

    private void Awake()
    {
        currentStamina = maxStamina;
    }

    private void Start()
    {
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
    }

    private void Update()
    {
        if (Time.time >= lastUseTime + regenDelay && currentStamina < maxStamina)
        {
            currentStamina = Mathf.Min(maxStamina, currentStamina + regenRate * Time.deltaTime);
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        }
    }

    public bool HasEnough(float amount)
    {
        return currentStamina >= amount;
    }

    public bool TryConsume(float amount)
    {
        if (currentStamina < amount) return false;
        Consume(amount);
        return true;
    }

    public void Consume(float amount)
    {
        currentStamina = Mathf.Max(0f, currentStamina - amount);
        lastUseTime = Time.time;
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
    }
}
