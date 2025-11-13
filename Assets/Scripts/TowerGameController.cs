using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SocialPlatforms;
using System.Text;

public class TowerGameController : GameBaseController
{
    public static TowerGameController Instance = null;
    public GameObject playerPrefab;
    public GameObject questionPrefab;
    // public GameObject questionUIText;
    public GameObject onTopUI;
    public GameObject answerPrefab;
    public GameObject obstaclePrefab;
    public Transform globalParent;
    public GameObject YouWin;
    public GameObject YouLose;

    public List<CharacterController> characterControllers = new List<CharacterController>();
    public List<WS_Client.QuestionData> questions = new List<WS_Client.QuestionData>();
    public List<WS_Client.AnswerData> answers = new List<WS_Client.AnswerData>();
    public Camera trackingCamera;
    private int playerID = 0;
    public Text debugText;

    // Map WS player key (string) -> CharacterController (ensures one GameObject per ws player)
    private Dictionary<string, CharacterController> playerControllersByKey = new Dictionary<string, CharacterController>();

    // Map question ID -> GameObject
    private Dictionary<int, GameObject> questionObjectsById = new Dictionary<int, GameObject>();

    // Map answer ID -> GameObject
    private Dictionary<int, GameObject> answerObjectsById = new Dictionary<int, GameObject>();
    private Dictionary<int, GameObject> obstacleObjectsById = new Dictionary<int, GameObject>();

    protected override void Awake()
    {
        if (Instance == null) Instance = this;
        base.Awake();
    }

    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();
        if (!string.IsNullOrEmpty(LoaderConfig.Instance.apiManager.jwt) && WS_Client.Instance != null)
        {
            WS_Client.Instance.jwt = LoaderConfig.Instance.apiManager.jwt;
        }

        // Subscribe to the order changed event
        if (WS_Client.Instance != null)
        {
            WS_Client.Instance.OnOrderChanged += HandleOrderChanged;
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (WS_Client.Instance != null)
        {
            WS_Client.Instance.OnOrderChanged -= HandleOrderChanged;
        }
    }

    // This method will be called whenever the order changes
    private void HandleOrderChanged(string newOrder)
    {
        Debug.Log($"Order changed to: {newOrder}");
        
        // Handle different order types
        switch (newOrder)
        {
            case "addPlayer":
                // Add your logic here
                break;
            case "removePlayer":
                // Add your logic here
                break;
            case "startGame":
                // Add your logic here
                break;
            case "endGame":
                // Add your logic here
                break;
            case "resetGame":
                // Add your logic here
                break;
            case "nextRound":
                // Add your logic here
                break;
            case "getAnswer":
                // Add your logic here
                break;
            case "submitCorrectAnswer":
                submitCorrectAnswerHandler();
                break;
            case "submitWrongAnswer":
                submitWrongAnswerHandler();
                break;
            default:
                break;
        }
    }

    // Update is called once per frame
    void Update()
    {
        // If no data, nothing to do
        if (WS_Client.Instance.GameData == null) return;
        var players = WS_Client.Instance.GameData.players;
        if (players == null) return;

        this.playerNumber = players.Count;

        // Track which uids are currently present this frame
        var currentKeys = new HashSet<string>();

        // Get local player's uid (if available)
        int localUid = -1;
        if (WS_Client.Instance != null && WS_Client.Instance.public_UserInfo != null)
        {
            localUid = WS_Client.Instance.public_UserInfo.uid;
        }

        StringBuilder debugSB = new StringBuilder();
        // Create missing players and update positions for existing ones
        foreach (var player in players)
        {
            string debugInfo = "";
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
                        cc.transform.localPosition = otherPlayerPos;
                    }
                }
            }
            debugInfo += $"Player {player.uid} at ({player.position[0]}, {player.position[1]})\n";
            debugSB.Append(debugInfo);
        }

        this.debugText.text = debugSB.ToString();

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
    }

    void FixedUpdate()
    {
        // If no data, nothing to do
        if (WS_Client.Instance.GameData == null) return;
        var players = WS_Client.Instance.GameData.players;
        if (players == null) return;

        // Process questions
        if (WS_Client.Instance.GameData != null && WS_Client.Instance.GameData.questions != null)
        {
            var currentQuestionIds = new HashSet<int>();
            int currentRound = WS_Client.Instance.GameData.round; // 获取当前轮次(1-10)

            // 只处理当前轮次对应的问题
            int questionIndex = currentRound - 1; // round 1 对应 questions[0]

            if (questionIndex >= 0 && questionIndex < WS_Client.Instance.GameData.questions.Count)
            {
                var question = WS_Client.Instance.GameData.questions[questionIndex];
                currentQuestionIds.Add(question.id);

                if (!questionObjectsById.ContainsKey(question.id))
                {
                    // 使用问题数据中的位置信息
                    Vector3 questionPos = Vector3.zero;
                    if (question.position != null && question.position.Length >= 2)
                    {
                        questionPos = new Vector3(question.position[0], question.position[1], 0f);
                    }
                    else
                    {
                        // 备用位置：根据轮次水平分布
                        float spacing = 1000f;
                        float startX = -(WS_Client.Instance.GameData.questions.Count - 1) * spacing / 2f;
                        questionPos = new Vector3(startX + (questionIndex * spacing), 800f, 0f);
                    }

                    CreateQuestionObject(question, questionPos);
                    Debug.Log($"Created question for round {currentRound} at position ({questionPos.x}, {questionPos.y})");
                }
                else
                {
                    // 更新已存在问题对象的位置
                    var questionObj = questionObjectsById[question.id];
                    if (questionObj != null && question.position != null && question.position.Length >= 2)
                    {
                        Vector2 uiPosition = new Vector2(question.position[0] * 1500f, question.position[1] * 1500f);

                        RectTransform rectTransform = questionObj.GetComponent<RectTransform>();
                        if (rectTransform != null)
                        {
                            rectTransform.anchoredPosition = uiPosition;
                        }
                        else
                        {
                            questionObj.transform.position = new Vector3(uiPosition.x, uiPosition.y, 0f);
                        }
                    }
                }
            }

            // 移除不属于当前轮次的问题
            var questionsToRemove = new List<int>();
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
                Debug.Log($"Removed question from previous round: {id}");
            }

            if (WS_Client.Instance.GameData.players != null) {
                foreach (WS_Client.PlayerData player in WS_Client.Instance.GameData.players) {
                    if (player.uid == WS_Client.Instance.public_UserInfo.uid) {
                        continue;
                    }
                    CharacterController characterController = characterControllers.Find(c => c.UserId == player.uid);
                    if (characterController != null) {
                        characterController.transform.Find("AnswerBubble").gameObject.SetActive(player.answer_id != 0);
                        characterController.transform.Find("AnswerBubble").GetComponentInChildren<TextMeshProUGUI>().text = player.answer_id != 0 ? WS_Client.Instance.GameData.answers.Find(a => a.id == player.answer_id).content : "";
                    }
                }
            }
        }

        // Process answers
        if (WS_Client.Instance.GameData.answers != null)
        {
            var currentAnswerIds = new HashSet<int>();

            foreach (var answer in WS_Client.Instance.GameData.answers)
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
            var answersToRemove = new List<int>();
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

        // Process obstacles 
        if (WS_Client.Instance.GameData.obstacles != null)
        {
            var currentObstacleIds = new HashSet<int>();

            // Debug.Log($"=== 障碍物处理开始 ===");
            // Debug.Log($"当前帧障碍物数量: {WS_Client.Instance.GameData.obstacles.Count}");

            foreach (var obstacle in WS_Client.Instance.GameData.obstacles)
            {
                if (obstacle.id == 0)
                {
                    // Debug.LogWarning("跳过ID为空的障碍物");
                    continue;
                }

                currentObstacleIds.Add(obstacle.id);
                //  Debug.Log($"处理障碍物: ID={obstacle.id}, Position=[{obstacle.position?[0]}, {obstacle.position?[1]}]");
                if (!obstacleObjectsById.ContainsKey(obstacle.id))
                {
                    // Create obstacle at position from data - 与answer相同的创建逻辑
                    // Debug.Log($"创建新障碍物: {obstacle.id}");
                    Vector3 obstaclePos = Vector3.zero;
                    if (obstacle.position != null && obstacle.position.Length >= 2)
                    {
                        obstaclePos = new Vector3(obstacle.position[0], obstacle.position[1], 0f);
                    }
                    CreateObstacleObject(obstacle, obstaclePos);
                }
                else
                {
                    // Update obstacle position if it exists - 与answer相同的更新逻辑
                    // Debug.Log($"更新已存在障碍物: {obstacle.id}");
                    var obstacleObj = obstacleObjectsById[obstacle.id];
                    if (obstacleObj != null && obstacle.position != null && obstacle.position.Length >= 2)
                    {
                        Vector2 uiPosition = new Vector2(obstacle.position[0] * 1500f, obstacle.position[1] * 1500f);

                        RectTransform rectTransform = obstacleObj.GetComponent<RectTransform>();
                        if (rectTransform != null)
                        {
                            rectTransform.anchoredPosition = uiPosition;
                        }
                        else
                        {
                            obstacleObj.transform.localPosition = new Vector3(uiPosition.x, uiPosition.y, 0f);
                        }

                        // Debug.Log($"Updated obstacle {obstacle.id} position to ({obstacle.position[0]}, {obstacle.position[1]})");
                    }
                }
            }

            // Remove obstacles that no longer exist - 与answer相同的清理逻辑
            var obstaclesToRemove = new List<int>();
            foreach (var kv in obstacleObjectsById)
            {
                if (!currentObstacleIds.Contains(kv.Key))
                {
                    obstaclesToRemove.Add(kv.Key);
                }
            }

            foreach (var id in obstaclesToRemove)
            {
                RemoveObstacleObject(id);
            }
        }
    }

    private void CreatePlayerFromData(WS_Client.PlayerData player, Vector3 startPos, string key, bool isLocal = false)
    {
        // Instantiate without parent, set world position, then attach to parent preserving world pos
        var characterController = GameObject.Instantiate(this.playerPrefab, this.globalParent).GetComponent<CharacterController>();
        if (characterController == null)
        {
            Debug.LogError("playerPrefab missing CharacterController component");
            GameObject.Destroy(characterController.gameObject);
            return;
        }

        int uid = player.uid;
        characterController.detectCamera = this.trackingCamera;
        characterController.gameObject.name = "Player_" + uid;
        characterController.UserName = "Player_" + uid;
        characterController.UserId = uid;
        if (isLocal) characterController.gameObject.tag = "MainPlayer";
        this.characterControllers.Add(characterController);

        // set world-space start position
        characterController.transform.localPosition = startPos;

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

        if (question.content == null)
        {
            Debug.LogError("question.content is null!");
            return;
        }

        var questionObj = GameObject.Instantiate(questionPrefab, this.globalParent);
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
        // TextMeshProUGUI textComponent = questionObj.GetComponentInChildren<TextMeshProUGUI>();
        // Debug.Log($"question.content: {question.content}");
        // if (textComponent != null)
        // {
        //     textComponent.text = question.content;
        // }

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

        // questionUIText.GetComponent<TextMeshProUGUI>().text = question.content;
        onTopUI.GetComponent<CanvasGroup>().alpha = 1;
        GameObject bg_FillInBlank = onTopUI.transform.Find("Bg/QABoard/bg_FillInBlank").gameObject;
        bg_FillInBlank.GetComponent<CanvasGroup>().alpha = 1;
        bg_FillInBlank.GetComponentInChildren<TextMeshProUGUI>().text = question.content;

        // Store the question data (you can add a component to store this if needed)
        // For now, just track the GameObject
        questionObjectsById[question.id] = questionObj;
        questions.Add(question);

    }

    private void RemoveQuestionObject(int id)
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

        var answerObj = GameObject.Instantiate(answerPrefab, this.globalParent);
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

    private void RemoveAnswerObject(int id)
    {
        if (answerObjectsById.TryGetValue(id, out var answerObj))
        {
            if (answerObj != null)
            {
                GameObject.Destroy(answerObj);
            }
            answerObjectsById.Remove(id);

            // Remove from list
            answers.RemoveAll(a => a.id == id);
        }
    }

    public void OnAnswerObjectTrigger(GameObject answerObject, int answerId, WS_Client.AnswerData answerData)
    {
        Debug.Log($"Answer {answerId} triggered - Content: {answerData?.content}");

        // Find and update the answer in GameData
        if (WS_Client.Instance.GameData?.answers != null)
        {
            WS_Client.AnswerData answer = WS_Client.Instance.GameData.answers.Find(a => a.id == answerId);
            if (answer != null)
            {
                answer.isOnPlayer = 1;
                Debug.Log($"Set answer {answerId} isOnPlayer to 1");

                // Send update to server so it syncs with all players
                WS_Client.Instance.updateAnswerOnPlayer(answerId);

                // if (WS_Client.Instance.GameData.players != null) {
                //     WS_Client.PlayerData player = WS_Client.Instance.GameData.players.Find(p => p.uid == WS_Client.Instance.public_UserInfo.uid);
                //     Debug.Log($"GameData player 1: {player?.uid} - {player?.answer_id} - {player?.answerContent} - {player?.isAnswerVisible}");
                //     if (player != null) {
                //         player.answer_id = answerId;
                //         player.answerContent = answerData.content;
                //         player.isAnswerVisible = 1;
                //     }
                //     Debug.Log($"GameData player 2: {player?.uid} - {player?.answer_id} - {player?.answerContent} - {player?.isAnswerVisible}");
                // }

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

    private void CreateObstacleObject(WS_Client.ObstacleData obstacle, Vector3 position)
    {
        if (obstaclePrefab == null)
        {
            Debug.LogError("obstaclePrefab is not assigned!");
            return;
        }

        var obstacleObj = GameObject.Instantiate(obstaclePrefab, this.globalParent);
        obstacleObj.name = "Obstacle_" + obstacle.id;

        // 使用与answer完全相同的坐标转换逻辑
        Vector2 uiPosition = new Vector2(position.x * 1500f, position.y * 1500f);

        // 使用RectTransform进行UI定位 - 与answer相同
        RectTransform rectTransform = obstacleObj.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = uiPosition;
        }
        else
        {
            // 回退到世界坐标（如果不是UI元素）
            obstacleObj.transform.localPosition = new Vector3(uiPosition.x, uiPosition.y, 0f);
        }

        obstacleObj.gameObject.SetActive(true);

        // 存储障碍物数据 - 与answer相同的模式
        obstacleObjectsById[obstacle.id] = obstacleObj;
    }
    private void RemoveObstacleObject(int id)
    {
        if (obstacleObjectsById.TryGetValue(id, out var obstacleObj))
        {
            if (obstacleObj != null)
            {
                GameObject.Destroy(obstacleObj);
            }
            obstacleObjectsById.Remove(id);
        }
    }

    private IEnumerator HideYouWinAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        YouWin.SetActive(false);
    }

    private IEnumerator HideYouLoseAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        YouLose.SetActive(false);
    }

    private void submitCorrectAnswerHandler()
    {
        YouWin.SetActive(true);
        StartCoroutine(HideYouWinAfterDelay(3f));
    }

    private void submitWrongAnswerHandler()
    {
        YouLose.SetActive(true);
        StartCoroutine(HideYouLoseAfterDelay(3f));
    }
}
