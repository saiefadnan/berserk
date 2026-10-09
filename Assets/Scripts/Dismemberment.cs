using UnityEngine;

public class Dismemberment: MonoBehaviour
{
    private CharacterJoint joint;
    private Rigidbody rb;
    [SerializeField]private GameObject bloodVfx;
    private ParticleSystem[] bloodEffects;
    void Start(){
        joint = GetComponent<CharacterJoint>();
        rb = GetComponent<Rigidbody>();
        bloodEffects = bloodVfx.GetComponentsInChildren<ParticleSystem>();
    }

    public void OnTriggerEnter(Collider other){
        // if(joint){
        //     joint.connectedBody = null;
        //     Destroy(joint);
        // }
        // transform.parent = null;
        //  if (rb)
        // {
        //     rb.isKinematic = false;
        //     rb.interpolation = RigidbodyInterpolation.None;
        //     rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        // }

        // // Optional: Add a small force so it "pops off"
        // rb.AddForce(Random.onUnitSphere * 3f, ForceMode.Impulse);
        transform.localScale = Vector3.zero; 
        Destroy(this);
        bloodVfx.transform.localPosition = transform.localPosition;
    }
    public void playBloodAnim(){
        // foreach (var effect in bloodEffects){
        //     effect.Play();
        // }
    }
}
