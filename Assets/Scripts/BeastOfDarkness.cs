using System;
using UnityEngine;

public class BeastOfDarkness : MonoBehaviour
{
    [Header("Rage Settings")]
    [SerializeField] public float maxRage = 100f;
    [SerializeField] public float currentRage = 0f;
    [SerializeField] private float drainRate = 7.5f;
    [SerializeField] private KeyCode activateKey = KeyCode.R;

    [Header("Combat Buffs")]
    [SerializeField] private float damageMultiplier = 2.0f;
    [SerializeField] private float attackSpeedMultiplier = 1.35f;

    [Header("Audio")]
    [SerializeField] private AudioClip roarAudio;
    [SerializeField] private AudioClip heartbeatLoop;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioSource heartbeatAudioSource;

    public bool IsActive { get; private set; }= false;
    public float DamageMultiplier => damageMultiplier;
    public float AttackSpeedMultiplier => attackSpeedMultiplier;

    public event Action<float, float> onRageChanged;

    private Animator animator;

    private void Awake(){
        animator = GetComponentInChildren<Animator>();
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
        if (heartbeatAudioSource == null)
        {
            heartbeatAudioSource = gameObject.AddComponent<AudioSource>();
            heartbeatAudioSource.playOnAwake = false;
            heartbeatAudioSource.loop = true;
            heartbeatAudioSource.spatialBlend = 0f;
        }
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

        if (audioSource != null && roarAudio != null)
        {
            audioSource.PlayOneShot(roarAudio, 1.0f);
        }

        if (heartbeatAudioSource != null && heartbeatLoop != null)
        {
            heartbeatAudioSource.clip = heartbeatLoop;
            heartbeatAudioSource.loop = true;
            heartbeatAudioSource.volume = 0.85f;
            heartbeatAudioSource.Play();
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
        if (heartbeatAudioSource != null)
        {
            heartbeatAudioSource.Stop();

        }

        Debug.Log("<color=grey>[Beast Subdued] Guts catches his breath. Normal combat restored.</color>");
    }



}
