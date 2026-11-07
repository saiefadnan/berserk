using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Camera & Visuals")]
    public Camera playerCamera;
    public GameObject figure;

    private Health healthComponent;

    [Header("Movement Settings")]
    public float gravity = 10f;
    public float lookSpeed = 2f;
    public float lookXLimit = 30f;
    public float defaultHeight = 2f;
    public float defaultWalkSpeed = 2.5f;
    public float defaultRunSpeed = 5f;
    public float crouchHeight = 1f;
    public float crouchSpeed = 3f;
    public bool isRunning = false;
    [SerializeField] private float sprintStaminaCost = 15f; // per second

    private float walkSpeed;
    private float runSpeed;
    private float smoothTime = 0.08f;
    private float _currentVelocity;
    private float currentSpeed;
    private Vector3 moveDirection;
    private float rotationX = 10f, rotationY;
    private bool canMove = true;
    private bool stepRunning = false;

    private CharacterController characterController;
    private Animator animator;
    private PlayerCombat combat;
    private BeastOfDarkness beast;
    private Stamina stamina;

    private EquipWeapon equipWeaponComponent;
    private GameObject swordHolder;
    private GameObject weapon;

     [Header("Audio")]
    [SerializeField] private AudioSource footAudioSrc;
    [SerializeField] private AudioClip footStep;

    private static readonly int EqMovParam = Animator.StringToHash("eq_mov");
    private static readonly int SpeedParam = Animator.StringToHash("speed");

    public bool CanMove => canMove && combat.CanMove;

    private void Awake()
    {
        healthComponent = GetComponent<Health>();
        characterController = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        combat = GetComponent<PlayerCombat>();
        beast = GetComponent<BeastOfDarkness>();
        stamina = GetComponent<Stamina>();
        if (stamina == null) stamina = gameObject.AddComponent<Stamina>();
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        walkSpeed = defaultWalkSpeed;
        runSpeed = defaultRunSpeed;
    }

    void Update()
    {
        AnimatorStateInfo baseState = animator.GetCurrentAnimatorStateInfo(0);
        AnimatorStateInfo upperState = animator.GetCurrentAnimatorStateInfo(1);

        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);


        bool isWalking = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.A);
        bool wantsRun = Input.GetKey(KeyCode.LeftShift) && isWalking;

        if (wantsRun && stamina != null && !stamina.IsExhausted)
        {
            isRunning = true;
            stamina.Consume(sprintStaminaCost * Time.deltaTime);
        }
        else
        {
            isRunning = false;
        }

        float targetSpeed = !canMove || !isWalking ? 0 : (isRunning ? 1.0f : 0.5f);
        float speedMultiplier = (beast != null && beast.IsActive) ? 1.25f : 1.0f;
        float curSpeedX = canMove ? (isRunning ? runSpeed : walkSpeed) * speedMultiplier * Input.GetAxis("Vertical") : 0;
        float curSpeedY = canMove ? (isRunning ? runSpeed : walkSpeed) * speedMultiplier * Input.GetAxis("Horizontal") : 0;
        bool defaultState = baseState.IsName("idle");
        bool shoot = Input.GetMouseButton(1);

        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, 5f * Time.deltaTime);
        animator.SetFloat(SpeedParam, currentSpeed);
        moveDirection = baseState.IsName("equip") || combat.IsAttacking || combat.IsGuarding ? Vector3.zero : (forward * curSpeedX) + (right * curSpeedY);

        if (combat.IsAttacking && !combat.IsDodging)
        {
            float normTime = baseState.normalizedTime % 1;
            if (normTime < 0.25f)
            {
                var targetAngle = transform.eulerAngles.y;
                var angle = Mathf.SmoothDampAngle(figure.transform.eulerAngles.y, targetAngle, ref _currentVelocity, smoothTime);
                figure.transform.rotation = Quaternion.Euler(0.0f, angle, 0.0f);
                StopFootStep();
            }

        }
        else if (combat.IsGuarding)
        {
            FaceAngle(transform.eulerAngles.y);
            StopFootStep();
        }
        else if (!combat.IsDodging && moveDirection != Vector3.zero)
        {
            var targetAngle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;
            FaceAngle(targetAngle);
            HandleFootSteps();
        }
        else
        {
            StopFootStep();
        }

        if (upperState.IsName("shoot"))
        {
            var aimAngle = Mathf.Atan2(transform.forward.x, transform.forward.z) * Mathf.Rad2Deg;
            FaceAngle(aimAngle);
        }

        animator.SetBool(EqMovParam, moveDirection != Vector3.zero && (isRunning || isWalking) && combat.IsEquipped);
        moveDirection.y -= gravity * Time.deltaTime;

        //crouchSetup
        if (Input.GetKey(KeyCode.R) && canMove)
        {
            characterController.height = crouchHeight;
            walkSpeed = crouchSpeed;
            runSpeed = crouchSpeed;
        }
        else
        {
            characterController.height = defaultHeight;
            walkSpeed = defaultWalkSpeed;
            runSpeed = defaultRunSpeed;
        }

        //cameraMovement
        HandleCameraLook(upperState, isWalking);
        if (!combat.IsDodging)
        {
            characterController.Move(moveDirection * Time.deltaTime);
        }

    }
    private void HandleFootSteps(){
        if (!characterController.isGrounded && !combat.IsAttacking)
        {
            StopFootStep();
        }
        else
        {
            PlayFootStep();
        }
    }

    private void HandleCameraLook(AnimatorStateInfo upperState, bool isWalking){
        if (canMove)
        {
            rotationX -= Input.GetAxis("Mouse Y") * lookSpeed;
            rotationX = Mathf.Clamp(rotationX, -lookXLimit, !upperState.IsName("shoot") ? lookXLimit : 25f);
            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
            rotationY = Input.GetAxis("Mouse X") * lookSpeed;
            if (!upperState.IsName("shoot") && !isWalking && !combat.IsGuarding) figure.transform.localRotation *= Quaternion.Euler(0, -rotationY, 0);
            transform.localRotation *= Quaternion.Euler(0, rotationY, 0);
        }
    }

    private void FaceAngle(float targetAngle){
        var angle = Mathf.SmoothDampAngle(figure.transform.eulerAngles.y, targetAngle, ref _currentVelocity, smoothTime);
        figure.transform.rotation = Quaternion.Euler(0.0f, angle, 0.0f);
    }

    private void PlayFootStep()
    {
        if(footAudioSrc.isPlaying){
            footAudioSrc.pitch = isRunning ? 1.6f : 1.0f;
            return;
        }
        footAudioSrc.clip = footStep;
        footAudioSrc.loop = true;
        footAudioSrc.Play();
        stepRunning = true;
        footAudioSrc.pitch = isRunning ? 1.6f : 1.0f;
    }
    private void StopFootStep()
    {
        footAudioSrc.Stop();
        stepRunning = false;
    }
}
