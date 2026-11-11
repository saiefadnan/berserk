using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHUD : MonoBehaviour
{
    [Header("Health Bar (Dark Souls Ghost Drain)")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider ghostHealthSlider;
    [SerializeField] private float ghostShrinkSpeed = 1.6f;
    [SerializeField] private float ghostDelay = 0.45f;

    [Header("Stamina Bar")]
    [SerializeField] private Slider staminaSlider;

    [Header("Beast Rage Gauge")]
    [SerializeField] private Slider rageSlider;
    [SerializeField] private TextMeshProUGUI ragePromptText;

    [Header("Arm Cannon Widget")]
    [SerializeField] private Image cannonReadyIcon;
    [SerializeField] private Image cannonCooldownOverlay; // Image type: Filled (Radial 360)

    private Health playerHealth;
    private Stamina playerStamina;
    private BeastOfDarkness playerBeast;
    private ShootMecha playerCannon;

    private float ghostTimer = 0f;

    private void Start()
    {
        FindPlayerReferences();
        SubscribeEvents();
    }

    private void FindPlayerReferences()
    {
        GameObject player = GameObject.FindWithTag("Player") ?? GameObject.Find("player");
        if (player == null) return;

        playerHealth = player.GetComponent<Health>();
        playerStamina = player.GetComponent<Stamina>();
        playerBeast = player.GetComponent<BeastOfDarkness>();
        playerCannon = player.GetComponent<ShootMecha>() ?? player.GetComponentInChildren<ShootMecha>();
    }

    private void SubscribeEvents()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += UpdateHealth;
            UpdateHealth(playerHealth.CurrentHealth, playerHealth.MaxHealth);
        }

        if (playerStamina != null)
        {
            playerStamina.OnStaminaChanged += UpdateStamina;
            UpdateStamina(playerStamina.CurrentStamina, playerStamina.MaxStamina);
        }

        if (playerBeast != null)
        {
            playerBeast.onRageChanged += UpdateRage;
            UpdateRage(playerBeast.currentRage, playerBeast.maxRage);
        }
    }

    private void Update()
    {
        HandleGhostHealth();
        HandleCannonWidget();
        HandleRagePulse();
    }

    private void UpdateHealth(float current, float max)
    {
        float ratio = (max > 0) ? current / max : 0f;
        if (healthSlider != null) healthSlider.value = ratio;
        ghostTimer = ghostDelay; // Reset lag countdown
    }

    private void HandleGhostHealth()
    {
        if (ghostHealthSlider == null || healthSlider == null) return;

        if (ghostTimer > 0)
        {
            ghostTimer -= Time.deltaTime;
        }
        else if (ghostHealthSlider.value > healthSlider.value)
        {
            ghostHealthSlider.value = Mathf.MoveTowards(ghostHealthSlider.value, healthSlider.value, ghostShrinkSpeed * Time.deltaTime);
        }
    }

    private void UpdateStamina(float current, float max)
    {
        if (staminaSlider != null)
        {
            staminaSlider.value = (max > 0) ? current / max : 0f;
        }
    }

    private void UpdateRage(float current, float max)
    {
        float ratio = (max > 0) ? current / max : 0f;
        if (rageSlider != null) rageSlider.value = ratio;

        if (ragePromptText != null)
        {
            bool canUnleash = ratio >= 0.99f && playerBeast != null && !playerBeast.IsActive;
            ragePromptText.gameObject.SetActive(canUnleash);
        }
    }

    private void HandleRagePulse()
    {
        if (ragePromptText != null && ragePromptText.gameObject.activeSelf)
        {
            // Subtle menacing demonic pulse on the [R] UNLEASH prompt
            float pulse = (Mathf.Sin(Time.time * 6f) + 1f) * 0.5f;
            Color c = ragePromptText.color;
            c.a = Mathf.Lerp(0.45f, 1f, pulse);
            ragePromptText.color = c;
        }
    }

    private void HandleCannonWidget()
    {
        if (playerCannon == null) return;

        if (cannonCooldownOverlay != null)
        {
            cannonCooldownOverlay.fillAmount = playerCannon.CooldownNormalized;
        }

        if (cannonReadyIcon != null)
        {
            cannonReadyIcon.color = playerCannon.IsCannonReady ? Color.white : new Color(0.35f, 0.35f, 0.35f, 0.65f);
        }
    }

    private void OnDestroy()
    {
        if (playerHealth != null) playerHealth.OnHealthChanged -= UpdateHealth;
        if (playerStamina != null) playerStamina.OnStaminaChanged -= UpdateStamina;
        if (playerBeast != null) playerBeast.onRageChanged -= UpdateRage;
    }
}
