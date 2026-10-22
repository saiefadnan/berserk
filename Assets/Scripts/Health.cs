using System;
using UnityEngine;

public class Health: MonoBehaviour, IDamageable{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private SurfaceType _surfaceType = SurfaceType.Flesh;

    private float currentHealth;
    private bool isDead = false;

    [NonSerialized] public bool isInvulnerable = false;
    [NonSerialized] public float damageReductionPercent = 0f; // 0 = no reduction, 0.85 = block 85%

        // IDamageable implementation
    public SurfaceType surfaceType => _surfaceType;
    public GameObject victim => gameObject;

        // Events for other systems to subscribe to (UI, Audio, Dismemberment, AI)
    public event Action<DamageData> OnDamaged;
    public event Action<DamageData> OnDeath;
    public event Action<float, float> OnHealthChanged; // (currentHealth, maxHealth)

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => isDead;                                                                           

    private void Awake()
    {
        currentHealth = maxHealth;
    }
    private void Start(){
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void TakeDamage(DamageData data){
        if(isDead) return;
        // 1. Check for i-Frames (e.g. Guts dodge roll)
        if (isInvulnerable)
        {
            Debug.Log($"<color=cyan>[Combat] {gameObject.name} DODGED! (i-Frames active)</color>");
            return;
        }

           // 2. Apply damage reduction (e.g. Dragonslayer guard block)
        float finalDamage = data.amount * (1f - Mathf.Clamp01(damageReductionPercent));

        currentHealth -= finalDamage;
        currentHealth = Mathf.Max(0f, currentHealth);

            // Notify listeners of damage and health change
        OnDamaged?.Invoke(data);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

            // 3. Check for Death
        if (currentHealth <= 0f && !isDead)
        {
            isDead = true;
            OnDeath?.Invoke(data);
        }        
    }

    public void Heal(float amount)
    {
        if (isDead) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void Revive(float healthAmount)
        {
            isDead = false;
            currentHealth = Mathf.Clamp(healthAmount, 1f, maxHealth);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }
}