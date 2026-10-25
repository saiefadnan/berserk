using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class BeastVignette : MonoBehaviour
{
    [Header("Target & Camera")]
    [SerializeField] private BeastOfDarkness targetBeast;
    [SerializeField] private Camera playerCamera;

    [Header("Tunnel Vision Settings")]
    [SerializeField] private float normalFOV = 60f;
    [SerializeField] private float beastFOV = 52f;
    [SerializeField] private float fovTransitionSpeed = 4f;

    [Header("Vignette Pulse")]
    [SerializeField] private Color bloodColor = new Color(0.75f, 0.05f, 0.05f, 1f);
    [SerializeField] private float minPulseAlpha = 0.35f;
    [SerializeField] private float maxPulseAlpha = 0.70f;

    private Image vignetteImage;
    private float currentAlpha = 0f;

    private void Awake(){
        vignetteImage = GetComponent<Image>();
        if(playerCamera == null){
            playerCamera = Camera.main;
        }
        if(targetBeast == null){
            GameObject player = GameObject.Find("player_controller") ??
            GameObject.FindWithTag("Player") ??
            GameObject.Find("player");

            if(player != null){
                targetBeast = player.GetComponent<BeastOfDarkness>() ??
                player.GetComponentInChildren<BeastOfDarkness>();
            }
        }


        vignetteImage.color = new Color(bloodColor.r, bloodColor.g, bloodColor.b, 0f);
        vignetteImage.sprite = GenerateVignetteSprite();
        vignetteImage.type = Image.Type.Sliced;
    }

    private void Update()
    {
        bool isBeastActive = targetBeast != null && targetBeast.IsActive;

        float targetAlpha = 0f;
        float targetFOV = normalFOV;

        if(isBeastActive){
            float heartBeat = Mathf.Clamp01(Mathf.Sin(Time.time * 6f) * 0.7f + Mathf.Sin(Time.time * 12f) * 0.3f);
            targetFOV = beastFOV;
            targetAlpha = Mathf.Lerp(minPulseAlpha, maxPulseAlpha, heartBeat);
        }

        currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, 3.5f * Time.deltaTime);
        vignetteImage.color = new Color(bloodColor.r, bloodColor.g, bloodColor.b, currentAlpha);

        if(playerCamera != null){
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, fovTransitionSpeed * Time.deltaTime);
        }
    }

    private Sprite GenerateVignetteSprite(){
        int size = 256;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = size / 2f;
        Color[] pixels = new Color[size*size];


        for(int y = 0; y < size; y++){
            for(int x = 0; x < size; x++){
                float dx = (x - center) / center;
                float dy = (y - center) / center;
                float dist = Mathf. Sqrt(dx*dx + dy*dy);
                float fade = Mathf.Clamp01((dist - 0.4f) / 0.6f);
                float alpha = fade * fade;
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        tex.wrapMode = TextureWrapMode.Clamp;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
}
