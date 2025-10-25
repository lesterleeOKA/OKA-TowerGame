using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target; // Assign your character's transform in the inspector
    public Vector3 offset = new Vector3(0, 0, -10); // Default offset for 2D

    public float smoothSpeed = 0.125f;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;
    }
}
