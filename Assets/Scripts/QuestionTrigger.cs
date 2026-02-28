using TMPro;
using UnityEngine;

public enum Team
{
    FirstTeam = 0,
    SecondTeam = 1
}

public class QuestionTrigger : MonoBehaviour
{
    public int questionId;
    [SerializeField]
    private Team team;
    public TextMeshProUGUI correctAnswerText;    


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
            if (characterController != null && gameController != null)
            {
                // Get the AnswerTrigger component from the answer GameObject
                int answerId = characterController.answerId;
                if (client.GameData.players != null)
                {
                    WS_Client.PlayerData clientPlayer = client.GameData.players.Find(p => p.uid == client.public_UserInfo.uid);
                    if (clientPlayer != null)
                    {

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

                        if (clientPlayerIndex != -1 && clientPlayerIndex % 2 == (int)this.team)
                        {
                            if (this.correctAnswerText != null)
                            {
                                this.correctAnswerText.text = clientPlayer.answerContent;
                            }

                            TowerGameController.Instance?.showTeamGetScore((int)team);
                            clientPlayer.answer_id = 0;
                            clientPlayer.answerContent = "";
                            clientPlayer.isAnswerVisible = 0;

                            _ = client.submitAnswer(answerId);
                            characterController.showAnswerBubble(0, "");
                        }

                    }
                }
            }
        }
    }
}

