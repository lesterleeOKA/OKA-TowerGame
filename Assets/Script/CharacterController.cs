using TMPro;
using UnityEngine;

public class CharacterController : UserData
{
    public Camera detectCamera;
    public float followSpeed = 6f;
    public float acc = 2f;
    public GameObject answerObject;
    private float currectSpeed = 2f;
    private Animator animator;
    private Vector3 lastPosition;
    private Transform imageTransform;
    private Transform answerBubbleTransform;
    public int direction = 0;
    public string key = "";
    public bool IsLocalPlayer = false; 
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
            if (Input.GetMouseButtonDown(0))
            {
                isMouseDown = true;
            }

            if (Input.GetMouseButtonUp(0))
            {
                isMouseDown = false;
            }

            if (isMouseDown)
            {
                FollowMouse();
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
                x = this.transform.localPosition.x,
                y = this.transform.localPosition.y,
            };

            WS_Client.Instance.UpdateServerPosition(posData, destData);
        }
        
        if (Input.GetKeyDown(KeyCode.J))
        {
            showAnswerBubble(1);
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            showAnswerBubble(0);
        }

        this.UpdateAnimation();
    }

    private void FollowMouse()
    {
        if(this.detectCamera == null) this.detectCamera = Camera.main;
        Vector3 mousePosition = Input.mousePosition;
        mousePosition = this.detectCamera.ScreenToWorldPoint(mousePosition);
        mousePosition.z = transform.position.z;

        float distance = Vector3.Distance(transform.position, mousePosition);
        currectSpeed = Mathf.Min(currectSpeed + acc * Time.deltaTime, followSpeed);
        
        transform.position = Vector3.MoveTowards(transform.position, mousePosition, currectSpeed * Time.deltaTime);
    }

    private void UpdateAnimation()
    {
        Vector3 movement = transform.localPosition - lastPosition;
        lastPosition = transform.localPosition;

        float speed = movement.magnitude;
        animator.SetFloat("Speed", speed);

       // Debug.Log("Speed:" + speed);

        if (speed > 0.01f)
        {
            //Debug.Log("movement x:" + Mathf.Abs(movement.x));
            //Debug.Log("movement y:" + Mathf.Abs(movement.y));

            if (Mathf.Abs(movement.x) > Mathf.Abs(movement.y))
            {
                if (movement.x > 0)
                {
                    this.direction = 1; // 向右
                    if (imageTransform != null)
                    {
                        imageTransform.localScale = new Vector3(1f, 1f, 1f);
                    }
                }
                else
                {
                    this.direction = -1;// 向左
                    if (imageTransform != null)
                    {
                        imageTransform.localScale = new Vector3(-1f, 1f, 1f);
                    }
                }
            }
            else
            {
                if (movement.y > 0)
                {
                    this.direction = 2;// 向上
                }
                else
                {
                    this.direction = 1;// 向下
                }
            }
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