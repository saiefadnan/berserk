using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    private Vector3 originalLocalPos;
    private Coroutine shakeCoroutine;

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

    void Start()
    {
        originalLocalPos = transform.localPosition;
    }

    public void Shake(float duration, float magnitude)
    {
        if (!gameObject.activeInHierarchy) return;

        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            transform.localPosition = originalLocalPos;
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
            float x = (Random.value * 2f - 1f) * magnitude * damper;
            float y = (Random.value * 2f - 1f) * magnitude * damper;

            transform.localPosition = originalLocalPos + new Vector3(x, y, 0f);

            // Unscaled time so hitstop micro-freezes don't pause the screen rattle
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        transform.localPosition = originalLocalPos;
        shakeCoroutine = null;
    }
}
