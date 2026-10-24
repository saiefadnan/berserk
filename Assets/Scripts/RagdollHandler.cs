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

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        agent = GetComponent<NavMeshAgent>();
        healthComponent = GetComponent<Health>();
        rootCollider = GetComponent<Collider>();
        rootRb = GetComponent<Rigidbody>();

        boneRigidbodies = GetComponentsInChildren<Rigidbody>();
        boneColliders = GetComponentsInChildren<Collider>();

        SetRagdollActive(false);
    }

    public void SetRagdollActive(bool active)
    {
        if (animator != null) animator.enabled = !active;
        if (agent != null) agent.enabled = !active;

        if (boneRigidbodies != null)
        {
            foreach (Rigidbody rb in boneRigidbodies)
            {
                if (rb == null || rb == rootRb) continue;
                rb.isKinematic = !active;
            }
        }

        if (boneColliders != null)
        {
            foreach (Collider col in boneColliders)
            {
                if (col == null || col == rootCollider) continue;
                col.enabled = active;
            }
        }

        if (rootCollider != null) rootCollider.enabled = !active;
        if (rootRb != null) rootRb.isKinematic = true;
    }

    public void TriggerRagdoll(Vector3 hitDirection)
    {
        SetRagdollActive(true);
        if (boneRigidbodies != null && boneRigidbodies.Length > 0)
        {
            Rigidbody impactBone = null;
            if (boneRigidbodies.Length > 1 && boneRigidbodies[1] != null && boneRigidbodies[1] != rootRb)
            {
                impactBone = boneRigidbodies[1]; // Usually Hips
            }
            else
            {
                for (int i = 0; i < boneRigidbodies.Length; i++)
                {
                    if (boneRigidbodies[i] != null && boneRigidbodies[i] != rootRb)
                    {
                        impactBone = boneRigidbodies[i];
                        break;
                    }
                }
            }

            if (impactBone != null)
            {
                Vector3 force = (hitDirection != default ? hitDirection.normalized : -transform.forward) * 16f + Vector3.up * 4f;
                impactBone.AddForce(force, ForceMode.Impulse);
            }
        }
    }
}
