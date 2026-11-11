using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Health))]
public class DismembermentReceiver : MonoBehaviour
{
    [Header("Bones (Auto-discovered if left empty)")]
    [SerializeField] private Transform headBone;
    [SerializeField] private Transform leftArmBone;
    [SerializeField] private Transform rightArmBone;
    [SerializeField] private Transform leftLegBone;
    [SerializeField] private Transform rightLegBone;

    [Header("Equipment Props (Detachable)")]
    [SerializeField] private GameObject helmetProp;
    [SerializeField] private GameObject shieldProp;
    [SerializeField] private GameObject weaponProp;
    [SerializeField] private GameObject[] extraProps;

    [Header("VFX Prefabs")]
    [SerializeField] private GameObject stumpBloodFountainPrefab;
    [SerializeField] private GameObject chunkExplosionPrefab;

    private Health healthComponent;
    private Animator animator;
    private readonly HashSet<GameObject> detachedProps = new HashSet<GameObject>();

    private void Awake()
    {
        healthComponent = GetComponent<Health>();
        animator = GetComponentInChildren<Animator>();

        if (animator != null && animator.isHuman)
        {
            if (headBone == null) headBone = animator.GetBoneTransform(HumanBodyBones.Head);
            if (leftArmBone == null) leftArmBone = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            if (rightArmBone == null) rightArmBone = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            if (leftLegBone == null) leftLegBone = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
            if (rightLegBone == null) rightLegBone = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
        }
    }

    private void OnEnable()
    {
        if (healthComponent != null)
        {
            healthComponent.OnDeath += HandleDeath;
        }
    }

    private void OnDisable()
    {
        if (healthComponent != null)
        {
            healthComponent.OnDeath -= HandleDeath;
        }
    }

    private void HandleDeath(DamageData data)
    {
        TriggerDismemberment(data.hitDirection, data.hitPoint);
    }

    private void TriggerDismemberment(Vector3 hitDirection, Vector3 hitPoint)
    {
        float headDist = (headBone != null && hitPoint != default) ? Vector3.Distance(hitPoint, headBone.position) : float.MaxValue;
        float leftArmDist = (leftArmBone != null && hitPoint != default) ? Vector3.Distance(hitPoint, leftArmBone.position) : float.MaxValue;
        float rightArmDist = (rightArmBone != null && hitPoint != default) ? Vector3.Distance(hitPoint, rightArmBone.position) : float.MaxValue;
        float leftLegDist = (leftLegBone != null && hitPoint != default) ? Vector3.Distance(hitPoint, leftLegBone.position) : float.MaxValue;
        float rightLegDist = (rightLegBone != null && hitPoint != default) ? Vector3.Distance(hitPoint, rightLegBone.position) : float.MaxValue;

        bool hitHeadZone = (headBone != null) && (hitPoint == default || hitPoint.y > (headBone.position.y - 0.55f) || (headDist < leftArmDist && headDist < rightArmDist && headDist < 0.6f));
        bool hitLeftArm = (leftArmBone != null) && (leftArmDist <= rightArmDist) && (leftArmDist < 1.1f);
        bool hitRightArm = (rightArmBone != null) && (rightArmDist < 1.1f);
        bool hitLeftLeg = (leftLegBone != null) && (leftLegDist <= rightLegDist) && (leftLegDist < 1.2f || hitPoint.y < (leftLegBone.position.y + 0.35f));
        bool hitRightLeg = (rightLegBone != null) && (rightLegDist < 1.2f || hitPoint.y < (rightLegBone.position.y + 0.35f));
        if (hitHeadZone)
        {
            Decapitate(hitDirection);
            DropEquipment(hitDirection * 0.5f);
        }
        if (hitLeftArm)
        {
            SeverLimb(leftArmBone, shieldProp, hitDirection);
            if (weaponProp != null && weaponProp != rightArmBone?.gameObject) DetachAndLaunchProp(weaponProp, hitDirection * 0.5f + Vector3.up * 1f, 3f);
        }
        if (hitRightArm)
        {
            SeverLimb(rightArmBone, weaponProp, hitDirection);
            if (shieldProp != null && shieldProp != leftArmBone?.gameObject) DetachAndLaunchProp(shieldProp, hitDirection * 0.5f + Vector3.up * 1f, 3f);
        }
        if (hitLeftLeg)
        {
            SeverLimb(leftLegBone, null, hitDirection);
            DropEquipment(hitDirection * 0.5f);
        }
        if (hitRightLeg)
        {
            SeverLimb(rightLegBone, null, hitDirection);
            DropEquipment(hitDirection * 0.5f);
        }
        else
        {
            DropEquipment(hitDirection);
        }

        // Launch any extra armor props
        if (extraProps != null)
        {
            foreach (GameObject prop in extraProps)
            {
                if (prop != null) DetachAndLaunchProp(prop, hitDirection * 1.5f + Vector3.up * 1.5f, 4f);
            }
        }
    }

    private void SpawnGoreVFX(Transform bone, Vector3 hitDirection)
    {
        if (bone == null) return;

        if (chunkExplosionPrefab != null)
        {
            GameObject chunks = Instantiate(chunkExplosionPrefab, bone.position, Quaternion.LookRotation(hitDirection.normalized));
            Destroy(chunks, 1f);
        }

        if (stumpBloodFountainPrefab != null)
        {
            Transform stump = (bone.parent != null) ? bone.parent : transform;
            GameObject fountain = Instantiate(stumpBloodFountainPrefab, bone.position, Quaternion.LookRotation(bone.forward), stump);
            Destroy(fountain, 4f);
        }
    }

    private void SeverLimb(Transform limbBone, GameObject attachedProp, Vector3 hitDirection)
    {
        if (limbBone == null) return;

        // Pop the shield/sword off the severed arm (guard against skeleton bone)
        if (attachedProp != null && attachedProp != limbBone.gameObject)
        {
            Vector3 launchVelocity = (hitDirection.normalized * 3f) + (Vector3.up * 1.5f);
            DetachAndLaunchProp(attachedProp, launchVelocity, 6f);
        }

        SpawnGoreVFX(limbBone, hitDirection);

        // Cleanly shrink the arm bone (and its forearm/hand children) to zero
        limbBone.localScale = Vector3.zero;
    }

    public void DropEquipment(Vector3 hitDirection)
    {
        if (shieldProp != null && shieldProp != leftArmBone?.gameObject) DetachAndLaunchProp(shieldProp, hitDirection * 1.5f + Vector3.up * 1.5f, 4f);
        if (weaponProp != null && weaponProp != rightArmBone?.gameObject) DetachAndLaunchProp(weaponProp, hitDirection * 1.5f + Vector3.up * 1.5f, 4f);
    }

    public void Decapitate(Vector3 hitDirection)
    {
        // Detach helmet if present (guard against skeleton bone)
        if (helmetProp != null && helmetProp != headBone?.gameObject)
        {
            Vector3 launchVelocity = (hitDirection.normalized * 3f) + (Vector3.up * 1.5f);
            DetachAndLaunchProp(helmetProp, launchVelocity, 6f);
        }
        SpawnGoreVFX(headBone, hitDirection);
        if (headBone != null)
        {
            headBone.localScale = Vector3.zero;
        }
    }

    private void DetachAndLaunchProp(GameObject prop, Vector3 launchVelocity, float torqueForce)
    {
        if (prop == null) return;
        if(prop.transform.parent == null || detachedProps.Contains(prop)) return;
        detachedProps.Add(prop);
        SkinnedMeshRenderer smr = prop.GetComponent<SkinnedMeshRenderer>();
        if (smr != null)
        {
            UnityEngine.Mesh bakedMesh = new UnityEngine.Mesh();
            smr.BakeMesh(bakedMesh);
            Material[] mats = smr.sharedMaterials;
            Vector3 meshCenter = bakedMesh.bounds.center;
            Vector3[] verts = bakedMesh.vertices;
            for (int i = 0; i < verts.Length; i++)
            {
                verts[i] -= meshCenter;
            }
            bakedMesh.vertices = verts;
            bakedMesh.RecalculateBounds();
            bakedMesh.RecalculateNormals();

            smr.enabled = false;
            Destroy(smr);

            MeshFilter mf = prop.GetComponent<MeshFilter>() ?? prop.AddComponent<MeshFilter>();
            if (mf != null) mf.sharedMesh = bakedMesh;

            MeshRenderer mr = prop.GetComponent<MeshRenderer>() ?? prop.AddComponent<MeshRenderer>();
            if (mr != null) mr.materials = mats;

            prop.transform.position += prop.transform.TransformDirection(meshCenter);
        }
        prop.transform.SetParent(null);
        Rigidbody rb = prop.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = prop.AddComponent<Rigidbody>();
        }
        rb.isKinematic = false;
        rb.useGravity = true;
        Collider col = prop.GetComponent<Collider>();
        if (col == null)
        {
            BoxCollider box = prop.AddComponent<BoxCollider>();
            box.size = new Vector3(0.3f, 0.3f, 0.3f);
        }
        rb.AddForce(launchVelocity, ForceMode.Impulse);
        rb.AddTorque(Random.insideUnitSphere * torqueForce, ForceMode.Impulse);
        Destroy(prop, 10f);
    }
}
