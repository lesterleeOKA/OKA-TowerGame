using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SocialPlatforms;

public class TowerGameController : GameBaseController
{
    public static TowerGameController Instance = null;
    public GameObject playerPrefab;
    public GameObject questionPrefab;
    public GameObject answerPrefab;
    public Transform parent;
    public GameObject YouWin;
    public List<CharacterController> characterControllers = new List<CharacterController>();
    public List<WS_Client.QuestionData> questions = new List<WS_Client.QuestionData>();
    public List<WS_Client.AnswerData> answers = new List<WS_Client.AnswerData>();
    private int playerID = 0;
    private float lastLogTime = 0f;

    // Map WS player key (string) -> CharacterController (ensures one GameObject per ws player)
    private Dictionary<string, CharacterController> playerControllersByKey = new Dictionary<string, CharacterController>();
    
    // Map question ID -> GameObject
    private Dictionary<string, GameObject> questionObjectsById = new Dictionary<string, GameObject>();
    
    // Map answer ID -> GameObject
    private Dictionary<string, GameObject> answerObjectsById = new Dictionary<string, GameObject>();


    protected override void Awake()
    {
        if (Instance == null) Instance = this;
        base.Awake();
    }

    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();
    }

    // Update is called once per frame
    void Update()
    {
        // If no data, nothing to do
        if (WS_Client.GameData == null) return;
        var players = WS_Client.GameData.players;
        if (players == null) return;

        this.playerNumber = players.Count;

        // Track which uids are currently present this frame
        var currentKeys = new HashSet<string>();

        // Get local player's uid (if available)
        int localUid = -1;
        if (WS_Client.Instance != null && WS_Client.Instance.pulic_UserInfo != null)
        {
            localUid = WS_Client.Instance.pulic_UserInfo.uid;
        }

        if (Time.time - lastLogTime >= 2f)
        {
            foreach (var p in players)
            {
                // Debug.Log($"  Player - ID: {p.player_id}, UID: {p.uid}, Pos: [{p.position[0]}, {p.position[1]}], Dest: [{p.destination[0]}, {p.destination[1]}]");
            }
            lastLogTime = Time.time;
        }

        // Create missing players and update positions for existing ones
        foreach (var player in players)
        {
            string key = !string.IsNullOrEmpty(player.player_id) ? player.player_id : player.uid.ToString();
            currentKeys.Add(key);

            bool isLocal = (player.uid == localUid);
            if (!playerControllersByKey.ContainsKey(key))
            {
                CreatePlayerFromData(player, Vector3.zero, key, isLocal);
            }

            if (!isLocal)
            {
                Vector3 otherPlayerPos = Vector3.zero;
                if (player.position != null && player.position.Length >= 2)
                {
                    otherPlayerPos = new Vector3(player.position[0], player.position[1], 0f);
                }

                var cc = playerControllersByKey[key];
                if (cc != null)
                {
                    // don't override local player's client-controlled transform
                    if (player.uid != localUid && !cc.IsLocalPlayer)
                    {
                         cc.transform.position = otherPlayerPos;
                    }
                }
            }
        }

        // Remove controllers for players who left
        var toRemove = new List<string>();
        foreach (var kv in playerControllersByKey)
        {
            if (!currentKeys.Contains(kv.Key))
            {
                toRemove.Add(kv.Key);
            }
        }

        foreach (var key in toRemove)
        {
            RemovePlayer(key);
        }

        // Process questions
        if (WS_Client.GameData.questions != null)
        {
            var currentQuestionIds = new HashSet<string>();
            
            for (int i = 0; i < WS_Client.GameData.questions.Count; i++)
            {
                var question = WS_Client.GameData.questions[i];
                currentQuestionIds.Add(question.id);
                
                if (!questionObjectsById.ContainsKey(question.id))
                {
                    // Create question at a position based on index (spread them out horizontally)
                    float spacing = 1000f; // Space between questions
                    float startX = -(WS_Client.GameData.questions.Count - 1) * spacing / 2f; // Center the questions
                    Vector3 questionPos = new Vector3(startX + (i * spacing), 800f, 0f); // Increased y from 300 to 800
                    CreateQuestionObject(question, questionPos);
                }
            }
            
            // Remove questions that no longer exist
            var questionsToRemove = new List<string>();
            foreach (var kv in questionObjectsById)
            {
                if (!currentQuestionIds.Contains(kv.Key))
                {
                    questionsToRemove.Add(kv.Key);
                }
            }
            foreach (var id in questionsToRemove)
            {
                RemoveQuestionObject(id);
            }
        }

        // Process answers
        if (WS_Client.GameData.answers != null)
        {
            var currentAnswerIds = new HashSet<string>();
            
            foreach (var answer in WS_Client.GameData.answers)
            {
                currentAnswerIds.Add(answer.id);
                
                if (!answerObjectsById.ContainsKey(answer.id))
                {
                    // Create answer at position from data
                    Vector3 answerPos = Vector3.zero;
                    if (answer.position != null && answer.position.Length >= 2)
                    {
                        answerPos = new Vector3(answer.position[0], answer.position[1], 0f);
                    }
                    CreateAnswerObject(answer, answerPos);
                }
                else
                {
                    // Update answer position if it exists
                    var answerObj = answerObjectsById[answer.id];
                    if (answerObj != null && answer.position != null && answer.position.Length >= 2)
                    {
                        Vector2 uiPosition = new Vector2(answer.position[0] * 1500f, answer.position[1] * 1500f);
                        RectTransform rectTransform = answerObj.GetComponent<RectTransform>();
                        if (rectTransform != null)
                        {
                            rectTransform.anchoredPosition = uiPosition;
                        }
                        else
                        {
                            answerObj.transform.position = new Vector3(uiPosition.x, uiPosition.y, 0f);
                        }
                    }
                }
            }
            
            // Remove answers that no longer exist
            var answersToRemove = new List<string>();
            foreach (var kv in answerObjectsById)
            {
                if (!currentAnswerIds.Contains(kv.Key))
                {
                    answersToRemove.Add(kv.Key);
                }
            }
            foreach (var id in answersToRemove)
            {
                RemoveAnswerObject(id);
            }
        }
    }

    private void CreatePlayerFromData(WS_Client.PlayerData player, Vector3 startPos, string key, bool isLocal = false)
    {
        // Instantiate without parent, set world position, then attach to parent preserving world pos

        var characterController = GameObject.Instantiate(this.playerPrefab, this.parent).GetComponent<CharacterController>();
        if (characterController == null)
        {
            Debug.LogError("playerPrefab missing CharacterController component");
            GameObject.Destroy(characterController.gameObject);
            return;
        }

        int uid = player.uid;
        characterController.gameObject.name = "Player_" + uid;
        characterController.UserName = "Player_" + uid;
        characterController.UserId = uid;
        if(isLocal) characterController.gameObject.tag = "MainPlayer";
        this.characterControllers.Add(characterController);

        // set world-space start position
        characterController.transform.position = startPos;

        // mark local player for client-side control
        characterController.IsLocalPlayer = isLocal;
        if (isLocal)
        {
            Debug.Log($"Local player created for uid={uid}");
        }

        playerControllersByKey[key] = characterController;
        characterController.key = key;

        // keep an incremental id for legacy naming if needed
        this.playerID = Mathf.Max(this.playerID, uid + 1);
        Debug.Log($"Created player GameObject for uid={uid} at {startPos} (isLocal={isLocal})");
    }

    private void RemovePlayer(string key)
    {
        if (playerControllersByKey.TryGetValue(key, out var cc))
        {
            if (cc != null)
            {
                this.characterControllers.Remove(cc);
                GameObject.Destroy(cc.gameObject);
                Debug.Log($"[TowerGameController] Removed player GameObject for key={key}");
            }
            playerControllersByKey.Remove(key);
        }
    }

    private void CreateQuestionObject(WS_Client.QuestionData question, Vector3 position)
    {
        if (questionPrefab == null)
        {
            Debug.LogError("questionPrefab is not assigned!");
            return;
        }

        var questionObj = GameObject.Instantiate(questionPrefab, this.parent);
        questionObj.name = "Question_" + question.id;
        
        // Use RectTransform for UI positioning
        RectTransform rectTransform = questionObj.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = new Vector2(position.x, position.y);
        }
        else
        {
            // Fallback to world position if not a UI element
            questionObj.transform.position = position;
        }
        
        // Set text on TextMeshProUGUI component in child
        TextMeshProUGUI textComponent = questionObj.GetComponentInChildren<TextMeshProUGUI>();
        if (textComponent != null)
        {
            textComponent.text = question.content;
        }
        
        // Add QuestionTrigger component for collision detection
        QuestionTrigger questionTrigger = questionObj.GetComponent<QuestionTrigger>();
        if (questionTrigger == null)
        {
            questionTrigger = questionObj.AddComponent<QuestionTrigger>();
        }
        questionTrigger.questionId = question.id;
        questionTrigger.questionData = question;
        Debug.Log($"Added QuestionTrigger component to {question.id}");
        
        questionObj.gameObject.SetActive(true);
        
        // Store the question data (you can add a component to store this if needed)
        // For now, just track the GameObject
        questionObjectsById[question.id] = questionObj;
        questions.Add(question);
        
    }

    private void RemoveQuestionObject(string id)
    {
        if (questionObjectsById.TryGetValue(id, out var questionObj))
        {
            if (questionObj != null)
            {
                GameObject.Destroy(questionObj);
                Debug.Log($"[TowerGameController] Removed question GameObject for id={id}");
            }
            questionObjectsById.Remove(id);
            
            // Remove from list
            questions.RemoveAll(q => q.id == id);
        }
    }

    private void CreateAnswerObject(WS_Client.AnswerData answer, Vector3 position)
    {
        if (answerPrefab == null)
        {
            Debug.LogError("answerPrefab is not assigned!");
            return;
        }

        var answerObj = GameObject.Instantiate(answerPrefab, this.parent);
        answerObj.name = "Answer_" + answer.id;
        
        // Scale position for UI (multiply by 500 for canvas coordinates)
        Vector2 uiPosition = new Vector2(position.x * 1500f, position.y * 1500f);
        
        // Use RectTransform for UI positioning
        RectTransform rectTransform = answerObj.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = uiPosition;
        }
        else
        {
            // Fallback to world position if not a UI element
            answerObj.transform.position = new Vector3(uiPosition.x, uiPosition.y, position.z);
        }
        
        // Set text on TextMeshProUGUI component in child
        TextMeshProUGUI textComponent = answerObj.GetComponentInChildren<TextMeshProUGUI>();
        if (textComponent != null)
        {
            textComponent.text = answer.content;
        }
        
        AnswerTrigger answerTrigger = answerObj.GetComponent<AnswerTrigger>();
        if (answerTrigger == null)
        {
            answerTrigger = answerObj.AddComponent<AnswerTrigger>();
        }
        answerTrigger.answerId = answer.id;
        answerTrigger.answerData = answer;
        
        // BoxCollider2D boxCollider = answerObj.GetComponent<BoxCollider2D>();
        // if (boxCollider == null)
        // {
        //     boxCollider = answerObj.AddComponent<BoxCollider2D>();
        // }
        // boxCollider.isTrigger = true;
        // boxCollider.size = new Vector2(300f, 120f); 
        
        answerObj.gameObject.SetActive(true);
        
        // Store the answer data
        answerObjectsById[answer.id] = answerObj;
        answers.Add(answer);
    }

    private void RemoveAnswerObject(string id)
    {
        if (answerObjectsById.TryGetValue(id, out var answerObj))
        {
            if (answerObj != null)
            {
                GameObject.Destroy(answerObj);
                Debug.Log($"[TowerGameController] Removed answer GameObject for id={id}");
            }
            answerObjectsById.Remove(id);
            
            // Remove from list
            answers.RemoveAll(a => a.id == id);
        }
    }

    public void OnAnswerObjectTrigger(GameObject answerObject, string answerId, WS_Client.AnswerData answerData)
    {
        Debug.Log($"Answer {answerId} triggered - Content: {answerData?.content}");
        
        // Find and update the answer in GameData
        if (WS_Client.GameData?.answers != null)
        {
            WS_Client.AnswerData answer = WS_Client.GameData.answers.Find(a => a.id == answerId);
            if (answer != null)
            {
                answer.isOnPlayer = 1;
                Debug.Log($"Set answer {answerId} isOnPlayer to 1");
                
                // You can add more logic here:
                // - Send update to server
                // - Update UI
                // - Trigger effects
            }
            else
            {
                Debug.LogWarning($"Answer {answerId} not found in GameData.answers");
            }
        }
    }

    public void OnQuestionObjectTrigger(GameObject questionObject, string questionId, WS_Client.QuestionData questionData, string answerId, WS_Client.AnswerData answerData)
    {
        Debug.Log($"Player submitted answer {answerId} to question {questionId}");
        
        // Check if it's correct (already checked in QuestionTrigger, but double-check here)
        if (answerData.question_id == questionId)
        {
            Debug.Log("CORRECT ANSWER!");
            YouWin.SetActive(true);
            // You can add logic here:
            // - Show "You Win!" UI
            // - Update score
            // - Send to server
            // - Play victory animation
            // - Mark answer as submitted
            if (WS_Client.GameData?.answers != null)
            {
                WS_Client.AnswerData answer = WS_Client.GameData.answers.Find(a => a.id == answerId);
                if (answer != null)
                {
                    answer.isSubmitted = 1;
                    answer.isOnPlayer = 0; // Remove from player
                    Debug.Log($"Marked answer {answerId} as submitted");
                }
            }
        }
        else
        {
            Debug.LogWarning($"Answer {answerId} doesn't match question {questionId}!");
        }
    }
}
