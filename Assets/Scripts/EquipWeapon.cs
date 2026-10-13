using UnityEngine;

public class EquipWeapon : MonoBehaviour
{
    [SerializeField] private GameObject weapon;
    [SerializeField] private GameObject equipped;
    [SerializeField] private GameObject unequipped;
    [SerializeField] private GameObject shootingMechanism;
    private Sword sword;
    private ShootMecha shootingMechaComponent;
    GameObject currEquipped;
    GameObject currUnequipped;
    public static bool equip;
    public bool nextAttack = false;

    [Header("Socket Alignment")]
    private readonly Vector3 defaultLocalPos = new Vector3(-0.22f, 0.60f, 0.90f);
    private readonly Quaternion defaultLocalRot = Quaternion.Euler(61.32f, 161.51f, 74.56f);

    private readonly Vector3 guardLocalPos = new Vector3(0.61f, -0.38f, 0.78f);
    private readonly Quaternion guardLocalRot = Quaternion.Euler(298.64f, 199.55f, 347.32f);

    // private readonly Vector3 attackLocalPos = new Vector3(-0.947f, -0.057f, -0.940f);
    private readonly Vector3 attackLocalPos = new Vector3(-0.562f, 0.008f, -1.037f);
    private readonly Quaternion attackLocalRot = Quaternion.Euler(31.00f, 19.85f, 100.88f);
    [Header("Two-Handed IK")]
    [SerializeField] private float ikSmoothSpeed = 12f;
    private float currentIKWeight = 0f;
    private Transform leftHandGripPoint;
    private Animator animator;

    [SerializeField] private float socketTransitionSpeed = 16f;
    private bool isGuarding = false;
    private bool isAttacking = false;
    private bool wasAttacking = false;

    void Start()
    {
        currUnequipped = Instantiate(weapon, unequipped.transform);
        currEquipped = Instantiate(weapon, equipped.transform);
        currEquipped.SetActive(false);
        currUnequipped.SetActive(true);
        sword = currEquipped.GetComponentInChildren<Sword>();
        shootingMechaComponent = shootingMechanism.GetComponent<ShootMecha>();
        animator = GetComponent<Animator>();
        if (currEquipped != null)
        {
            leftHandGripPoint = currEquipped.transform.Find("LeftHandGrip");
        }
    }

    public void SetGuarding(bool guarding)
    {
        isGuarding = guarding;
    }

    public void SetAttacking(bool attacking)
    {
        isAttacking = attacking;
    }
    void OnAnimatorIK(int layerIndex)
    {
        if (animator == null) return;
        float targetWeight = (equip && (isAttacking || isGuarding) && leftHandGripPoint != null) ? 1f : 0f;
        currentIKWeight = Mathf.MoveTowards(currentIKWeight, targetWeight, Time.deltaTime * 10f);
        if (currentIKWeight > 0.001f && leftHandGripPoint != null)
        {
            Debug.Log("Setting IK for left hand with weight: " + currentIKWeight);
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, currentIKWeight);
            // animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, currentIKWeight);
            animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandGripPoint.position);
            // animator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandGripPoint.rotation);
        }
        else
        {
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);
            // animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);
        }
    }
    void LateUpdate()
    {
        if (currEquipped != null && equip)
        {
            if (isAttacking)
            {
                // Snap directly into grip during attack swings for solid hold
                currEquipped.transform.localPosition = attackLocalPos;
                currEquipped.transform.localRotation = attackLocalRot;
            }
            else
            {
                Vector3 targetPos = isGuarding ? guardLocalPos : defaultLocalPos;
                Quaternion targetRot = isGuarding ? guardLocalRot : defaultLocalRot;

                currEquipped.transform.localPosition = Vector3.Lerp(currEquipped.transform.localPosition, targetPos, Time.deltaTime * socketTransitionSpeed);
                currEquipped.transform.localRotation = Quaternion.Slerp(currEquipped.transform.localRotation, targetRot, Time.deltaTime * socketTransitionSpeed);
            }
            wasAttacking = isAttacking;
        }
    }

    /// Start is called once before the first execution of Update after the MonoBehaviour is created


    void Equip()
    {
        equip = !equip;

        currEquipped.SetActive(equip);
        currUnequipped.SetActive(!equip);
        shootingMechaComponent.enabled = !equip;
    }

    void PlaySlashAudio()
    {
        if (currEquipped != null)
        {
            if (sword) sword.AudioSlash();
        }
    }

    void PlaySlashAnim()
    {
        if (currEquipped != null)
        {
            if (sword) sword.AnimateSlash();
        }
    }

    void EnableDamage()
    {
        Debug.Log("damage enabled...");
        if (currEquipped != null)
        {
            if (sword) sword.EnableDamage();
        }
    }

    void EnableNextMove()
    {
        nextAttack = true;
        Debug.Log("Next attack enabled");
    }

    void DisableNextMove()
    {
        nextAttack = false;
        Debug.Log("Next attack disabled");
    }

}
