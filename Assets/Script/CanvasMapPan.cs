using UnityEngine;

public class CanvasMapPan : MonoBehaviour
{
    [Tooltip("RectTransform of the whole map (must be child of the Canvas).")]
    public RectTransform mapRect;

    [Tooltip("RectTransform of the player (must be child of the mapRect).")]
    public RectTransform playerRect;

    [Tooltip("RectTransform of the Canvas (root).")]
    public RectTransform canvasRect;

    [Tooltip("Where on screen the player should be kept (0,0 = center).")]
    public Vector2 followTarget = Vector2.zero;

    [Tooltip("Smooth time for panning.")]
    public float smoothTime = 0.12f;

    private Vector2 velocity;

    void LateUpdate()
    {
        if(mapRect == null) mapRect = GameObject.FindGameObjectWithTag("GameBackground").GetComponent<RectTransform>();
        if (playerRect == null) { 
            if(GameObject.FindGameObjectWithTag("MainPlayer") != null)
                playerRect = GameObject.FindGameObjectWithTag("MainPlayer").GetComponent<RectTransform>();
        }
        if (playerRect == null || canvasRect == null) return;

        // Player position relative to map center
        Vector2 playerLocal = playerRect.anchoredPosition;

        // Desired map anchored position so player appears at followTarget in canvas space:
        Vector2 desiredMapPos = followTarget - playerLocal;

        // Compute clamped range so map does not reveal outside the texture.
        Vector2 mapSize = mapRect.rect.size;
        Vector2 canvasSize = canvasRect.rect.size;

        // If map is larger than canvas, compute half difference as clamp extents.
        Vector2 halfDiff = (mapSize - canvasSize) * 0.5f;

        Vector2 min = new Vector2(-halfDiff.x, -halfDiff.y);
        Vector2 max = new Vector2(halfDiff.x, halfDiff.y);

        // If map is smaller than canvas, center it
        if (mapSize.x <= canvasSize.x)
        {
            desiredMapPos.x = 0f;
        }
        else
        {
            desiredMapPos.x = Mathf.Clamp(desiredMapPos.x, min.x, max.x);
        }

        if (mapSize.y <= canvasSize.y)
        {
            desiredMapPos.y = 0f;
        }
        else
        {
            desiredMapPos.y = Mathf.Clamp(desiredMapPos.y, min.y, max.y);
        }

        // Smoothly move the map anchored position
        Vector2 current = mapRect.anchoredPosition;
        Vector2 smoothed = Vector2.SmoothDamp(current, desiredMapPos, ref velocity, smoothTime);
        mapRect.anchoredPosition = smoothed;
    }
}