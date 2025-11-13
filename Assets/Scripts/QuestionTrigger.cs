using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuestionTrigger : MonoBehaviour
{
    public int questionId;
    public WS_Client.QuestionData questionData;

    void Update()
    {
        // Look up current question data from GameData (not the cached copy)
        WS_Client.QuestionData currentQuestionData = WS_Client.Instance.GameData?.questions?.Find(q => q.id == questionId);
        
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

    private void OnPlayerEnterQuestion(GameObject player)
    {
        
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
                    WS_Client.Instance.submitAnswer(answerTrigger.answerId);
                        
                    characterController.showAnswerBubble(0);
                    characterController.answerObject = null;

                    if (WS_Client.Instance.GameData.players != null) {
                        WS_Client.PlayerData clientPlayer = WS_Client.Instance.GameData.players.Find(p => p.uid == WS_Client.Instance.public_UserInfo.uid);
                        if (clientPlayer != null) {
                            clientPlayer.answer_id = 0;
                            clientPlayer.answerContent = "";
                            clientPlayer.isAnswerVisible = 0;
                        }
                    }
                }
            }
        }
    }
}

