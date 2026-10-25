using System.Collections;
using UnityEngine;

public class Sword : MonoBehaviour
{
    public float damage = 10f;
    public ParticleSystem slashEffect;
    [SerializeField] private GameObject flashEffectPrefab;
    [SerializeField] private GameObject bloodEffectPrefab;
    public AudioSource audioSource;
    public AudioClip hitAudio;
    public AudioClip slashSound;
    [SerializeField] private AudioClip beastHitAudio;
    public TrailRenderer swordTrail;
    private bool cool = false;
    private Animator animator;
    private Coroutine hitStopCoroutine;
    private Color defaultTrailStartColor = Color.white;
    private Color defaultTrailEndColor = new Color(1f, 1f, 1f, 0f);

    private void Start()
    {
        animator = GetComponentInParent<Animator>();
         if (swordTrail != null)
        {
            defaultTrailStartColor = swordTrail.startColor;
            defaultTrailEndColor = swordTrail.endColor;
        }
    }

    // In Inspector: check "Is Trigger" on the Box Collider
    private void OnTriggerEnter(Collider other)
    {
        if (cool)
        {
            IDamageable damageable = other.GetComponent<IDamageable>() ?? other.GetComponentInParent<IDamageable>();
            if (damageable == null || damageable.victim == transform.root.gameObject) return;

            // No ContactPoint available with triggers, use transform instead
            Vector3 hitPoint = other.ClosestPoint(transform.position);
            Vector3 hitNormal = (hitPoint - transform.position).normalized;

            if (damageable.surfaceType == SurfaceType.Flesh)
            {
                PlayBloodSplatter(hitPoint, hitNormal, other.transform);
                // Guts Combat Juice: Micro-hitstop & heavy impact camera shake
                Animator enemyAnim = other.GetComponentInChildren<Animator>();
                TriggerHitStop(0.07f, enemyAnim);
            }

            PlaySparkFlash(transform.position, other.transform.position);
            BeastOfDarkness beast = transform.root.GetComponent<BeastOfDarkness>();
            bool isBeastActive = (beast != null && beast.IsActive);
            PlayHitAudio(isBeastActive);
            Vector3 slashDirection = transform.forward;

            float finalDamage = damage;
            if (isBeastActive)
            {
                finalDamage *= beast.DamageMultiplier;
            }
            else if (beast != null)
            {
                beast.AddRage(8f);
            }

            DamageData data = new DamageData
            {
                amount = finalDamage,
                hitDirection = slashDirection,
                hitPoint = hitPoint,
                attacker = transform.root.gameObject
            };

            damageable.TakeDamage(data);
            if (CameraShake.Instance != null)
            {
                CameraShake.Instance.Shake(0.16f, 0.20f);
            }
        }
    }

    public void TriggerHitStop(float duration, Animator enemyAnimator)
    {
        if (hitStopCoroutine != null) StopCoroutine(hitStopCoroutine);
        hitStopCoroutine = StartCoroutine(DoHitStop(duration, enemyAnimator));
    }

    private IEnumerator DoHitStop(float duration, Animator enemyAnimator)
    {
        if (animator != null) animator.speed = 0.0f;
        if (enemyAnimator != null) enemyAnimator.speed = 0.0f;
        yield return new WaitForSecondsRealtime(duration);
        BeastOfDarkness beast = transform.root.GetComponent<BeastOfDarkness>();
        float resumeSpeed = (beast != null && beast.IsActive) ?
            beast.AttackSpeedMultiplier : 1.0f;
        if (animator != null) animator.speed = resumeSpeed ;
        if (enemyAnimator != null) enemyAnimator.speed = 1.0f;
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
        // if (audioSource != null && slashSound != null)
        //     audioSource.PlayOneShot(slashSound);
    }

    public void PlayHitAudio(bool isBeast = false)
    {
        if (audioSource == null) return;

        if (isBeast && beastHitAudio != null)
        {
            audioSource.PlayOneShot(beastHitAudio);
        }
        else if (hitAudio != null)
        {
            audioSource.PlayOneShot(hitAudio);
        }
    }

    public void EnableDamage()
    {
        cool = true;
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
    }


    public void AnimateSlash()
    {
        if (audioSource != null && slashSound != null)
            audioSource.PlayOneShot(slashSound);

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

    public void PlayBloodSplatter(Vector3 hitPoint, Vector3 normal, Transform parent = null)
    {
        if (bloodEffectPrefab != null)
        {
            Quaternion rot = (normal.sqrMagnitude > 0.001f) ? Quaternion.LookRotation(normal) : Quaternion.identity;
            GameObject effect = Instantiate(bloodEffectPrefab, hitPoint, rot, parent);
            Destroy(effect, 3f);
        }
    }
}
