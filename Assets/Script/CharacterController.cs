using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CharacterController : UserData
{
    public Camera detectCamera;
    public float followSpeed = 1600f;
    public float acc = 640f;
    public GameObject answerObject;
    private float currectSpeed = 640f;
    private Vector3 lastPosition;
    private Transform imageTransform;
    public CanvasGroup answerBubble;
    public TMPro.TextMeshProUGUI answerText;
    public int direction = 0;
    public string key = "";
    public bool IsLocalPlayer = false; 
    private Vector3 localDestination = Vector3.zero;
    public bool isMouseDown = false; 
    public CanvasGroup localPlayer;
    // Network throttling
    private float lastNetworkUpdateTime = 0f;
    public float networkUpdateInterval = 0.1f; // Send updates every 0.1 seconds (10 times per second)
    private Texture2D standTexture;
    private Texture2D walkTexture;
    private Sprite standSprite;
    private Sprite walkSprite;
    private Image characterUIImage;
    private AspectRatioFitter aspectRatio;
    private Coroutine walkingCoroutine;
    public float textureAnimationFrameRate = 2f;
    private Vector3 smoothVelocity = Vector3.zero;
    private float smoothTime = 0.08f; // tune this to reduce fling; lower = snappier, higher = smoother
    private float maxMoveSpeed => followSpeed * (1 / TowerGameController.Instance.clientMapScale); // maximum units per second

    void Start()
    {
        lastPosition = transform.position;
        imageTransform = transform.Find("image");

        if (transform.parent != null)
            localDestination = transform.localPosition;
        else
            localDestination = transform.position;
    }
    public void setLocalPlayer(bool _isLocalPlayer = false)
    {
        this.IsLocalPlayer = _isLocalPlayer;
        SetUI.Set(this.localPlayer, _isLocalPlayer);
    }
    public void setPlayerTag(Sprite tag)
    {
        Transform playerTagTransform = transform.Find("playerTag");
        if (playerTagTransform != null)
        {
            Image playerTagImage = playerTagTransform.GetComponent<Image>();
            if (playerTagImage != null)
            {
                playerTagImage.sprite = tag;
            }
        }
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
        // Create and cache sprites to avoid creating them during animation
        this.standSprite = Sprite.Create(
            standTexture,
            new Rect(0, 0, standTexture.width, standTexture.height),
            new Vector2(0.5f, 0.5f)
        );
        this.walkSprite = Sprite.Create(
            walkTexture,
            new Rect(0, 0, walkTexture.width, walkTexture.height),
            new Vector2(0.5f, 0.5f)
        );
        // Apply the stand texture immediately (idle state)
        this.SetIdleTexture();
    }
    private void SetIdleTexture()
    {
        if (standSprite == null)
        {
            LogController.Instance.debug($"SetIdleTexture: standSprite is NULL for {gameObject.name}");
            return;
        }
        if (characterUIImage != null)
        {
            characterUIImage.sprite = standSprite;
        }
        else
        {
            LogController.Instance.debugError($"SetIdleTexture: No image component found on {gameObject.name}! imageTransform={imageTransform != null}");
        }
    }
    // Start the walking animation
    private void PlayWalkingAnimation()
    {
        try
        {
            // If already walking or no cached sprites, do nothing
            if (walkingCoroutine != null || walkSprite == null || standSprite == null) return;

            walkingCoroutine = StartCoroutine(WalkingAnimationCoroutine());
        }
        catch (System.Exception ex)
        {
            LogController.Instance.debugError($"Error starting walking animation for {gameObject.name}: {ex.Message}");
            walkingCoroutine = null;
        }
    }
    // Stop the walking animation
    private void StopWalkingAnimation()
    {
        try
        {
            if (walkingCoroutine != null)
            {
                StopCoroutine(walkingCoroutine);
                walkingCoroutine = null;
                SetIdleTexture();
            }
        }
        catch (System.Exception ex)
        {
            LogController.Instance.debugError($"Error stopping walking animation for {gameObject.name}: {ex.Message}");
            // Ensure we reset the coroutine reference even if StopCoroutine fails
            walkingCoroutine = null;
        }
    }
    // Coroutine to alternate between walk and stand textures
    private IEnumerator WalkingAnimationCoroutine()
    {
        bool useWalkSprite = false;

        while (true)
        {
            // Use cached sprites instead of creating new ones
            Sprite currentSprite = useWalkSprite ? walkSprite : standSprite;

            if (characterUIImage != null && currentSprite != null)
            {
                characterUIImage.sprite = currentSprite;
            }

            // Toggle between sprites
            useWalkSprite = !useWalkSprite;

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
        try
        {
            if(this.IsLocalPlayer)
            {
                if (this.IsPointerOverUIButton())
                {
                    isMouseDown = false;
                    if (transform.parent != null)
                        localDestination = transform.localPosition;
                    else
                        localDestination = transform.position;
                    smoothVelocity = Vector3.zero;
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
                    if (transform.parent != null)
                        localDestination = transform.localPosition;
                    else
                        localDestination = transform.position;
                    smoothVelocity = Vector3.zero;
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
                // Throttle network updates
                if (Time.time - lastNetworkUpdateTime >= networkUpdateInterval)
                {
                    lastNetworkUpdateTime = Time.time;
                    
                    // Check WS_Client.Instance exists before using it
                    if (WS_Client.Instance != null)
                    {
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

                        // Debug.Log("UpdateServerPosition: posData=" + posData.x + " - " + posData.y + " - destData=" + destData.x + " - " + destData.y);
                        WS_Client.Instance.UpdateServerPosition(posData, destData);
                    }
                }
            }
            UpdateAnimation();
            lastPosition = transform.localPosition;
        }
        catch (System.Exception ex)
        {
            LogController.Instance.debugError($"Error in CharacterController.FixedUpdate for {gameObject.name}: {ex.Message}\n{ex.StackTrace}");
        }
    }

    void LateUpdate()
    {
        Vector3 current = (transform.parent != null) ? transform.localPosition : transform.position;
        Vector3 target = localDestination;

        float dt = Time.deltaTime;
        float maxStep = maxMoveSpeed * dt;

        // Smooth damp towards target. Use maxStep limiter to avoid large per-frame jumps.
        Vector3 next = Vector3.SmoothDamp(current, target, ref smoothVelocity, smoothTime, maxMoveSpeed, dt);

        // clamp per-frame movement to maxStep to avoid fling when target suddenly jumps
        Vector3 delta = next - current;
        if (delta.magnitude > maxStep)
        {
            next = current + delta.normalized * maxStep;
            smoothVelocity = Vector3.zero;
        }

        // Stop threshold: snap to target and clear velocity when very close
        const float stopThreshold = 0.02f;
        if ((target - current).magnitude <= stopThreshold)
        {
            smoothVelocity = Vector3.zero;
            next = target;
        }

        if (transform.parent != null)
        {
            transform.localPosition = new Vector3(next.x, next.y, transform.localPosition.z);
        }
        else
        {
            transform.position = new Vector3(next.x, next.y, transform.position.z);
        }
    }

    private void calLocalDestination()
    {
        if (this.detectCamera == null) this.detectCamera = Camera.main;

        // Get input position from touch or mouse
        Vector3 inputPosition;
        if (Input.touchCount > 0)
            inputPosition = Input.GetTouch(0).position;
        else
            inputPosition = Input.mousePosition;

        // Convert screen point to world point on plane of character z
        Ray ray = (this.detectCamera != null) ? this.detectCamera.ScreenPointToRay(inputPosition) : Camera.main.ScreenPointToRay(inputPosition);
        Plane plane = new Plane(Vector3.forward, new Vector3(0f, 0f, transform.position.z));
        if (plane.Raycast(ray, out float enter))
        {
            Vector3 worldPoint = ray.GetPoint(enter);

            // Set a target some distance in direction of the pointer, but do not snap
            Vector3 dir = (worldPoint - ((transform.parent != null) ? transform.parent.TransformPoint(transform.localPosition) : transform.position));
            if (dir.sqrMagnitude > 0.0001f)
            {
                dir.Normalize();
                Vector3 worldDestination = (transform.parent != null ? transform.parent.TransformPoint(transform.localPosition) : transform.position) + dir * maxMoveSpeed;

                // store as local if parent exists, otherwise world pos
                if (transform.parent != null)
                    localDestination = transform.parent.InverseTransformPoint(worldDestination);
                else
                    localDestination = worldDestination;

                // keep z consistent
                localDestination.z = transform.localPosition.z;
            }
        }
    }

    public void setLocalDestination(Vector3 destination)
    {
        localDestination = destination;
    }

    private void FollowLocalDestination()
    {
        float distance = Vector3.Distance(transform.localPosition, localDestination);

        if (!this.IsLocalPlayer && distance > 500f)
        {
            LogController.Instance.debug("Teleporting player due to large desync: distance=" + distance);
            transform.localPosition = new Vector3(localDestination.x, localDestination.y, transform.localPosition.z);
        }
        else if (distance > 10f)
        {
            var newFollowSpeed = followSpeed * (1 / TowerGameController.Instance.clientMapScale);
            currectSpeed = Mathf.Min(currectSpeed + acc * Time.deltaTime, newFollowSpeed);
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, localDestination, currectSpeed * Time.deltaTime);
        }
    }

    private void UpdateAnimation()
    {
        try
        {
            Vector3 movement = localDestination - transform.localPosition;
            float distance = Vector3.Distance(transform.localPosition, localDestination);

            float speed = movement.magnitude;

            // add a horizontal deadzone to avoid rapid left/right flips
            const float horizontalDeadzone = 0.15f;

            if (speed > 0f)
            {
                if (Mathf.Abs(movement.x) > horizontalDeadzone)
                {
                    if (movement.x > 0)
                    {
                        this.direction = 2; // 向右
                        if (imageTransform != null)
                        {
                            imageTransform.localScale = new Vector3(-1f, 1f, 1f);
                        }
                    }
                    else
                    {
                        this.direction = 1;// 向左
                        if (imageTransform != null)
                        {
                            imageTransform.localScale = new Vector3(1f, 1f, 1f);
                        }
                    }
                }
            }
            else
            {
                this.direction = 0;// 停止
            }

            if ((!IsLocalPlayer && distance > 0.01f) || (IsLocalPlayer && isMouseDown))
            {
                PlayWalkingAnimation();
            }
            else
            {
                StopWalkingAnimation();
            }
        }
        catch (System.Exception ex)
        {
            LogController.Instance.debugError($"Error in UpdateAnimation for {gameObject.name}: {ex.Message}");
        }
    }

    public void TriggerCorrectAnimation()
    {
        if (imageTransform != null)
        {
            imageTransform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
        }
    }

    public void ResetTrigger()
    {
        if (imageTransform != null)
        {
            imageTransform.localScale = new Vector3(1f, 1f, 1f);
        }
    }

    public void showAnswerBubble(int show, string _answer = "")
    {
        SetUI.Set(this.answerBubble, show == 1);
        if(this.answerText != null)
        {
            this.answerText.text = _answer;
        }   

        AudioController.Instance?.PlayAudio(9);
    }
}