using System.Collections.Generic;
using UnityEngine;

public class LivePerformanceHUD : MonoBehaviour
{
    [Header("Toggle Keybindings")]
    [SerializeField] private KeyCode toggleKey = KeyCode.F3;
    [SerializeField] private KeyCode compactKey = KeyCode.F4;
    [SerializeField] private bool showOnStart = false;

    [Header("Display Tuning")]
    [SerializeField] private int targetFrameRate = 60;
    [SerializeField] private float updateInterval = 0.15f; // Update text 6-7 times/sec to avoid GC allocation

    private bool isVisible = true;
    private bool isCompact = false;

    // Performance tracking
    private float fpsAccumulator = 0f;
    private int fpsFrames = 0;
    private float currentFps = 60f;
    private float currentFrameTimeMs = 16.6f;
    private float minFps = float.MaxValue;
    private float maxFps = 0f;
    private float onePercentLowFps = 60f;
    private float resetTimer = 0f;

    // Rolling frame history for 1% low calculations
    private readonly List<float> frameTimeHistory = new List<float>(120);

    // Memory tracking
    private float allocatedMemoryMB = 0f;
    private int gcCollections = 0;

    // Combat & Scene metrics
    private int activeEnemiesCount = 0;
    private int activeRagdollRbCount = 0;
    private float currentTimeScale = 1.0f;

    // GUI Styles
    private GUIStyle headerStyle;
    private GUIStyle labelStyle;
    private GUIStyle boxStyle;
    private GUIStyle badgeGreen;
    private GUIStyle badgeYellow;
    private GUIStyle badgeRed;
    private Texture2D bgTexture;

    private string cachedDisplayString = "";
    private float lastTextUpdateTime = 0f;

    private static LivePerformanceHUD instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInitialize()
    {
        if (instance == null && FindFirstObjectByType<LivePerformanceHUD>() == null)
        {
            GameObject hudObj = new GameObject("LivePerformanceHUD");
            hudObj.AddComponent<LivePerformanceHUD>();
            DontDestroyOnLoad(hudObj);
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        isVisible = showOnStart;
    }

    private void Update()
    {
        // Toggle HUD on/off
        if (Input.GetKeyDown(toggleKey))
        {
            isVisible = !isVisible;
        }

        // Toggle Compact vs Detailed
        if (Input.GetKeyDown(compactKey))
        {
            isCompact = !isCompact;
        }

        if (!isVisible) return;

        // Frame timing calculation (unscaled to accurately measure real performance during slow-mo)
        float unscaledDt = Time.unscaledDeltaTime;
        fpsAccumulator += unscaledDt;
        fpsFrames++;

        if (unscaledDt > 0f)
        {
            frameTimeHistory.Add(unscaledDt);
            if (frameTimeHistory.Count > 120)
            {
                frameTimeHistory.RemoveAt(0);
            }
        }

        // Update metrics periodically
        if (Time.unscaledTime - lastTextUpdateTime >= updateInterval)
        {
            currentFps = fpsFrames / fpsAccumulator;
            currentFrameTimeMs = unscaledDt * 1000f;

            if (currentFps < minFps && currentFps > 5f) minFps = currentFps;
            if (currentFps > maxFps) maxFps = currentFps;

            // 1% Low calculation
            CalculateOnePercentLow();

            // Reset min/max every 10 seconds to stay relevant
            resetTimer += updateInterval;
            if (resetTimer >= 10f)
            {
                minFps = currentFps;
                maxFps = currentFps;
                resetTimer = 0f;
            }

            // Memory stats
            allocatedMemoryMB = (float)(System.GC.GetTotalMemory(false) / (1024.0 * 1024.0));
            gcCollections = System.GC.CollectionCount(0);

            // Scene & Combat stats
            currentTimeScale = Time.timeScale;
            EnemyCombat[] enemies = FindObjectsByType<EnemyCombat>(FindObjectsSortMode.None);
            activeEnemiesCount = 0;
            foreach (var e in enemies)
            {
                if (e != null && !e.IsDead) activeEnemiesCount++;
            }

            Rigidbody[] rbs = FindObjectsByType<Rigidbody>(FindObjectsSortMode.None);
            activeRagdollRbCount = 0;
            foreach (var rb in rbs)
            {
                if (rb != null && !rb.isKinematic && rb.gameObject.activeInHierarchy)
                    activeRagdollRbCount++;
            }

            // Cache formatted display string
            UpdateCachedString();

            fpsAccumulator = 0f;
            fpsFrames = 0;
            lastTextUpdateTime = Time.unscaledTime;
        }
    }

    private void CalculateOnePercentLow()
    {
        if (frameTimeHistory.Count < 20) return;

        List<float> sorted = new List<float>(frameTimeHistory);
        sorted.Sort();

        // 99th percentile frame time = 1% lowest FPS
        int index99 = Mathf.Clamp((int)(sorted.Count * 0.99f), 0, sorted.Count - 1);
        float worstFrameTime = sorted[index99];
        if (worstFrameTime > 0.0001f)
        {
            onePercentLowFps = 1f / worstFrameTime;
        }
    }

    private void UpdateCachedString()
    {
        if (isCompact)
        {
            cachedDisplayString = $"FPS: {currentFps:F0} ({currentFrameTimeMs:F1}ms) | 1% Low: {onePercentLowFps:F0} | RAM: {allocatedMemoryMB:F1}MB";
        }
        else
        {
            cachedDisplayString =
                $"<b>[LIVE PERFORMANCE ANALYZER]</b>  (Toggle: F3 | Mode: F4)\n" +
                $"────────────────────────────────────────\n" +
                $"<b>FPS:</b> {currentFps:F1} FPS  ({currentFrameTimeMs:F1} ms)\n" +
                $"<b>Stability:</b> 1% Low: {onePercentLowFps:F1} | Min: {minFps:F0} | Max: {maxFps:F0}\n" +
                $"<b>GC Heap:</b> {allocatedMemoryMB:F1} MB  (Collections: {gcCollections})\n" +
                $"<b>Physics RBs:</b> {activeRagdollRbCount} active Rigidbodies\n" +
                $"<b>Combat:</b> {activeEnemiesCount} active foes | TimeScale: {currentTimeScale:F2}x\n" +
                $"────────────────────────────────────────\n" +
                $"<b>Engine:</b> Unity {Application.unityVersion} | Target: {targetFrameRate} FPS";
        }
    }

    private void OnGUI()
    {
        if (!isVisible) return;

        InitStyles();

        float padding = 12f;
        float width = isCompact ? 340f : 380f;
        float height = isCompact ? 34f : 178f;
        float x = Screen.width - width - padding;
        float y = padding;

        Rect rect = new Rect(x, y, width, height);

        // Dark translucent background card
        GUI.Box(rect, GUIContent.none, boxStyle);

        // Color-coded status badge indicator dot (top-left of box)
        Color statusColor = (currentFps >= 55f) ? new Color(0.1f, 1f, 0.4f) :
                            (currentFps >= 30f) ? new Color(1f, 0.85f, 0.1f) :
                                                  new Color(1f, 0.2f, 0.2f);

        GUI.color = statusColor;
        GUI.DrawTexture(new Rect(x + 10f, y + (isCompact ? 11f : 12f), 8f, 8f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Content Text
        Rect textRect = new Rect(x + 24f, y + (isCompact ? 6f : 8f), width - 30f, height - 12f);
        GUI.Label(textRect, cachedDisplayString, labelStyle);
    }

    private void InitStyles()
    {
        if (boxStyle != null) return;

        // Solid translucent dark background texture
        bgTexture = new Texture2D(1, 1);
        bgTexture.SetPixel(0, 0, new Color(0.04f, 0.05f, 0.08f, 0.88f));
        bgTexture.Apply();

        boxStyle = new GUIStyle();
        boxStyle.normal.background = bgTexture;

        labelStyle = new GUIStyle();
        labelStyle.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (labelStyle.font == null) labelStyle.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        labelStyle.fontSize = isCompact ? 13 : 12;
        labelStyle.normal.textColor = new Color(0.92f, 0.94f, 0.98f, 1f);
        labelStyle.richText = true;
    }

    private void OnDestroy()
    {
        if (bgTexture != null) Destroy(bgTexture);
    }
}
