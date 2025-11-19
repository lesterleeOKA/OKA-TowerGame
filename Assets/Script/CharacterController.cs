using TMPro;
using UnityEngine;

public class CharacterController : UserData
{
    public Camera detectCamera;
    public float followSpeed = 800f;
    public float acc = 320f;
    public GameObject answerObject;
    private float currectSpeed = 320f;
    private Animator animator;
    private Vector3 lastPosition;
    private Transform imageTransform;
    private Transform answerBubbleTransform;
    public int direction = 0;
    public string key = "";
    public bool IsLocalPlayer = false; 
    private Vector3 localDestination = Vector3.zero;
    public bool isMouseDown = false; 


    void Start()
    {
        animator = GetComponent<Animator>();
        lastPosition = transform.position;
        imageTransform = transform.Find("image");
        answerBubbleTransform = transform.Find("AnswerBubble");

    }

    void Update()
    {
        if(IsLocalPlayer)
        {
            // Handle both mouse and touch input
            if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
            {
                isMouseDown = true;
            }

            if (Input.GetMouseButtonUp(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended))
            {
                isMouseDown = false;
            }

            // Also set isMouseDown to false if no touches are detected
            if (Input.touchCount == 0 && !Input.GetMouseButton(0))
            {
                isMouseDown = false;
            }

            if (isMouseDown) 
            {
                calLocalDestination();
            }
            else
            {
                animator.SetFloat("Speed", 0);
                animator.SetInteger("Direction", 0);
            }

            WS_Client.PositionData posData = new WS_Client.PositionData
            {
                x = this.transform.localPosition.x,
                y = this.transform.localPosition.y,
            };

            WS_Client.PositionData destData = new WS_Client.PositionData
            {
                x = localDestination.x,
                y = localDestination.y,
            };

            WS_Client.Instance.UpdateServerPosition(posData, destData);
        }

        FollowLocalDestination();
    }

    private void calLocalDestination() {
        if(this.detectCamera == null) this.detectCamera = Camera.main;
        
        // Get input position from touch or mouse
        Vector3 inputPosition;
        if (Input.touchCount > 0)
        {
            inputPosition = Input.GetTouch(0).position;
        }
        else
        {
            inputPosition = Input.mousePosition;
        }

        // Convert screen position to world position
        // Set z to the distance from camera to the character's plane
        inputPosition.z = detectCamera.WorldToScreenPoint(transform.position).z;
        inputPosition = detectCamera.ScreenToWorldPoint(inputPosition);

        // Convert world position to local position (relative to parent)
        if (transform.parent != null)
        {
            localDestination = transform.parent.InverseTransformPoint(inputPosition);
        }
        else
        {
            localDestination = inputPosition;
        }
        
        // Maintain the character's local z position
        localDestination.z = transform.localPosition.z;
    }

    public void setLocalDestination(Vector3 destination)
    {
        localDestination = destination;
    }

    private void FollowLocalDestination()
    {
        float distance = Vector3.Distance(transform.localPosition, localDestination);

        // if (distance > 1000f)
        // {
        //     transform.localPosition = new Vector3(localDestination.x, localDestination.y, 0.1f);
        // }
        // else if (distance > 0.01f)
        if (distance > 0.01f)
        {
            currectSpeed = Mathf.Min(currectSpeed + acc * Time.deltaTime, followSpeed);
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, localDestination, currectSpeed * Time.deltaTime);
        }
        UpdateAnimation();
    }

    private void UpdateAnimation()
    {
        Vector3 movement = localDestination - transform.localPosition;

        float speed = movement.magnitude;
        animator.SetFloat("Speed", speed);

    //    Debug.Log("Speed:" + speed);

        if (speed > 0f)
        {
            // Debug.Log("movement x:" + movement.x);
            // Debug.Log("movement y:" + Mathf.Abs(movement.y));

            if (movement.x > 0)
            {
                this.direction = 2; // 向右
                if (imageTransform != null)
                {
                    imageTransform.localScale = new Vector3(1f, 1f, 1f);
                }
            } else {
                this.direction = 1;// 向左
                if (imageTransform != null)
                {
                    imageTransform.localScale = new Vector3(-1f, 1f, 1f);
                }
            }
            // else
            // {
            //     if (movement.y > 0)
            //     {
            //         this.direction = 2;// 向上
            //     }
            //     else
            //     {
            //         this.direction = 1;// 向下
            //     }
            // }
        }
        else
        {
            this.direction = 0;// 停止
        }

        animator.SetInteger("Direction", this.direction);
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

    public void showAnswerBubble(int show)
    {
        if (show == 1)
        {
            answerBubbleTransform.gameObject.SetActive(true);
        }
        else
        {
            answerBubbleTransform.gameObject.SetActive(false);
        }
    }
}