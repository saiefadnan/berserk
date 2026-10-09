using UnityEngine;
using TMPro;
using UnityEngine.Animations.Rigging;

public class ShootMecha : MonoBehaviour
{
    [SerializeField] private  float normalFOV;
    [SerializeField] private  float zoomFOV;
    [SerializeField] private  float zoomSpeed;
    [SerializeField] private  Vector3 normalPos;
    [SerializeField] private  Vector3 zoomPos;
    [SerializeField] private  Camera camera;
    [SerializeField] private  float arrowSpeed;
    [SerializeField] private  AudioSource audioSource;
    [SerializeField] private  AudioClip shotAudio;
    [SerializeField] private  GameObject Target;
    [SerializeField] private  Rig AimRig;

    public GameObject bullets;
    private bool isAiming, fire, isAimActive;
    public Transform bulletEmitter;
    public LineRenderer trajectoryLine;
    public float drag;
    public float increment;
    public int maxpoints;
    public float rayOverlap;
    public Vector3 nextPosition;


    // Update is called once per frame
    void Update()
    {
        //aim setup
        isAiming = Input.GetMouseButton(1);
        fire = Input.GetMouseButtonDown(0);
        isAimActive = Target.activeInHierarchy;
        AimRig.weight = isAiming ? 1f : 0f;
        Ray ray = new Ray(bulletEmitter.position, (MouseTarget.Instance.GetHitPoint() - bulletEmitter.position).normalized);
        PredictTrajectory(ray);

        if (isAiming && fire && isAimActive)
        {
            GameObject bullet = Instantiate(bullets, bulletEmitter.position, Quaternion.LookRotation(ray.direction));
            Rigidbody rb = bullet.GetComponent<Rigidbody>();
            rb.linearVelocity = ray.direction * arrowSpeed;
            audioSource.PlayOneShot(shotAudio);
            Destroy(bullet, 10f);
        }
        //camera setup
        float targetFOV = isAiming ? zoomFOV : normalFOV;
        camera.fieldOfView = Mathf.Lerp(camera.fieldOfView, targetFOV, Time.deltaTime * zoomSpeed);
        Vector3 targetPos = isAiming ? zoomPos : normalPos;
        camera.transform.localPosition = Vector3.Lerp(camera.transform.localPosition, targetPos, Time.deltaTime * zoomSpeed);
    }

    private void PredictTrajectory(Ray ray)
    {
        Vector3 velocity = arrowSpeed * ray.direction;
        Vector3 position = ray.origin;
        trajectoryLine.enabled = isAiming && isAimActive;
        if(!(isAiming && isAimActive)) return;
        trajectoryLine.positionCount = maxpoints;
        trajectoryLine.SetPosition(0, position);
        int pointIndex = 1;

        for (int i = 0; i + 1 < maxpoints; i++)
        {
            velocity = CalculateNewVelocity(velocity, drag, increment);
            nextPosition = position + velocity * increment;

            float overlap = Vector3.Distance(position, nextPosition) * rayOverlap;
            if (Physics.Raycast(position, velocity.normalized, out RaycastHit hit, overlap))
            {
                trajectoryLine.positionCount = pointIndex + 1;
                trajectoryLine.SetPosition(pointIndex, hit.point);
                return;
            }

            position = nextPosition;
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









