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

    [Header("Blocking Tuning")]
    [Tooltip("Chance (0-1) enemy blocks when player is attacking")]
    public float blockChance = 0.3f;
    public float blockDuration = 1.0f;
    [SerializeField] private AudioClip clashAudio;
    [SerializeField] private GameObject sparkPrefab;


    public bool IsDead => isDead;
    public bool IsFakingDeath => isFakingDeath;
    public bool CanAttack => !alreadyAttacked && !isDead && !isFakingDeath && !isBlocking && !isKnockedDown;
    public bool IsBlocking => isBlocking;
    public bool IsAttacking => alreadyAttacked;
    public bool IsKnockedDown => isKnockedDown;

    private bool alreadyAttacked;
    private bool isDead;
    private bool isFakingDeath;
    private bool isBlocking;
    private bool isKnockedDown = false;
    private Coroutine blockCoroutine;
    private Coroutine knockdownRoutine;

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

        // Distance & tactical selection:
        // dist > 2.2m: Lunging Gap-Closer (attack2)
        // close range: 60% Light Slash (attack0), 40% Heavy Overhead Cleave (attack1)
        float dist = Vector3.Distance(transform.position, playerTarget.position);
        int attackIndex = (dist > 2.2f) ? 2 : (Random.value < 0.6f ? 0 : 1);

        animator.ResetTrigger("run");
        animator.SetInteger("attackIndex", attackIndex);
        animator.SetTrigger("attack");

        StartCoroutine(AttackDamageRoutine(playerTarget, attackIndex));

        alreadyAttacked = true;
        float cooldown = (attackIndex == 1) ? 1.8f : timeBetweenAttacks;
        Invoke(nameof(ResetAttack), cooldown);
    }


    public bool RequestAttackPermission(){
        if (!CanAttack) return false;
        if (CombatDirector.Instance == null) return true;
        return CombatDirector.Instance.RequestAttackToken(this);
    }
    private IEnumerator AttackDamageRoutine(Transform playerTarget, int attackIndex)
    {
        // Custom wind-up delay and reach per attack
        float delay = 0.30f;
        float reach = 2.5f;
        float dmg = Random.Range(minDamage, maxDamage + 1);

        if (attackIndex == 1) // Heavy Overhead Cleave
        {
            delay = 0.55f;
            reach = 2.6f;
            dmg *= 1.4f; // Crushing heavy blow
        }
        else if (attackIndex == 2) // Lunging Gap-Closer
        {
            delay = 0.40f;
            reach = 3.2f; // Extended lunge reach
            dmg *= 1.15f;
        }

        yield return new WaitForSeconds(delay);

        if (isDead || isFakingDeath || playerTarget == null) yield break;

        float currentDist = Vector3.Distance(transform.position, playerTarget.position);
        if (currentDist <= reach)
        {
            IDamageable target = playerTarget.GetComponentInParent<IDamageable>() ?? playerTarget.GetComponent<IDamageable>();
            if (target != null)
            {
                DamageData data = new DamageData
                {
                    amount = dmg,
                    hitDirection = transform.forward,
                    hitPoint = playerTarget.position + Vector3.up * 1.2f,
                    attacker = gameObject
                };
                target.TakeDamage(data);
                Debug.Log($"<color=orange>[ENEMY ATTACK {attackIndex}] Landed {dmg:F1} damage on Player!</color>");
            }
        }
    }

    public void TryBlock(){
        if(isDead || isFakingDeath || isBlocking) return;
        if (Random.value < blockChance){
            if(blockCoroutine!=null){
                StopCoroutine(blockCoroutine);
            }
            blockCoroutine = StartCoroutine(BlockRoutine());
        }
    }

    private IEnumerator BlockRoutine(){
        isBlocking = true;
        if (healthComponent != null) healthComponent.damageReductionPercent = 0.85f; // Mitigate 85% damage!
        animator.SetTrigger("block");

        yield return new WaitForSeconds(blockDuration);
        if (healthComponent != null) healthComponent.damageReductionPercent = 0f;
        isBlocking = false;
        blockCoroutine = null;
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

        if(isBlocking){
            if (audioSource != null && clashAudio != null)
                audioSource.PlayOneShot(clashAudio);
            Debug.Log("<color=yellow><b>[ENEMY BLOCK] Deflected Dragonslayer blow with Shield/Sword Block!</b></color>");
            return;
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null && !rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        if(data.reactionType == HitReactionType.Knockdown){
            TriggerKnockdown(data);
        }else{
            animator.SetTrigger("hit_react");
            if (agent != null && agent.enabled){
                float push = data.knockbackForce > 0f ? data.knockbackForce * 0.15f : 0.4f;
                agent.Move(data.hitDirection * push);
            }
        }
    }

    private void TriggerKnockdown(DamageData data){
        if(knockdownRoutine != null){
            StopCoroutine(knockdownRoutine);
        }
        knockdownRoutine = StartCoroutine(KnockdownRoutine(data));
        if (CombatDirector.Instance != null){
            CombatDirector.Instance.ReleaseAttackToken(this);
        }
    }

    private IEnumerator KnockdownRoutine(DamageData data){
        isKnockedDown = true;
        if(agent!=null && agent.enabled){
            agent.isStopped = true;
            agent.ResetPath();
        }

        animator.SetTrigger("knock_down");

        Vector3 pushDir = data.hitDirection;
        pushDir.y = 0f;
        float force = data.knockbackForce > 0f ? data.knockbackForce : 6f;

        float elapsed = 0f;
        float slideDuration = 0.35f;
        while(elapsed < slideDuration){
            float speed = Mathf.Lerp(force, 0f, elapsed/slideDuration);
            if(agent!=null && agent.enabled){
                agent.Move(pushDir * (speed * Time.deltaTime));
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        yield return new WaitForSeconds(2.2f);
        if (!isDead && !isFakingDeath){
            if(agent!=null && agent.enabled){
                agent.isStopped = true;
                isKnockedDown = false;
            }
        }
        knockdownRoutine = null;
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
        ExecutionTarget execTarget = GetComponent<ExecutionTarget>();
        bool isExecuted = execTarget != null && execTarget.IsBeingExecuted;
        if (!isExecuted && !killedByBeast && !isFakingDeath && Random.value < fakeDeathChance)
        {
            StartCoroutine(FakeDeathRoutine(data.attacker != null ? data.attacker.transform : null));
        }
        else
        {
            float launchMultiplier = killedByBeast ? 2.8f : 1.0f;
            Die(data.hitDirection, data.hitPoint, launchMultiplier);
        }
    }

    private void ResetAttack(){
        alreadyAttacked = false;
        if (CombatDirector.Instance != null){
            CombatDirector.Instance.ReleaseAttackToken(this);
        }
    }

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
        if (CombatDirector.Instance != null){
            CombatDirector.Instance.ReleaseAttackToken(this);
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

        // Fallback: find player if attacker was null
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
        }

        // Only get back up if player is nearby
        if (playerTransform != null && Vector3.Distance(transform.position, playerTransform.position) < 18f)
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
