using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class ExecutionHandler : MonoBehaviour
{
    [Header("Execution Settings")]
    [SerializeField] private KeyCode executionKey = KeyCode.F;
    [SerializeField] private float executionRange = 2.8f;
    [SerializeField] private float sweetSpotDistance = 1.4f;
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
    private PlayerMovement playerMovement;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        health = GetComponent<Health>();
        beast = GetComponent<BeastOfDarkness>();
        combat = GetComponent<PlayerCombat>();
        playerMovement = GetComponent<PlayerMovement>();
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
        if (playerMovement != null) playerMovement.enabled = false;

        // 1. Align Guts with the kneeling target
        Vector3 dirToTarget = (target.transform.position - transform.position);
        dirToTarget.y = 0;
        if (dirToTarget != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(dirToTarget);
            Vector3 sweetSpot = target.transform.position - (dirToTarget.normalized * sweetSpotDistance);
            sweetSpot.y = transform.position.y;
            if (characterController != null) characterController.enabled = false;
            transform.position = sweetSpot;
            if (characterController != null)characterController.enabled = true;
        }

        Camera cam = Camera.main;
        if (cam != null){
            StartCoroutine(CinematicCameraSequence(cam, target.transform, cam.fieldOfView));
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
        if (playerMovement != null) playerMovement.enabled = true;
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

    private IEnumerator CinematicCameraSequence(Camera cam, Transform targetEnemy, float originalFOV){
        Vector3 initialLocalPos = cam.transform.localPosition;
        Quaternion initialLocalRot = cam.transform.localRotation;

        Vector3 rightOffset = transform.right * 1.5f + Vector3.up * 0.9f - transform.forward * 1.8f;
        Vector3 cinematicWorldPos = transform.position + rightOffset;
        Vector3 lookTarget = (transform.position + targetEnemy.position) * 0.5f + Vector3.up * 1.1f;
        Quaternion cinematicWorldRot = Quaternion.LookRotation(lookTarget - cinematicWorldPos);

        float elapsed = 0f;
        float transitionIn = 0.22f;
        Vector3 startWorldPos = cam.transform.position;
        Quaternion startWorldRot = cam.transform.rotation;

        while(elapsed < transitionIn){
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / transitionIn);
            cam.transform.position = Vector3.Lerp(cam.transform.position, cinematicWorldPos, t);
            cam.transform.rotation = Quaternion.Slerp(startWorldRot, cinematicWorldRot, t);
            cam.fieldOfView = Mathf.Lerp(originalFOV, 46f, t);
            yield return null;
        }
        yield return new WaitForSecondsRealtime(1f);

        elapsed = 0f;
        float transitionOut = 0.35f;
        Vector3 returnStartPos = cam.transform.position;
        Quaternion returnStartRot = cam.transform.rotation;

        while(elapsed < transitionOut){
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / transitionOut);
            Vector3 targetWorldPos = cam.transform.parent != null ?
            cam.transform.parent.TransformPoint(initialLocalPos)
            : returnStartPos;
            Quaternion targetWorldRot = cam.transform.parent != null ?
            cam.transform. parent.rotation * initialLocalRot
            : returnStartRot;
            cam.transform.position = Vector3.Lerp(returnStartPos, targetWorldPos, t);
            cam.transform.rotation = Quaternion.Slerp(returnStartRot, targetWorldRot, t);
            cam.fieldOfView = Mathf.Lerp(46f, originalFOV, t);
            yield return null;
        }

        cam.transform.localPosition = initialLocalPos;
        cam.transform.localRotation = initialLocalRot;
        cam.fieldOfView = originalFOV;
    }
}
