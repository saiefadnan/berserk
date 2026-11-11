using UnityEngine;

public class EnemyWeapon: MonoBehaviour{
    [SerializeField] private float damage = 10f;
    [SerializeField] private TrailRenderer weaponTrail;
    private bool cool = false;
    private EnemyController combatOwner;


    private void Awake()
    {
        combatOwner = GetComponent<EnemyController>() ?? GetComponentInParent<EnemyController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("player damage triggered..." + other.name);
        if(!cool || other.transform.root == transform.root) return;
        if(combatOwner == null || combatOwner.IsDead) return;
        IDamageable damageable = other.GetComponent<IDamageable>() ?? other.GetComponentInParent<IDamageable>();
        if(damageable==null) return;
        Vector3 hitPoint = other.ClosestPoint(transform.position);
        Vector3 pushDirection = (other.transform.position - transform.root.position);
        pushDirection.y = 0f;
        DamageData data = new DamageData{
            amount = damage,
            hitPoint = hitPoint,
            hitDirection = pushDirection,
            attacker = transform.root.gameObject,
            reactionType = HitReactionType.SwordMedium
        };

        damageable.TakeDamage(data);
    }

    public void EnableDamage(){
        cool = true;
        if(weaponTrail!=null)weaponTrail.emitting = true;
        Invoke(nameof(DisableDamage), 0.5f);
    }

    public void DisableDamage(){
        if(weaponTrail!=null)weaponTrail.emitting = false;
        cool = false;
    }
}
