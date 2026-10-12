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

    [Header("Guard Socket Alignment")]
    private readonly Vector3 defaultLocalPos = new Vector3(-0.22f, 0.60f, 0.90f);
    private readonly Quaternion defaultLocalRot = Quaternion.Euler(61.32f, 161.51f, 74.56f);

    private readonly Vector3 guardLocalPos = new Vector3(0.61f, -0.38f, 0.78f);
    private readonly Quaternion guardLocalRot = Quaternion.Euler(298.64f, 199.55f, 347.32f);

    [SerializeField] private float guardTransitionSpeed = 16f;
    private bool isGuarding = false;

    public void SetGuarding(bool guarding)
    {
        isGuarding = guarding;
    }

    void LateUpdate()
    {
        if (currEquipped != null && equip)
        {
            Vector3 targetPos = isGuarding ? guardLocalPos : defaultLocalPos;
            Quaternion targetRot = isGuarding ? guardLocalRot : defaultLocalRot;

            currEquipped.transform.localPosition = Vector3.Lerp(currEquipped.transform.localPosition, targetPos, Time.deltaTime * guardTransitionSpeed);
            currEquipped.transform.localRotation = Quaternion.Slerp(currEquipped.transform.localRotation, targetRot, Time.deltaTime * guardTransitionSpeed);
        }
    }

    /// Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currUnequipped = Instantiate(weapon, unequipped.transform);
        currEquipped = Instantiate(weapon, equipped.transform);
        currEquipped.SetActive(false);
        currUnequipped.SetActive(true);
        sword = currEquipped.GetComponentInChildren<Sword>();
        shootingMechaComponent = shootingMechanism.GetComponent<ShootMecha>();
    }

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
