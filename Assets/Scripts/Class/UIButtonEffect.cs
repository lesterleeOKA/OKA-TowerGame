using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using UnityEngine.UI;

[RequireComponent(typeof(EventTrigger))]
public class UIButtonEffect : MonoBehaviour
{
    private EventTrigger eventTrigger = null;
    public float scaleRatio = 0.75f;
    private Button buttonClick = null;
    public bool pointDown = false;
    public bool isFliped = false;
    public ButtonScaleType buttonScaleType = ButtonScaleType.ScaleAll;
    public enum ButtonScaleType
    {
        ScaleX,
        ScaleY,
        ScaleAll,
        ScaleCustomize
    }

    // Start is called before the first frame update
    void Start()
    {
        this.isFliped = this.GetComponent<RectTransform>().localScale.x < 0f;
        this.buttonClick = this.GetComponent<Button>();
        this.eventTrigger = this.GetComponent<EventTrigger>();
        EventTrigger.Entry pointerDownEntry = new EventTrigger.Entry();
        pointerDownEntry.eventID = EventTriggerType.PointerDown;
        pointerDownEntry.callback.AddListener(scaleSmallBtn);
        eventTrigger.triggers.Add(pointerDownEntry);

        EventTrigger.Entry pointerUpEntry = new EventTrigger.Entry();
        pointerUpEntry.eventID = EventTriggerType.PointerUp;
        pointerUpEntry.callback.AddListener(scaleToOriginal);
        eventTrigger.triggers.Add(pointerUpEntry);
    }

    void scaleSmallBtn(BaseEventData data)
    {
        if(this.buttonClick != null)
        {
            if (this.buttonClick.interactable)
            {
                switch (this.buttonScaleType)
                {
                    case ButtonScaleType.ScaleX:
                        this.transform.DOScaleX(this.isFliped ? -this.scaleRatio : this.scaleRatio, 0.3f);
                        break;
                    case ButtonScaleType.ScaleY:
                        this.transform.DOScaleY(this.scaleRatio, 0.3f);
                        break;
                    case ButtonScaleType.ScaleAll:
                        this.transform.DOScale(this.isFliped ? -this.scaleRatio : this.scaleRatio, 0.3f);
                        break;
                    case ButtonScaleType.ScaleCustomize:
                        Vector3 customScale = new Vector3(this.isFliped ? -this.scaleRatio : this.scaleRatio, this.scaleRatio, 1f);
                        this.transform.DOScale(customScale, 0.3f);
                        break;
                }
                this.pointDown = true;
            }
        }
        else
        {
            switch (this.buttonScaleType)
            {
                case ButtonScaleType.ScaleX:
                    this.transform.DOScaleX(this.isFliped ? -this.scaleRatio : this.scaleRatio, 0.3f);
                    break;
                case ButtonScaleType.ScaleY:
                    this.transform.DOScaleY(this.scaleRatio, 0.3f);
                    break;
                case ButtonScaleType.ScaleAll:
                    this.transform.DOScale(this.isFliped ? -this.scaleRatio : this.scaleRatio, 0.3f);
                    break;
                case ButtonScaleType.ScaleCustomize:
                    Vector3 customScale = new Vector3(this.isFliped ? -this.scaleRatio : this.scaleRatio, this.scaleRatio, 1f);
                    this.transform.DOScale(customScale, 0.3f);
                    break;
            }
            this.pointDown = true;
        }
    }

    void scaleToOriginal(BaseEventData data)
    {
        if (this.buttonClick != null)
        {
            if (this.buttonClick.interactable)
            {
                switch (this.buttonScaleType)
                {
                    case ButtonScaleType.ScaleX:
                        this.transform.DOScaleX(this.isFliped ? -1f : 1f, 0.3f);
                        break;
                    case ButtonScaleType.ScaleY:
                        this.transform.DOScaleY(1f, 0.3f);
                        break;
                    case ButtonScaleType.ScaleAll:
                        this.transform.DOScale(this.isFliped ? -1f : 1f, 0.3f);
                        break;
                    case ButtonScaleType.ScaleCustomize:
                        Vector3 customScale = new Vector3(this.isFliped ? -1f : 1f, 1f, 1f);
                        this.transform.DOScale(customScale, 0.3f);
                        break;
                }
                this.pointDown = false;
            }
        }
        else
        {
            switch (this.buttonScaleType)
            {
                case ButtonScaleType.ScaleX:
                    this.transform.DOScaleX(this.isFliped ? -1f : 1f, 0.3f);
                    break;
                case ButtonScaleType.ScaleY:
                    this.transform.DOScaleY(1f, 0.3f);
                    break;
                case ButtonScaleType.ScaleAll:
                    this.transform.DOScale(this.isFliped ? -1f : 1f, 0.3f);
                    break;
                case ButtonScaleType.ScaleCustomize:
                    Vector3 customScale = new Vector3(this.isFliped ? -1f : 1f, 1f, 1f);
                    this.transform.DOScale(customScale, 0.3f);
                    break;
            }
            this.pointDown = false;
        }
    }
}
