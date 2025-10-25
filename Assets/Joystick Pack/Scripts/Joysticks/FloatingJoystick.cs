using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class FloatingJoystick : Joystick
{
    public bool firstTouch = false;
    protected override void Start()
    {
        base.Start();
        //background.gameObject.SetActive(false);
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        if (!this.firstTouch)
        {
            this.firstTouch = true;
        }
        else
        {
            var targetPosition = ScreenPointToAnchoredPosition(eventData.position);
            background.anchoredPosition = targetPosition;
        }
        //background.gameObject.SetActive(true);
        base.OnPointerDown(eventData);
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        //background.gameObject.SetActive(false);
        base.OnPointerUp(eventData);
    }
}