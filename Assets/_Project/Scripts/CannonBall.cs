using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CannonBall : MonoBehaviour {

    [Header("Explosion Settings")]
    [SerializeField] private float blastRadius = 3.5f;
    [SerializeField] private float blastDamage = 85f;
    [SerializeField] private float explosionForce = 18f;
    [SerializeField] private LayerMask hitLayers = ~0;

    [Header("VFX & Audio")]
    [SerializeField] private GameObject explosionVfxPrefab;
    [SerializeField] private AudioClip explosionAudio;
    [SerializeField] private float lifetime = 5f;

    [HideInInspector] public GameObject owner;

    private Rigidbody rb;
    private bool hasExploded = false;

    private void Awake(){
        rb = GetComponent<Rigidbody>();
        Destroy(gameObject, lifetime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(hasExploded) return;
        ContactPoint contact = collision.GetContact(0);
        Explode(contact.point, contact.normal);
    }

    private void Explode(Vector3 impactPoint, Vector3 impactNormal){
        hasExploded = true;

        if(explosionVfxPrefab!=null){
            Quaternion rot = Quaternion.LookRotation(impactNormal);
            GameObject vfx = Instantiate(explosionVfxPrefab, impactPoint, rot);
            vfx.transform.localScale = Vector3.one * 2.5f;
            Destroy(vfx, 3f);
        }

        if(explosionAudio != null){
            AudioSource.PlayClipAtPoint(explosionAudio, impactPoint, 1.0f);
        }

        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.Shake(0.25f, 0.35f);
        }

        Collider[] hits = Physics.OverlapSphere(impactPoint, blastRadius, hitLayers, QueryTriggerInteraction.Ignore);
        HashSet<GameObject> hitVictims = new HashSet<GameObject>();

        foreach(Collider col in hits){
            if(col == null) continue;
            if(owner != null && (col.gameObject == owner || col.transform.IsChildOf(owner.transform))) continue;

            IDamageable damageable = col.GetComponent<IDamageable>() ?? col.GetComponentInParent<IDamageable>();

            if(damageable != null){
                if(!hitVictims.Contains(damageable.victim)){
                    hitVictims.Add(damageable.victim);

                    Vector3 blastDir = (damageable.victim.transform.position - impactPoint).normalized;
                    if (blastDir == Vector3.zero) blastDir = -impactNormal;
                    blastDir += Vector3.up * 0.4f;

                    DamageData data = new DamageData
                    {
                        amount = blastDamage,
                        hitDirection = blastDir.normalized,
                        hitPoint = col.ClosestPoint(impactPoint),
                        attacker = owner != null ? owner : gameObject,
                        reactionType = HitReactionType.CannonballKnockdown,
                        knockbackForce = explosionForce
                    };

                    damageable.TakeDamage(data);
                }
            }

            if (col != null)
            {
                Rigidbody hitRb = col.attachedRigidbody;
                if(hitRb != null && !hitRb.isKinematic){
                    hitRb.AddExplosionForce(explosionForce, impactPoint, blastRadius, 1.2f, ForceMode.Impulse);
                }
            }
        }
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, blastRadius);
    }
}
