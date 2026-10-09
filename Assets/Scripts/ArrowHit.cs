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
        if(!obj.gameObject.CompareTag("Arrow")){
            rb.isKinematic = true;
            transform.position = obj.contacts[0].point - obj.contacts[0].normal* 0.01f;
            
            if(obj.gameObject.CompareTag("Enemy")) {
                Vector3 pos = obj.contacts[0].point + obj.contacts[0].normal * 0.02f;
                Quaternion rotation = Quaternion.LookRotation(obj.contacts[0].normal);
                GameObject blood = Instantiate(bloodVfx, pos, rotation);
                hitArea = obj.transform.Find("mixamorig:Hips").Find("mixamorig:Spine").Find("hitArea");
                transform.SetParent(hitArea);
                blood.transform.SetParent(hitArea);
                playBloodAnim(blood);
                obj.gameObject.GetComponent<EnemyController>().TakeDamage(20);
            }
        }

        // transform.rotation = Quaternion.LookRotation(-obj.contacts[0].normal);
        // transform.SetParent(obj.transform,true);
    }

    public void playBloodAnim(GameObject blood){
        ParticleSystem[] bloodEffects = blood.GetComponentsInChildren<ParticleSystem>();
        foreach (var effect in bloodEffects){
            effect.Play();
        }
    }
}
