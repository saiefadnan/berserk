using System;
using System.Collections;
using UnityEngine;

public class DeathScreenUI : MonoBehaviour
{
    public static DeathScreenUI Instance { get; private set; }

    [Header("UI Groups")]
    [Tooltip("CanvasGroup on this panel to control overall opacity")]
    [SerializeField] private CanvasGroup screenCanvasGroup;
    [Tooltip("RectTransform of the 'YOU DIED' text to animate scale")]
    [SerializeField] private RectTransform youDiedTextRect;
    [Tooltip("CanvasGroup on the 'Press SPACE to Rise' prompt")]
    [SerializeField] private CanvasGroup promptCanvasGroup;

    [Header("Timing")]
    [SerializeField] private float delayBeforeFade = 0.8f;
    [SerializeField] private float fadeDuration = 2.0f;
    [SerializeField] private float textStartScale = 0.9f;
    [SerializeField] private float textEndScale = 1.05f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip youDiedAudio;

    public event Action OnRespawnRequested;

    private bool isDisplaying = false;
    private bool canRespawn = false;
    private Coroutine displayRoutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(gameObject);

        if (screenCanvasGroup == null) screenCanvasGroup = GetComponent<CanvasGroup>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        ResetUI();
    }

    private void Start()
    {
        // Automatically find and subscribe to PlayerDeathHandler if present
        PlayerDeathHandler deathHandler = FindFirstObjectByType<PlayerDeathHandler>();
        if (deathHandler != null)
        {
            deathHandler.OnPlayerDeathSequenceStart += ShowDeathScreen;
        }
    }

    private void OnDestroy()
    {
        PlayerDeathHandler deathHandler = FindFirstObjectByType<PlayerDeathHandler>();
        if (deathHandler != null)
        {
            deathHandler.OnPlayerDeathSequenceStart -= ShowDeathScreen;
        }
    }

    private void Update()
    {
        if (!canRespawn) return;

        // Subtle pulsing prompt animation
        if (promptCanvasGroup != null)
        {
            promptCanvasGroup.alpha = Mathf.PingPong(Time.unscaledTime * 1.5f, 0.6f) + 0.4f;
        }

        // Trigger respawn on Space or Left Click
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            canRespawn = false;
            OnRespawnRequested?.Invoke();
        }
    }

    public void ShowDeathScreen()
    {
        if (isDisplaying) return;
        isDisplaying = true;
        if (displayRoutine != null) StopCoroutine(displayRoutine);
        displayRoutine = StartCoroutine(DeathSequenceRoutine());
    }

    private IEnumerator DeathSequenceRoutine()
    {
        // 1. Initial wait while player collapses
        yield return new WaitForSecondsRealtime(delayBeforeFade);

        // 2. Play death sound
        if (audioSource != null && youDiedAudio != null)
        {
            audioSource.PlayOneShot(youDiedAudio);
        }

        // 3. Fade in screen and slowly expand text
        if (screenCanvasGroup != null)
        {
            screenCanvasGroup.blocksRaycasts = true;
            screenCanvasGroup.interactable = true;
        }

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);

            // Smooth ease-out curve
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            if (screenCanvasGroup != null)
            {
                screenCanvasGroup.alpha = smoothT;
            }

            if (youDiedTextRect != null)
            {
                float scale = Mathf.Lerp(textStartScale, textEndScale, smoothT);
                youDiedTextRect.localScale = new Vector3(scale, scale, 1f);
            }

            yield return null;
        }

        if (screenCanvasGroup != null) screenCanvasGroup.alpha = 1f;

        canRespawn = true;
    }

    public void ResetUI()
    {
        isDisplaying = false;
        canRespawn = false;

        if (screenCanvasGroup != null)
        {
            screenCanvasGroup.alpha = 0f;
            screenCanvasGroup.blocksRaycasts = false;
            screenCanvasGroup.interactable = false;
        }

        if (youDiedTextRect != null)
        {
            youDiedTextRect.localScale = new Vector3(textStartScale, textStartScale, 1f);
        }

        if (promptCanvasGroup != null)
        {
            promptCanvasGroup.alpha = 0f;
        }
    }
}
