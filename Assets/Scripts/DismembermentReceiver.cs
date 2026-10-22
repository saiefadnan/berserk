using UnityEngine;

[RequireComponent(typeof(Health))]
public class DismembermentReceiver : MonoBehaviour{
    [Header("Bones & Props")]
    [SerializeField] private Transform headBone;        //
    [SerializeField] private GameObject helmetProp;     // Paladin_J_Nordstrom_Helmet(if equipped)
    [SerializeField] private GameObject weaponProp;
    [SerializeField] private GameObject shieldProp;

    [Header("VFX Prefabs")]
    [SerializeField] private GameObject stumpBloodFountainPrefab;
    [SerializeField] private GameObject chunkExplosionPrefab;

    private Health healthComponent;
    private Animator animator;

    private void Awake(){
        healthComponent = GetComponent<Health>();
        animator = GetComponentInChildren<Animator>();

        if(headBone==null && animator!=null && animator.isHuman){
            headBone = animator.GetBoneTransform(HumanBodyBones.Head);
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
        if (weaponProp != null)
        {
            DetachAndLaunchProp(weaponProp, hitDirection * 2f + Vector3.up * 2f, 5f);
        }
        if (shieldProp != null)
        {
            DetachAndLaunchProp(shieldProp, hitDirection * 2f + Vector3.up * 2f, 5f);
        }

        bool hitHeadZone = (headBone != null) && (hitPoint == default || hitPoint.y > (headBone.position.y - 0.65f));

        if (hitHeadZone)
        {
            Decapitate(hitDirection);
        }
    }

    private void Decapitate(Vector3 hitDirection)
    {
        // Detach helmet if present
        if (helmetProp != null)
        {
            Vector3 launchVelocity = (hitDirection.normalized * 3f) + (Vector3.up * 1.5f);
            DetachAndLaunchProp(helmetProp, launchVelocity, 6f);
        }
        if(chunkExplosionPrefab!=null && headBone!=null){
            GameObject chunks = Instantiate(chunkExplosionPrefab,headBone.position, Quaternion.LookRotation(hitDirection.normalized));
            Destroy(chunks, 1f);
        }if(stumpBloodFountainPrefab!=null){
            Transform stump = (headBone!=null && headBone.parent!=null)?headBone.parent:transform;
            GameObject fountain = Instantiate(stumpBloodFountainPrefab, stump.position, Quaternion.LookRotation(stump.up), stump);
            Destroy(fountain, 4f);
        }
        if (headBone != null)
        {
            headBone.localScale = Vector3.zero;
        }
    }

    private void DetachAndLaunchProp(GameObject prop, Vector3 launchVelocity, float torqueForce)
    {
        if (prop == null) return;
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

            MeshFilter mf = prop.AddComponent<MeshFilter>();
            mf.sharedMesh = bakedMesh;

            MeshRenderer mr = prop.AddComponent<MeshRenderer>();
            mr.materials = mats;

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
