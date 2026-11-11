using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class PlayerDeathHandler : MonoBehaviour
{
    [Header("Hitstop & Slow Motion")]
    [SerializeField] private float lethalTimeScale = 0.15f;
    [SerializeField] private float hitstopDurationRealtime = 0.4f;

    [Header("Camera & Impact FX")]
    [SerializeField] private float lethalShakeDuration = 0.35f;
    [SerializeField] private float lethalShakeIntensity = 0.35f;
    [SerializeField] private AudioClip lethalImpactAudio;

    private Health health;
    private Stamina stamina;
    private PlayerMovement movement;
    private PlayerCombat combat;
    private TargetLockSystem targetLock;
    private BeastOfDarkness beast;
    private Animator animator;
    private AudioSource audioSource;
    private CharacterController characterController;
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;

    private static readonly int DeathParam = Animator.StringToHash("death");

    public event Action OnPlayerDeathSequenceStart;

    private void Awake()
    {
        health = GetComponent<Health>();
        movement = GetComponent<PlayerMovement>();
        combat = GetComponent<PlayerCombat>();
        targetLock = GetComponent<TargetLockSystem>() ?? GetComponentInParent<TargetLockSystem>();
        beast = GetComponent<BeastOfDarkness>();
        animator = GetComponentInChildren<Animator>();
        audioSource = GetComponent<AudioSource>();
        characterController = GetComponent<CharacterController>();
        stamina = GetComponent<Stamina>();
    }

    private void Start(){
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;

        if (DeathScreenUI.Instance != null){
            DeathScreenUI.Instance.OnRespawnRequested += Respawn;
        }
    }

    private void OnEnable()
    {
        if (health != null)
        {
            health.OnDeath += HandleDeath;
        }
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.OnDeath -= HandleDeath;
        }
    }

    private void HandleDeath(DamageData data)
    {
        Debug.Log("<color=red><b>[PlayerDeathHandler] Guts has fallen in combat!</b></color>");

        // 1. Break and clear target lock
        if (targetLock != null && targetLock.IsLockedOn)
        {
            targetLock.Unlock();
        }

        // 2. Play death knockdown animation and mute upper body layers
        if (animator != null)
        {
            animator.SetLayerWeight(1, 0f);
            animator.SetTrigger(DeathParam);
        }

        // 3. Audio & Camera impact
        if (audioSource != null && lethalImpactAudio != null)
        {
            audioSource.PlayOneShot(lethalImpactAudio);
        }

        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.Shake(lethalShakeDuration, lethalShakeIntensity);
        }

        // 4. Begin dramatic slow-motion lethal hitstop
        StartCoroutine(LethalHitstopRoutine());

        // 5. Notify listeners (e.g. "YOU DIED" UI overlay)
        OnPlayerDeathSequenceStart?.Invoke();
    }

    private IEnumerator LethalHitstopRoutine()
    {
        Time.timeScale = lethalTimeScale;
        yield return new WaitForSecondsRealtime(hitstopDurationRealtime);

        float elapsed = 0f;
        float restoreDuration = 0.35f;
        while (elapsed < restoreDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            Time.timeScale = Mathf.Lerp(lethalTimeScale, 1.0f, elapsed / restoreDuration);
            yield return null;
        }
        Time.timeScale = 1.0f;
    }

    public void Respawn(){
        if (characterController != null)characterController.enabled = false;
        transform.position = spawnPosition;
        transform.rotation = spawnRotation;
        if (characterController != null)characterController.enabled = true;

        if (health != null) health.Revive(health.MaxHealth);
        if (stamina != null)stamina.Consume(-stamina.MaxStamina);
        if (animator != null){
            animator.Play("Locomotion_Equipped", 0, 0f);
            animator.SetLayerWeight(1, 1f);
        }
        if (DeathScreenUI.Instance != null){
            DeathScreenUI.Instance.ResetUI();
        }
        Debug.Log("<color=green><b>[Respawn] Guts has risen at the checkpoint!</b></color>");
    }
}
