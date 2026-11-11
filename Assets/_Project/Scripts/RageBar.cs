using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class RageBar : MonoBehaviour
{

    [Header("UI Sliders")]
    public Slider rageSlider;

    [Header("Target")]
    [SerializeField] private BeastOfDarkness targetBeast;

    [Header("Optional Prompt (e.g. PRESS [R] Text)")]
    [SerializeField] private GameObject readyPrompt;

    private void Awake(){
        if (rageSlider == null)
            {
                rageSlider = GetComponent<Slider>();
            }

            // Auto-find Guts if not dragged in Inspector
            if (targetBeast == null)
            {
                GameObject player = GameObject.Find("player_controller") ??
                GameObject.FindWithTag("Player") ??
                GameObject.Find("player");
                if (player != null)
                {
                    targetBeast = player.GetComponent<BeastOfDarkness>() ??
                    player.GetComponentInChildren<BeastOfDarkness>();
                }
            }
    }

    private void OnEnable()
    {
        if (targetBeast != null)
        {
            targetBeast.onRageChanged += HandleRageChanged;
        }
    }

    private void OnDisable()
    {
        if (targetBeast != null)
        {
            targetBeast.onRageChanged -= HandleRageChanged;
        }
    }

    private void Start()
    {
        if (targetBeast != null)
        {
            HandleRageChanged(targetBeast.currentRage, targetBeast.maxRage); // Initial sync on boot
        }
    }


    private void HandleRageChanged(float current, float max)
    {
        if (rageSlider != null)
        {
            rageSlider.maxValue = max;
            rageSlider.value = current;
        }

        if (readyPrompt != null && targetBeast != null)
        {
            bool isFull = current >= max && !targetBeast.IsActive;
            readyPrompt.SetActive(isFull);
        }
    }
}
