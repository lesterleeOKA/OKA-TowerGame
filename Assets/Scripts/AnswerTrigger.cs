using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class AnswerTrigger : MonoBehaviour
{
    public int answerId;
    public WS_Client.AnswerData answerData;

    void Update()
    {
        // Look up current answer data from GameData (not the cached copy)
        WS_Client.AnswerData currentAnswerData = WS_Client.Instance.GameData?.answers?.Find(a => a.id == answerId);
        
        if (currentAnswerData != null)
        {
            // Update the cached answerData reference
            answerData = currentAnswerData;
            
            // Show/hide answer based on isOnPlayer value
            gameObject.SetActive(currentAnswerData.isOnPlayer == 0);
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
        if (characterController != null)
        {
            characterController.showAnswerBubble(1);
            characterController.answerObject = this.gameObject;
            characterController.transform.Find("AnswerBubble").GetComponentInChildren<TextMeshProUGUI>().text = answerData.content;
            if (TowerGameController.Instance != null)
            {
                TowerGameController.Instance.OnAnswerObjectTrigger(this.gameObject, answerId, answerData);
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

