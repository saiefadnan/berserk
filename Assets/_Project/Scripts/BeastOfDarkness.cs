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

    [Header("Visual Effects")]
    [SerializeField] private GameObject redEyeVFX;
    private TrailRenderer eyeTrail;
    private Light eyeLight;

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
        SetupRedEyeTrail();
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

        if (eyeTrail != null) eyeTrail.emitting = true;
        if (eyeLight != null) eyeLight.enabled = true;
        if (redEyeVFX != null) redEyeVFX.SetActive(true);

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
        if (eyeTrail != null) eyeTrail.emitting = false;
        if (eyeLight != null) eyeLight.enabled = false;
        if (redEyeVFX != null) redEyeVFX.SetActive(false);
        Debug.Log("<color=grey>[Beast Subdued] Guts catches his breath. Normal combat restored.</color>");
    }

    private void SetupRedEyeTrail()
    {
        if(redEyeVFX == null){
            Transform head = FindChildRecursive(transform, "mixamorig:Head")
            ?? FindChildRecursive(transform, "Head");

            if(head != null){
                redEyeVFX = new GameObject("BeastRedEye");
                redEyeVFX.transform.SetParent(head, false);
                redEyeVFX.transform.localPosition = new Vector3(0.06f, 0.12f, 0.14f);

                eyeTrail = redEyeVFX.AddComponent<TrailRenderer>();
                eyeTrail.time = 0.22f;
                eyeTrail.startWidth = 0.08f;
                eyeTrail.endWidth = 0.0f;
                eyeTrail.material = new Material(Shader.Find("Sprites/Default"));
                eyeTrail.startColor = new Color(1.0f, 0.08f, 0.08f, 0.95f);
                eyeTrail.endColor = new Color(0.3f, 0.0f, 0.0f, 0.0f);
                eyeTrail.emitting = false;

                // Add ominous red point light glow
                eyeLight = redEyeVFX.AddComponent<Light>();
                eyeLight.type = LightType.Point;
                eyeLight.color = Color.red;
                eyeLight.range = 1.2f;
                eyeLight.intensity = 1.8f;
                eyeLight.enabled = false;
                redEyeVFX.SetActive(false);
            }
        }
    }

    private Transform FindChildRecursive(Transform parent, string name)
    {
        if (parent.name.Equals(name, StringComparison.OrdinalIgnoreCase)) return parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindChildRecursive(parent.GetChild(i),name);
            if (found != null) return found;
        }
        return null;
    }

}
