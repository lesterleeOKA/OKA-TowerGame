using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AutoMovingGroup : MonoBehaviour
{
    [Header("Assign the player object to follow")]
    public Transform playerTransform;

    [Header("Distance behind the player")]
    public float followDistance = 250f;

    [Header("Rotation offset (degrees, e.g. 0 for right, 90 for up)")]
    public float rotationOffset = 90f;

    private Vector3 lastPlayerPosition;
    private Vector3 lastMoveDir = Vector3.right;

    public CanvasGroup textCg;
    public RawImage textBgImage;
    public TextMeshProUGUI answerText;
    private Vector3 velocity = Vector3.zero;
    public void Init(PlayerController _player)
    {
        if (_player != null)
        {
            var groupMoving = _player.GetComponent<GroupMoving>();
            groupMoving.textCg = this.textCg;
            groupMoving.textBgImage = this.textBgImage;
            groupMoving.answerText = this.answerText;
        }
        this.playerTransform = _player.transform;
        this.lastPlayerPosition = _player.transform.localPosition;
    }

    void LateUpdate()
    {
        if (this.playerTransform == null)
            return;

        Vector3 moveDir = this.playerTransform.localPosition - this.lastPlayerPosition;
        if (moveDir.sqrMagnitude > 0.001f)
        {
            this.lastMoveDir = moveDir.normalized;
        }

        Vector3 targetPos = this.playerTransform.localPosition - this.lastMoveDir * followDistance;
        float smoothTime = 0.02f;
        this.transform.localPosition = Vector3.SmoothDamp(this.transform.localPosition, targetPos, ref velocity, smoothTime);

        float angle = Mathf.Atan2(this.lastMoveDir.y, this.lastMoveDir.x) * Mathf.Rad2Deg + rotationOffset;
        this.transform.rotation = Quaternion.Euler(0, 0, angle);

        this.lastPlayerPosition = this.playerTransform.localPosition;

        if (this.answerText != null)
        {
            this.answerText.rectTransform.rotation = Quaternion.identity;
        }
    }
}