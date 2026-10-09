using UnityEngine;

public class MouseTarget : MonoBehaviour
{
    public static MouseTarget Instance;
    [SerializeField] private GameObject Aim;
    [SerializeField] private Camera mainCamera;
    public static RaycastHit raycastHit;
    public Vector3 GetHitPoint()=> raycastHit.point;
    
    void Awake(){
        Instance = this;
    }

    void Update()
    {   
        bool isAiming = Input.GetMouseButton(1);
        Debug.Log("Right Click Held: " + isAiming);
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if(Physics.Raycast(ray, out raycastHit) && isAiming && !EquipWeapon.equip){
            Aim.SetActive(true);
            Aim.transform.position = raycastHit.point;
        }else{
            Aim.SetActive(false);
        }
    }
}



