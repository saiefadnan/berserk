using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }
    private float frequency = 25f;
    private Coroutine shakeCoroutine;
    private Vector3 currentShakeOffset = Vector3.zero;
    private Vector3 lastAppliedOffset = Vector3.zero;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(this);
            return;
        }
    }

    public void Shake(float duration, float magnitude)
    {
        if (!gameObject.activeInHierarchy) return;

        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
        }
        shakeCoroutine = StartCoroutine(DoShake(duration, magnitude));
    }

    private IEnumerator DoShake(float duration, float magnitude)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float progress = elapsed / duration;
            // Quadratic falloff for sharp impact kick and smooth decay
            float damper = 1f - (progress * progress);
            float x = (Mathf.PerlinNoise(Time.time * frequency, 0f) - 0.5f) * 2f * magnitude * damper;
            float y = (Mathf.PerlinNoise(0f, Time.time * frequency) - 0.5f) * 2f * magnitude * damper;

            currentShakeOffset = new Vector3(x, y, 0f);

            // Unscaled time so hitstop micro-freezes don't pause the screen rattle
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        currentShakeOffset = Vector3.zero;
        shakeCoroutine = null;
    }

    void LateUpdate()
    {
        // Revert last frame's shake offset so third-person camera controllers maintain their true position
        transform.localPosition -= lastAppliedOffset;

        // Apply this frame's dynamic shake offset
        transform.localPosition += currentShakeOffset;

        // Track what was applied
        lastAppliedOffset = currentShakeOffset;
    }

    void OnDisable()
    {
        // Safety cleanup if disabled during shake
        transform.localPosition -= lastAppliedOffset;
        currentShakeOffset = Vector3.zero;
        lastAppliedOffset = Vector3.zero;
    }
}
