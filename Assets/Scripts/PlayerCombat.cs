using UnityEngine;

public class PlayerCombat : MonoBehaviour{
    [Header("Dodge Settings")]
    [SerializeField] private KeyCode dodgeKey = KeyCode.LeftAlt;
    [SerializeField] private float dodgeSpeed = 12f;
    [SerializeField] private float dodgeDuration = 0.35f;
    [SerializeField] private float dodgeCooldown = 0.5f;


    [Header("Combo Timing")]
    [SerializeField] private float attackBufferDuration = 0.3f;

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


    private bool isEquipped = false;
    private bool isGuarding = false;
    private bool isDodging = false;
    private bool isBackstep = false;
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
    private static readonly int IsGuardingParam = Animator.StringToHash("isGuarding");
    private static readonly int GuardHitParam = Animator.StringToHash("guardHit");

    public bool IsEquipped => isEquipped;
    public bool IsGuarding => isGuarding;
    public bool IsDodging => isDodging;
    public bool IsAttacking { get; private set; }
    public bool CanMove => !IsAttacking && !isGuarding && !isDodging;

     private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        healthComponent = GetComponent<Health>();
        equipWeaponComponent = GetComponentInChildren<EquipWeapon>();
        playerMovement = GetComponent<PlayerMovement>();

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

            // Sync Upper Layer Weight: 0 during attacks/guard/dodge, 1 during locomotion sword carry
            animator.SetLayerWeight(1, (IsAttacking || isGuarding || isDodging) ? 0f : 1f);
        }

    private void HandleShoot(){
        bool rightClick = Input.GetMouseButton(1);
        animator.SetBool(ShootParam, rightClick && !isEquipped);
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
            animator.SetTrigger(GuardHitParam);
            Vector3 sparkPoint = (data.hitPoint != Vector3.zero)?data.hitPoint: transform.position + transform.forward + Vector3.up * 1.2f;
            if (sparkPrefab != null){
                GameObject spark =  Instantiate(sparkPrefab, sparkPoint, Quaternion.identity);
                Destroy(spark, 1.5f);
            }
            if (combatAudioSource != null && clashAudio != null) combatAudioSource.PlayOneShot(clashAudio);
            if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.16f, 0.22f);
            Debug.Log("<color=yellow><b>[CLASH] Dragonslayer Parried Blow!</b></color>");
        }
    }

    private void HandleDodge(){
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
            float currentDodgeSpeed = (isBackstep ? dodgeSpeed * 0.7f : dodgeSpeed) * Mathf.Sqrt(speedFactor);
            Vector3 dodgeVelocity = currentDodgeSpeed * dodgeDirection;
            characterController.Move(dodgeVelocity * Time.deltaTime);
        }

        if (Input.GetKeyDown(dodgeKey) && !isDodging && dodgeCooldownTimer <= 0 && CanMove)
        {
            isDodging = true;
            if (healthComponent != null) healthComponent.isInvulnerable = true;
            dodgeTimer = dodgeDuration;
            dodgeCooldownTimer = dodgeCooldown;
            animator.SetTrigger(DodgeParam);

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

            attackBufferTimer = 0;
            animator.ResetTrigger(Attack1Param);
            animator.ResetTrigger(Attack2Param);
            animator.ResetTrigger(Attack3Param);
        }
    }

    private void HandleGuard(){
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

    private void HandleAttackCombo(bool inAttack1, bool inAttack2, bool inAttack3){
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
