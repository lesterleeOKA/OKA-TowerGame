using TMPro;
using TouchScript.Gestures.TransformGestures;
using UnityEngine;
using UnityEngine.UI;

public class GroupMoving : MonoBehaviour
{
    public static GroupMoving Instance = null;
    private RectTransform rectTransform;
    public bool isDragging = false;
    public bool isMouseDragging = false;
    public EdgeCollider2D edgeCollider2D;
    private Vector2 targetPosition;
    public CanvasGroup textCg;
    public RawImage textBgImage;
    public TextMeshProUGUI answerText;
    private bool isRotating = false;
    private TransformGesture transformGesture = null;
   // private Tween moveTween;
    //public Ease dragMoveSmoothType = Ease.OutQuad;
   // public float movingDuration = 1f;
    private float lastRotation = 0f;
    public float PointerAllowDistance = 50f;
    public Submit submitItem;
    public bool isBlockingMovement = false;
    public float moveSpeed = 800f;
    private Vector2 velocity = Vector2.zero;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        rectTransform = GetComponent<RectTransform>();
        targetPosition = rectTransform.anchoredPosition;
        lastRotation = rectTransform.eulerAngles.z;
        transformGesture = GetComponent<TransformGesture>();
        if (transformGesture != null)
        {
            transformGesture.Transformed += OnTransformed;
        }
    }

    private void OnDestroy()
    {
        if (transformGesture != null)
        {
            transformGesture.Transformed -= OnTransformed;
        }
       // playerVisible[0] = false;
       // playerVisible[1] = false;
       // this.UpdatePlayerVisible();
    }

    public void OnDisable()
    {
        this.edgeCollider2D.enabled = false;
        this.GetComponent<TransformGesture>().enabled = false;
    }
    public void ResetMovingPlane()
    {
        /*if (moveTween != null && moveTween.IsActive())
        {
            moveTween.Kill();
        }*/
        rectTransform.anchoredPosition = Vector2.zero;
        targetPosition = Vector2.zero;
        rectTransform.localRotation = Quaternion.identity;
        lastRotation = 0f;
        this.ResetAnswerBoard();
    }

    public void ResetAnswerBoard()
    {
        this.setAnswer("", null);
        this.submitItem?.clear();
        this.isBlockingMovement = false;
    }

    private void OnTransformed(object sender, System.EventArgs e)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
    if (ExternalCaller.DeviceType == 1 && transformGesture.NumPointers < 2 && LoaderConfig.Instance.PlayerNumbers == 2) return;
#endif
        bool allPointersNearUI = true;
        Rect rect = rectTransform.rect;
        rect.xMin -= PointerAllowDistance;
        rect.xMax += PointerAllowDistance;
        rect.yMin -= PointerAllowDistance;
        rect.yMax += PointerAllowDistance;

        for (int i = 0; i < transformGesture.NumPointers; i++)
        {
            Vector2 screenPos = transformGesture.getPointScreenPosition(i);
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform,
                screenPos,
                Camera.main,
                out localPoint
            );
            if (!rect.Contains(localPoint))
            {
                allPointersNearUI = false;
                break;
            }
        }
        if (!allPointersNearUI) return;

        float moveAmount = ((Vector2)transformGesture.LocalDeltaPosition).magnitude;
        float rotateAmount = Mathf.Abs(transformGesture.DeltaRotation);

        if (moveAmount > rotateAmount * 2f)
        {
            targetPosition += (Vector2)transformGesture.LocalDeltaPosition;
        }

        rectTransform.Rotate(0, 0, transformGesture.DeltaRotation);
        float currentRotation = rectTransform.eulerAngles.z;
        float deltaRotation = Mathf.DeltaAngle(lastRotation, currentRotation);
        this.isRotating = Mathf.Abs(deltaRotation) > 5f;
        lastRotation = currentRotation;
    }

    public void setAnswer(string content, Texture textBg)
    {
        if (this.textCg != null && this.textBgImage != null)
        {
            this.textBgImage.texture = textBg;
            this.textCg.alpha = string.IsNullOrEmpty(content)? 0f : 1f;
            this.answerText.text = content;
            Material materialAnswer = this.answerText.fontSharedMaterial;
            if (materialAnswer != null)
            {
                materialAnswer.SetFloat(ShaderUtilities.ID_OutlineWidth, SetUI.ContainsChinese(content) ? 0f : 0.5f);
                materialAnswer.SetFloat(ShaderUtilities.ID_FaceDilate, SetUI.ContainsChinese(content) ? 0.3f : 1f);
            }
        }
    }


    Vector2[] GetWorldRectCorners(RectTransform rt)
    {
        Vector3[] worldCorners = new Vector3[4];
        rt.GetWorldCorners(worldCorners);
        return new Vector2[] {
        worldCorners[0], // Bottom Left
        worldCorners[1], // Top Left
        worldCorners[2], // Top Right
        worldCorners[3]  // Bottom Right
    };
    }

    Vector2[] GetPredictedWorldRectCorners(RectTransform rt, Vector2 anchoredPos, float angle)
    {
        // Get the parent transform (for world conversion)
        RectTransform parent = rt.parent as RectTransform;
        Vector2 size = Vector2.Scale(rt.sizeDelta, rt.localScale);
        Vector2 pivot = rt.pivot;

        // Calculate local corners
        Vector2[] localCorners = new Vector2[4];
        localCorners[0] = new Vector2(-pivot.x * size.x, -pivot.y * size.y); // Bottom Left
        localCorners[1] = new Vector2(-pivot.x * size.x, (1 - pivot.y) * size.y); // Top Left
        localCorners[2] = new Vector2((1 - pivot.x) * size.x, (1 - pivot.y) * size.y); // Top Right
        localCorners[3] = new Vector2((1 - pivot.x) * size.x, -pivot.y * size.y); // Bottom Right

        Quaternion rot = Quaternion.Euler(0, 0, angle);

        Vector2[] worldCorners = new Vector2[4];
        for (int i = 0; i < 4; i++)
        {
            // Apply rotation and position in local space
            Vector3 rotated = rot * localCorners[i];
            Vector3 localPos = rotated + (Vector3)anchoredPos;
            // Convert to world space
            worldCorners[i] = parent != null ? parent.TransformPoint(localPos) : rt.TransformPoint(localPos);
        }
        return worldCorners;
    }

    bool EdgeIntersectsPolygon(Vector2[] polyA, Vector2[] polyB)
    {
        int countA = polyA.Length;
        int countB = polyB.Length;
        for (int i = 0; i < countA; i++)
        {
            Vector2 a1 = polyA[i];
            Vector2 a2 = polyA[(i + 1) % countA];
            for (int j = 0; j < countB; j++)
            {
                Vector2 b1 = polyB[j];
                Vector2 b2 = polyB[(j + 1) % countB];
                if (SegmentsIntersect(a1, a2, b1, b2))
                    return true;
            }
        }
        // Also check if one polygon is completely inside the other
        if (PointInPolygon(polyA[0], polyB) || PointInPolygon(polyB[0], polyA))
            return true;
        return false;
    }

    // 判斷兩線段是否相交
    bool SegmentsIntersect(Vector2 p1, Vector2 p2, Vector2 q1, Vector2 q2)
    {
        float o1 = Orientation(p1, p2, q1);
        float o2 = Orientation(p1, p2, q2);
        float o3 = Orientation(q1, q2, p1);
        float o4 = Orientation(q1, q2, p2);

        if (o1 * o2 < 0 && o3 * o4 < 0)
            return true;
        return false;
    }

    float Orientation(Vector2 a, Vector2 b, Vector2 c)
    {
        return (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
    }

    // Point-in-polygon (ray casting)
    bool PointInPolygon(Vector2 point, Vector2[] poly)
    {
        int n = poly.Length;
        int j = n - 1;
        bool inside = false;
        for (int i = 0; i < n; j = i++)
        {
            if (((poly[i].y > point.y) != (poly[j].y > point.y)) &&
                (point.x < (poly[j].x - poly[i].x) * (point.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x))
                inside = !inside;
        }
        return inside;
    }

    bool IsBlockedByUIObstacle(Vector2 nextPosition, float nextAngle)
    {
        Vector2[] myCorners = GetPredictedWorldRectCorners(rectTransform, nextPosition, nextAngle);

        var obstacles = GameObject.FindGameObjectsWithTag("Obstacle");
        foreach (var obj in obstacles)
        {
            RectTransform obstacleRect = obj.GetComponent<RectTransform>();
            Vector2[] obsPoly = GetWorldRectCorners(obstacleRect);

            if (EdgeIntersectsPolygon(myCorners, obsPoly)) {
                return true;
            }
        }

        return false;
    }

    private void Update()
    {
        targetPosition = this.ClampToScreen(targetPosition);
        if (transformGesture.NumPointers == 0)
        {
            this.targetPosition = rectTransform.anchoredPosition;
            this.velocity = Vector2.zero;
        }

        // Only move if not rotating and not blocked
        if (!isRotating && /*!IsBlockedByUIObstacle(targetPosition, rectTransform.eulerAngles.z)*/ !this.isBlockingMovement)
        {
            float adjustedSpeed = moveSpeed * LoaderConfig.Instance.gameSetup.playersMovingSpeed;
            float smoothTime = 0.1f;

            rectTransform.anchoredPosition = Vector2.SmoothDamp(
                rectTransform.anchoredPosition,
                targetPosition,
                ref velocity,
                smoothTime,
                adjustedSpeed
            );
        }
        else if (/*IsBlockedByUIObstacle(targetPosition, rectTransform.eulerAngles.z)*/this.isBlockingMovement)
        {
            targetPosition = rectTransform.anchoredPosition;
        }

        if (this.isRotating && !this.isBlockingMovement)
        {
            float nextAngle = this.rectTransform.eulerAngles.z + this.transformGesture.DeltaRotation;
            if (!IsBlockedByUIObstacle(this.rectTransform.anchoredPosition, nextAngle))
            {
                this.rectTransform.rotation = Quaternion.Euler(0, 0, nextAngle);
            }
            else
            {
                this.isRotating = false;
            }

            this.targetPosition = this.rectTransform.anchoredPosition;
        }

        if (this.answerText != null)
        {
            this.answerText.rectTransform.rotation = Quaternion.identity;
        }
    }
    Vector2 ClampToScreen(Vector2 pos)
    {
        RectTransform parentRect = rectTransform.parent as RectTransform;
        Vector2 size = rectTransform.sizeDelta * rectTransform.lossyScale;
        Vector2 min = parentRect.rect.min + size / 2f;
        Vector2 max = parentRect.rect.max - size / 2f;

        pos.x = Mathf.Clamp(pos.x, min.x, max.x);
        pos.y = Mathf.Clamp(pos.y, min.y, max.y);
        return pos;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the other collider has a specific tag, e.g., "Player"
        if (other.CompareTag("Word"))
        {
            var cell = other.GetComponent<Cell>();
            if (cell != null)
            {
                cell.setCellEnterColor(true, GameController.Instance.showCells);

                foreach (var player in GameController.Instance.playerControllers)
                {
                    if (player == null) continue;
                    if (cell.isSelected && player.Retry > 0)
                    {
                        LogController.Instance.debug("Player has entered the trigger!" + other.name);
                        AudioController.Instance?.PlayAudio(9);

                        var gridManager = GameController.Instance.gridManager;
                        if (gridManager.isMCType)
                        {
                            if (player.collectedCell.Count > 0)
                            {
                                var latestCell = player.collectedCell[player.collectedCell.Count - 1];
                                latestCell.SetTextStatus(true);
                                player.collectedCell.RemoveAt(player.collectedCell.Count - 1);
                            }
                        }
                        player.setAnswer(cell.content.text);
                        this.setAnswer(cell.content.text, cell.cellTexture.texture);
                        player.collectedCell.Add(cell);
                    }
                }

                cell.SetTextStatus(false);
            }
        }
        else if (other.CompareTag("Submit") && !this.isBlockingMovement)
        {
            this.submitItem = other.GetComponent<Submit>();
            if(this.submitItem != null)
            {
                this.submitItem.setContent(this.textBgImage.texture, this.answerText.text);

                if (this.submitItem.IsContainContent)
                {
                    var gameTimer = GameController.Instance.gameTimer;
                    int currentTime = Mathf.FloorToInt(((gameTimer.gameDuration - gameTimer.currentTime) / gameTimer.gameDuration) * 100);

                    foreach (var player in GameController.Instance.playerControllers)
                    {
                        if (player == null) continue;
                        this.isBlockingMovement = true;
                        player.checkAnswer(currentTime, () =>
                        {
                            player.playerReset();
                            this.isBlockingMovement = false;
                        });
                    }
                    this.setAnswer("", null);
                }     
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            if (collision.gameObject.GetComponent<Obstacle>() != null && !this.isBlockingMovement)
            {
                LogController.Instance.debug("Player has entered the Obstacle Area!" + collision.gameObject.name);
                collision.gameObject.GetComponent<Obstacle>().PlayAudioEffect();
                this.isBlockingMovement = true;
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Obstacle")&& this.isBlockingMovement)
        {
            LogController.Instance.debug("Player has exited the Obstacle Area!" + collision.gameObject.name);
            this.isBlockingMovement = false;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Word"))
        {
            var cell = other.GetComponent<Cell>();
            if (cell != null)
            {
                cell.setCellEnterColor(false);
                if (cell.isSelected)
                {
                    LogController.Instance.debug("Player has exited the trigger!" + other.name);
                }
            }
        }
        else if (other.CompareTag("Submit"))
        {
            LogController.Instance.debug("Player has exited the Submit Area!" + other.name);
        }
    }
}