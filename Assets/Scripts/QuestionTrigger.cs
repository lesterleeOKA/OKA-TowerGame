using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuestionTrigger : MonoBehaviour
{
    public string questionId;
    public WS_Client.QuestionData questionData;

    void Update()
    {
        // Look up current question data from GameData (not the cached copy)
        WS_Client.QuestionData currentQuestionData = WS_Client.GameData?.questions?.Find(q => q.id == questionId);
        
        if (currentQuestionData != null)
        {
            // Update the cached questionData reference
            questionData = currentQuestionData;
            
            // Debug.Log($"Question {questionId} content: {currentQuestionData.content}");
            
            // You can add logic here to show/hide question based on game state
            // gameObject.SetActive(someCondition);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the colliding object is a player
        if (other.CompareTag("MainPlayer") || other.CompareTag("Player"))
        {
            Debug.Log($"Player entered question trigger: {questionId} - {questionData?.content}");
            OnPlayerEnterQuestion(other.gameObject);
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

    private void OnTriggerExit2D(Collider2D other)
    {
        // Check if the colliding object is a player
        if (other.CompareTag("MainPlayer") || other.CompareTag("Player"))
        {
            Debug.Log($"Player exited question trigger: {questionId} - {questionData?.content}");
            OnPlayerExitQuestion(other.gameObject);
        }
    }

    private void OnPlayerEnterQuestion(GameObject player)
    {
        // Custom logic when player enters the question area
        Debug.Log($"Player {player.name} is now on question {questionId}");
        
        // Get CharacterController component
        CharacterController characterController = player.GetComponent<CharacterController>();
        if (characterController != null)
        {
            // Check if player has an answer
            if (characterController.answerObject != null && TowerGameController.Instance != null)
            {
                // Get the AnswerTrigger component from the answer GameObject
                AnswerTrigger answerTrigger = characterController.answerObject.GetComponent<AnswerTrigger>();
                if (answerTrigger != null && answerTrigger.answerData != null)
                {
                    // Check if answer matches this question
                    if (answerTrigger.answerData.question_id == questionId)
                    {
                        Debug.Log($"Correct! Answer {answerTrigger.answerId} matches Question {questionId}");
                        
                        // Call TowerGameController with proper parameters
                        TowerGameController.Instance.OnQuestionObjectTrigger(this.gameObject, questionId, questionData, answerTrigger.answerId, answerTrigger.answerData);
                        
                        // Hide answer bubble and clear the answer
                        characterController.showAnswerBubble(0);
                        characterController.answerObject = null;
                    }
                    else
                    {
                        Debug.Log($"Wrong! Answer {answerTrigger.answerId} (for Q:{answerTrigger.answerData.question_id}) doesn't match Question {questionId}");
                    }
                }
            }
            Debug.Log($"Player entered question area: {questionData?.content}");
        }
        else
        {
            Debug.LogWarning($"Player {player.name} does not have a CharacterController component!");
        }
        
        // You can add more logic here:
        // - Show question UI
        // - Display question content
        // - Update server that player is viewing this question
    }

    private void OnPlayerExitQuestion(GameObject player)
    {
        // Custom logic when player exits the question area
        Debug.Log($"Player {player.name} left question {questionId}");
        
        // Get CharacterController component
        CharacterController characterController = player.GetComponent<CharacterController>();
        if (characterController != null)
        {
            // Hide question bubble or UI
            // characterController.showQuestionBubble(0);
            Debug.Log($"Player left question area");
        }
        
        // You can add more logic here:
        // - Hide question UI
        // - Update server that player left this question
    }
}

