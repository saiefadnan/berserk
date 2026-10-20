using UnityEngine;
using UnityEngine.AI;
using System.Collections;

/// <summary>
/// ZombieEnemy - Smart, horrifying, creepy, unpredictable and hard to beat.
/// Drop-in replacement for your original EnemyController.
/// </summary>
public class EnemyController : MonoBehaviour, IDamageable
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
    public float health = 150f;
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

    [Header("Dismemberment Setup")]
    [SerializeField] private Transform headBone;        //
    [SerializeField] private GameObject helmetProp;     // Paladin_J_Nordstrom_Helmet(if equipped)
    [SerializeField] private GameObject weaponProp;
    [SerializeField] private GameObject shieldProp;
    [SerializeField] private GameObject stumpBloodFountainPrefab;
    [SerializeField] private GameObject chunkExplosionPrefab;
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
        if (headBone == null && animator != null && animator.isHuman)
        {
            headBone = animator.GetBoneTransform(HumanBodyBones.Head);
        }
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

    public SurfaceType surfaceType => SurfaceType.Flesh;
    public GameObject victim => gameObject;
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
            if(player.TryGetComponent(out IDamageable target))
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

    private void ResetAttack() => alreadyAttacked = false;

    // ─────────────────────────────────────────────
    //  TAKE DAMAGE
    // ─────────────────────────────────────────────

    public void TakeDamage(DamageData data)
    {
        TakeDamage(data.amount, data.hitDirection, data.hitPoint);
    }
    public void TakeDamage(int damage, Vector3 hitDirection = default, Vector3 hitPoint = default)
    {
        if (dead) return;

        // Stop physics drift
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null && !rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        health -= damage;
        if (health <= 0)
        {
            // Maybe fake death?
            if (!fakingDeath && Random.value < fakeDeathChance)
                StartCoroutine(FakeDeath());
            else
                Die(hitDirection, hitPoint);
        }
        else
        {
            animator.SetTrigger("hit_react");
        }
    }

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
        TriggerDismemberment(hitDirection, hitPoint);
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

    private void TriggerDismemberment(Vector3 hitDirection, Vector3 hitPoint)
    {
        if (weaponProp != null)
        {
            DetachAndLaunchProp(weaponProp, hitDirection * 2f + Vector3.up * 2f, 5f);
        }
        if (shieldProp != null)
        {
            DetachAndLaunchProp(shieldProp, hitDirection * 2f + Vector3.up * 2f, 5f);
        }

        bool hitHeadZone = (headBone != null) && (hitPoint == default || hitPoint.y > (headBone.position.y - 0.65f));

        if (hitHeadZone)
        {
            Decapitate(hitDirection);
        }


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

    private void Decapitate(Vector3 hitDirection)
    {
        // Detach helmet if present
        if (helmetProp != null)
        {
            Vector3 launchVelocity = (hitDirection.normalized * 3f) + (Vector3.up * 1.5f);
            DetachAndLaunchProp(helmetProp, launchVelocity, 6f);
        }
        if(chunkExplosionPrefab!=null && headBone!=null){
            GameObject chunks = Instantiate(chunkExplosionPrefab,headBone.position, Quaternion.LookRotation(hitDirection.normalized));
            Destroy(chunks, 1f);
        }if(stumpBloodFountainPrefab!=null){
            Transform stump = (headBone!=null && headBone.parent!=null)?headBone.parent:transform;
            GameObject fountain = Instantiate(stumpBloodFountainPrefab, stump.position, Quaternion.LookRotation(stump.up), stump);
            Destroy(fountain, 4f);
        }
        if (headBone != null)
        {
            headBone.localScale = Vector3.zero;
        }
    }

    private void DetachAndLaunchProp(GameObject prop, Vector3 launchVelocity, float torqueForce)
    {
        if (prop == null) return;
        SkinnedMeshRenderer smr = prop.GetComponent<SkinnedMeshRenderer>();
        if (smr != null)
        {
            UnityEngine.Mesh bakedMesh = new UnityEngine.Mesh();
            smr.BakeMesh(bakedMesh);
            Material[] mats = smr.sharedMaterials;
            Vector3 meshCenter = bakedMesh.bounds.center;
            Vector3[] verts = bakedMesh.vertices;
            for (int i = 0; i < verts.Length; i++)
            {
                verts[i] -= meshCenter;
            }
            bakedMesh.vertices = verts;
            bakedMesh.RecalculateBounds();
            bakedMesh.RecalculateNormals();

            smr.enabled = false;
            Destroy(smr);

            MeshFilter mf = prop.AddComponent<MeshFilter>();
            mf.sharedMesh = bakedMesh;

            MeshRenderer mr = prop.AddComponent<MeshRenderer>();
            mr.materials = mats;

            prop.transform.position += prop.transform.TransformDirection(meshCenter);
        }
        prop.transform.SetParent(null);
        Rigidbody rb = prop.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = prop.AddComponent<Rigidbody>();
        }
        rb.isKinematic = false;
        rb.useGravity = true;
        Collider col = prop.GetComponent<Collider>();
        if (col == null)
        {
            BoxCollider box = prop.AddComponent<BoxCollider>();
            box.size = new Vector3(0.3f, 0.3f, 0.3f);
        }
        rb.AddForce(launchVelocity, ForceMode.Impulse);
        rb.AddTorque(Random.insideUnitSphere * torqueForce, ForceMode.Impulse);
        Destroy(prop, 10f);
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
        health = 1f; // survive on 1 hp
        agent.isStopped = true;
        animator.SetTrigger("knock_down");

        // Wait a random 3-7 seconds
        float wait = Random.Range(3f, 7f);
        yield return new WaitForSeconds(wait);

        // Only get back up if player is nearby
        if (Vector3.Distance(transform.position, player.position) < sightRange)
        {
            health = 50f; // revive with partial HP
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
        if (health / 150f < 0.4f && !isLimping)
        {
            isLimping = true;
            chaseSpeed = Mathf.Max(chaseSpeed - 1.5f, 2f);
            agent.speed = chaseSpeed;
            // Optionally set animator bool: animator.SetBool("isLimping", true);
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