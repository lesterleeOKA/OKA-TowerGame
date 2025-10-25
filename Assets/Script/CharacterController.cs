using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class CharacterController : MonoBehaviour
{
    public float followSpeed = 6f;
    public float acc = 2f;
    private float currectSpeed = 2f;
    private Animator animator;
    private Vector3 lastPosition;
    private Transform imageTransform;
    public GameObject controlPanel;
    public bool isMouseOverControlPanel = false;


    void Start()
    {
        //followSpeed = GameManager.Instance.playerSpeed;

        animator = GetComponent<Animator>();
        lastPosition = transform.position;
        imageTransform = transform.Find("image");

        EventTrigger trigger = controlPanel.AddComponent<EventTrigger>();

        EventTrigger.Entry entryEnter = new EventTrigger.Entry();
        entryEnter.eventID = EventTriggerType.PointerEnter;
        entryEnter.callback.AddListener((eventData) => { OnMouseEnterControlPanel(); });
        trigger.triggers.Add(entryEnter);

        EventTrigger.Entry entryExit = new EventTrigger.Entry();
        entryExit.eventID = EventTriggerType.PointerExit;
        entryExit.callback.AddListener((eventData) => { 
            OnMouseExitControlPanel();
            });
        trigger.triggers.Add(entryExit);
    }

    // Update is called once per frame
    void Update()
    {
        if (isMouseOverControlPanel)
        {
            
            FollowMouse();
            UpdateAnimation();
        }
        else
        {
            animator.SetFloat("Speed", 0);
            animator.SetInteger("Direction", 0);
        }
    }

    private void FollowMouse()
    {
        Vector3 mousePosition = Input.mousePosition;
        mousePosition = Camera.main.ScreenToWorldPoint(mousePosition);
        mousePosition.z = transform.position.z;

        //transform.position = Vector3.MoveTowards(transform.position, mousePosition, followSpeed * Time.deltaTime);

        float distance = Vector3.Distance(transform.position, mousePosition);

        currectSpeed = Mathf.Min(currectSpeed + acc * Time.deltaTime, followSpeed);
        transform.position = Vector3.MoveTowards(transform.position, mousePosition, currectSpeed * Time.deltaTime);
    }

    private void UpdateAnimation()
    {
        Vector3 movement = transform.position - lastPosition;
        lastPosition = transform.position;

        float speed = movement.magnitude;
        animator.SetFloat("Speed", speed);



        if (speed > 0.01f)
        {
            if (Mathf.Abs(movement.x) > Mathf.Abs(movement.y))
            {
                if (movement.x > 0)
                {
                    animator.SetInteger("Direction", 1); // 向右
                    if (imageTransform != null)
                    {
                        imageTransform.localScale = new Vector3(1f, 1f, 1f);
                    }
                }
                else
                {
                    animator.SetInteger("Direction", -1); // 向左
                    if (imageTransform != null)
                    {
                        imageTransform.localScale = new Vector3(-1f, 1f, 1f); // 反轉 X 軸

                    }
                }
            }
            else
            {
                if (movement.y > 0)
                {
                    animator.SetInteger("Direction", 2); // 向上
                }
                else
                {
                    animator.SetInteger("Direction", 1); // 向下
                }
            }
        }
        else
        {
            animator.SetInteger("Direction", 0); // 靜止
        }
    }

    public void TriggerCorrectAnimation()
    {
        animator.SetTrigger("Correct");
        imageTransform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
    }

    public void ResetTrigger()
    {
        animator.ResetTrigger("Correct");
        imageTransform.localScale = new Vector3(1f, 1f, 1f);
    }

    private void OnMouseEnterControlPanel()
    {
        //Debug.Log("Mouse Entered Control Panel");
        isMouseOverControlPanel = true;
    }

    private void OnMouseExitControlPanel()
    {
        //Debug.Log("Mouse Exited Control Panel");
        isMouseOverControlPanel = false;
    }
}
