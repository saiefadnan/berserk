using UnityEngine;

/// <summary>
/// ZombieEnemy - Smart, horrifying, creepy, unpredictable and hard to beat.
/// Drop-in replacement for your original EnemyController.
/// </summary>
public class EnemyPerception : MonoBehaviour{

    [Header("Ranges")]
    public float sightRange = 18f;
    public float attackRange = 2.2f;
    public float hearingRange = 10f;

    [Header("Vision Cone & Obstacles")]
    [Range(30f, 180f)]
    public float viewAngle = 110f;
    public LayerMask obstacleMask;
    public LayerMask whatIsGround;

    [Header("Target")]
    public Transform player;

    // Public Perception States (Read by AI Brain)
    public bool CanSeePlayer { get; private set; }
    public bool IsInAttackRange { get; private set; }
    public bool CanHearPlayer { get; private set; }
    public float DistanceToPlayer { get; private set; } = float.MaxValue;
    public Vector3 LastKnownPosition { get; private set; }

    private Animator animator;
    private PlayerMovement playerMovement;

    private void Awake()
    {

        animator = GetComponentInChildren<Animator>();
        if (player == null)
        {

            GameObject pObj = GameObject.FindWithTag("Player") ??
            GameObject.Find("player_controller") ?? GameObject.Find("player");
            if (pObj != null)
            {
                player = pObj.transform;
            }
        }
        if(player != null){
            playerMovement = player.GetComponent<PlayerMovement>() ??
            player.GetComponentInParent<PlayerMovement>();
        }
    }


    // ─────────────────────────────────────────────
    //  PERCEPTION
    // ─────────────────────────────────────────────
    public void UpdatePerception()
    {
        if(player == null) return;
        DistanceToPlayer = Vector3.Distance(player.position, transform.position);
        IsInAttackRange = (DistanceToPlayer <= attackRange);

        AnimatorStateInfo stateinfo = (animator != null)? animator.GetCurrentAnimatorStateInfo(0) : default;
        bool isSprinting = (playerMovement != null && playerMovement.isRunning);
        bool isHitReact = stateinfo.IsName("hit_react");

        CanHearPlayer =  (isSprinting && DistanceToPlayer <= hearingRange) || isHitReact;
        CanSeePlayer =  IsInAttackRange && CanSeePlayer;

        if(isHitReact)
        {
            LastKnownPosition = player.position;
        }
        if(DistanceToPlayer <= sightRange)
        {
            Vector3 dirToPlayer = (player.position - transform.position).normalized;
            dirToPlayer.y = 0;

            float angle = Vector3.Angle(transform.forward, dirToPlayer);
            if(angle < viewAngle / 2f)
            {
                Vector3 eyeOrigin = transform.position + Vector3.up * 1.5f;
                Vector3 playerTarget = player.position + Vector3.up * 1.2f;
                Vector3 rayDir = (playerTarget - eyeOrigin).normalized;

                LayerMask blockMask = (obstacleMask != default)? obstacleMask : whatIsGround;

                if(!Physics.Raycast(eyeOrigin, rayDir, DistanceToPlayer, blockMask))
                {
                    CanSeePlayer = true;
                    LastKnownPosition = player.position;
                }
            }
        }
    }

    public void ClearLastKnownPosition(){
        LastKnownPosition = Vector3.zero;
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

        Vector3 leftRay = Quaternion.Euler(0, -viewAngle / 2f, 0) * transform.forward;
        Vector3 rightRay = Quaternion.Euler(0, viewAngle / 2f, 0) * transform.forward;

        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position + Vector3.up, leftRay * sightRange);
        Gizmos.DrawRay(transform.position + Vector3.up, rightRay * sightRange);
    }
}
