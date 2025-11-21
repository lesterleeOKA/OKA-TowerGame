using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to your root Canvas. Forces ScaleWithScreenSize and prefers height matching so UI always fits
/// when browser height is smaller than design resolution. Also disables camera letterbox so world objects
/// are not clipped by camera.rect black bars.
/// </summary>
[RequireComponent(typeof(Canvas))]
[RequireComponent(typeof(CanvasScaler))]
public class CanvasResolutionController : MonoBehaviour
{
    public int referenceWidth = 2360;
    public int referenceHeight = 1640;
    [Tooltip("If true, prefer height matching (match = 1). If false, prefer width matching (match = 0).")]
    public bool preferHeight = true;

    CanvasScaler canvasScaler;

    void Awake()
    {
        canvasScaler = GetComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(referenceWidth, referenceHeight);
        canvasScaler.matchWidthOrHeight = preferHeight ? 1f : 0f;

        // Make sure Canvas fills the screen (important for WebGL)
        var canvas = GetComponent<Canvas>();
        if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            // ScreenSpace - Overlay prevents camera rect letterboxing from hiding UI
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        // Ensure main camera uses full viewport (no letterbox)
        if (Camera.main != null)
        {
            Camera.main.rect = new Rect(0f, 0f, 1f, 1f);
        }
    }

    void OnRectTransformDimensionsChange()
    {
        // keep match mode stable on resize
        if (canvasScaler != null)
        {
            canvasScaler.referenceResolution = new Vector2(referenceWidth, referenceHeight);
            canvasScaler.matchWidthOrHeight = preferHeight ? 1f : 0f;
        }
    }
}