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
        // Two-handed attack animations already drive both hands naturally on the hilt.
        // IK is only needed during guard stance.
        float targetWeight = (equip && isGuarding && leftHandGripPoint != null) ? 1f : 0f;
        currentIKWeight = Mathf.MoveTowards(currentIKWeight, targetWeight, Time.deltaTime * 10f);
        if (currentIKWeight > 0.001f && leftHandGripPoint != null)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, currentIKWeight);
            animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandGripPoint.position);
        }
        else
        {
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);
        }
    }
    void LateUpdate()
    {
        if (currEquipped != null && equip)
        {
            if (isGuarding)
            {
                currEquipped.transform.localPosition = Vector3.Lerp(currEquipped.transform.localPosition, guardLocalPos, Time.deltaTime * socketTransitionSpeed);
                currEquipped.transform.localRotation = Quaternion.Slerp(currEquipped.transform.localRotation, guardLocalRot, Time.deltaTime * socketTransitionSpeed);
            }
            else if (isAttacking)
            {
                // Locked into solid hand grip during all attack swings
                currEquipped.transform.localPosition = defaultLocalPos;
                currEquipped.transform.localRotation = defaultLocalRot;
            }
            else
            {
                // Smoothly return from guard if we were guarding, otherwise maintain default grip
                currEquipped.transform.localPosition = Vector3.Lerp(currEquipped.transform.localPosition, defaultLocalPos, Time.deltaTime * socketTransitionSpeed);
                currEquipped.transform.localRotation = Quaternion.Slerp(currEquipped.transform.localRotation, defaultLocalRot, Time.deltaTime * socketTransitionSpeed);
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
