
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Health))]
public class RagdollHandler : MonoBehaviour
{
    private Rigidbody[] boneRigidbodies;
    private Collider[] boneColliders;
    private Animator animator;
    private NavMeshAgent agent;
    private Health healthComponent;
    private Collider rootCollider;
    private Rigidbody rootRb;

    private void Awake(){
        animator = GetComponentInChildren<Animator>();
        agent = GetComponent<NavMeshAgent>();
        healthComponent = GetComponent<Health>();
        rootCollider = GetComponent<Collider>();
        rootRb = GetComponent<Rigidbody>();

        boneRigidbodies = GetComponentsInChildren<Rigidbody>();
        boneColliders = GetComponentsInChildren<Collider>();

        SetRagdollActive(false);
    }


    public void SetRagdollActive(bool active){
        if (animator != null) animator.enabled = !active;
        if (agent != null) agent.enabled = !active;

        foreach(Rigidbody rb in boneRigidbodies){
            if(rb == rootRb) continue;
            rb.isKinematic = !active;
        }

        foreach (Collider col in boneColliders)
            {
                if (col == rootCollider) continue;
                col.enabled = active;
            }

        if (rootCollider != null) rootCollider.enabled = !active;
            if (rootRb != null) rootRb.isKinematic = true;
    }

    public void  TriggerRagdoll(Vector3 hitDirection){
        SetRagdollActive(true);
        if(boneRigidbodies.Length > 1){
            Rigidbody impactBone = boneRigidbodies[1]; // Usually Hips
            Vector3 force = (hitDirection != default ? hitDirection.normalized : -transform.forward) * 16f + Vector3.up * 4f;
            impactBone.AddForce(force, ForceMode.Impulse);
        }
    }

}
