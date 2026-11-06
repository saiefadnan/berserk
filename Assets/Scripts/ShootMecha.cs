using UnityEngine;
using TMPro;
using UnityEngine.Animations.Rigging;

public class ShootMecha : MonoBehaviour
{
    [Header("Camera Zoom Settings")]
    [SerializeField] private  float normalFOV;
    [SerializeField] private  float zoomFOV;
    [SerializeField] private  float zoomSpeed;
    [SerializeField] private  Vector3 normalPos;
    [SerializeField] private  Vector3 zoomPos;
    [SerializeField] private  Camera playerCamera;


    [Header("Cannon Blast & Recoil")]
    [SerializeField] private  float projectileSpeed = 40f;
    [SerializeField] private float recoilKick = 1.2f;
    [SerializeField] private float fireCooldown = 2.0f;
    [SerializeField] private GameObject muzzleFlashVfx;

    [Header("Audio & Targeting")]
    [SerializeField] private  AudioSource audioSource;
    [SerializeField] private  AudioClip shotAudio;
    [SerializeField] private  GameObject Target;
    [SerializeField] private  Rig AimRig;


    [Header("Projectile & Trajectory")]
    public GameObject bullets;
    public Transform bulletEmitter;
    public LineRenderer trajectoryLine;
    public float drag = 0.05f;
    public float increment = 0.05f;
    public int maxpoints = 30;
    public float rayOverlap = 0.05f;
    public float projectileRadius = 0.25f;

    private bool isAiming;
    private bool isAimActive;
    private float cooldownTimer = 0f;
    public Vector3 nextPosition;
    private CharacterController playerController;


    private void Awake()
    {
        playerController = GetComponentInParent<CharacterController>();
        if (playerCamera == null) playerCamera = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {

        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
        }

        //aim setup
        isAiming = Input.GetMouseButton(1);
        bool fire = Input.GetMouseButtonDown(0);
        isAimActive = Target.activeInHierarchy;
        if (AimRig != null) AimRig.weight = isAiming ? 1f : 0f;

        Ray ray = new Ray(bulletEmitter.position, (MouseTarget.Instance.GetHitPoint() - bulletEmitter.position).normalized);
        PredictTrajectory(ray);

        if (isAiming && fire && isAimActive && cooldownTimer <= 0f)
        {
            FireCannon(ray);
        }

        //camera Smooth Zoom
        if(playerCamera != null){
            float targetFOV = isAiming ? zoomFOV : normalFOV;
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * zoomSpeed);
            Vector3 targetPos = isAiming ? zoomPos : normalPos;
            playerCamera.transform.localPosition = Vector3.Lerp(playerCamera.transform.localPosition, targetPos,Time.deltaTime * zoomSpeed);
        }
    }

    private void FireCannon(Ray ray){

        cooldownTimer = fireCooldown;

        GameObject bullet = Instantiate(bullets, bulletEmitter.position, Quaternion.LookRotation(ray.direction));
        Rigidbody rb = bullet.GetComponent<Rigidbody>();

        if(rb != null){
            rb.linearVelocity = ray.direction * projectileSpeed;

            Destroy(bullet, 10f);
        }

        CannonBall cannonBall = bullet.GetComponent<CannonBall>();
        if (cannonBall != null)
        {
            cannonBall.owner = transform.root.gameObject;
        }

        if (muzzleFlashVfx != null)
        {
            GameObject flash = Instantiate(muzzleFlashVfx, bulletEmitter.position, Quaternion.LookRotation(ray.direction));
            flash.transform.localScale = Vector3.one * 1.8f;
            Destroy(flash, 2.5f);
        }

        if (audioSource != null && shotAudio != null)
        {
            audioSource.PlayOneShot(shotAudio);
        }

        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.Shake(0.25f, 0.35f);
        }

        if (playerController != null)
        {
            Vector3 recoilDir = -transform.root.forward;
            playerController.Move(recoilDir * recoilKick);
        }
    }

    private void PredictTrajectory(Ray ray)
    {
        if (trajectoryLine == null) return;
        trajectoryLine.enabled = isAiming && isAimActive;

        if(!(isAiming && isAimActive)) return;

        Vector3 velocity = projectileSpeed * ray.direction;
        Vector3 position = ray.origin;
        trajectoryLine.positionCount = maxpoints;
        trajectoryLine.SetPosition(0, position);
        int pointIndex = 1;

        for (int i = 0; i + 1 < maxpoints; i++)
        {
            velocity = CalculateNewVelocity(velocity, drag, increment);
            Vector3 nextPos = position + velocity * increment;

            float overlap = Vector3.Distance(position, nextPos) * rayOverlap;
            if (Physics.SphereCast(position, projectileRadius, velocity.normalized, out RaycastHit hit, overlap))
            {
                trajectoryLine.positionCount = pointIndex + 1;
                trajectoryLine.SetPosition(pointIndex, hit.point);
                return;
            }

            position = nextPos;
            trajectoryLine.SetPosition(pointIndex++, position);
        }
    }

    private Vector3 CalculateNewVelocity(Vector3 velocity, float drag, float increment)
    {
        velocity += Physics.gravity * increment;
        velocity *= Mathf.Clamp01(1f - drag * increment);
        return velocity;
    }


}









