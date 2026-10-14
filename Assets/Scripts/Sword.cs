using System.Collections;
using UnityEngine;

public class Sword : MonoBehaviour
{
    public int damage = 10;
    public ParticleSystem slashEffect;
    [SerializeField] private GameObject flashEffectPrefab;
    public AudioSource audioSource;
    public AudioClip hitAudio;
    public AudioClip slashSound;
    public TrailRenderer swordTrail;
    private bool cool = false;
    private Animator animator;
    private Coroutine hitStopCoroutine;

    // In Inspector: check "Is Trigger" on the Box Collider
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            EnemyController enemy = other.GetComponent<EnemyController>() ?? other.GetComponentInParent<EnemyController>();
            if (enemy != null && cool)
            {
                // No ContactPoint available with triggers, use transform instead
                PlaySparkFlash(transform.position, other.transform.position);
                PlayHitAudio();
                enemy.TakeDamage(damage);

                // Guts Combat Juice: Micro-hitstop & heavy impact camera shake
                TriggerHitStop(0.06f);
                if (CameraShake.Instance != null)
                {
                    CameraShake.Instance.Shake(0.16f, 0.20f);
                }
            }
        }
    }

    public void TriggerHitStop(float duration)
    {
        if (hitStopCoroutine != null) StopCoroutine(hitStopCoroutine);
        hitStopCoroutine = StartCoroutine(DoHitStop(duration));
    }

    private IEnumerator DoHitStop(float duration)
    {
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1.0f;
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
}
