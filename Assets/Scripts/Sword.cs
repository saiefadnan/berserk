using UnityEngine;

public class Sword : MonoBehaviour
{
    public int damage = 10;
    public ParticleSystem slashEffect;
    [SerializeField] private GameObject flashEffectPrefab;
    public AudioSource audioSource;
    public AudioClip hitAudio;
    public AudioClip slashSound;
    private bool cool = false;
    private Animator animator;

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
            }
        }
    }

    public void PlaySparkFlash(Vector3 hitPos, Vector3 enemyPos)
    {
        Vector3 dir = (hitPos - enemyPos).normalized;
        GameObject effect = Instantiate(
            flashEffectPrefab,
            hitPos,
            Quaternion.LookRotation(dir)
        );
        Destroy(effect, 2f);
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
        Invoke(nameof(DisableDamage), 0.5f);
    }
    void DisableDamage()
    {
        cool = false;
    }

    public void AudioSlash()
    {
        if (audioSource != null && slashSound != null)
            audioSource.PlayOneShot(slashSound);
    }
    public void AnimateSlash()
    {
        if (audioSource != null && slashSound != null)
            audioSource.PlayOneShot(slashSound);

        if (slashEffect != null)
        {
            slashEffect.Play(true);
        }
    }
}
