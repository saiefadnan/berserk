using UnityEngine;

public class EquipWeapon : MonoBehaviour
{
    [SerializeField] private GameObject weapon;
    [SerializeField] private GameObject equipped;
    [SerializeField] private GameObject unequipped;
    [SerializeField] private GameObject shootingMechanism;
    
    GameObject currEquipped;
    GameObject currUnequipped;
    public static bool equip;
    public bool nextAttack = false;

    /// Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currUnequipped=Instantiate(weapon,unequipped.transform);
    }

    void Equip(){
        if(!equip){
            Destroy(currEquipped);
            equip=true;
            currEquipped=Instantiate(weapon,equipped.transform);
            Destroy(currUnequipped);
            shootingMechanism.GetComponent<ShootMecha>().enabled=false;
        }
        else{
            Destroy(currUnequipped);
            equip=false;
            currUnequipped=Instantiate(weapon,unequipped.transform);
            Destroy(currEquipped);
            shootingMechanism.GetComponent<ShootMecha>().enabled=true;
        }
    }

    void PlaySlashAudio(){
        if (currEquipped != null)
        {
            Sword swd = currEquipped.GetComponentInChildren<Sword>();
            if (swd != null) swd.AudioSlash();
        }
    }
    
    void PlaySlashAnim(){
        if (currEquipped != null)
        {
            Sword swd = currEquipped.GetComponentInChildren<Sword>();
            if (swd != null) swd.AnimateSlash();
        }
    }

    void EnableDamage(){
        Debug.Log("damage enabled...");
        if (currEquipped != null)
        {
            Sword swd = currEquipped.GetComponentInChildren<Sword>();
            if (swd != null) swd.EnableDamage();
        }
    }

    void EnableNextMove(){
        nextAttack = true;
        Debug.Log("Next attack enabled");   
    }

    void DisableNextMove(){
        nextAttack = false;
        Debug.Log("Next attack disabled");
    }

}
