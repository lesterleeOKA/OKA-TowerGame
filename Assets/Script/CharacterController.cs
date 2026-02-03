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
    public CharacterAnimation characterAnimation;
    public RawImage characterUIImage;
    public float textureAnimationFrameRate = 2f;
    private Vector3 smoothVelocity = Vector3.zero;
    private float smoothTime = 0.08f; // tune this to reduce fling; lower = snappier, higher = smoother
    private float maxMoveSpeed => followSpeed * (1 / TowerGameController.Instance.clientMapScale); // maximum units per second
    private static PointerEventData s_pointerEventData;
    private static List<RaycastResult> s_raycastResults = new List<RaycastResult>(8);


    void Start()
    {
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
    public void SetCostumeTextures(CharacterSet characterSet)
    {
        // Initialize image components if not done yet (in case this is called before Start)

        if(this.characterAnimation != null)
        {
            this.characterAnimation.characterSet = characterSet;
            this.characterAnimation.setIdling();
        }
    }


    //Fixed the touch and mouse click conflict with UI Buttons
    private bool IsPointerOverUIButton()
    {
        if (EventSystem.current == null) return false;

        // Reuse static PointerEventData and results to avoid allocations
        if (s_pointerEventData == null) s_pointerEventData = new PointerEventData(EventSystem.current);

        // Check touches first (multi-touch safe)
        for (int i = 0; i < Input.touchCount; ++i)
        {
            Touch t = Input.GetTouch(i);

            s_pointerEventData.pointerId = t.fingerId;
            s_pointerEventData.position = t.position;

            s_raycastResults.Clear();
            EventSystem.current.RaycastAll(s_pointerEventData, s_raycastResults);

            for (int r = 0; r < s_raycastResults.Count; ++r)
            {
                var go = s_raycastResults[r].gameObject;
                if (go == null) continue;

                // treat as UI only if it, or a parent, has a Button component
                if (go.GetComponent<Button>() != null) return true;
                Transform tt = go.transform;
                while (tt.parent != null)
                {
                    tt = tt.parent;
                    if (tt.GetComponent<Button>() != null) return true;
                }
            }
        }

        // Mouse fallback: raycast at mouse position and look specifically for Buttons.
        // Avoid EventSystem.current.IsPointerOverGameObject() because it often returns true for full-screen canvases/blocks.
        s_pointerEventData.pointerId = -1;
        s_pointerEventData.position = Input.mousePosition;

        s_raycastResults.Clear();
        EventSystem.current.RaycastAll(s_pointerEventData, s_raycastResults);

        for (int r = 0; r < s_raycastResults.Count; ++r)
        {
            var go = s_raycastResults[r].gameObject;
            if (go == null) continue;

            if (go.GetComponent<Button>() != null) return true;
            Transform tt = go.transform;
            while (tt.parent != null)
            {
                tt = tt.parent;
                if (tt.GetComponent<Button>() != null) return true;
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
                    if (transform.parent != null)
                        localDestination = transform.localPosition;
                    else
                        localDestination = transform.position;
                    smoothVelocity = Vector3.zero;
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
                        _ = WS_Client.Instance.UpdateServerPosition(posData, destData);
                    }
                }
            }
            UpdateAnimation();
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
                    var imageTransform = this.characterUIImage?.transform;
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

            if(this.characterAnimation == null) return;
            if ((!IsLocalPlayer && distance > 0.01f) || (IsLocalPlayer && isMouseDown))
            {
                this.characterAnimation.PlayWalking(1);
            }
            else
            {
                this.characterAnimation.setIdling();
            }
        }
        catch (System.Exception ex)
        {
            LogController.Instance.debugError($"Error in UpdateAnimation for {gameObject.name}: {ex.Message}");
        }
    }

    public void showAnswerBubble(int show, string _answer = "")
    {
        SetUI.Set(this.answerBubble, show == 1);
        if(this.answerText != null)
        {
            this.answerText.text = _answer;
        }   

        if(show==1) AudioController.Instance?.PlayAudio(9);
    }
}