using UnityEngine;

public class ArrowHit : MonoBehaviour
{
    [SerializeField] private Rigidbody rb;
    private bool hit=false;
    private Transform hitArea;
    public GameObject bloodVfx;
    
    void OnCollisionEnter(Collision obj){
        if(hit)return;
        hit=true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        IDamageable damageable = obj.gameObject.GetComponent<IDamageable>() ?? obj.gameObject.GetComponentInParent<IDamageable>();
        if(damageable!=null && damageable.victim == transform.root.gameObject) return;
       
            rb.isKinematic = true;
            transform.position = obj.contacts[0].point - obj.contacts[0].normal* 0.01f;
            
            if(damageable.surfaceType == SurfaceType.Flesh && bloodVfx!=null) {
                Vector3 pos = obj.contacts[0].point + obj.contacts[0].normal * 0.02f;
                Quaternion rotation = Quaternion.LookRotation(obj.contacts[0].normal);
                GameObject blood = Instantiate(bloodVfx, pos, rotation);
                hitArea = obj.transform.Find("mixamorig:Hips").Find("mixamorig:Spine").Find("hitArea");
                transform.SetParent(hitArea);
                blood.transform.SetParent(hitArea);
                playBloodAnim(blood);
            }

            DamageData data = new DamageData
            {
                amount = 20,
                hitDirection = transform.forward,
                hitPoint = obj.contacts[0].point,
                attacker = gameObject
            };
            damageable.TakeDamage(data);
    }

    public void playBloodAnim(GameObject blood){
        ParticleSystem[] bloodEffects = blood.GetComponentsInChildren<ParticleSystem>();
        foreach (var effect in bloodEffects){
            effect.Play();
        }
    }
}
