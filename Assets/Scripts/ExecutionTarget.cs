using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;

[RequireComponent(typeof(Health))]
public class ExecutionTarget : MonoBehaviour
{
    [Header("Stagger Tuning")]
    [Tooltip("Health percentage (0-1) below which enemy staggers for execution")]
    [SerializeField] private float executionHealthThreshold = 0.28f;
    [Tooltip("How long enemy stays kneeling before recovering if not executed")]
    [SerializeField] private float staggerDuration = 4.5f;

    [Header("UI Feedback")]
    [SerializeField] private Vector3 promptOffset = new Vector3(0, 2.0f, 0);

    public bool IsStaggered { get; private set; }
    public bool IsBeingExecuted { get; private set; }

    private Health health;
    private NavMeshAgent agent;
    private Animator animator;
    private EnemyCombat combat;
    private DismembermentReceiver dismemberment;

    private Coroutine staggerCoroutine;
    private GameObject promptCanvasObject;
    private Text promptText;
    private Transform playerCamera;

    private bool hasStaggeredOnce = false;

    private void Awake()
    {
        health = GetComponent<Health>();
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        combat = GetComponent<EnemyCombat>();
        dismemberment = GetComponent<DismembermentReceiver>();
    }

    private void Start()
    {
        if (Camera.main != null)
        {
            playerCamera = Camera.main.transform;
        }
        CreateWorldPrompt();
    }

    private void OnEnable()
    {
        if (health != null)
        {
            health.OnDamaged += HandleDamaged;
            health.OnDeath += HandleDeath;
        }
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.OnDamaged -= HandleDamaged;
            health.OnDeath -= HandleDeath;
        }
    }

    private void Update()
    {
        if (IsStaggered && !IsBeingExecuted && promptCanvasObject != null)
        {
            if (playerCamera == null && Camera.main != null)
                playerCamera = Camera.main.transform;

            if (playerCamera != null)
            {
                promptCanvasObject.transform.position = transform.position + promptOffset;
                promptCanvasObject.transform.rotation = Quaternion.LookRotation(promptCanvasObject.transform.position - playerCamera.position);

                // Subtle pulsing glow
                float pulse = 1f + 0.15f * Mathf.Sin(Time.time * 6f);
                promptCanvasObject.transform.localScale = Vector3.one * 0.012f * pulse;
            }
        }
    }

    private void HandleDamaged(DamageData data)
    {
        if (health.IsDead || health.CurrentHealth <= 0f || IsStaggered || IsBeingExecuted) return;
        if(data.reactionType == HitReactionType.CannonballKnockdown) return;
        // Check if HP reached execution threshold
        if (!hasStaggeredOnce && health.CurrentHealth <= (health.MaxHealth * executionHealthThreshold))
        {
            TriggerStagger();
        }
    }

    private void HandleDeath(DamageData data)
    {
        HidePrompt();
        if (staggerCoroutine != null) StopCoroutine(staggerCoroutine);
    }

    public void TriggerStagger(float overrideDuration = -1f)
    {
        if (health.IsDead || IsBeingExecuted) return;

        hasStaggeredOnce = true;
        IsStaggered = true;

        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        if (animator != null)
        {
            animator.ResetTrigger("attack");
            animator.ResetTrigger("run");
            animator.SetTrigger("downed");
        }

        ShowPrompt();

        float dur = overrideDuration > 0f ? overrideDuration : staggerDuration;
        if (staggerCoroutine != null) StopCoroutine(staggerCoroutine);
        staggerCoroutine = StartCoroutine(StaggerCountdown(dur));

        Debug.Log($"<color=red><b>[STAGGER] {gameObject.name} is STAGGERED! Press [F] to EXECUTE!</b></color>");
    }

    private IEnumerator StaggerCountdown(float duration)
    {
        yield return new WaitForSeconds(duration);

        if (!IsBeingExecuted && !health.IsDead && IsStaggered)
        {
            RecoverFromStagger();
        }
    }

    private void RecoverFromStagger()
    {
        IsStaggered = false;
        HidePrompt();

        // Restore small adrenaline health so they don't instantly re-stagger
        if (health != null && health.CurrentHealth < health.MaxHealth * 0.20f)
        {
            health.Heal(health.MaxHealth * 0.15f);
        }

        if (animator != null)
        {
            animator.SetTrigger("hit_react");
        }

        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }

        Debug.Log($"<color=yellow>[STAGGER] {gameObject.name} recovered from stagger!</color>");
    }

    public void OnExecuted(Vector3 executionDirection)
    {
        IsBeingExecuted = true;
        IsStaggered = false;
        HidePrompt();

        if (staggerCoroutine != null) StopCoroutine(staggerCoroutine);

        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.enabled = false;
        }

        // Guaranteed decapitation and equipment drop
        if (dismemberment != null)
        {
            dismemberment.Decapitate(executionDirection);
            dismemberment.DropEquipment(executionDirection * 1.5f + Vector3.up * 1f);
        }

        // Deal fatal lethal damage to trigger standard ragdoll launch
        GameObject playerObj = (playerCamera != null) ? playerCamera.root.gameObject : GameObject.FindWithTag("Player");
        DamageData fatalData = new DamageData
        {
            amount = 9999f,
            hitDirection = executionDirection.normalized * 18f + Vector3.up * 5f,
            hitPoint = transform.position + Vector3.up * 1.5f,
            attacker = playerObj
        };
        health.TakeDamage(fatalData);
    }

    public void ShowPrompt()
    {
        if (promptCanvasObject != null) promptCanvasObject.SetActive(true);
    }

    public void HidePrompt()
    {
        if (promptCanvasObject != null) promptCanvasObject.SetActive(false);
    }

    private void CreateWorldPrompt()
    {
        promptCanvasObject = new GameObject("ExecutionPrompt_Canvas");
        promptCanvasObject.transform.SetParent(transform);
        promptCanvasObject.transform.position = transform.position + promptOffset;

        Canvas canvas = promptCanvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        CanvasScaler scaler = promptCanvasObject.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 20;

        promptCanvasObject.transform.localScale = Vector3.one * 0.012f;

        GameObject textObj = new GameObject("PromptText");
        textObj.transform.SetParent(promptCanvasObject.transform, false);

        promptText = textObj.AddComponent<Text>();
        promptText.text = "[F] EXECUTE";
        promptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (promptText.font == null) promptText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        promptText.fontSize = 42;
        promptText.fontStyle = FontStyle.Bold;
        promptText.alignment = TextAnchor.MiddleCenter;
        promptText.color = new Color(1f, 0.15f, 0.15f, 1f);

        Outline outline = textObj.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2, -2);

        RectTransform rt = textObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(300, 80);

        promptCanvasObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (promptCanvasObject != null) Destroy(promptCanvasObject);
    }
}
