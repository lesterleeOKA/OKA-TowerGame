using UnityEngine;
using UnityEngine.EventSystems;

public class BlockParentGesture : MonoBehaviour, IPointerDownHandler
{
    public void OnPointerDown(PointerEventData eventData)
    {
        // This will block the event from reaching the parent.
        // You can add custom logic here if needed.
    }
}
