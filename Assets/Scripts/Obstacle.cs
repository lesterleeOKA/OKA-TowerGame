using System.Collections;
using UnityEngine;

public class Obstacle : MonoBehaviour
{
    public int cellId;
    public float minMoveDistance = 20f;
    public float maxMoveDistance = 60f;
    public float minMoveSpeed = 0.5f;
    public float maxMoveSpeed = 2f;

    private Vector2 startPos;
    private float moveDistance;
    private float moveSpeed;
    private Vector2 moveDir; // (1,0)=horizontal, (0,1)=vertical, (-1,0)=reverse horizontal, (0,-1)=reverse vertical
    private AudioSource audioEffect;
    public bool isPlayingAudio = false;

    void Start()
    {
        if(this.audioEffect == null) this.audioEffect = GetComponent<AudioSource>();
        // Initialize movement properties
        // this.RandomizeMovement();
    }

    void Update()
    {
        // float offset = Mathf.Sin(Time.time * moveSpeed) * moveDistance;
        // Vector2 newPos = startPos + moveDir * offset;
        // if (TryGetComponent<RectTransform>(out var rect))
        //     rect.anchoredPosition = newPos;
        // else
        //     transform.localPosition = (Vector3)newPos;
    }

    // Reset the position and reinitialize movement properties
    public void ResetPosition(Vector2 newStartPos)
    {
        this.startPos = newStartPos;
        // this.RandomizeMovement();
    }

    // Randomize movement properties
    private void RandomizeMovement()
    {
        int maxRandomId = 3;
        // 0 = idle, 1 = horizontal, 2 = vertical
        if(this.cellId > 31 && this.cellId < 40)
            maxRandomId = 2;

        int movementType = Random.Range(1, maxRandomId);

        if (movementType == 0)
        {
            // Idle: no movement
            moveDir = Vector2.zero;
            moveDistance = 0f;
            moveSpeed = 0f;
        }
        else
        {
            // Randomly choose direction (1 or -1)
            int dir = Random.value > 0.5f ? 1 : -1;
            if (movementType == 1)
                moveDir = new Vector2(dir, 0); // Horizontal
            else
                moveDir = new Vector2(0, dir); // Vertical

            // Randomize range and speed
            moveDistance = Random.Range(minMoveDistance, maxMoveDistance);
            moveSpeed = Random.Range(minMoveSpeed, maxMoveSpeed);
        }
    }

    public void PlayAudioEffect()
    {
        if(this.audioEffect != null)
        {
            StartCoroutine(this.playAudioAutoReset(this.audioEffect.clip.length));
        }
    }

    IEnumerator playAudioAutoReset(float _delay)
    {
        if (!this.isPlayingAudio)
        {
            this.isPlayingAudio = true;
            this.audioEffect.Play();
            yield return new WaitForSeconds(_delay);
            this.isPlayingAudio = false;
        }
    }
}
