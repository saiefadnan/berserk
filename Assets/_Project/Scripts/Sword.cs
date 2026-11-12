using System.Collections;
using UnityEngine;
using System.Collections.Generic;

public class Sword : MonoBehaviour
{
    [SerializeField] private float damage = 10f;
    [SerializeField] private ParticleSystem slashEffect;
    [SerializeField] private GameObject flashEffectPrefab;
    [SerializeField] private GameObject bloodEffectPrefab;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hitAudio;
    [SerializeField] private AudioClip slashSound;
    [SerializeField] private AudioClip[] comboSwingSounds;
    [SerializeField] private AudioClip clashAudio;
    [SerializeField] private AudioClip beastHitAudio;
    [SerializeField] private TrailRenderer swordTrail;
    [SerializeField] private float trailWidth = 1.6f;

    [Header("Impact Audio Polish")]
    [SerializeField] private AudioSource impactAudioSource;
    [SerializeField] private AudioClip[] fleshCleaveClips;
    [SerializeField] private AudioClip heavyCleaveClip;


    private readonly HashSet<IDamageable> targetsHitThisSwing = new HashSet<IDamageable>();
    private BoxCollider swordCollider;
    private bool cool = false;
    private bool hitAnyTargetThisSwing = false;
    private Animator animator;
    private Coroutine hitStopCoroutine;
    private Color defaultTrailStartColor = Color.white;
    private Color defaultTrailEndColor = new Color(1f, 1f, 1f, 0f);
    private int currentSwingIndex = 0;
    private float lastSlashAudioTime = -1f;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>() ?? GetComponentInParent<AudioSource>();

        if (impactAudioSource == null)
        {
            AudioSource[] sources = GetComponents<AudioSource>();
            if (sources.Length > 1)
            {
                impactAudioSource = sources[1];
            }
            else
            {
                impactAudioSource = gameObject.AddComponent<AudioSource>();
                impactAudioSource.playOnAwake = false;
                impactAudioSource.spatialBlend = 1.0f;
                impactAudioSource.minDistance = 2.0f;
                impactAudioSource.maxDistance = 25.0f;
            }
        }
        swordCollider = GetComponent<BoxCollider>();
    }

    private void Start()
    {
        animator = GetComponentInParent<Animator>();
         if (swordTrail != null)
        {
            defaultTrailStartColor = swordTrail.startColor;
            defaultTrailEndColor = swordTrail.endColor;
            swordTrail.widthMultiplier = trailWidth;
        }
    }

    // In Inspector: check "Is Trigger" on the Box Collider
    private void OnTriggerEnter(Collider other)
    {
        if (cool)
        {
            IDamageable damageable = other.GetComponent<IDamageable>() ?? other.GetComponentInParent<IDamageable>();
            if (damageable == null || damageable.victim == transform.root.gameObject) return;
            if(targetsHitThisSwing.Contains(damageable)) return;
            targetsHitThisSwing.Add(damageable);
            hitAnyTargetThisSwing = true;
            // No ContactPoint available with triggers, use transform instead
            Vector3 hitPoint = other.ClosestPoint(swordCollider.bounds.center);
            Vector3 hitNormal = (hitPoint - transform.position).normalized;
            EnemyCombat enemyCombat = other.GetComponent<EnemyCombat>() ?? other.GetComponentInParent<EnemyCombat>();
            bool isBlocked = (enemyCombat != null && enemyCombat.IsBlocking) || (damageable.surfaceType == SurfaceType.Metal);
            BeastOfDarkness beast = transform.root.GetComponent<BeastOfDarkness>();
            bool isBeastActive = (beast != null && beast.IsActive);

            // Determine attack tier from player animator
            AnimatorStateInfo baseState = (animator != null) ? animator.GetCurrentAnimatorStateInfo(0) : default;
            bool inAttack3 = baseState.IsName("attack3");
            bool inAttack2 = baseState.IsName("attack2");

            if(isBlocked){
                PlaySparkFlash(hitPoint, other.transform.position);
                PlayClashAudio();
                Animator enemyAnim = other.GetComponentInParent<Animator>();
                TriggerHitStop(0.09f, enemyAnim);
                if (CameraShake.Instance != null)
                {
                    CameraShake.Instance.Shake(0.18f, 0.28f);
                }
            }
            else if (damageable.surfaceType == SurfaceType.Flesh)
            {
                PlayBloodSplatter(hitPoint, hitNormal);
                // Guts Combat Juice: Micro-hitstop & heavy impact camera shake
                Animator enemyAnim = other.GetComponentInParent<Animator>();
                TriggerHitStop(0.07f, enemyAnim);
                PlayHitAudio(isBeastActive, inAttack3 || isBeastActive);
            }

            Vector3 pushDirection = (other.transform.position - transform.root.position);
            pushDirection.y = 0f;
            if (pushDirection.sqrMagnitude > 0.001f) pushDirection.Normalize();
            else pushDirection = transform.root.forward;

            HitReactionType reaction = HitReactionType.SwordLight;
            float knockback = 2.0f;

            if (inAttack3)
            {
                reaction = HitReactionType.SwordKnockdown;
                knockback = isBeastActive ? 9.0f : 6.5f;
            }
            else if (inAttack2)
            {
                reaction = HitReactionType.SwordMedium;
                knockback = 4.0f;
            }
            else
            {
                reaction = HitReactionType.SwordLight;
                knockback = 2.0f;
            }

            float finalDamage = damage;
            if (isBeastActive)
            {
                finalDamage *= beast.DamageMultiplier;
                Health playerHealth = transform.root.GetComponent<Health>();
                if (playerHealth != null)
                {
                    playerHealth.Heal(5f);
                }
            }
            else if (beast != null)
            {
                beast.AddRage(8f);
            }

            DamageData data = new DamageData
            {
                amount = finalDamage,
                hitDirection = pushDirection,
                hitPoint = hitPoint,
                attacker = transform.root.gameObject,
                reactionType = reaction,
                knockbackForce = knockback
            };

            damageable.TakeDamage(data);
            if (CameraShake.Instance != null)
            {
                if (inAttack3 || isBeastActive)
                    CameraShake.Instance.Shake(0.24f, 0.40f); // Massive screen shudder on heavy cleave!!
                else
                    CameraShake.Instance.Shake(0.16f, 0.20f);
            }
        }
    }

    private Animator currentPausedEnemyAnimator;

    public void TriggerHitStop(float duration, Animator enemyAnimator)
    {
        if (hitStopCoroutine != null)
        {
            StopCoroutine(hitStopCoroutine);
            if (currentPausedEnemyAnimator != null) currentPausedEnemyAnimator.speed = 1.0f;
        }
        hitStopCoroutine = StartCoroutine(DoHitStop(duration, enemyAnimator));
    }

    private IEnumerator DoHitStop(float duration, Animator enemyAnimator)
    {
        currentPausedEnemyAnimator = enemyAnimator;
        if (animator != null) animator.speed = 0.0f;
        if (enemyAnimator != null) enemyAnimator.speed = 0.0f;
        yield return new WaitForSecondsRealtime(duration);
        BeastOfDarkness beast = transform.root.GetComponent<BeastOfDarkness>();
        float resumeSpeed = (beast != null && beast.IsActive) ?
            beast.AttackSpeedMultiplier : 1.0f;
        if (animator != null) animator.speed = resumeSpeed;
        if (enemyAnimator != null) enemyAnimator.speed = 1.0f;
        currentPausedEnemyAnimator = null;
        hitStopCoroutine = null;
    }

    public void PlaySparkFlash(Vector3 hitPos, Vector3 enemyPos)
    {
        Vector3 dir = (hitPos - enemyPos).normalized;
        if (flashEffectPrefab != null)
        {
            GameObject effect = Instantiate(
                flashEffectPrefab,
                hitPos,
                Quaternion.LookRotation(dir)
            );
            Destroy(effect, 2f);
        }
    }

    public void AudioSlash()
    {
        PlaySlashSoundInternal();
    }

    private void PlaySlashSoundInternal()
    {
        // Debounce to prevent double-firing if both PlaySlashAudio and EnableDamage/PlaySlashAnim are called
        if (Time.time - lastSlashAudioTime < 0.15f) return;
        lastSlashAudioTime = Time.time;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>() ?? GetComponentInParent<AudioSource>();

        if (audioSource == null) return;

        BeastOfDarkness beast = transform.root.GetComponent<BeastOfDarkness>();
        bool isBeastActive = (beast != null && beast.IsActive);

        AudioClip clipToPlay = null;
        if (comboSwingSounds != null && comboSwingSounds.Length > 0)
        {
            clipToPlay = comboSwingSounds[currentSwingIndex % comboSwingSounds.Length];
            currentSwingIndex = (currentSwingIndex + 1) % comboSwingSounds.Length;
        }
        else
        {
            clipToPlay = slashSound;
        }

        if (clipToPlay != null)
        {
            // Beast Mode: deeper demonic pitch (0.82-0.88), normal: slight organic pitch nuances (0.96-1.04)
            audioSource.pitch = isBeastActive ? Random.Range(0.82f, 0.88f) : Random.Range(0.96f, 1.04f);
            audioSource.PlayOneShot(clipToPlay);
        }
    }

    public void PlayHitAudio(bool isBeast = false, bool isHeavyStrike = false)
    {
        AudioSource source = impactAudioSource != null ? impactAudioSource : audioSource;
        if (source == null) return;

        // Organic pitch shift to eliminate repetitive drone
        source.pitch = isBeast ? Random.Range(0.82f, 0.90f) : Random.Range(0.92f, 1.08f);
        AudioClip baseClip = isBeast ? (beastHitAudio ?? hitAudio) : hitAudio;
        if (baseClip != null)
        {
            source.PlayOneShot(baseClip, 0.85f);
        }

        // Layer visceral flesh cleave
        if (isHeavyStrike && heavyCleaveClip != null)
        {
            source.PlayOneShot(heavyCleaveClip, 1.0f);
        }
        else if (fleshCleaveClips != null && fleshCleaveClips.Length > 0)
        {
            AudioClip cleaveClip = fleshCleaveClips[Random.Range(0, fleshCleaveClips.Length)];
            if (cleaveClip != null) source.PlayOneShot(cleaveClip, 0.9f);
        }
    }

    public void PlayClashAudio()
    {
        AudioSource source = impactAudioSource != null ? impactAudioSource : audioSource;
        if (source == null)
            source = GetComponent<AudioSource>() ?? GetComponentInParent<AudioSource>();

        if (source != null && clashAudio != null)
        {
            source.pitch = Random.Range(0.95f, 1.05f);
            source.PlayOneShot(clashAudio, 1.0f);
        }
    }

    public void EnableDamage()
    {
        cool = true;
        targetsHitThisSwing.Clear();
        hitAnyTargetThisSwing = false;
        AnimateSlash();
        if (swordTrail != null) {
            BeastOfDarkness beast = transform.root.GetComponent<BeastOfDarkness>();
           if (beast != null && beast.IsActive)
            {
                // Intense glowing crimson red!
                swordTrail.startColor = new Color(1.0f, 0.05f, 0.05f, 0.95f);
                swordTrail.endColor = new Color(0.4f, 0.0f, 0.0f, 0.0f);
            }
            else
            {
                // Normal sword trail
                swordTrail.startColor = defaultTrailStartColor;
                swordTrail.endColor = defaultTrailEndColor;
            }
            swordTrail.emitting = true;
        }
        Invoke(nameof(DisableDamage), 0.5f);
    }

    void DisableDamage()
    {
        cool = false;
        if (swordTrail != null) swordTrail.emitting = false;
        if (!hitAnyTargetThisSwing && CombatDirector.Instance != null){
            CombatDirector.Instance.NotifyPlayerWhiff(transform.root.position);
        }
    }


    public void AnimateSlash()
    {
        PlaySlashSoundInternal();

        if (slashEffect != null)
        {
            slashEffect.Play(true);
        }

        // Kinetic swing kick when cutting through the air
        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.Shake(0.10f, 0.08f);
        }
    }

    public void PlayBloodSplatter(Vector3 hitPoint, Vector3 normal)
    {
        if (bloodEffectPrefab != null)
        {
            Quaternion rot = (normal.sqrMagnitude > 0.001f) ? Quaternion.LookRotation(normal) : Quaternion.identity;
            GameObject effect = Instantiate(bloodEffectPrefab, hitPoint, rot);
            Destroy(effect, 3f);
        }
    }
}
