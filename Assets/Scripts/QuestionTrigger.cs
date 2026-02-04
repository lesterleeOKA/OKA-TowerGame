using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Team
{
    BlueTeam = 0,
    OrangeTeam = 1
}

public class QuestionTrigger : MonoBehaviour
{
    public int questionId;
    [SerializeField]
    private Team team;
    
    // To get the integer value from the enum, use: (int)team
    // Example: int teamValue = (int)team; // Returns 1 for BlueTeam, 2 for OrangeTeam
    
    // public WS_Client.QuestionData questionData;

    // void Update()
    // {
    //     // Look up current question data from GameData (not the cached copy)
    //     WS_Client.QuestionData currentQuestionData = WS_Client.Instance.GameData?.questions?.Find(q => q.id == questionId);
        
    //     if (currentQuestionData != null)
    //     {
    //         // Update the cached questionData reference
    //         questionData = currentQuestionData;
            
    //         // Debug.Log($"Question {questionId} content: {currentQuestionData.content}");
            
    //         // You can add logic here to show/hide question based on game state
    //         // gameObject.SetActive(someCondition);
    //     }
    // }

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
        var client = WS_Client.Instance;
        var gameController = TowerGameController.Instance;
        CharacterController characterController = player.GetComponent<CharacterController>();
        if (characterController != null)
        {
            // Check if player has an answer
            if (characterController.answerObject != null && gameController != null)
            {
                // Get the AnswerTrigger component from the answer GameObject
                AnswerTrigger answerTrigger = characterController.answerObject.GetComponent<AnswerTrigger>();
                if (answerTrigger != null)
                {
                    if (client.GameData.players != null) {
                        WS_Client.PlayerData clientPlayer = client.GameData.players.Find(p => p.uid == client.public_UserInfo.uid);
                        if (clientPlayer != null) {

                            // Find the index of the scoreboard matching the client player's key
                            int clientPlayerIndex = -1;
                            for (int i = 0; i < gameController.scoreboardControllers.Length; i++)
                            {
                                scoreboardController sb = gameController.scoreboardControllers[i].GetComponent<scoreboardController>();
                                if (sb != null && sb.key == clientPlayer.player_id)
                                {
                                    clientPlayerIndex = i;
                                    break;
                                }
                            }

                            if (clientPlayerIndex != -1 && clientPlayerIndex % 2 == (int)this.team) {
                                clientPlayer.answer_id = 0;
                                clientPlayer.answerContent = "";
                                clientPlayer.isAnswerVisible = 0;

                                _= client.submitAnswer(answerTrigger.answerId);
                            
                                characterController.showAnswerBubble(0, "");
                                characterController.answerObject = null;
                            }
                            
                        }
                    }
                }
            }
        }
    }
}

