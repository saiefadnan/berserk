using UnityEngine;

public class EquipWeapon : MonoBehaviour
{
    [SerializeField] private GameObject weapon;
    [SerializeField] private GameObject equipped;
    [SerializeField] private GameObject unequipped;
    [SerializeField] private GameObject shootingMechanism;
    private Sword sword;
    private ShootMecha shootingMechaComponent;
    [System.NonSerialized] public GameObject currEquipped;
    [System.NonSerialized] public GameObject currUnequipped;
    public static bool equip;
    public bool nextAttack = false;

    [Header("Socket Alignment")]
    private readonly Vector3 defaultLocalPos = new Vector3(-0.22f, 0.60f, 0.90f);
    private readonly Quaternion defaultLocalRot = Quaternion.Euler(61.32f, 161.51f, 74.56f);

    private readonly Vector3 guardLocalPos = new Vector3(0.61f, -0.38f, 0.78f);
    private readonly Quaternion guardLocalRot = Quaternion.Euler(298.64f, 199.55f, 347.32f);

    [Header("Holster Alignment (Back / Shoulder)")]
    private readonly Vector3 unequippedLocalPos = new Vector3(-0.224f, 0.63f, 0.9f);
    private readonly Quaternion unequippedLocalRot = Quaternion.Euler(118.686f, -18.4899f, -105.44f);

    [Header("Two-Handed IK")]
    [SerializeField] private float ikSmoothSpeed = 12f;
    [SerializeField] private Vector3 leftHandGripOffset = new Vector3(0.00096f, 0.00013f, 0.00141f);
    private float currentIKWeight = 0f;
    private Transform leftHandGripPoint;
    private Animator animator;

    [SerializeField] private float socketTransitionSpeed = 16f;
    private bool isGuarding = false;
    private bool isAttacking = false;
    private bool wasAttacking = false;

    private void Awake()
    {
        equip = false;
    }

    void Start()
    {
        equip = false;
        currUnequipped = (unequipped != null && unequipped.transform.childCount > 0) ? unequipped.transform.GetChild(0).gameObject : Instantiate(weapon, unequipped.transform);
        currEquipped = (equipped != null && equipped.transform.childCount > 0) ? equipped.transform.GetChild(0).gameObject : Instantiate(weapon, equipped.transform);

        if (currUnequipped != null)
        {
            currUnequipped.transform.localPosition = unequippedLocalPos;
            currUnequipped.transform.localRotation = unequippedLocalRot;
        }

        currEquipped.SetActive(false);
        currUnequipped.SetActive(true);
        sword = currEquipped.GetComponentInChildren<Sword>();
        if (shootingMechanism != null)
            shootingMechaComponent = shootingMechanism.GetComponent<ShootMecha>();
        animator = GetComponent<Animator>();
        if (currEquipped != null)
        {
            leftHandGripPoint = currEquipped.transform.Find("LeftHandGrip");
            if (leftHandGripPoint == null)
            {
                GameObject gripGo = new GameObject("LeftHandGrip");
                gripGo.transform.SetParent(currEquipped.transform, false);
                leftHandGripPoint = gripGo.transform;
            }
            leftHandGripPoint.localPosition = leftHandGripOffset;
            leftHandGripPoint.localRotation = Quaternion.identity;
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
        // Two-handed grip IK during guard stance and attack swings
        float targetWeight = (equip && (isGuarding || isAttacking) && leftHandGripPoint != null) ? 1f : 0f;
        float blendSpeed = isAttacking ? 30f : ikSmoothSpeed;
        currentIKWeight = Mathf.MoveTowards(currentIKWeight, targetWeight, Time.deltaTime * blendSpeed);
        if (currentIKWeight > 0.001f && leftHandGripPoint != null)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, currentIKWeight);
            animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandGripPoint.position);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);
            animator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandGripPoint.rotation);
        }
        else
        {
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);
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


    public void ForceEquip()
    {
        if (currEquipped == null) return;
        if (!equip) Equip();
    }

    void Equip()
    {
        equip = !equip;

        currEquipped.SetActive(equip);
        currUnequipped.SetActive(!equip);
        if (shootingMechaComponent != null)
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
