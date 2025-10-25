using UnityEngine;
using UnityEngine.UI;

public class ShiningEffect : MonoBehaviour
{
    public float speed = 1f;
    public Color startColor = Color.white;
    public Color endColor = Color.yellow;
    private RawImage rawImage;
    public Material material;
    private Color initialColor; // Store initial color
    private float initialBrightness = 0f; // Store initial brightness
    private bool isEffectActive = false; // Track if the effect is active

    private void Start()
    {
        this.rawImage = GetComponent<RawImage>();
        this.initialColor = this.rawImage.color;
    }

    private void Update()
    {
        if (this.isEffectActive)
        {
            float lerp = Mathf.PingPong(Time.time * speed, 1f);
            this.rawImage.color = Color.Lerp(startColor, endColor, lerp);
            if (this.material != null)
            {
                float brightnessValue = Mathf.PingPong(Time.time * speed, 1f);
                this.material.SetFloat("_Brightness_Fade_1", brightnessValue);
            }
        }
    }

    private void OnEnable()
    {
        this.isEffectActive = true; // Set the effect as active
        if (this.material != null)
        {
            this.material.SetFloat("_Brightness_Fade_1", this.initialBrightness); // Reset to initial brightness
        }
    }

    private void OnDisable()
    {
        this.isEffectActive = false; // Set the effect as inactive
        if (this.material != null)
        {
            this.material.SetFloat("_Brightness_Fade_1", this.initialBrightness); // Reset to initial brightness
        }
        if (this.rawImage != null)
        {
            this.rawImage.color = initialColor; // Reset to initial color
        }
    }
}