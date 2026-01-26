using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class AnswerTrigger : MonoBehaviour
{
    public int answerId;
    private CanvasGroup canvas;

    void Start()
    {
        canvas = GetComponent<CanvasGroup>();
    }

    void Update()
    {
        
    }

    public void checkAnswerVisibility() {
        WS_Client.AnswerData currentAnswerData = WS_Client.Instance.GameData?.answers?.Find(a => a.id == answerId);
        
        if (currentAnswerData != null)
        {
            // Set canvas alpha to 0 and disable trigger when isOnPlayer == 0
            if (currentAnswerData.isOnPlayer == 1)
            {
                if (canvas != null)
                {
                    canvas.alpha = 0;
                }
                Collider2D trigger = GetComponent<Collider2D>();
                if (trigger != null)
                {
                    trigger.enabled = false;
                }
            }
            else
            {
                if (canvas != null)
                {
                    canvas.alpha = 1;
                }
                Collider2D trigger = GetComponent<Collider2D>();
                if (trigger != null)
                {
                    trigger.enabled = true;
                }
            }
        }
        else
        {
            Debug.LogWarning($"Answer {answerId} not found in GameData.answers");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the colliding object is a player
        if (other.CompareTag("MainPlayer") || other.CompareTag("Player"))
        {
            OnPlayerEnterAnswer(other.gameObject);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // Called every frame while the collider is inside the trigger
        if (other.CompareTag("MainPlayer") || other.CompareTag("Player"))
        {
            // You can add continuous collision logic here if needed
        }
    }


    private void OnPlayerEnterAnswer(GameObject player)
    {
        // Custom logic when player enters the answer area
        
        // Show answer bubble on the player
        CharacterController characterController = player.GetComponent<CharacterController>();

        if (characterController.UserId != WS_Client.Instance.public_UserInfo.uid) return;

        if (characterController != null)
        {
            WS_Client.AnswerData currentAnswerData = WS_Client.Instance.GameData?.answers?.Find(a => a.id == answerId);
            characterController.answerObject = this.gameObject;
            characterController.showAnswerBubble(1, currentAnswerData?.content ?? "");
            if (TowerGameController.Instance != null && currentAnswerData != null)
            {
                TowerGameController.Instance.OnAnswerObjectTrigger(this.gameObject, answerId, currentAnswerData);
            }
        }
        else
        {
            Debug.LogWarning($"Player {player.name} does not have a CharacterController component!");
        }
        
        // Call TowerGameController to handle answer trigger
        
        
        // You can add more logic here:
        // - Update server that player is on this answer
        // - Allow player to pick up/select the answer with a key press
    }
}

