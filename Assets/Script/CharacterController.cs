using UnityEngine;
using UnityEngine.EventSystems;

public class CharacterController : UserData
{
    public float followSpeed = 6f;
    public float acc = 2f;
    private float currectSpeed = 2f;
    private Animator animator;
    private Vector3 lastPosition;
    private Transform imageTransform;
    private Transform answerBubbleTransform;

    public string key = "";
    public bool IsLocalPlayer = false; // 新增：标记是否为本地玩家[7](@ref)
    public bool isMouseDown = false; // 新增：标记鼠标是否按下[7](@ref)


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
                UpdateAnimation();
            }
            else
            {
                animator.SetFloat("Speed", 0);
                animator.SetInteger("Direction", 0);
            }

            WS_Client.PositionData posData = new WS_Client.PositionData
            {
                x = this.transform.position.x,
                y = this.transform.position.y
            };

            WS_Client.PositionData destData = new WS_Client.PositionData
            {
                x = this.transform.position.x,
                y = this.transform.position.y
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
    }

    private void FollowMouse()
    {
        // 获取鼠标位置并转换为世界坐标[1,7](@ref)
        Vector3 mousePosition = Input.mousePosition;
        mousePosition = Camera.main.ScreenToWorldPoint(mousePosition);
        mousePosition.z = transform.position.z;

        // 计算距离并更新速度
        float distance = Vector3.Distance(transform.position, mousePosition);
        currectSpeed = Mathf.Min(currectSpeed + acc * Time.deltaTime, followSpeed);
        
        // 使用MoveTowards平滑移动[7](@ref)
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
                        imageTransform.localScale = new Vector3(-1f, 1f, 1f);
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
            animator.SetInteger("Direction", 0); // 停止
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