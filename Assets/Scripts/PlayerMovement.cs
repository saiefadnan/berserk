using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour, IDamageable
{
    public Camera playerCamera;
    public GameObject figure;
    public float health = 100f;
    public float jumpPower = 7f;
    public float gravity = 10f;
    public float lookSpeed = 2f;
    public float lookXLimit = 30f;
    public float defaultHeight = 2f;
    public float defaultWalkSpeed = 2.5f;
    public float defaultRunSpeed = 5f;
    public float crouchHeight = 1f;
    public float crouchSpeed = 3f;

    private float walkSpeed;
    private float runSpeed;
    private float smoothTime = 0.08f;
    private float _currentVelocity;
    private float currentSpeed;
    private AnimatorStateInfo stateInfo, stateInfo1;
    private Vector3 moveDirection;
    private float rotationX = 0, rotationY;
    private CharacterController characterController;
    private Animator animator;
    private EquipWeapon equipWeaponComponent;
    private GameObject swordHolder;
    private GameObject weapon;
    private bool canMove = true;
    private bool equip;
    private bool stepRunning = false;
    private float attackBufferTimer = 0f;
    private const float AttackBufferTimer = 0.3f;
    [SerializeField] private AudioSource footAudioSrc;
    [SerializeField] private AudioSource jumpAudioSrc;
    [SerializeField] private AudioClip footStep;
    [SerializeField] private AudioClip jumpSound;

    [Header("Dodge Settings")]
    [SerializeField] private KeyCode dodgeKey = KeyCode.LeftAlt;
    [SerializeField] private float dodgeSpeed = 12f;
    [SerializeField] private float dodgeDuration = 0.35f;
    [SerializeField] private float dodgeCooldown = 0.5f;

    private bool isDodging = false;
    private bool isGuarding = false;
    private bool isBackstep = false;
    private bool isInvulnerable = false;
    private float dodgeTimer = 0f;
    private float dodgeCooldownTimer = 0f;
    private Vector3 dodgeDirection;

    private static readonly int Attack1Param = Animator.StringToHash("attack1");
    private static readonly int Attack2Param = Animator.StringToHash("attack2");
    private static readonly int Attack3Param = Animator.StringToHash("attack3");
    private static readonly int EquipParam = Animator.StringToHash("equip");
    private static readonly int ShootParam = Animator.StringToHash("shoot");
    private static readonly int EqMovParam = Animator.StringToHash("eq_mov");

    private static readonly int IdleParam = Animator.StringToHash("idle");
    private static readonly int JumpParam = Animator.StringToHash("jump");
    private static readonly int SpeedParam = Animator.StringToHash("speed");
    private static readonly int IsEquippedParam = Animator.StringToHash("isEquipped");
    private static readonly int DodgeParam = Animator.StringToHash("dodge");
    private static readonly int IsGuardingParam = Animator.StringToHash("isGuarding");
    private static readonly int GuardHitParam = Animator.StringToHash("guardHit");

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        equipWeaponComponent = GetComponentInChildren<EquipWeapon>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        walkSpeed = defaultWalkSpeed;
        runSpeed = defaultRunSpeed;
    }

    void Update()
    {
        stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        stateInfo1 = animator.GetCurrentAnimatorStateInfo(1);
        AnimatorStateInfo nextStateInfo = animator.IsInTransition(0) ? animator.GetNextAnimatorStateInfo(0) : default;
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);

        bool isWalking = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.A);
        bool isRunning = Input.GetKey(KeyCode.LeftShift) && isWalking;
        float targetSpeed = !canMove || !isWalking ? 0 : (isRunning ? 1.0f : 0.5f);
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, 5f * Time.deltaTime);
        animator.SetFloat(SpeedParam, currentSpeed);
        float curSpeedX = canMove ? (isRunning ? runSpeed : walkSpeed) * Input.GetAxis("Vertical") : 0;
        float curSpeedY = canMove ? (isRunning ? runSpeed : walkSpeed) * Input.GetAxis("Horizontal") : 0;
        float movementDirectionY = moveDirection.y;
        bool attack = Input.GetMouseButtonDown(0);
        bool inAttack1 = stateInfo.IsName("attack1") || (animator.IsInTransition(0) && nextStateInfo.IsName("attack1"));
        bool inAttack2 = stateInfo.IsName("attack2") || (animator.IsInTransition(0) && nextStateInfo.IsName("attack2"));
        bool inAttack3 = stateInfo.IsName("attack3") || (animator.IsInTransition(0) && nextStateInfo.IsName("attack3"));
        bool attackAnimPlaying = inAttack1 || inAttack2 || inAttack3;
        bool defaultState = stateInfo.IsName("idle");
        bool shoot = Input.GetMouseButton(1);

        if (dodgeCooldownTimer > 0)
        {
            dodgeCooldownTimer -= Time.deltaTime;
        }
        if (isDodging)
        {
            dodgeTimer -= Time.deltaTime;
            if (dodgeTimer <= 0)
            {
                isDodging = false;
                isInvulnerable = false;
            }
        }

        if (Input.GetKeyDown(dodgeKey) && !isDodging && dodgeCooldownTimer <= 0 && canMove)
        {
            isDodging = true;
            isInvulnerable = true;
            dodgeTimer = dodgeDuration;
            dodgeCooldownTimer = dodgeCooldown;
            animator.SetTrigger(DodgeParam);

            Vector3 inputDir = (forward * Input.GetAxisRaw("Vertical") + right * Input.GetAxisRaw("Horizontal")).normalized;

            if (inputDir.sqrMagnitude > 0.01f)
            {
                dodgeDirection = inputDir;
                isBackstep = false;
            }
            else
            {
                dodgeDirection = -transform.forward;
                isBackstep = true;
            }

            attackBufferTimer = 0;
            animator.ResetTrigger(Attack1Param);
            animator.ResetTrigger(Attack2Param);
            animator.ResetTrigger(Attack3Param);
        }
        isGuarding = equip && Input.GetMouseButton(1) && !isDodging && !attackAnimPlaying;
        animator.SetBool(IsGuardingParam, isGuarding);
        if (equipWeaponComponent != null)
        {
            equipWeaponComponent.SetGuarding(isGuarding);
            equipWeaponComponent.SetAttacking(attackAnimPlaying);
        }
        animator.SetBool(ShootParam, shoot && !equip);
        moveDirection = stateInfo.IsName("equip") || attackAnimPlaying || isGuarding ? Vector3.zero : (forward * curSpeedX) + (right * curSpeedY);

        if (attackAnimPlaying && !isDodging)
        {
            float normTime = stateInfo.normalizedTime % 1;
            if (normTime < 0.25f)
            {
                var targetAngle = transform.eulerAngles.y;
                var angle = Mathf.SmoothDampAngle(figure.transform.eulerAngles.y, targetAngle, ref _currentVelocity, smoothTime);
                figure.transform.rotation = Quaternion.Euler(0.0f, angle, 0.0f);
                StopFootStep();
                stepRunning = false;
            }

            if (animator.speed != 1.0f)
            {
                animator.speed = 1.0f;
            }
        }
        else
        {
            if (animator.speed != 1.0f)
            {
                animator.speed = 1.0f;
            }
        }
        if (isGuarding)
        {
            var targetAngle = transform.eulerAngles.y;
            var angle = Mathf.SmoothDampAngle(figure.transform.eulerAngles.y, targetAngle, ref _currentVelocity, smoothTime);
            figure.transform.rotation = Quaternion.Euler(0.0f, angle, 0.0f);
            StopFootStep();
            stepRunning = false;
        }
        else if (!isDodging && moveDirection != Vector3.zero)
        {
            var targetAngle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;
            var angle = Mathf.SmoothDampAngle(figure.transform.eulerAngles.y, targetAngle, ref _currentVelocity, smoothTime);
            figure.transform.rotation = Quaternion.Euler(0.0f, angle, 0.0f);
            if (!characterController.isGrounded && !attackAnimPlaying)
            {
                StopFootStep();
                stepRunning = false;
            }
            else
            {
                bool isEquippedMoving = stateInfo.IsName("eq_mov") || stateInfo.IsName("eq_run") ||
                                       (animator.IsInTransition(0) && (nextStateInfo.IsName("eq_mov") || nextStateInfo.IsName("eq_run")));
                bool isUnequippedMoving = stateInfo.IsName("uneq_mov") || stateInfo.IsName("uneq_fmov") ||
                                         (animator.IsInTransition(0) && (nextStateInfo.IsName("uneq_mov") || nextStateInfo.IsName("uneq_fmov")));
                bool moving = isEquippedMoving || isUnequippedMoving;

                if (!stepRunning && moving)
                {
                    PlayFootStep();
                    stepRunning = true;
                }
                if (!isRunning && !isEquippedMoving)
                {
                    footAudioSrc.pitch = 1f;
                }
                else
                {
                    footAudioSrc.pitch = 2.0f;
                }
            }
        }
        else
        {
            StopFootStep();
            stepRunning = false;
        }
        if (stateInfo1.IsName("shoot"))
        {
            var targetAngle = Mathf.Atan2(transform.forward.x, transform.forward.z) * Mathf.Rad2Deg;
            var angle = Mathf.SmoothDampAngle(figure.transform.eulerAngles.y, targetAngle, ref _currentVelocity, smoothTime);
            figure.transform.rotation = Quaternion.Euler(0.0f, angle, 0.0f);
        }

        if (Input.GetKeyDown(KeyCode.E) && !stateInfo.IsName("equip"))
        {
            equip = !equip;
            animator.SetTrigger(EquipParam);
            animator.SetBool(IsEquippedParam, equip);
        }


        if (attackBufferTimer > 0)
        {
            attackBufferTimer -= Time.deltaTime;
        }
        if (equip && Input.GetMouseButtonDown(0))
        {
            attackBufferTimer = AttackBufferTimer;
        }
        if (equip && attackBufferTimer > 0)
        {
            if (inAttack1)
            {
                animator.SetTrigger(Attack2Param);
                attackBufferTimer = 0;
            }
            else if (inAttack2)
            {
                animator.SetTrigger(Attack3Param);
                attackBufferTimer = 0;
            }
            else if (inAttack3)
            {
                animator.SetTrigger(Attack1Param);
                attackBufferTimer = 0;
            }
            else if (!attackAnimPlaying)
            {
                animator.ResetTrigger(Attack2Param);
                animator.ResetTrigger(Attack3Param);
                animator.SetTrigger(Attack1Param);
                attackBufferTimer = 0;
            }
        }
        animator.SetBool(EqMovParam, moveDirection != Vector3.zero && (isRunning || isWalking) && equip);
        //jumpSetup
        if (Input.GetButton("Jump") && !isDodging && canMove && characterController.isGrounded)
        {
            moveDirection.y = jumpPower;
            animator.SetTrigger(JumpParam);
            PlayJumpSound();
        }
        else
        {
            moveDirection.y = movementDirectionY;
        }
        if (!characterController.isGrounded)
        {
            moveDirection.y -= gravity * Time.deltaTime;
        }

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

        if (isDodging)
        {
            float speedFactor = Mathf.Clamp01(dodgeTimer / dodgeDuration);
            float currentDodgeSpeed = (isBackstep ? dodgeSpeed * 0.7f : dodgeSpeed) * Mathf.Sqrt(speedFactor);
            Vector3 dodgeVelocity = currentDodgeSpeed * dodgeDirection;
            dodgeVelocity.y = moveDirection.y;
            characterController.Move(dodgeVelocity * Time.deltaTime);

            if (!isBackstep)
            {
                var targetAngle = Mathf.Atan2(dodgeDirection.x, dodgeDirection.z) * Mathf.Rad2Deg;
                var angle = Mathf.SmoothDampAngle(figure.transform.eulerAngles.y, targetAngle, ref _currentVelocity, smoothTime);
                figure.transform.rotation = Quaternion.Euler(0.0f, angle, 0.0f);
            }
        }
        else
        {
            characterController.Move(moveDirection * Time.deltaTime);
        }


        //cameraMovement
        if (canMove)
        {
            rotationX -= Input.GetAxis("Mouse Y") * lookSpeed;
            rotationX = Mathf.Clamp(rotationX, -lookXLimit, !stateInfo1.IsName("shoot") ? lookXLimit : 25f);
            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
            rotationY = Input.GetAxis("Mouse X") * lookSpeed;
            if (!stateInfo1.IsName("shoot") && !isWalking && !isGuarding) figure.transform.localRotation *= Quaternion.Euler(0, -rotationY, 0);
            transform.localRotation *= Quaternion.Euler(0, rotationY, 0);
        }

    }

    public SurfaceType surfaceType => SurfaceType.Flesh;
    public GameObject victim => gameObject;

    public void TakeDamage(DamageData data){
        TakeDamage(data.amount);
    }

    public void TakeDamage(int damage)
    {
        if (isInvulnerable)
        {
            Debug.Log("<color=cyan>DODGED! (i-Frames protected Guts)</color>");
            return;
        }
        if (isGuarding)
        {
            animator.SetTrigger(GuardHitParam);
            Debug.Log("<color=yellow>BLOCKED! (Dragonslayer absorbed the blow)</color>");
            health -= damage * 0.15f;
            return;
        }
        health -= damage;
    }

    private void PlayJumpSound()
    {
        jumpAudioSrc.PlayOneShot(jumpSound);
    }

    private void PlayFootStep()
    {
        footAudioSrc.clip = footStep;
        footAudioSrc.Play();
    }
    private void StopFootStep()
    {
        footAudioSrc.Stop();
    }
}
