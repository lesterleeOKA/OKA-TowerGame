using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Small helper attached to minimap marker GameObjects (pooled or created).
/// Keeps cached Image/RawImage refs and exposes Initialize/ResetForPool methods
/// so marker state is always overwritten on reuse.
/// </summary>
public class MinimapMarkerView : MonoBehaviour
{
    public RectTransform RectTransform { get; private set; }
    public Image MarkerImage { get; private set; }
    public RawImage IconImage { get; private set; }

    // assigned key for debugging / lookup
    public string Key { get; private set; }

    void Awake()
    {
        RectTransform = GetComponent<RectTransform>();
        MarkerImage = GetComponent<Image>();
        IconImage = transform.Find("Icon")?.GetComponent<RawImage>();
    }

    /// <summary>
    /// Initialize or overwrite marker visuals. Call every time marker is (re)used.
    /// </summary>
    public void Initialize(string key, Sprite markerSprite, Texture iconTex, bool isLocal)
    {
        Key = key;

        // ensure rect & image exist
        if (RectTransform == null) RectTransform = gameObject.AddComponent<RectTransform>();
        if (MarkerImage == null) MarkerImage = gameObject.GetComponent<Image>() ?? gameObject.AddComponent<Image>();
        MarkerImage.sprite = markerSprite;
        MarkerImage.raycastTarget = false;

        // ensure icon child exists only for local player markers
        if (isLocal)
        {
            if (IconImage == null)
            {
                Transform t = transform.Find("Icon");
                if (t != null) IconImage = t.GetComponent<RawImage>();
            }

            if (IconImage == null)
            {
                var iconGo = new GameObject("Icon");
                iconGo.transform.SetParent(RectTransform, false);
                var rt = iconGo.AddComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
                rt.localScale = Vector3.one;
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(12f, 12f);

                IconImage = iconGo.AddComponent<RawImage>();
                IconImage.raycastTarget = false;
            }

            if (iconTex != null)
            {
                IconImage.enabled = true;
                IconImage.texture = iconTex;
                float aspect = (iconTex.width > 0 && iconTex.height > 0) ? (float)iconTex.width / iconTex.height : 1f;
                if (aspect >= 1f) IconImage.rectTransform.sizeDelta = new Vector2(75f, 75f / aspect);
                else IconImage.rectTransform.sizeDelta = new Vector2(75f * aspect, 75f);
            }
            else
            {
                IconImage.enabled = false;
            }
        }
        else
        {
            // Non-local: ensure icon hidden if it was present from previous usage
            if (IconImage != null) IconImage.enabled = false;
        }
    }

    /// <summary>
    /// Prepare marker for returning to pool / reuse. Should be called before Release().
    /// </summary>
    public void ResetForPool()
    {
        Key = null;
        gameObject.SetActive(false);
        // optionally clear sprite and icon to avoid visual bleed before reuse
        if (MarkerImage != null) MarkerImage.sprite = null;
        if (IconImage != null)
        {
            IconImage.texture = null;
            IconImage.enabled = false;
        }
    }
}