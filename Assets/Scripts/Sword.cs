using System.Collections;
using UnityEngine;

public class Sword : MonoBehaviour
{
    public int damage = 10;
    public ParticleSystem slashEffect;
    [SerializeField] private GameObject flashEffectPrefab;
    [SerializeField] private GameObject bloodEffectPrefab;
    public AudioSource audioSource;
    public AudioClip hitAudio;
    public AudioClip slashSound;
    public TrailRenderer swordTrail;
    private bool cool = false;
    private Animator animator;
    private Coroutine hitStopCoroutine;

    private void Start()
    {
        animator = GetComponentInParent<Animator>();
    }

    // In Inspector: check "Is Trigger" on the Box Collider
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            EnemyController enemy = other.GetComponent<EnemyController>() ?? other.GetComponentInParent<EnemyController>();
            if (enemy != null && cool)
            {
                // No ContactPoint available with triggers, use transform instead
                Vector3 hitPoint = other.ClosestPoint(transform.position);
                Vector3 hitNormal = (hitPoint - transform.position).normalized;

                PlayBloodSplatter(hitPoint, hitNormal);
                PlaySparkFlash(transform.position, other.transform.position);
                PlayHitAudio();
                Vector3 slashDirection = transform.forward;
                enemy.TakeDamage(damage, slashDirection, hitPoint);

                // Guts Combat Juice: Micro-hitstop & heavy impact camera shake
                Animator enemyAnim = enemy.GetComponentInChildren<Animator>();
                TriggerHitStop(0.07f, enemyAnim);
                if (CameraShake.Instance != null)
                {
                    CameraShake.Instance.Shake(0.16f, 0.20f);
                }
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
        if (animator != null) animator.speed = 1.0f;
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

    public void PlayHitAudio()
    {
        if (audioSource != null && hitAudio != null)
            audioSource.PlayOneShot(hitAudio);
    }

    public void EnableDamage()
    {
        cool = true;
        AnimateSlash();
        if (swordTrail != null) swordTrail.emitting = true;
        Invoke(nameof(DisableDamage), 0.5f);
    }

    void DisableDamage()
    {
        cool = false;
        if (swordTrail != null) swordTrail.emitting = false;
    }

    public void AudioSlash()
    {
        // if (audioSource != null && slashSound != null)
        //     audioSource.PlayOneShot(slashSound);
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
