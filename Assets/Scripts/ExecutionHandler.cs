using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class ExecutionHandler : MonoBehaviour
{
    [Header("Execution Settings")]
    [SerializeField] private KeyCode executionKey = KeyCode.F;
    [SerializeField] private float executionRange = 2.8f;
    [SerializeField] private float terrorRadius = 6.5f;

    [Header("Audio (Optional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip executionWhoosh;

    public bool IsExecuting { get; private set; }

    private Animator animator;
    private Health health;
    private BeastOfDarkness beast;
    private PlayerCombat combat;
    private CharacterController characterController;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        health = GetComponent<Health>();
        beast = GetComponent<BeastOfDarkness>();
        combat = GetComponent<PlayerCombat>();
        characterController = GetComponent<CharacterController>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        if (IsExecuting) return;

        // Check for staggered enemies in front of Guts
        ExecutionTarget nearestTarget = FindNearestStaggeredTarget();

        if (nearestTarget != null && Input.GetKeyDown(executionKey))
        {
            StartCoroutine(ExecuteRoutine(nearestTarget));
        }
    }

    private ExecutionTarget FindNearestStaggeredTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, executionRange);
        ExecutionTarget bestTarget = null;
        float closestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            ExecutionTarget target = hit.GetComponent<ExecutionTarget>() ?? hit.GetComponentInParent<ExecutionTarget>();
            if (target != null && target.IsStaggered && !target.IsBeingExecuted)
            {
                Vector3 toTarget = (target.transform.position - transform.position).normalized;
                toTarget.y = 0;

                // Must be facing the enemy
                if (Vector3.Dot(transform.forward, toTarget) > 0.35f)
                {
                    float dist = Vector3.Distance(transform.position, target.transform.position);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        bestTarget = target;
                    }
                }
            }
        }
        return bestTarget;
    }

    private IEnumerator ExecuteRoutine(ExecutionTarget target)
    {
        IsExecuting = true;

        // 1. Align Guts with the kneeling target
        Vector3 dirToTarget = (target.transform.position - transform.position);
        dirToTarget.y = 0;
        if (dirToTarget != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(dirToTarget);
        }

        // 2. Trigger Guts Great Sword Downward Cleave
        if (animator != null)
        {
            animator.SetTrigger("execute");
        }

        if (audioSource != null && executionWhoosh != null)
        {
            audioSource.PlayOneShot(executionWhoosh);
        }

        // 3. Cinematic Slow-Motion at blade apex (~0.35s in)
        yield return new WaitForSeconds(0.35f);

        Time.timeScale = 0.20f;
        yield return new WaitForSecondsRealtime(0.35f);
        Time.timeScale = 1.0f;

        // 4. Fatal Cleave Impact!
        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.Shake(0.38f, 0.45f);
        }

        // Trigger target decapitation & fatal death
        if (target != null && !target.IsBeingExecuted)
        {
            target.OnExecuted(transform.forward);
        }

        // 5. Guts Rewards: Health + Beast Rage
        if (health != null)
        {
            health.Heal(25f);
        }

        if (beast != null)
        {
            beast.AddRage(35f);
        }

        // 6. Crowd Reaction: Enemies freeze in terror
        ApplyCrowdTerror(target != null ? target.gameObject : null);

        // 7. Follow-through recovery
        yield return new WaitForSeconds(0.65f);

        IsExecuting = false;
    }

    private void ApplyCrowdTerror(GameObject executedEnemy)
    {
        Collider[] surrounding = Physics.OverlapSphere(transform.position, terrorRadius);
        foreach (var col in surrounding)
        {
            if (col.gameObject == executedEnemy || col.transform.root == transform.root) continue;

            EnemyController controller = col.GetComponent<EnemyController>() ?? col.GetComponentInParent<EnemyController>();
            if (controller != null && !controller.IsDead)
            {
                Animator enemyAnim = controller.GetComponentInChildren<Animator>();
                if (enemyAnim != null)
                {
                    enemyAnim.SetTrigger("hit_react");
                }

                NavMeshAgent agent = controller.GetComponent<NavMeshAgent>();
                if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
                {
                    StartCoroutine(PauseAgentForTerror(agent, 1.5f));
                }
            }
        }
    }

    private IEnumerator PauseAgentForTerror(NavMeshAgent agent, float duration)
    {
        if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh) yield break;
        agent.isStopped = true;
        yield return new WaitForSeconds(duration);
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }
    }
}
