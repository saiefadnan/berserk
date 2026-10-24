using System;
using UnityEngine;

public class BeastOfDarkness : MonoBehaviour
{
    [Header("Rage Settings")]
    [SerializeField] private float maxRage = 100f;
    [SerializeField] private float currentRage = 0f;
    [SerializeField] private float drainRate = 7.5f;
    [SerializeField] private KeyCode activateKey = KeyCode.R;

    [Header("Combat Buffs")]
    [SerializeField] private float damageMultiplier = 2.0f;
    [SerializeField] private float attackSpeedMultiplier = 1.35f;

    public bool IsActive { get; private set; }= false;
    public float DamageMultiplier => damageMultiplier;
    public float AttackSpeedMultiplier => attackSpeedMultiplier;

    public event Action<float, float> onRageChanged;

    private Animator animator;

    private void Awake(){
        animator = GetComponentInChildren<Animator>();
    }

    private void Start(){
        onRageChanged?.Invoke(currentRage, maxRage);
    }

    private void Update(){
        if(IsActive){
            currentRage -= drainRate * Time.deltaTime;
            onRageChanged?.Invoke(currentRage, maxRage);
            if(currentRage <= 0){
                currentRage = 0;
                SubdueBeast();
            }
        }else{
            if(currentRage >= maxRage && Input.GetKeyDown(activateKey)){
                UnleashBeast();
            }
        }
    }

    public void AddRage(float amount){
        if (IsActive) return;
        currentRage = Mathf.Clamp(currentRage + amount, 0f, maxRage);
        onRageChanged?.Invoke(currentRage, maxRage);
    }

    private void UnleashBeast(){
        if (IsActive) return;
        IsActive = true;
        currentRage = maxRage;
        if (animator != null)
        {
            animator.speed = attackSpeedMultiplier;
        }

        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.Shake(0.35f, 0.45f);
        }

        Debug.Log("<color=red><b>[BEAST OF DARKNESS UNLEASHED!] Damage x2, Attack Speed +35%!</b></color>");
    }


    private void SubdueBeast(){
        if (!IsActive) return;
        IsActive = false;
        if (animator != null)
        {
            animator.speed = 1.0f;
        }

        Debug.Log("<color=grey>[Beast Subdued] Guts catches his breath. Normal combat restored.</color>");
    }



}
