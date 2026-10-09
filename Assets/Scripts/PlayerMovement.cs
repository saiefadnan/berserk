using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    public Camera playerCamera;
    public GameObject figure;
    public float walkSpeed;
    public float runSpeed;
    public float health = 100f;
    public float jumpPower = 7f;
    public float gravity = 10f;
    public float lookSpeed = 2f;
    public float lookXLimit = 30f;
    public float defaultHeight = 2f;
    public float crouchHeight = 1f;
    public float crouchSpeed = 3f;

    private float smoothTime = 0.05f;
    private float _currentVelocity;
    private AnimatorStateInfo stateInfo, stateInfo1;
    private Vector3 moveDirection;
    private float rotationX = 0, rotationY;
    private CharacterController characterController;
    private Animator animator;
    private GameObject swordHolder;
    private GameObject weapon;
    private bool canMove = true;
    private bool equip;
    private bool stepRunning = false;
    private int attackIndex = 1;
    private readonly float attackDelay = 2f;
    private float attackTimer = 2f;
    [SerializeField] private AudioSource footAudioSrc;
    [SerializeField] private AudioSource jumpAudioSrc;
    [SerializeField] private AudioClip footStep;
    [SerializeField] private AudioClip jumpSound;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        //speedSetup 
        attackTimer -= Time.deltaTime;
        stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        stateInfo1 = animator.GetCurrentAnimatorStateInfo(1);
        AnimatorStateInfo nextStateInfo = animator.IsInTransition(0) ? animator.GetNextAnimatorStateInfo(0) : default;
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);
        //Debug.Log($"Attack animation playing: {attackAnimPlaying}");
        bool isWalking = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.A);
        bool isRunning = Input.GetKey(KeyCode.LeftShift) && isWalking;
        float curSpeedX = canMove ? (isRunning ? runSpeed : walkSpeed) * Input.GetAxis("Vertical") : 0;
        float curSpeedY = canMove ? (isRunning ? runSpeed : walkSpeed) * Input.GetAxis("Horizontal") : 0;
        float movementDirectionY = moveDirection.y;
        bool attack = Input.GetMouseButtonDown(0);
        bool inAttack1 = stateInfo.IsName("attack1") || (animator.IsInTransition(0) && nextStateInfo.IsName("attack1"));
        bool inAttack2 = stateInfo.IsName("attack2") || (animator.IsInTransition(0) && nextStateInfo.IsName("attack2"));
        bool inAttack3 = stateInfo.IsName("attack3") || (animator.IsInTransition(0) && nextStateInfo.IsName("attack3"));
        bool attackAnimPlaying = inAttack1 || inAttack2 || inAttack3;
        bool defaultState = stateInfo.IsName("idle");
        //bool attack1 = Input.GetMouseButtonDown(0);
        bool shoot = Input.GetMouseButton(1);

        animator.SetBool("shoot", shoot && !equip);
        moveDirection = stateInfo.IsName("equip") || attackAnimPlaying ? Vector3.zero : (forward * curSpeedX) + (right * curSpeedY);
        //Debug.Log(moveDirection); 

        if (moveDirection != Vector3.zero)
        {
            var targetAngle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;
            //var angle = Mathf.SmoothDamp(figure.transform.eulerAngles.y,targetAngle,ref _currentVelocity,smoothTime);
            figure.transform.rotation = Quaternion.Euler(0.0f, targetAngle, 0.0f);
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
            //var angle = Mathf.SmoothDamp(figure.transform.eulerAngles.y,targetAngle,ref _currentVelocity,smoothTime);
            figure.transform.rotation = Quaternion.Euler(0.0f, targetAngle, 0.0f);
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            equip = !equip;
            animator.SetTrigger("equip");
        }

        if (equip)
        {
            if (attack)
            {
                attackTimer = attackDelay;

                if (inAttack1)
                {
                    animator.SetTrigger("attack2");
                }
                else if (inAttack2)
                {
                    animator.SetTrigger("attack3");
                }
                else if (!inAttack3)
                {
                    animator.ResetTrigger("attack2");
                    animator.ResetTrigger("attack3");
                    animator.SetTrigger("attack1");
                }
            }
            else if (attackTimer <= 0)
            {
                attackIndex = 1;
            }
        }
        // animator.SetBool("Uuneq_mov",moveDirection!=Vector3.zero && isWalking && !isRunning && !equip && shoot);
        // animator.SetBool("Uuneq_fmov",moveDirection!=Vector3.zero && isRunning && !equip && shoot);
        animator.SetBool("eq_mov", moveDirection != Vector3.zero && (isRunning || isWalking) && equip);
        animator.SetBool("uneq_mov", moveDirection != Vector3.zero && isWalking && !isRunning && !equip);
        animator.SetBool("uneq_fmov", moveDirection != Vector3.zero && isRunning && !equip);
        animator.SetBool("idle", !isRunning && !isWalking);
        //jumpSetup
        if (Input.GetButton("Jump") && canMove && characterController.isGrounded)
        {
            moveDirection.y = jumpPower;
            animator.SetTrigger("jump");
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
            walkSpeed = 2.5f;
            runSpeed = 5f;
        }

        characterController.Move(moveDirection * Time.deltaTime);

        //cameraMovement
        if (canMove)
        {
            rotationX -= Input.GetAxis("Mouse Y") * lookSpeed;
            rotationX = Mathf.Clamp(rotationX, -lookXLimit, !stateInfo1.IsName("shoot") ? lookXLimit : 25f);
            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
            rotationY = Input.GetAxis("Mouse X") * lookSpeed;
            if (!stateInfo1.IsName("shoot")) figure.transform.localRotation *= Quaternion.Euler(0, -rotationY, 0);
            transform.localRotation *= Quaternion.Euler(0, rotationY, 0);
        }

    }

    public void TakeDamage(int damage)
    {
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
