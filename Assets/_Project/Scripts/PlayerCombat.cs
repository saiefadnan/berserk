using UnityEngine;

public class PlayerCombat : MonoBehaviour{
    [Header("Dodge / Roll Settings")]
    [SerializeField] private KeyCode dodgeKey = KeyCode.LeftAlt;
    [SerializeField] private float dodgeSpeed = 12f;
    [SerializeField] private float dodgeDuration = 0.45f;
    [SerializeField] private float dodgeCooldown = 0.5f;
    [SerializeField] private float dodgeStaminaCost = 20f;

    [Header("Combo Timing")]
    [SerializeField] private float attackBufferDuration = 0.3f;

    [Header("Attack Step-In Mobility")]
    [SerializeField] private float attack1LungeSpeed = 0.76f;
    [SerializeField] private float attack2LungeSpeed = 0.88f;
    [SerializeField] private float attack3LungeSpeed = 2.8f;
    [SerializeField] private float dodgeCancelNormalizedTime = 0.50f;

    [Header("Clash Feedback")]
    [SerializeField] private AudioClip clashAudio;
    [SerializeField] private GameObject sparkPrefab;
    [SerializeField] private AudioSource combatAudioSource;

    // Component References
    private Animator animator;
    private CharacterController characterController;
    private Health healthComponent;
    private EquipWeapon equipWeaponComponent;
    private PlayerMovement playerMovement;
    private Stamina stamina;

    private bool isEquipped = false;
    private bool isGuarding = false;
    private bool isDodging = false;
    private bool isBackstep = false;
    private bool isShooting = false;
    private float dodgeTimer = 0f;
    private float dodgeCooldownTimer = 0f;
    private Vector3 dodgeDirection;
    private float attackBufferTimer = 0f;

    private static readonly int Attack1Param = Animator.StringToHash("attack1");
    private static readonly int Attack2Param = Animator.StringToHash("attack2");
    private static readonly int Attack3Param = Animator.StringToHash("attack3");
    private static readonly int EquipParam = Animator.StringToHash("equip");
    private static readonly int ShootParam = Animator.StringToHash("shoot");
    private static readonly int IsEquippedParam = Animator.StringToHash("isEquipped");
    private static readonly int DodgeParam = Animator.StringToHash("dodge");
    private static readonly int DodgeDirParam = Animator.StringToHash("dodge_dir");
    private static readonly int IsGuardingParam = Animator.StringToHash("isGuarding");
    private static readonly int GuardHitParam = Animator.StringToHash("guardHit");

    public bool IsEquipped => isEquipped;
    public bool IsGuarding => isGuarding;
    public bool IsDodging => isDodging;
    public bool IsShooting => isShooting;
    public bool IsAttacking { get; private set; }
    public bool CanMove => !IsAttacking && !isGuarding && !isDodging && (executionHandler == null || !executionHandler.IsExecuting) && (healthComponent == null || !healthComponent.IsDead);
    public bool CanCancelIntoDodge
    {
        get
        {
            if (!IsAttacking) return true;
            if (animator == null) return false;
            AnimatorStateInfo baseState = animator.GetCurrentAnimatorStateInfo(0);
            float normTime = baseState.normalizedTime % 1f;
            return normTime >= dodgeCancelNormalizedTime;
        }
    }

    private ExecutionHandler executionHandler;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        healthComponent = GetComponent<Health>();
        equipWeaponComponent = GetComponentInChildren<EquipWeapon>();
        playerMovement = GetComponent<PlayerMovement>();
        executionHandler = GetComponent<ExecutionHandler>();
        stamina = GetComponent<Stamina>();
    }

    private void Start()
    {
        if (animator != null)
        {
            animator.ResetTrigger(Attack1Param);
            animator.ResetTrigger(Attack2Param);
            animator.ResetTrigger(Attack3Param);
            animator.ResetTrigger(Animator.StringToHash("execute"));
        }
    }

    private void OnEnable()
    {
        if (healthComponent != null)
        {
            healthComponent.OnDamaged += HandleDamaged;
        }
    }

    private void OnDisable()
    {
        if (healthComponent != null)
        {
            healthComponent.OnDamaged -= HandleDamaged;
        }
    }

    private void Update()
    {
        if (healthComponent != null && healthComponent.IsDead) return;
        if (executionHandler != null && executionHandler.IsExecuting) return;

        AnimatorStateInfo baseState = animator.GetCurrentAnimatorStateInfo(0);
        AnimatorStateInfo upperState = animator.GetCurrentAnimatorStateInfo(1);
        AnimatorStateInfo nextStateInfo = animator.IsInTransition(0) ? animator.GetNextAnimatorStateInfo(0) : default;

        bool inAttack1 = baseState.IsName("attack1") || (animator.IsInTransition(0) && nextStateInfo.IsName("attack1"));
        bool inAttack2 = baseState.IsName("attack2") || (animator.IsInTransition(0) && nextStateInfo.IsName("attack2"));
        bool inAttack3 = baseState.IsName("attack3") || (animator.IsInTransition(0) && nextStateInfo.IsName("attack3"));
        IsAttacking = inAttack1 || inAttack2 || inAttack3;

        HandleEquipToggle(baseState);
        HandleDodge();
        HandleGuard();
        HandleShoot();
        HandleAttackCombo(inAttack1, inAttack2, inAttack3);
        HandleAttackLunge(inAttack1, inAttack2, inAttack3, baseState);

        // Sync Upper Layer Weight: seamlessly muted during attacks, guards, and quickstep dodges
        bool inDodgeState = baseState.IsName("dodge") || (animator.IsInTransition(0) && nextStateInfo.IsName("dodge"));
        animator.SetLayerWeight(1, (IsAttacking || isGuarding || isDodging || inDodgeState) ? 0f : 1f);
    }

    private void HandleAttackLunge(bool inAttack1, bool inAttack2, bool inAttack3, AnimatorStateInfo baseState)
    {
        if (!IsAttacking || isDodging) return;

        float normTime = baseState.normalizedTime % 1f;

        // Active impulse window: between 8% and 42% of the animation (the forward step into the slash)
        if (normTime >= 0.08f && normTime <= 0.42f)
        {
            float peakSpeed = inAttack3 ? attack3LungeSpeed : (inAttack2 ? attack2LungeSpeed : attack1LungeSpeed);
            // Ease-out curve: explosive forward step that settles as the blade makes impact
            float progress = Mathf.InverseLerp(0.08f, 0.42f, normTime);
            float currentSpeed = peakSpeed * (1f - (progress * progress));

            Vector3 forwardMove = transform.forward * (currentSpeed * Time.deltaTime);
            characterController.Move(forwardMove);
        }
    }

    private void HandleShoot()
    {
        isShooting = Input.GetMouseButton(1) && !isEquipped;
        animator.SetBool(ShootParam, isShooting);
    }

    private void HandleEquipToggle(AnimatorStateInfo baseState)
    {
        if (Input.GetKeyDown(KeyCode.E) && !baseState.IsName("equip"))
        {
            isEquipped = !isEquipped;
            animator.SetTrigger(EquipParam);
            animator.SetBool(IsEquippedParam, isEquipped);
        }
    }

    private void HandleDamaged(DamageData data)
    {
        if (isGuarding)
        {
            if (stamina != null) stamina.Consume(12f);
            animator.SetTrigger(GuardHitParam);
            Vector3 sparkPoint = (data.hitPoint != Vector3.zero) ? data.hitPoint : transform.position + transform.forward + Vector3.up * 1.2f;
            if (sparkPrefab != null)
            {
                GameObject spark = Instantiate(sparkPrefab, sparkPoint, Quaternion.identity);
                Destroy(spark, 1.5f);
            }
            if (combatAudioSource != null && clashAudio != null) combatAudioSource.PlayOneShot(clashAudio);
            if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.16f, 0.22f);
            Debug.Log("<color=yellow><b>[CLASH] Dragonslayer Parried Blow!</b></color>");
        }
    }

    private void HandleDodge()
    {
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
                if (healthComponent != null) healthComponent.isInvulnerable = false;
            }

            float speedFactor = Mathf.Clamp01(dodgeTimer / dodgeDuration);
            // Explosive initial skid impulse that rapidly decelerates as boots brake on the ground
            float currentDodgeSpeed = (isBackstep ? dodgeSpeed * 0.85f : dodgeSpeed) * Mathf.Pow(speedFactor, 1.35f);
            Vector3 dodgeVelocity = currentDodgeSpeed * dodgeDirection;
            characterController.Move(dodgeVelocity * Time.deltaTime);
        }

        // Allowed to dodge from neutral OR by cancelling the recovery frames of an attack
        bool canInitiateDodge = (!IsAttacking || CanCancelIntoDodge) && !isGuarding && (executionHandler == null || !executionHandler.IsExecuting);

        if (Input.GetKeyDown(dodgeKey) && !isDodging && dodgeCooldownTimer <= 0 && canInitiateDodge)
        {
            if (stamina != null && !stamina.TryConsume(dodgeStaminaCost)) return;

            isDodging = true;
            if (healthComponent != null) healthComponent.isInvulnerable = true;
            dodgeTimer = dodgeDuration;
            dodgeCooldownTimer = dodgeCooldown;

            Vector3 forward = transform.forward;
            Vector3 right = transform.right;
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

            // Directional Quickstep Blend: -1 for Backstep hop, +1 for Forward/Lateral combat skid
            animator.SetFloat(DodgeDirParam, isBackstep ? -1f : 1f);
            animator.SetTrigger(DodgeParam);

            // Visceral kick
            if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.09f, 0.12f);

            attackBufferTimer = 0;
            animator.ResetTrigger(Attack1Param);
            animator.ResetTrigger(Attack2Param);
            animator.ResetTrigger(Attack3Param);
        }
    }

    private void HandleGuard()
    {
        bool rightClick = Input.GetMouseButton(1);
        isGuarding = IsEquipped && rightClick && !isDodging && !IsAttacking;
        if (healthComponent != null)
        {
            healthComponent.damageReductionPercent = isGuarding ? 0.85f : 0f;
        }
        animator.SetBool(IsGuardingParam, isGuarding);
        if (equipWeaponComponent != null)
        {
            equipWeaponComponent.SetGuarding(isGuarding);
            equipWeaponComponent.SetAttacking(IsAttacking);
        }
    }

    private void HandleAttackCombo(bool inAttack1, bool inAttack2, bool inAttack3)
    {
        if (attackBufferTimer > 0)
        {
            attackBufferTimer -= Time.deltaTime;
        }
        if (IsEquipped && Input.GetMouseButtonDown(0))
        {
            attackBufferTimer = attackBufferDuration;
        }
        if (IsEquipped && attackBufferTimer > 0)
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
            else if (!IsAttacking)
            {
                animator.ResetTrigger(Attack2Param);
                animator.ResetTrigger(Attack3Param);
                animator.SetTrigger(Attack1Param);
                attackBufferTimer = 0;
            }
        }
    }
}
