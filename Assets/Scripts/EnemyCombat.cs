using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EnemyCombat : MonoBehaviour {

    [Header("Audio (Optional)")]
    public AudioSource audioSource;
    public AudioClip[] screamClips;     // assign 2-3 horror scream clips

    [Header("Damage & Timing")]
    public float minDamage = 10f;
    public float maxDamage = 25f;         // random per swing — unpredictable!
    public float timeBetweenAttacks = 1.4f;

    [Header("Behavior Tuning")]
    [Tooltip("Chance (0-1) enemy fakes death then gets back up")]
    public float fakeDeathChance = 0.35f;

    public bool IsDead => isDead;
    public bool IsFakingDeath => isFakingDeath;
    public bool CanAttack => !alreadyAttacked && !isDead && !isFakingDeath;

    private bool alreadyAttacked;
    private bool isDead;
    private bool isFakingDeath;

    private Health healthComponent;
    private Animator animator;
    private RagdollHandler ragdollHandler;
    private NavMeshAgent agent;

    private void Awake()
    {
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        healthComponent = GetComponent<Health>();
        ragdollHandler = GetComponent<RagdollHandler>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }


    // ─────────────────────────────────────────────
    //  ATTACK
    // ─────────────────────────────────────────────
    public void PerformAttack(Transform playerTarget)
    {
        if (!CanAttack || playerTarget == null) return;

        agent.SetDestination(transform.position);


            animator.ResetTrigger("run");
            animator.SetTrigger("attack");

            // Deal random damage to the player
            float dmg = Random.Range(minDamage, maxDamage + 1);
            IDamageable target = playerTarget.GetComponentInParent<IDamageable>() ?? playerTarget.GetComponent<IDamageable>();
            AnimatorStateInfo stateinfo = animator.GetCurrentAnimatorStateInfo(0);
            if(target!=null && stateinfo.IsName("attack0"))
            {
                DamageData data = new DamageData
                {
                    amount = dmg,
                    hitDirection = transform.forward,
                    hitPoint = playerTarget.position,
                    attacker = gameObject
                };
                target.TakeDamage(data);
            }

            alreadyAttacked = true;
            Invoke(nameof(ResetAttack), timeBetweenAttacks);
    }

    private void OnEnable()
    {
        if (healthComponent != null)
        {
            healthComponent.OnDamaged += HandleDamaged;
            healthComponent.OnDeath += HandleDeath;
        }
    }

    private void OnDisable()
    {
        if (healthComponent != null)
        {
            healthComponent.OnDamaged -= HandleDamaged;
            healthComponent.OnDeath -= HandleDeath;
        }
    }

    private void HandleDamaged(DamageData data)
    {
        if (isDead) return;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null && !rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        animator.SetTrigger("hit_react");
    }

    private void HandleDeath(DamageData data)
    {
        if (isDead) return;
        bool killedByBeast = false;

        if (data.attacker != null)
        {
            BeastOfDarkness beast = data.attacker.GetComponent<BeastOfDarkness>();
            if (beast != null && beast.IsActive)
            {
                killedByBeast = true;
                // Visceral Vampirism: Carnage feasting (+20 HP burst on kill)
                Health killerHealth = data.attacker.GetComponent<Health>();
                if (killerHealth != null)
                {
                    killerHealth.Heal(20f);
                    Debug.Log("<color=red><b>[BEAST FEAST] +20 HP absorbed from kill!</b></color>");
                }
            }
        }

        if (!killedByBeast && !isFakingDeath && Random.value < fakeDeathChance)
        {
            StartCoroutine(FakeDeathRoutine(data.attacker != null ? data.attacker.transform : null));
        }
        else
        {
            float launchMultiplier = killedByBeast ? 2.8f : 1.0f;
            Die(data.hitDirection, data.hitPoint, launchMultiplier);
        }
    }

    private void ResetAttack() => alreadyAttacked = false;
    // ─────────────────────────────────────────────
    //  DEATH
    // ─────────────────────────────────────────────
    private void Die(Vector3 hitDirection = default, Vector3 hitPoint = default, float forceMultiplier = 1.0f)
    {
        isDead = true;
        StopAllCoroutines();
        if (audioSource != null && audioSource.isPlaying) audioSource.Stop();
        if (agent != null && agent.enabled) agent.enabled = false;

        // Trigger ragdoll ONLY on actual death!
        if (ragdollHandler != null)
        {
            ragdollHandler.TriggerRagdoll(hitDirection, forceMultiplier);
        }
        Invoke(nameof(DestroyEnemy), 10f);
    }

    private void DestroyEnemy() => Destroy(gameObject);


    // ─────────────────────────────────────────────
    //  COROUTINE: FAKE DEATH
    //  Falls down, waits, then gets back up when player is close
    // ─────────────────────────────────────────────
    private IEnumerator FakeDeathRoutine(Transform playerTransform)
    {
        isFakingDeath = true;
        if (audioSource != null && audioSource.isPlaying) audioSource.Stop();
        if (agent != null && agent.enabled) agent.isStopped = true;
        animator.SetTrigger("knock_down");

        // Wait a random 3-7 seconds
        float wait = Random.Range(3f, 7f);
        yield return new WaitForSeconds(wait);

        // Only get back up if player is nearby
        if (Vector3.Distance(transform.position, playerTransform.position) < 18f)
        {
            isFakingDeath = false;
            isDead = false;
            if (agent != null && agent.enabled) agent.isStopped = false;
            if (healthComponent != null) healthComponent.Revive(40);
            animator.SetTrigger("hit_react");
            if (audioSource != null && screamClips != null && screamClips.Length > 0){
                audioSource.clip = screamClips[Random.Range(0, screamClips.Length)];
                audioSource.Play();
            }
        }
        else
        {
            Die();
        }
    }

}
