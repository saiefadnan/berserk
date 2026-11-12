using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Tracks")]
    [SerializeField] private AudioClip explorationTrack;
    [SerializeField] private AudioClip combatTrack;

    [Header("Settings")]
    [Range(0f, 1f)] [SerializeField] private float masterVolume = 0.45f;
    [SerializeField] private float crossFadeDuration = 1.5f;
    [SerializeField] private float combatExitCooldown = 3.5f;

    private AudioSource explorationSource;
    private AudioSource combatSource;
    private float targetCombatWeight = 0f;
    private float currentCombatWeight = 0f;
    private float combatExitTimer = 0f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        explorationSource = gameObject.AddComponent<AudioSource>();
        explorationSource.loop = true;
        explorationSource.playOnAwake = false;
        explorationSource.spatialBlend = 0f; // 2D Stereo

        combatSource = gameObject.AddComponent<AudioSource>();
        combatSource.loop = true;
        combatSource.playOnAwake = false;
        combatSource.spatialBlend = 0f; // 2D Stereo
    }

    private void Start()
    {
        if (explorationTrack != null && explorationSource != null)
        {
            explorationSource.clip = explorationTrack;
            explorationSource.volume = masterVolume;
            explorationSource.Play();
        }

        if (combatTrack != null && combatSource != null)
        {
            combatSource.clip = combatTrack;
            combatSource.volume = 0f;
            combatSource.Play();
        }
    }

    private void Update()
    {
        bool isEngaged = CheckCombatState();

        if (isEngaged)
        {
            targetCombatWeight = 1f;
            combatExitTimer = combatExitCooldown;
        }
        else if (combatExitTimer > 0f)
        {
            combatExitTimer -= Time.deltaTime;
            targetCombatWeight = 1f;
        }
        else
        {
            targetCombatWeight = 0f;
        }

        float fadeSpeed = 1f / Mathf.Max(0.1f, crossFadeDuration);
        currentCombatWeight = Mathf.MoveTowards(currentCombatWeight, targetCombatWeight, Time.deltaTime * fadeSpeed);

        if (explorationSource != null)
        {
            explorationSource.volume = (1f - currentCombatWeight * 0.75f) * masterVolume;
        }

        if (combatSource != null)
        {
            combatSource.volume = currentCombatWeight * masterVolume;
        }
    }

    private bool CheckCombatState()
    {
        if (CombatDirector.Instance != null && CombatDirector.Instance.HasActiveThreat())
        {
            return true;
        }
        return false;
    }
}
