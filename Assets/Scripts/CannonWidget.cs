using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CannonWidget : MonoBehaviour
{
    [Header("UI Elements")]
    public Image cannonIcon;
    public Image cooldownOverlay; // Image Type = Filled, Radial 360
    public TextMeshProUGUI cooldownText; // Optional: shows "READY" or "1.4s"

    [Header("Target")]
    [SerializeField] private ShootMecha targetCannon;

    private void Awake()
    {
        if (targetCannon == null)
        {
            GameObject player = GameObject.Find("player_controller") ??
                                GameObject.FindWithTag("Player") ??
                                GameObject.Find("player");
            if (player != null)
                targetCannon = player.GetComponent<ShootMecha>() ?? player.GetComponentInChildren<ShootMecha>();
        }
    }

    private void Update()
    {
        if (targetCannon == null) return;

        if (cooldownOverlay != null)
        {
            cooldownOverlay.fillAmount = targetCannon.CooldownNormalized;
        }

        if (cannonIcon != null)
        {
            cannonIcon.color = targetCannon.IsCannonReady ? Color.white : new Color(0.4f, 0.4f, 0.4f, 0.6f);
        }

        if (cooldownText != null)
        {
            if (targetCannon.IsCannonReady)
            {
                cooldownText.text = "<color=#FFAA00>READY</color>";
            }
            else
            {
                float remainingSeconds = targetCannon.CooldownNormalized * 2.0f;
                cooldownText.text = $"{remainingSeconds:F1}s";
            }
        }
    }
}
