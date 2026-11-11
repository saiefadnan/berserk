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

    [Header("Perception")]
    [Range(30f, 180f)]
    public float viewAngle = 110f;
    public LayerMask obstacleMask;

    [Header("Damage")]
    public float minDamage = 10f;
    public float maxDamage = 25f;         // random per swing — unpredictable!

    [Header("Speed")]
    public float patrolSpeed = 1.2f;
    public float chaseSpeed = 4.5f;
    public float chargeSpeed = 8f;     // lunge speed

    [Header("Tactical Circling")]
    public float combatZoneRadius = 4f;
    public float orbitRadius = 3.2f;
    public float strafeSpeed = 2.0f;

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

    public bool IsDead => (combat != null) ? combat.IsDead : false;
    private bool isStaring;
    private bool isCharging;
    private bool playerInSightRange;
    private bool playerInAttackRange;
    private bool playGroanOnce = true;

    // Hearing
    private bool playerHeard;
    private PlayerMovement playerMovement;
    private EnemyPerception perception;
    private EnemyCombat combat;
    private ExecutionTarget executionTarget;

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
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        agent.avoidancePriority = Random.Range(30, 70);
        // Random start offset so multiple enemies don't sync perfectly
        decisionTimer = Random.Range(0f, DECISION_INTERVAL);
        // Start subtle ambient breathing
        StartCoroutine(AmbientBreathing());
        if (player != null)
        {
           playerMovement = player.GetComponent<PlayerMovement>() ??
           player.GetComponentInParent<PlayerMovement>();
        }
        perception = GetComponent<EnemyPerception>();
        if (perception == null)
        {
            perception = gameObject.AddComponent<EnemyPerception>();
        }
        combat = GetComponent<EnemyCombat>();
        if (combat == null){
            combat = gameObject.AddComponent<EnemyCombat>();
        }
        executionTarget = GetComponent<ExecutionTarget>();
        if (executionTarget == null){
            executionTarget = gameObject.AddComponent<ExecutionTarget>();
        }

        // Guard against stale Inspector radius where combatZone < orbitRadius
        if (combatZoneRadius < orbitRadius + 0.8f)
        {
            combatZoneRadius = orbitRadius + 1.2f;
        }
    }

    private void Start(){
        if (CombatDirector.Instance != null)CombatDirector.Instance.RegisterCombatant(this);
    }

    // ─────────────────────────────────────────────
    //  UPDATE LOOP
    // ─────────────────────────────────────────────
    void Update()
    {
        if ((combat != null && (combat.IsDead || combat.IsFakingDeath || combat.IsKnockedDown)) || (executionTarget != null && (executionTarget.IsStaggered || executionTarget.IsBeingExecuted))) return;

        SyncAnimatorStopState();
        //DetectPlayer();
        if (perception != null){
            perception.UpdatePerception();
            playerInSightRange = perception.CanSeePlayer;
            playerInAttackRange = perception.IsInAttackRange;
            playerHeard = perception.CanHearPlayer;
        }

        decisionTimer -= Time.deltaTime;
        if (decisionTimer <= 0f)
        {
            decisionTimer = DECISION_INTERVAL + Random.Range(-0.5f, 0.5f);
            RollUnpredictableDecision();
        }

        // Sense incoming Dragonslayer swings within weapon reach (3.8m)
        float distToPlayer = (perception != null) ? perception.DistanceToPlayer : (player != null ? Vector3.Distance(transform.position, player.position) : 99f);
        PlayerCombat playerCombat = player != null ? (player.GetComponent<PlayerCombat>() ?? player.GetComponentInParent<PlayerCombat>()) : null;
        if (playerCombat != null && combat!=null && !combat.IsAttacking && (playerCombat.IsAttacking || playerCombat.IsShooting) && distToPlayer <= 3.8f && combat != null)
        {
            combat.TryBlock();
        }

        // Main FSM
        if(distToPlayer <= combatZoneRadius){
            if (combat != null && combat.RequestAttackPermission()){
                if (combat == null || !combat.IsBlocking){
                    if (playerInAttackRange && playerInSightRange){
                        Attack();
                    }else{
                        Chase();
                    }
                }
            }else{
                OrbitPlayer();
            }
        }
        else if (playerInSightRange || playerHeard)
            Chase();
        else
            Patrol();

        // Limp below 40% health
        CheckLimping();
    }

    // ─────────────────────────────────────────────
    //  PATROL — shambling, erratic path
    // ─────────────────────────────────────────────
    private void Patrol()
    {
        playGroanOnce = true;
        agent.speed = patrolSpeed;
        if (perception != null && perception.LastKnownPosition != Vector3.zero)
        {
            SearchWalkPoint(perception.LastKnownPosition);
        }
        else if (!walkPointSet) SearchWalkPoint();

        if(walkPointSet) agent.SetDestination(walkPoint);

        if ((transform.position - walkPoint).magnitude < 1f)
        {
            walkPointSet = false;
            if (perception != null)
            {
                perception.ClearLastKnownPosition();
            }
        }

        animator.SetTrigger("walk");
    }

    private void SearchWalkPoint(Vector3 targetPos = default)
    {
        if(targetPos!=default)
        {
            walkPoint = targetPos;
        }
        else
        {
            float rZ = Random.Range(-walkPointRange, walkPointRange);
            float rX = Random.Range(-walkPointRange, walkPointRange);
            walkPoint = new Vector3(
                transform.position.x + rX,
                transform.position.y,
                transform.position.z + rZ);
        }

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
            AlertNearbyAllies(16f);
        }

        if (Random.value < flankChance)
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


    private void OrbitPlayer(){
        if(player == null) return;
        if(combat!=null && combat.IsAttacking) return;

        float flankAngle = (CombatDirector.Instance != null) ? CombatDirector.Instance.GetAsignedFlankAngle(this) : 0f;
        Vector3 gutsFacing = player.forward;
        gutsFacing.y = 0f;
        gutsFacing.Normalize();

        Vector3 flankDirection = Quaternion.Euler(0f, flankAngle, 0f) * gutsFacing;
        Vector3 desiredPos = player.position + (flankDirection * orbitRadius);

        if(NavMesh.SamplePosition(desiredPos, out NavMeshHit hit, 2.5f, NavMesh.AllAreas)){
            agent.speed = strafeSpeed;
            agent.SetDestination(hit.position);
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

        if(combat!=null && combat.CanAttack){
            combat.PerformAttack(player);
        }
    }

    private void DestroyEnemy() => Destroy(gameObject);

    // ─────────────────────────────────────────────
    //  UNPREDICTABLE DECISIONS  (called every ~3s)
    // ─────────────────────────────────────────────
    private void RollUnpredictableDecision()
    {
        if (!playerInSightRange || IsDead) return;

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
        SetAgentStopped(true);
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

        SetAgentStopped(false);
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
        SetAgentStopped(true);

        // Wind-up pause — very unsettling
        animator.SetTrigger("walk");
        yield return new WaitForSeconds(0.6f);

        // SCREAM then CHARGE
        PlayRandomClip(screamClips);
        SetAgentStopped(false);
        if (agent != null && agent.isActiveAndEnabled &&  agent.isOnNavMesh){
            agent.speed = chargeSpeed;
        }
        animator.SetTrigger("run");

        float elapsed = 0f;
        while (elapsed < 1.5f)
        {
            if (agent != null && agent.isActiveAndEnabled &&  agent.isOnNavMesh){
                agent.speed = chargeSpeed;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (agent != null && agent.isActiveAndEnabled &&  agent.isOnNavMesh){
            agent.speed = chaseSpeed;
        }
        isCharging = false;
        chargeRoutine = null;
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
            if (!IsDead && breathingClips != null && breathingClips.Length > 0)
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
        SetAgentStopped(stateInfo.IsName("hit_react"));
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

    private void AlertNearbyAllies(float radius){
        Collider[] hits = Physics.OverlapSphere(transform.position, radius);
        foreach (Collider hit in hits)
        {
            if(hit.gameObject == gameObject) continue;
            EnemyController ally = hit.GetComponent<EnemyController>() ?? hit.GetComponentInParent<EnemyController>();
            if (ally != null && !ally.IsDead)
            {
                ally.ReceiveAllyAlert(player!=null ? player.position : transform.position);
            }
        }
    }

    public void ReceiveAllyAlert(Vector3 alertPosition){
        if(IsDead) return;
        if(!playerInSightRange || !playerHeard){
            playerHeard = true;
            if(agent!=null && agent.isOnNavMesh){
                agent.SetDestination(alertPosition);
            }
            if(playGroanOnce){
                PlayRandomClip(screamClips);
                playGroanOnce = false;
            }
        }
    }

    private void OnDisable(){
        if (CombatDirector.Instance != null) CombatDirector.Instance.UnregisterCombatant(this);
    }

    private void SetAgentStopped(bool stopped){
        if(agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh){
            agent.isStopped = stopped;
        }
    }
}
