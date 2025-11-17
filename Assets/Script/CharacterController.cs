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
    private Camera cachedCamera; // Cache camera reference 


    void Start()
    {
        animator = GetComponent<Animator>();
        lastPosition = transform.position;
        imageTransform = transform.Find("image");
        answerBubbleTransform = transform.Find("AnswerBubble");
        
        // Cache camera reference to avoid repeated lookups
        cachedCamera = detectCamera != null ? detectCamera : Camera.main;
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
                FollowMouse();
            }
            else
            {
                animator.SetFloat("Speed", 0);
                animator.SetInteger("Direction", 0);
            }

            // Update GameData position instead of sending WebSocket every frame
            // Let WS_Client.ConstantSyncData() handle the actual syncing (runs every 0.1s)
            var wsInstance = WS_Client.Instance;
            if (wsInstance != null && wsInstance.GameData != null && wsInstance.public_UserInfo != null)
            {
                float[] currentPos = new float[] { this.transform.localPosition.x, this.transform.localPosition.y };
                wsInstance.UpdatePlayerPositionInGameData(wsInstance.public_UserInfo.uid, currentPos, currentPos);
            }
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
        // Use cached camera instead of checking every frame
        if(cachedCamera == null) cachedCamera = detectCamera != null ? detectCamera : Camera.main;
        
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
        
        inputPosition = cachedCamera.ScreenToWorldPoint(inputPosition);
        inputPosition.z = transform.position.z;

        currectSpeed = Mathf.Min(currectSpeed + acc * Time.deltaTime, followSpeed);
        
        transform.position = Vector3.MoveTowards(transform.position, inputPosition, currectSpeed * Time.deltaTime);
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