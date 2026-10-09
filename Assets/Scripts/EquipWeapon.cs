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
