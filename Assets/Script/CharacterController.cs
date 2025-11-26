using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
    public CanvasGroup localPlayer;

    private Texture2D standTexture;
    private Texture2D walkTexture;
    private Image characterUIImage;
    private AspectRatioFitter aspectRatio;
    private Coroutine walkingCoroutine;
    public float textureAnimationFrameRate = 0.5f;

    void Start()
    {
        animator = GetComponent<Animator>();
        lastPosition = transform.position;
        imageTransform = transform.Find("image");
        answerBubbleTransform = transform.Find("AnswerBubble");
    }

    public void setLocalPlayer(bool _isLocalPlayer = false)
    {
        this.IsLocalPlayer = _isLocalPlayer;
        SetUI.Set(this.localPlayer, _isLocalPlayer);
    }

    public void SetCostumeTextures(Texture2D stand, Texture2D walk)
    {
        // Initialize image components if not done yet (in case this is called before Start)
        if (imageTransform == null)
        {
            imageTransform = transform.Find("image");
            if (imageTransform != null)
            {
                characterUIImage = imageTransform.GetComponent<Image>();
                aspectRatio = characterUIImage.GetComponent<AspectRatioFitter>();
                aspectRatio.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
                aspectRatio.aspectRatio = (float)stand.width / (float)stand.height;
            }
        }

        this.standTexture = stand;
        this.walkTexture = walk;

        // Apply the stand texture immediately (idle state)
        SetIdleTexture();
    }

    private void SetIdleTexture()
    {
        if (standTexture == null)
        {
            Debug.LogWarning($"SetIdleTexture: standTexture is NULL for {gameObject.name}");
            return;
        }

        if (characterUIImage != null)
        {
            characterUIImage.sprite = Sprite.Create(
                standTexture,
                new Rect(0, 0, standTexture.width, standTexture.height),
                new Vector2(0.5f, 0.5f)
            );
        }
        else
        {
            Debug.LogError($"SetIdleTexture: No image component found on {gameObject.name}! imageTransform={imageTransform != null}");
        }
    }

    // Start the walking animation
    private void PlayWalkingAnimation()
    {
        // If already walking or no walk texture, do nothing
        if (walkingCoroutine != null || walkTexture == null || standTexture == null) return;

        Debug.Log($"PlayWalkingAnimation starting: {walkTexture.width}x{walkTexture.height} - {standTexture.width}x{standTexture.height}");
        walkingCoroutine = StartCoroutine(WalkingAnimationCoroutine());
    }

    // Stop the walking animation
    private void StopWalkingAnimation()
    {
        if (walkingCoroutine != null)
        {
            StopCoroutine(walkingCoroutine);
            walkingCoroutine = null;
            SetIdleTexture();
        }
    }

    // Coroutine to alternate between walk and stand textures
    private IEnumerator WalkingAnimationCoroutine()
    {
        bool useWalkTexture = false;

        while (true)
        {
            Texture2D currentTexture = useWalkTexture ? walkTexture : standTexture;

            if (characterUIImage != null)
            {
                characterUIImage.sprite = Sprite.Create(
                    currentTexture,
                    new Rect(0, 0, currentTexture.width, currentTexture.height),
                    new Vector2(0.5f, 0.5f)
                );
            }

            // Toggle between textures
            useWalkTexture = !useWalkTexture;

            // Wait for the frame duration
            yield return new WaitForSeconds(1f / textureAnimationFrameRate);
        }
    }


    //Fixed the touch and mouse click conflict with UI Buttons
    private bool IsPointerOverUIButton()
    {
        if (EventSystem.current == null) return false;

        PointerEventData pointerData = new PointerEventData(EventSystem.current);
        if (Input.touchCount > 0)
        {
            pointerData.position = Input.GetTouch(0).position;
        }
        else
        {
            pointerData.position = Input.mousePosition;
        }

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (var r in results)
        {
            if (r.gameObject == null) continue;
            // check for Button component on the hit GameObject or any parent
            if (r.gameObject.GetComponent<Button>() != null)
                return true;
            // sometimes the Button component is on a parent; walk up
            Transform t = r.gameObject.transform;
            while (t.parent != null)
            {
                t = t.parent;
                if (t.GetComponent<Button>() != null) return true;
            }
        }

        return false;
    }

    void Update()
    {
        if(this.IsLocalPlayer)
        {
            if (this.IsPointerOverUIButton())
            {
                return;
            }

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