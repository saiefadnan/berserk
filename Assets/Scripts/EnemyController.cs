using UnityEngine;
using UnityEngine.AI;
using System.Collections;

/// <summary>
/// ZombieEnemy - Smart, horrifying, creepy, unpredictable and hard to beat.
/// Drop-in replacement for your original EnemyController.
/// </summary>
public class EnemyController : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  REFERENCES
    // ─────────────────────────────────────────────
    [Header("References")]
    public NavMeshAgent agent;
    public Transform player;
    public LayerMask whatIsGround, whatIsPlayer;
    public AudioSource audioSource;
    public AudioClip groan;
    public AudioClip[] screamClips;     // assign 2-3 horror scream clips
    public AudioClip[] breathingClips;  // assign creepy breathing clips
    public ParticleSystem bloodSplatter;

    // ─────────────────────────────────────────────
    //  STATS
    // ─────────────────────────────────────────────
    [Header("Stats")]
    private Health healthComponent;
    public float walkPointRange = 10f;
    public float sightRange = 18f;
    public float attackRange = 2.2f;
    public float timeBetweenAttacks = 1.4f;

    [Header("Damage")]
    public int minDamage = 10;
    public int maxDamage = 25;         // random per swing — unpredictable!

    [Header("Speed")]
    public float patrolSpeed = 1.2f;
    public float chaseSpeed = 4.5f;
    public float chargeSpeed = 8f;     // lunge speed
    // ─────────────────────────────────────────────
    //  BEHAVIOUR TUNING
    // ─────────────────────────────────────────────
    [Header("Creepy Behaviour")]
    [Tooltip("Chance (0-1) enemy stops and STARES at player instead of chasing")]
    public float stareChance = 0.15f;

    [Tooltip("How long it stares before resuming")]
    public float stareDuration = 2.5f;

    [Tooltip("Chance (0-1) enemy does a surprise CHARGE lunge")]
    public float chargeChance = 0.2f;

    [Tooltip("Chance (0-1) enemy fakes death then gets back up")]
    public float fakeDeathChance = 0.35f;

    [Tooltip("Chance (0-1) enemy flanks player instead of running straight")]
    public float flankChance = 0.25f;

    [Tooltip("Max distance at which the enemy can HEAR the player (footsteps etc)")]
    public float hearingRange = 10f;

    // ─────────────────────────────────────────────
    //  PRIVATE STATE
    // ─────────────────────────────────────────────
    private Animator animator;
    private AnimatorStateInfo stateInfo;

    private Vector3 walkPoint;
    private bool walkPointSet;

    private bool alreadyAttacked;
    private bool dead;
    private bool fakingDeath;
    private bool isStaring;
    private bool isCharging;
    private bool playerInSightRange;
    private bool playerInAttackRange;
    private bool playGroanOnce = true;

    // Hearing
    private bool playerHeard;

    // Limping (below 40% health)
    private bool isLimping;

    // Coroutine handles
    private Coroutine stareRoutine;
    private Coroutine chargeRoutine;
    private Coroutine ambushRoutine;

    // How often to re-roll unpredictable decisions
    private float decisionTimer;
    private const float DECISION_INTERVAL = 3f;

    // ─────────────────────────────────────────────
    //  INIT
    // ─────────────────────────────────────────────
    private void Awake()
    {
        player = GameObject.Find("player").transform;
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        healthComponent = GetComponent<Health>();
        agent.speed = patrolSpeed;
        // Random start offset so multiple enemies don't sync perfectly
        decisionTimer = Random.Range(0f, DECISION_INTERVAL);
        // Start subtle ambient breathing
        StartCoroutine(AmbientBreathing());
    }

    // ─────────────────────────────────────────────
    //  UPDATE LOOP
    // ─────────────────────────────────────────────
    void Update()
    {
        if (dead && !fakingDeath) return;
        if (fakingDeath) return; // coroutine handles fake-death timing

        SyncAnimatorStopState();
        DetectPlayer();

        decisionTimer -= Time.deltaTime;
        if (decisionTimer <= 0f)
        {
            decisionTimer = DECISION_INTERVAL + Random.Range(-0.5f, 0.5f);
            RollUnpredictableDecision();
        }

        // Main FSM
        if (playerInAttackRange && playerInSightRange)
            Attack();
        else if (playerInSightRange || playerHeard)
            Chase();
        else
            Patrol();

        // Limp below 40% health
        CheckLimping();
    }

    // ─────────────────────────────────────────────
    //  DETECTION
    // ─────────────────────────────────────────────
    private void DetectPlayer()
    {
        playerInSightRange = Physics.CheckSphere(transform.position, sightRange, whatIsPlayer);
        playerInAttackRange = Physics.CheckSphere(transform.position, attackRange, whatIsPlayer);
        playerHeard = Physics.CheckSphere(transform.position, hearingRange, whatIsPlayer);
    }

    // ─────────────────────────────────────────────
    //  PATROL — shambling, erratic path
    // ─────────────────────────────────────────────
    private void Patrol()
    {
        playGroanOnce = true;
        agent.speed = patrolSpeed;

        if (!walkPointSet) SearchWalkPoint();
        else agent.SetDestination(walkPoint);

        if ((transform.position - walkPoint).magnitude < 1f)
            walkPointSet = false;

        animator.SetTrigger("walk");
    }

    private void SearchWalkPoint()
    {
        float rZ = Random.Range(-walkPointRange, walkPointRange);
        float rX = Random.Range(-walkPointRange, walkPointRange);
        walkPoint = new Vector3(
            transform.position.x + rX,
            transform.position.y,
            transform.position.z + rZ);

        if (Physics.Raycast(walkPoint, -transform.up, 2f, whatIsGround))
            walkPointSet = true;
    }

    // ─────────────────────────────────────────────
    //  CHASE
    // ─────────────────────────────────────────────
    private void Chase()
    {
        if (isStaring || isCharging) return;

        agent.speed = chaseSpeed;

        if (playGroanOnce)
        {
            PlayRandomClip(screamClips);
            playGroanOnce = false;
        }

        if (Random.value < flankChance && !alreadyAttacked)
        {
            // Flank: aim slightly to the side of the player
            Vector3 flankOffset = Quaternion.Euler(0, Random.Range(-45f, 45f), 0)
                                  * (transform.position - player.position).normalized
                                  * 2f;
            agent.SetDestination(player.position + flankOffset);
        }
        else
        {
            agent.SetDestination(player.position);
        }

        LookAtPlayerSmooth();
        animator.SetTrigger("run");
    }

    // ─────────────────────────────────────────────
    //  ATTACK
    // ─────────────────────────────────────────────
    private void Attack()
    {
        agent.SetDestination(transform.position);
        LookAtPlayerSmooth();

        if (!alreadyAttacked)
        {
            animator.ResetTrigger("run");
            animator.SetTrigger("attack");

            // Deal random damage to the player
            int dmg = Random.Range(minDamage, maxDamage + 1);
            IDamageable target = player.GetComponentInParent<IDamageable>() ?? player.GetComponent<IDamageable>();
            if(target!=null)
            {
                DamageData data = new DamageData
                {
                    amount = dmg,
                    hitDirection = transform.forward,
                    hitPoint = player.position,
                    attacker = gameObject
                };
                target.TakeDamage(data);
            }

            alreadyAttacked = true;
            Invoke(nameof(ResetAttack), timeBetweenAttacks);
        }
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
        if (dead) return;

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
        if (dead) return;

        if (!fakingDeath && Random.value < fakeDeathChance)
        {
            StartCoroutine(FakeDeath());
        }
        else
        {
            Die(data.hitDirection, data.hitPoint);
        }
    }

    private void ResetAttack() => alreadyAttacked = false;
    // ─────────────────────────────────────────────
    //  DEATH
    // ─────────────────────────────────────────────
    private void Die(Vector3 hitDirection = default, Vector3 hitPoint = default)
    {
        dead = true;
        StopAllCoroutines();
        if (agent != null)
        {
            agent.enabled = false;
        }
        if (animator != null)
        {
            animator.enabled = false;
        }
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;

            Vector3 forceDir = (hitDirection != default) ? hitDirection.normalized : -transform.forward;

            Vector3 launchVelocity = forceDir * 6f + Vector3.up * 1f;
            rb.AddForce(launchVelocity, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * 1f, ForceMode.Impulse);
        }
        Invoke(nameof(DestroyEnemy), 10f);
    }

    private void DestroyEnemy() => Destroy(gameObject);

    // ─────────────────────────────────────────────
    //  UNPREDICTABLE DECISIONS  (called every ~3s)
    // ─────────────────────────────────────────────
    private void RollUnpredictableDecision()
    {
        if (!playerInSightRange || dead) return;

        float roll = Random.value;

        if (roll < stareChance && stareRoutine == null)
        {
            stareRoutine = StartCoroutine(CreepyStare());
        }
        else if (roll < stareChance + chargeChance && chargeRoutine == null)
        {
            chargeRoutine = StartCoroutine(ChargeAttack());
        }
    }

    // ─────────────────────────────────────────────
    //  COROUTINE: CREEPY STARE
    //  Enemy stops dead, turns to face player, does nothing.
    // ─────────────────────────────────────────────
    private IEnumerator CreepyStare()
    {
        isStaring = true;
        agent.isStopped = true;
        animator.SetTrigger("walk"); // idle-ish pose

        float elapsed = 0f;
        // Play a low breathing sound
        PlayRandomClip(breathingClips);

        while (elapsed < stareDuration)
        {
            LookAtPlayerSmooth();
            elapsed += Time.deltaTime;
            yield return null;
        }

        agent.isStopped = false;
        isStaring = false;
        stareRoutine = null;
    }

    // ─────────────────────────────────────────────
    //  COROUTINE: CHARGE LUNGE
    //  Brief pause → sudden burst of speed toward player
    // ─────────────────────────────────────────────
    private IEnumerator ChargeAttack()
    {
        isCharging = true;
        agent.isStopped = true;

        // Wind-up pause — very unsettling
        animator.SetTrigger("walk");
        yield return new WaitForSeconds(0.6f);

        // SCREAM then CHARGE
        PlayRandomClip(screamClips);
        agent.isStopped = false;
        agent.speed = chargeSpeed;
        animator.SetTrigger("run");

        float elapsed = 0f;
        while (elapsed < 1.5f)
        {
            agent.SetDestination(player.position);
            elapsed += Time.deltaTime;
            yield return null;
        }

        agent.speed = chaseSpeed;
        isCharging = false;
        chargeRoutine = null;
    }

    // ─────────────────────────────────────────────
    //  COROUTINE: FAKE DEATH
    //  Falls down, waits, then gets back up when player is close
    // ─────────────────────────────────────────────
    private IEnumerator FakeDeath()
    {
        fakingDeath = true;
        agent.isStopped = true;
        animator.SetTrigger("knock_down");

        // Wait a random 3-7 seconds
        float wait = Random.Range(3f, 7f);
        yield return new WaitForSeconds(wait);

        // Only get back up if player is nearby
        if (Vector3.Distance(transform.position, player.position) < sightRange)
        {
            fakingDeath = false;
            dead = false;
            agent.isStopped = false;
            animator.SetTrigger("hit_react"); // "getting up" animation
            PlayRandomClip(screamClips);
        }
        else
        {
            // Player walked away — actually die
            Die();
        }
    }

    // ─────────────────────────────────────────────
    //  COROUTINE: AMBIENT BREATHING
    //  Plays creepy breathing sounds at random intervals
    // ─────────────────────────────────────────────
    private IEnumerator AmbientBreathing()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(4f, 10f));
            if (!dead && breathingClips != null && breathingClips.Length > 0)
            {
                float dist = Vector3.Distance(transform.position, player.position);
                if (dist < sightRange * 1.5f)
                    PlayRandomClip(breathingClips);
            }
        }
    }

    // ─────────────────────────────────────────────
    //  LIMPING — below 40% health
    // ─────────────────────────────────────────────
    private void CheckLimping()
    {
        if (healthComponent != null && (healthComponent.CurrentHealth / healthComponent.MaxHealth) < 0.4f && !isLimping)
        {
            isLimping = true;
            chaseSpeed = Mathf.Max(chaseSpeed - 1.5f, 2f);
            agent.speed = chaseSpeed;
        }
    }

    // ─────────────────────────────────────────────
    //  HELPERS
    // ─────────────────────────────────────────────
    private void SyncAnimatorStopState()
    {
        stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        agent.isStopped = stateInfo.IsName("hit_react");
    }

    private void LookAtPlayerSmooth()
    {
        Vector3 lookDir = new Vector3(player.position.x, transform.position.y, player.position.z);
        Quaternion target = Quaternion.LookRotation(lookDir - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, Time.deltaTime * 8f);
    }

    private void PlayRandomClip(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0 || audioSource == null) return;
        if (audioSource.isPlaying) return; // don't interrupt an ongoing clip
        audioSource.clip = clips[Random.Range(0, clips.Length)];
        audioSource.Play();
    }

    // ─────────────────────────────────────────────
    //  GIZMOS
    // ─────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, hearingRange);
    }
}