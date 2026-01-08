using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Class to hold all costume textures for a single costume
[System.Serializable]
public class CostumeTextures
{
    public Texture2D standTexture;
    public Texture2D walkTexture;
    public Texture2D jumpTexture;
}

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
    public GameObject readyButton;
    public GameObject blueTeamScore;
    public GameObject orangeTeamScore;
    public GameObject disconnectedUI;

    public List<CharacterController> characterControllers = new List<CharacterController>();
    public List<WS_Client.QuestionData> questions = new List<WS_Client.QuestionData>();
    public List<WS_Client.AnswerData> answers = new List<WS_Client.AnswerData>();
    public Camera trackingCamera;
    private int playerID = 0;
    public Text debugText;

    //this variable is only used in "next round order call" right now, change webSocket for start round position
    [SerializeField]
    private Vector3[] startingPos;

    private string costumeDataJson = "";
    public string accountCostumeId = ""; // ID of the costume currently equipped by the account
    public Dictionary<string, CostumeTextures> costumeTexturesById = new Dictionary<string, CostumeTextures>(); // costume_id -> CostumeTextures (stand, walk, jump)
    public Dictionary<string, CostumeData> costumeDataById = new Dictionary<string, CostumeData>(); // costume_id -> CostumeData
    public bool finishLoading = false; // Set to true after costume data and account costume ID are loaded
    private int loadingImagesCount = 0; // Track how many images are currently loading
    private int currentQuestionId = -1;

    // Map WS player key (string) -> CharacterController (ensures one GameObject per ws player)
    private Dictionary<string, CharacterController> playerControllersByKey = new Dictionary<string, CharacterController>();

    // Map question ID -> GameObject
    private Dictionary<int, GameObject> questionObjectsById = new Dictionary<int, GameObject>();

    // Map answer ID -> GameObject
    private Dictionary<int, GameObject> answerObjectsById = new Dictionary<int, GameObject>();
    private Dictionary<int, GameObject> obstacleObjectsById = new Dictionary<int, GameObject>();
    private HashSet<string> currentKeys = new HashSet<string>();
    public CharacterSet[] characterSets;
    public GameObject[] scoreboardControllers;
    public Sprite[] playerTags;



    /// <summary>
    /// minmap
    /// </summary>
    public RectTransform minimapParent;
    public RawImage minimapRawImage;              // assign the minimap RawImage in Inspector
    public RectTransform minimapMarkerPrefab;                    // small UI prefab (Image) for markers; set pivot (0.5,0.5)
    public RectTransform minimapMarkersParent;                   // parent under the minimap canvas (can be the RawImage rectTransform)
    public Vector2 minimapWorldBottomLeft = new Vector2(-50f, -50f); // world coords that map to minimap bottom-left
    public Vector2 minimapWorldTopRight = new Vector2(50f, 50f);

    private Dictionary<string, RectTransform> minimapMarkersByKey = new Dictionary<string, RectTransform>();

    // throttle SyncPlayers to reduce per-frame cost (seconds)
    public float syncPlayersInterval = 0.1f;
    private float lastSyncPlayersTime = 0f;

    // team colors cached once
    private static readonly Color TeamAColor = new Color(0.1647059f, 0.6666667f, 0.8784314f, 1f); // blue
    private static readonly Color TeamBColor = new Color(0.9647059f, 0.572549f, 0.1294118f, 1f); // yellow


    protected override void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        //DontDestroyOnLoad(this.gameObject);
    }

    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();

        // Subscribe to the order changed event
        if (WS_Client.Instance != null)
        {
            WS_Client.Instance.OnOrderChanged += HandleOrderChanged;
        }

        if (this.minimapParent == null)
        {
            this.minimapParent = this.minimapRawImage.rectTransform.parent as RectTransform;
        }

        var minimapScaler = this.minimapParent.GetComponent<CanvasScaler>();
        if (minimapScaler != null)
        {
            var referenceResolution = minimapScaler.referenceResolution;
            if (this.minimapParent.sizeDelta.x != referenceResolution.x)
            {
                float scaleFactor = Mathf.Clamp(referenceResolution.x / this.minimapParent.sizeDelta.x, 0.8f, 1.5f);
                //Debug.Log(scaleFactor);

                float scaledPosX = this.minimapRawImage.rectTransform.localPosition.x * scaleFactor;
                float scaledPosY = this.minimapRawImage.rectTransform.localPosition.y;

                //Debug.Log(this.minimapRawImage.rectTransform.localPosition);
                this.minimapRawImage.rectTransform.localPosition = new Vector3(scaledPosX, scaledPosY, 0f);
            }
        }

        // Wait for starwishApiCaller to be ready before fetching costume data
        StartCoroutine(WaitForStarwishApi());

        /*// change ready button to WS_Client's ready button
        if (WS_Client.Instance.GameData.players.Find(p => p.uid == WS_Client.Instance.public_UserInfo.uid).status != "playing") {
            readyButton.SetActive(true);
        }*/
    }

    private IEnumerator WaitForStarwishApi()
    {
        while (LoaderConfig.Instance == null || LoaderConfig.Instance.apiManager == null)
        {
            yield return null;
        }

        // Fetch both costume data and account costume ID in parallel
        // Task task1 = fetchCostumeData();
        Task task2 = fetchAccountCostumeId();
        
        // Wait for both tasks to complete
        yield return new WaitUntil(() => task2.IsCompleted);
        
        finishLoading = true;
    }

    protected async Task fetchAccountCostumeId()
    {
        try
        {
            if (LoaderConfig.Instance == null)
            {
                Debug.LogWarning("fetchAccountCostumeId: LoaderConfig.Instance is null. Aborting fetch.");
                return;
            }

            var api = LoaderConfig.Instance.apiManager;
            if (api == null)
            {
                Debug.LogWarning("fetchAccountCostumeId: LoaderConfig.Instance.apiManager is null. Aborting fetch.");
                return;
            }

            string jsonResponse = null;
            try
            {
                jsonResponse = await api.getCurrentAccount();
            }
            catch (Exception apiEx)
            {
                Debug.LogError($"getCurrentAccount() threw: {apiEx.Message}\n{apiEx.StackTrace}");
                return;
            }

            Debug.Log("jsonResponse: " + (jsonResponse ?? "null"));

            if (string.IsNullOrEmpty(jsonResponse))
            {
                Debug.LogWarning("fetchAccountCostumeId: api returned empty response.");
                return;
            }

            try
            {
                StarwishPartyAccountResponse accountResponse = JsonUtility.FromJson<StarwishPartyAccountResponse>(jsonResponse);

                if (accountResponse != null &&
                    accountResponse.data != null &&
                    accountResponse.data.equipped_costume_data != null &&
                    !string.IsNullOrEmpty(accountResponse.data.equipped_costume_data.costume_id))
                {
                    accountCostumeId = accountResponse.data.equipped_costume_data.costume_id;
                    Debug.Log($"fetchAccountCostumeId: accountCostumeId = {accountCostumeId}");
                }
                else
                {
                    Debug.LogWarning("fetchAccountCostumeId: No equipped costume found in account response");
                }
            }
            catch (Exception parseEx)
            {
                Debug.LogError($"Failed to parse account data JSON: {parseEx.Message}\n{parseEx.StackTrace}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to fetch account costume ID: {ex.Message}\n{ex.StackTrace}");
        }
    }

    private Vector2 WorldToMinimapAnchoredPosition(Vector2 worldPos)
    {
        if (minimapRawImage == null || minimapMarkersParent == null) return Vector2.zero;

        RectTransform mapRT = minimapRawImage.rectTransform;

        // world bounds
        float minX = minimapWorldBottomLeft.x;
        float maxX = minimapWorldTopRight.x;
        float minY = minimapWorldBottomLeft.y;
        float maxY = minimapWorldTopRight.y;

        if (Mathf.Approximately(maxX, minX) || Mathf.Approximately(maxY, minY))
        {
            Debug.LogWarning("Minimap world bounds invalid (min == max). Check minimapWorldBottomLeft/topRight.");
            return Vector2.zero;
        }

        // compute inner texture rect (local coords of mapRT)
        Rect inner = GetRawImageInnerRect(mapRT, minimapRawImage);

        // world extents
        float worldW = maxX - minX;
        float worldH = maxY - minY;
        if (Mathf.Approximately(worldW, 0f) || Mathf.Approximately(worldH, 0f))
        {
            Debug.LogWarning("Invalid world size for minimap mapping.");
            return Vector2.zero;
        }

        // Map world position to inner rect by linear scaling:
        // localX = inner.xMin + (worldPos.x - minX) * (inner.width / worldW)
        // localY = inner.yMin + (worldPos.y - minY) * (inner.height / worldH)
        float scaleX = inner.width / worldW;
        float scaleY = inner.height / worldH;

        float localX = inner.xMin + (worldPos.x - minX) * scaleX;
        float localY = inner.yMin + (worldPos.y - minY) * scaleY;

        // clamp into inner rect to avoid corners when out-of-bounds
        localX = Mathf.Clamp(localX, inner.xMin, inner.xMax);
        localY = Mathf.Clamp(localY, inner.yMin, inner.yMax);

        Vector2 localPointInMapRT = new Vector2(localX, localY);

        // If markers are direct children of the minimap RectTransform, return the local point directly.
        if (minimapMarkersParent == mapRT)
        {
            return localPointInMapRT;
        }

        // Otherwise convert to minimapMarkersParent local coordinates
        Canvas mapCanvas = minimapRawImage.canvas;
        Camera canvasCam = mapCanvas != null ? mapCanvas.worldCamera : null;

        Vector3 worldPoint = mapRT.TransformPoint(localPointInMapRT);
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(canvasCam, worldPoint);

        Vector2 anchoredPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(minimapMarkersParent, screenPoint, canvasCam, out anchoredPos);

#if UNITY_EDITOR
        if (debugText != null)
        {
            debugText.text = $"World({worldPos.x:F1},{worldPos.y:F1}) -> local({localX:F2},{localY:F2}) inner({inner.xMin:F1},{inner.yMin:F1},{inner.width:F1},{inner.height:F1})";
        }
#endif

        return anchoredPos;
    }

    private Rect GetRawImageInnerRect(RectTransform rt, RawImage rawImage)
    {
        Rect r = rt.rect;

        if (rawImage == null || rawImage.texture == null)
            return r;

        // If texture has no size, return full rect
        float texW = rawImage.texture.width;
        float texH = rawImage.texture.height;
        if (texW <= 0f || texH <= 0f) return r;

        float texAspect = texW / texH;
        float rectAspect = r.width / r.height;

        Rect inner = r;

        if (rectAspect > texAspect)
        {
            // letterbox horizontally — texture height fits, width is smaller
            float innerW = r.height * texAspect;
            inner.x = r.x + (r.width - innerW) * 0.5f;
            inner.width = innerW;
        }
        else
        {
            // letterbox vertically — texture width fits, height is smaller
            float innerH = r.width / texAspect;
            inner.y = r.y + (r.height - innerH) * 0.5f;
            inner.height = innerH;
        }

        return inner;
    }


    // protected async Task fetchCostumeData() {
    //     try 
    //     {
    //         // Wait for APIManager to be initialized
    //         if (LoaderConfig.Instance == null || LoaderConfig.Instance.apiManager == null)
    //         {
    //             Debug.LogError("LoaderConfig.Instance.apiManager is null. Make sure LoaderConfig is properly initialized.");
    //             return;
    //         }

    //         string jsonResponse = await LoaderConfig.Instance.apiManager.getCostumeData();
    //         costumeDataJson = jsonResponse;
    //         Debug.Log($"costumeData loaded: {costumeDataJson}");

    //         // Parse JSON to get all costumes
    //         if (!string.IsNullOrEmpty(costumeDataJson))
    //         {
    //             try
    //             {
    //                 CostumeListResponse costumeResponse = JsonUtility.FromJson<CostumeListResponse>(costumeDataJson);

    //                 if (costumeResponse != null && 
    //                     costumeResponse.data != null && 
    //                     costumeResponse.data.Length > 0)
    //                 {

    //                     // Store costume data and start loading images for each costume
    //                     foreach (CostumeData costume in costumeResponse.data)
    //                     {
    //                         if (costume != null && !string.IsNullOrEmpty(costume.costume_id))
    //                         {
    //                             // Store costume data
    //                             costumeDataById[costume.costume_id] = costume;

    //                             // Initialize CostumeTextures object for this costume
    //                             if (!costumeTexturesById.ContainsKey(costume.costume_id))
    //                             {
    //                                 costumeTexturesById[costume.costume_id] = new CostumeTextures();
    //                             }

    //                             // Load stand image
    //                             if (!string.IsNullOrEmpty(costume.img_src_stand))
    //                             {
    //                                 loadingImagesCount++;
    //                                 StartCoroutine(LoadCostumeImage(
    //                                     costume.costume_id, 
    //                                     costume.img_src_stand, 
    //                                     "stand"
    //                                 ));
    //                             }

    //                             // Load walk image
    //                             if (!string.IsNullOrEmpty(costume.img_src_walk))
    //                             {
    //                                 loadingImagesCount++;
    //                                 StartCoroutine(LoadCostumeImage(
    //                                     costume.costume_id, 
    //                                     costume.img_src_walk, 
    //                                     "walk"
    //                                 ));
    //                             }

    //                             // Load jump image
    //                             if (!string.IsNullOrEmpty(costume.img_src_jump))
    //                             {
    //                                 loadingImagesCount++;
    //                                 StartCoroutine(LoadCostumeImage(
    //                                     costume.costume_id, 
    //                                     costume.img_src_jump, 
    //                                     "jump"
    //                                 ));
    //                             }
    //                         }
    //                     }

    //                     // Wait for all images to finish loading
    //                     Debug.Log($"Started loading {loadingImagesCount} costume images. Waiting for completion...");
    //                     int maxWaitSeconds = 30; // Maximum wait time
    //                     float waitedTime = 0f;
    //                     while (loadingImagesCount > 0 && waitedTime < maxWaitSeconds)
    //                     {
    //                         await Task.Delay(100); // Wait 100ms between checks
    //                         waitedTime += 0.1f;
    //                     }

    //                     if (loadingImagesCount > 0)
    //                     {
    //                         Debug.LogWarning($"Timed out waiting for costume images. {loadingImagesCount} images still loading.");
    //                     }
    //                     else
    //                     {
    //                         Debug.Log("All costume images loaded successfully!");
    //                     }
    //                 }
    //                 else
    //                 {
    //                     Debug.LogWarning("No costume data found in response");
    //                 }
    //             }
    //             catch (System.Exception parseEx)
    //             {
    //                 Debug.LogError($"Failed to parse costume data JSON: {parseEx.Message}");
    //             }
    //         }
    //     }
    //     catch (System.Exception ex)
    //     {
    //         Debug.LogError($"Failed to fetch costume data: {ex.Message}");
    //     }
    // }

    // private IEnumerator LoadCostumeImage(string costumeId, string imageUrl, string imageType)
    // {
    //     using (UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(imageUrl))
    //     {
    //         // Set certificate handler to bypass SSL issues if needed
    //         request.certificateHandler = new WebRequestSkipCert();

    //         yield return request.SendWebRequest();

    //         if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
    //         {
    //             Texture2D loadedTexture = UnityEngine.Networking.DownloadHandlerTexture.GetContent(request);

    //             // Store the texture in the appropriate field based on imageType
    //             if (costumeTexturesById.ContainsKey(costumeId))
    //             {
    //                 CostumeTextures costumeTextures = costumeTexturesById[costumeId];

    //                 switch (imageType.ToLower())
    //                 {
    //                     case "stand":
    //                         costumeTextures.standTexture = loadedTexture;
    //                         break;
    //                     case "walk":
    //                         costumeTextures.walkTexture = loadedTexture;
    //                         break;
    //                     case "jump":
    //                         costumeTextures.jumpTexture = loadedTexture;
    //                         break;
    //                     default:
    //                         Debug.LogWarning($"Unknown image type '{imageType}' for costume ID {costumeId}");
    //                         break;
    //                 }
    //             }
    //         }
    //         else
    //         {
    //             Debug.LogError($"Failed to load costume {imageType} image for ID {costumeId}: {request.error}");
    //         }

    //         // Decrement the loading counter
    //         loadingImagesCount--;
    //     }
    // }

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
            break;
            case "removePlayer":
            break;
            case "reconnectPlayer":
                readyButton.SetActive(false);
                checkAnswerVisibility();
                StartCoroutine(updateQuestionUI());
                // SyncPlayers();
                break;
            case "startGame":
                readyButton.SetActive(false);
                StartGame.Instance.startGameSequence();
                StartCoroutine(updateQuestionUI());
                resetStartingPos();
                break;
            case "endGame":
                readyButton.SetActive(true);
                onTopUI.GetComponent<CanvasGroup>().alpha = 0;
                base.endGame();
                break;
            case "resetGame":
                readyButton.SetActive(true);
                onTopUI.GetComponent<CanvasGroup>().alpha = 0;
                resetStartingPos();
                // Add your logic here
                break;
            case "nextRound":
                StartCoroutine(updateScoreUI());
                StartCoroutine(updateQuestionUI());
                resetStartingPos();
                // Add your logic here
                break;
            case "getAnswer":
                checkAnswerVisibility();
                break;
            case "submitCorrectAnswer":
                submitCorrectAnswerHandler();
                break;
            case "submitWrongAnswer":
                submitWrongAnswerHandler();
                break;
            case "disconnected":
                disconnectedUI.SetActive(true);
                break;
            default:
                break;
        }
    }

    public void hideDisconnectedUI()
    {
        disconnectedUI.SetActive(false);
    }

    private IEnumerator updateScoreUI()
    {
        while (WS_Client.Instance.GameData == null) {
            yield return new WaitForSeconds(0.1f);
        }
        blueTeamScore.GetComponent<TextMeshProUGUI>().text = WS_Client.Instance.GameData.teamScore[0].ToString();
        orangeTeamScore.GetComponent<TextMeshProUGUI>().text = WS_Client.Instance.GameData.teamScore[1].ToString();
    }

    private IEnumerator updateQuestionUI()
    {
        while (WS_Client.Instance.GameData == null) {
            yield return new WaitForSeconds(0.1f);
        }

        
        List<WS_Client.QuestionData> questions = WS_Client.Instance.GameData.questions;
        onTopUI.GetComponent<CanvasGroup>().alpha = 1;
        GameObject bg_FillInBlank = onTopUI.transform.Find("Bg/QABoard/bg_FillInBlank").gameObject;
        bg_FillInBlank.GetComponent<CanvasGroup>().alpha = 1;
        
        int round = WS_Client.Instance.GameData.round;
        //wait until round != currentQuestionId
        while (round == currentQuestionId) {
            round = WS_Client.Instance.GameData.round;
            yield return new WaitForSeconds(0.1f);
        }

        if (round > 0) {
            bg_FillInBlank.GetComponentInChildren<TextMeshProUGUI>().text = WS_Client.Instance.GameData.questions[round-1].content;
            currentQuestionId = round;
        }
    }

    private void SyncPlayers()
    {
        try
        {
            // Defensive checks
            if (WS_Client.Instance == null || WS_Client.Instance.GameData == null) return;
            var players = WS_Client.Instance.GameData.players;
            if (players == null) return;

            // Clear currentKeys so we rebuild it from the authoritative GameData
            currentKeys.Clear();

            // Get local player's uid (if available)
            int localUid = -1;
            if (WS_Client.Instance.public_UserInfo != null)
            {
                localUid = WS_Client.Instance.public_UserInfo.uid;
            }

        // Track minimap presence while iterating players (reuse currentKeys)
        foreach (var player in players)
        {
            string key = !string.IsNullOrEmpty(player.player_id) ? player.player_id : player.uid.ToString();

            bool isLocal = (player.uid == localUid);
            if (!playerControllersByKey.ContainsKey(key))
            {
                Vector3 location = Vector3.zero;

                Debug.Log("CreatePlayerFromData 1: " + location + " - " + key + " - " + isLocal);
                if (player.position != null && player.position.Length >= 2)
                {
                    location = new Vector3(player.position[0], player.position[1], 0f);
                }
                CreatePlayerFromData(player, location, key, isLocal);
            }

            // mark as present for this cycle (used for player removal and minimap cleanup)
            currentKeys.Add(key);

            // update non-local players' destination
            if (!isLocal)
            {
                if (playerControllersByKey.TryGetValue(key, out var cc))
                {
                    if (cc != null)
                    {
                        Vector3 otherPlayerPos = Vector3.zero;
                        if (player.position != null && player.position.Length >= 2)
                        {
                            otherPlayerPos = new Vector3(player.position[0], player.position[1], 0f);
                        }
                        cc.setLocalDestination(otherPlayerPos);
                    }
                }
            }

            // --- Minimap marker update (merged here to avoid a second players pass) ---
            if (minimapRawImage != null && minimapMarkerPrefab != null && minimapMarkersParent != null)
            {
                // Determine world position for marker (prefer authoritative server position)
                Vector2 worldPos = Vector2.zero;
                if (player.position != null && player.position.Length >= 2)
                {
                    worldPos = new Vector2(player.position[0], player.position[1]);
                }
                else if (playerControllersByKey.TryGetValue(key, out var fallbackCc) && fallbackCc != null)
                {
                    Vector3 t = fallbackCc.transform.position; // use world-space position
                    worldPos = new Vector2(t.x, t.y);
                }

                Vector2 anchoredPos = WorldToMinimapAnchoredPosition(worldPos);

                if (!minimapMarkersByKey.TryGetValue(key, out var marker) || marker == null)
                {
                    // Instantiate as child of minimapRawImage.rectTransform (so local coordinates match)
                    RectTransform parentRT = minimapRawImage.rectTransform;
                    RectTransform instance = GameObject.Instantiate(minimapMarkerPrefab, parentRT);
                    instance.gameObject.name = $"MinimapMarker_{key}";

                    // Ensure marker uses centered anchors/pivot and neutral scale so anchoredPosition behaves predictably
                    instance.anchorMin = new Vector2(0.5f, 0.5f);
                    instance.anchorMax = new Vector2(0.5f, 0.5f);
                    instance.pivot = new Vector2(0.5f, 0.5f);
                    instance.localScale = Vector3.one;

                    minimapMarkersByKey[key] = instance;
                    marker = instance;

                    // Optional tint local player
                    var img = instance.GetComponent<Image>();
                    if (img != null)
                    {
                        // determine team once
                        int team = -1;
                        if (!string.IsNullOrEmpty(player.player_id))
                        {
                            // parse "playerN" safely
                            string idDigits = player.player_id.StartsWith("player", StringComparison.OrdinalIgnoreCase)
                                ? player.player_id.Substring(6)
                                : player.player_id;
                            if (int.TryParse(idDigits, out int parsedIndex))
                            {
                                team = Mathf.Max(0, (parsedIndex - 1) % 2);
                            }
                        }
                        if (team == -1)
                        {
                            team = (player.uid % 2 == 0) ? 0 : 1;
                        }

                        Color desiredColor = (team == 0) ? TeamAColor : TeamBColor;
                        // Only update color if different (avoids UI rebind cost)
                        if (img.color != desiredColor)
                        {
                            img.color = desiredColor;
                        }

                        // Only set sprite if necessary
                        Sprite desiredSprite = null;
                        if (!string.IsNullOrEmpty(player.costume_id) && int.TryParse(player.costume_id, out int costumeIndex))
                        {
                            int arrayIndex = costumeIndex - 1;
                            if (arrayIndex >= 0 && arrayIndex < this.characterSets.Length && this.characterSets[arrayIndex] != null)
                            {
                                desiredSprite = SetUI.ConvertTextureToSprite(this.characterSets[arrayIndex].defaultIcon as Texture2D);
                            }
                        }

                        if (desiredSprite != null && img.sprite != desiredSprite)
                        {
                            img.sprite = desiredSprite;
                            img.SetNativeSize(); // call once when sprite actually changes
                            img.rectTransform.sizeDelta = new Vector2(64, 64);
                        }
                    }
                }

                // Update marker position using the function that returns local map coords when markers are children
                if (marker != null)
                {
                    marker.anchoredPosition = anchoredPos;
                }
            }
        }

        // Remove controllers for players who left (keys not present in currentKeys)
        // Collect keys to remove to avoid modifying dictionary during iteration
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

        // Remove minimap markers for players that are gone (cleanup)
        if (minimapMarkersByKey != null && minimapMarkersByKey.Count > 0)
        {
            var markersToRemove = new List<string>();
            foreach (var kv in minimapMarkersByKey)
            {
                if (!currentKeys.Contains(kv.Key))
                {
                    if (kv.Value != null) GameObject.Destroy(kv.Value.gameObject);
                    markersToRemove.Add(kv.Key);
                }
            }
            foreach (var k in markersToRemove) minimapMarkersByKey.Remove(k);
        }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error in SyncPlayers: {ex.Message}\n{ex.StackTrace}");
        }
    }

    // Update is called once per frame
    void Update()
    {
        try
        {
            // If no data, nothing to do
            if (finishLoading && Time.time - lastSyncPlayersTime >= syncPlayersInterval)
            {
                lastSyncPlayersTime = Time.time;
                SyncPlayers();
            }   

            if (Input.GetKeyDown(KeyCode.P))
            {
                // printCostumeData(); // for DEV printCostumeData
                printGameData(); // for DEV printCostumeData
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error in Update: {ex.Message}\n{ex.StackTrace}");
        }
    }

    void printGameData() {
        // Debug.Log($"=== Game Data ===");
        // Debug.Log($"Players: {WS_Client.Instance.GameData.players.Count}");
        // Debug.Log($"Questions: {WS_Client.Instance.GameData.questions.Count}");
        // Debug.Log($"Answers: {WS_Client.Instance.GameData.answers.Count}");
        // Debug.Log($"Obstacles: {WS_Client.Instance.GameData.obstacles.Count}");
        // Debug.Log($"=== End Game Data ===");
        foreach (WS_Client.PlayerData player in WS_Client.Instance.GameData.players) {
            Debug.Log($"player: {player.uid} - {player.costume_id}");
        }
    }

    void printCostumeData()
    {
        Debug.Log($"=== Costume Data ({costumeDataById.Count} costumes loaded) ===");
        
        foreach (var kvp in costumeDataById)
        {
            string costumeId = kvp.Key;
            CostumeData costume = kvp.Value;
            
            bool isEquipped = costumeId == accountCostumeId;
            // Check if textures are loaded
            if (costumeTexturesById.ContainsKey(costumeId))
            {
                CostumeTextures textures = costumeTexturesById[costumeId];
                Debug.Log($"  Textures loaded: Stand={textures.standTexture != null}, Walk={textures.walkTexture != null}, Jump={textures.jumpTexture != null}");
            }
            else
            {
                Debug.Log($"  Textures: Not loaded yet");
            }
        }
        
        Debug.Log("=== End Costume Data ===");
    }

    void FixedUpdate()
    {
        try
        {
            // If no data, nothing to do
            if (WS_Client.Instance == null || WS_Client.Instance.GameData == null) return;
            var players = WS_Client.Instance.GameData.players;
            if (players == null || WS_Client.Instance.public_UserInfo == null) return;

            foreach (WS_Client.PlayerData player in WS_Client.Instance.GameData.players)
            {
                if (player == null || player.uid == WS_Client.Instance.public_UserInfo.uid)
                {
                    continue;
                }
                CharacterController characterController = characterControllers != null ? characterControllers.Find(c => c.UserId == player.uid) : null;
                if (characterController != null)
                {
                    Transform answerBubble = characterController.transform.Find("AnswerBubble");
                    if (answerBubble != null)
                    {
                        answerBubble.gameObject.SetActive(player.isAnswerVisible != 0);
                        
                        // Safely get answer content
                        string answerContent = "";
                        if (player.answer_id != 0 && WS_Client.Instance.GameData.answers != null)
                        {
                            var answer = WS_Client.Instance.GameData.answers.Find(a => a.id == player.answer_id);
                            if (answer != null)
                            {
                                answerContent = answer.content;
                            }
                            else
                            {
                                Debug.LogWarning($"Answer with id {player.answer_id} not found for player {player.uid}");
                            }
                        }
                        
                        TextMeshProUGUI textComponent = answerBubble.GetComponentInChildren<TextMeshProUGUI>();
                        if (textComponent != null)
                        {
                            textComponent.text = answerContent;
                        }
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error in FixedUpdate: {ex.Message}\n{ex.StackTrace}");
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
                    // Debug.Log($"answer: {answer.id} - {answer.content} - {answer.position[0]} - {answer.position[1]}");
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
                        Vector2 uiPosition = new Vector2(answer.position[0], answer.position[1]);
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
            // Create a list to avoid modifying dictionary during iteration
            var answersToRemove = new List<int>();
            foreach (var kv in answerObjectsById)
            {
                if (!currentAnswerIds.Contains(kv.Key))
                {
                    answersToRemove.Add(kv.Key);
                }
            }
            
            // Now remove them safely
            foreach (var answerId in answersToRemove)
            {
                RemoveAnswerObject(answerId);
            }
        }

        // // Process obstacles 
        // if (WS_Client.Instance.GameData.obstacles != null)
        // {
        //     var currentObstacleIds = new HashSet<int>();

        //     // Debug.Log($"=== 障碍物处理开始 ===");
        //     // Debug.Log($"当前帧障碍物数量: {WS_Client.Instance.GameData.obstacles.Count}");

        //     foreach (var obstacle in WS_Client.Instance.GameData.obstacles)
        //     {
        //         if (obstacle.id == 0)
        //         {
        //             // Debug.LogWarning("跳过ID为空的障碍物");
        //             continue;
        //         }

        //         currentObstacleIds.Add(obstacle.id);
        //         //  Debug.Log($"处理障碍物: ID={obstacle.id}, Position=[{obstacle.position?[0]}, {obstacle.position?[1]}]");
        //         if (!obstacleObjectsById.ContainsKey(obstacle.id))
        //         {
        //             // Create obstacle at position from data - 与answer相同的创建逻辑
        //             // Debug.Log($"创建新障碍物: {obstacle.id}");
        //             Vector3 obstaclePos = Vector3.zero;
        //             if (obstacle.position != null && obstacle.position.Length >= 2)
        //             {
        //                 obstaclePos = new Vector3(obstacle.position[0], obstacle.position[1], 0f);
        //             }
        //             CreateObstacleObject(obstacle, obstaclePos);
        //         }
        //         else
        //         {
        //             var obstacleObj = obstacleObjectsById[obstacle.id];
        //             if (obstacleObj != null && obstacle.position != null && obstacle.position.Length >= 2)
        //             {
        //                 Vector2 uiPosition = new Vector2(obstacle.position[0], obstacle.position[1]);

        //                 RectTransform rectTransform = obstacleObj.GetComponent<RectTransform>();
        //                 if (rectTransform != null)
        //                 {
        //                     rectTransform.anchoredPosition = uiPosition;
        //                 }
        //                 else
        //                 {
        //                     obstacleObj.transform.localPosition = new Vector3(uiPosition.x, uiPosition.y, 0f);
        //                 }

        //                 // Debug.Log($"Updated obstacle {obstacle.id} position to ({obstacle.position[0]}, {obstacle.position[1]})");
        //             }
        //         }
        //     }

        //     // Remove obstacles that no longer exist - 与answer相同的清理逻辑
        //     foreach (var kv in obstacleObjectsById)
        //     {
        //         if (!currentObstacleIds.Contains(kv.Key))
        //         {
        //             RemoveObstacleObject(kv.Key);
        //         }
        //     }

        // }
    }

    private void resetStartingPos()
    {
        for (int i = 0; i < characterControllers.Count; i++)
        {
            characterControllers[i].transform.localPosition = startingPos[i];
        }
    }

    private void CreatePlayerFromData(WS_Client.PlayerData player, Vector3 startPos, string key, bool isLocal = false)
    {
        try
        {
            if (player == null)
            {
                Debug.LogError("Attempted to create player from null PlayerData");
                return;
            }
            
            // Instantiate without parent, set world position, then attach to parent preserving world pos
            var characterController = GameObject.Instantiate(this.playerPrefab, this.globalParent).GetComponent<CharacterController>();
            
            // Find the scoreboardController with matching uid
            scoreboardController matchingScoreboard = null;

            if (string.IsNullOrEmpty(player.player_id))
            {
                Debug.LogError($"Player {player.uid} has null or empty player_id");
                return;
            }

            int playerIndex = int.Parse(player.player_id.Replace("player", "")) - 1;

        if (playerIndex >= 0 && playerIndex < this.scoreboardControllers.Length)
        {
            scoreboardController sb = this.scoreboardControllers[playerIndex].GetComponent<scoreboardController>();
            if (sb != null && sb.key == "")
            {
                matchingScoreboard = sb;
                matchingScoreboard.key = key;
            }
        }
        
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
        characterController.setLocalPlayer(isLocal);
        characterController.setPlayerTag(playerTags[playerIndex]);
        if (isLocal)
        {
            Debug.Log($"Local player created for uid={uid}");
        }

        playerControllersByKey[key] = characterController;
        characterController.key = key;

        // Safely parse and access costume with bounds checking
        if (!string.IsNullOrEmpty(player.costume_id) && int.TryParse(player.costume_id, out int costumeId))
        {
            int arrayIndex = costumeId - 1;
            if (arrayIndex >= 0 && arrayIndex < this.characterSets.Length && this.characterSets[arrayIndex] != null)
            {
                var characterSet = this.characterSets[arrayIndex];
                if (characterSet.walkingAnimationTextures != null && characterSet.walkingAnimationTextures.Length >= 2)
                {
                    characterController.SetCostumeTextures(
                        characterSet.walkingAnimationTextures[0] as Texture2D,
                        characterSet.walkingAnimationTextures[1] as Texture2D);
                }
                
                if (matchingScoreboard != null)
                {
                    matchingScoreboard.setScoreboard(key, characterSet.defaultIcon as Texture2D, player.ename);
                }
            }
            else
            {
                Debug.LogError($"Invalid costume_id {player.costume_id} for player {player.uid}. Valid range: 1-{this.characterSets.Length}");
            }
        }
        else
        {
            Debug.LogWarning($"Player {player.uid} has invalid or empty costume_id: {player.costume_id}");
        }

            // keep an incremental id for legacy naming if needed
            this.playerID = Mathf.Max(this.playerID, uid + 1);
            Debug.Log($"Created player GameObject for uid={uid} at {startPos} (isLocal={isLocal})");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error creating player from data: {ex.Message}\n{ex.StackTrace}");
        }
    }

    private void RemovePlayer(string key)
    {
        if (playerControllersByKey.TryGetValue(key, out var cc))
        {
            if (cc != null)
            {
                // Find matching scoreboard
                GameObject scoreboardObj = System.Array.Find(this.scoreboardControllers, obj => obj.GetComponent<scoreboardController>().key == cc.key);
                if (scoreboardObj != null) {
                    Debug.Log("RemovePlayer: scoreboardObj=" + scoreboardObj.name + cc.key);
                    scoreboardController matchingScoreboard = scoreboardObj.GetComponent<scoreboardController>();
                    if (matchingScoreboard != null) {
                        matchingScoreboard.resetScoreboard();
                    }
                }
                this.characterControllers.Remove(cc);
                GameObject.Destroy(cc.gameObject);
                Debug.Log($"[TowerGameController] Removed player GameObject for key={key}");
            }
            playerControllersByKey.Remove(key);
        }

        this.RemovePlayerMarker(key);
    }

    private void RemovePlayerMarker(string key)
    {
        if (minimapMarkersByKey.TryGetValue(key, out var marker))
        {
            if (marker != null)
            {
                GameObject.Destroy(marker.gameObject);
            }
            minimapMarkersByKey.Remove(key);
            Debug.Log($"[TowerGameController] Removed minimap marker for key={key}");
        }
    }

    // private void CreateQuestionObject(WS_Client.QuestionData question, Vector3 position)
    // {
    //     Debug.Log($"CreateQuestionObject: {question.id} - {question.content}");
    //     if (questionPrefab == null)
    //     {
    //         Debug.LogError("questionPrefab is not assigned!");
    //         return;
    //     }

    //     if (question.content == null)
    //     {
    //         Debug.LogError("question.content is null!");
    //         return;
    //     }

    //     var questionObj = GameObject.Instantiate(questionPrefab, this.globalParent);
    //     questionObj.name = "Question_" + question.id;

    //     // Use RectTransform for UI positioning
    //     RectTransform rectTransform = questionObj.GetComponent<RectTransform>();
    //     if (rectTransform != null)
    //     {
    //         rectTransform.anchoredPosition = new Vector2(position.x, position.y);
    //     }
    //     else
    //     {
    //         // Fallback to world position if not a UI element
    //         questionObj.transform.position = position;
    //     }

    //     // Set text on TextMeshProUGUI component in child
    //     // TextMeshProUGUI textComponent = questionObj.GetComponentInChildren<TextMeshProUGUI>();
    //     // Debug.Log($"question.content: {question.content}");
    //     // if (textComponent != null)
    //     // {
    //     //     textComponent.text = question.content;
    //     // }

    //     // Add QuestionTrigger component for collision detection
    //     QuestionTrigger questionTrigger = questionObj.GetComponent<QuestionTrigger>();
    //     if (questionTrigger == null)
    //     {
    //         questionTrigger = questionObj.AddComponent<QuestionTrigger>();
    //     }
    //     questionTrigger.questionId = question.id;
    //     questionTrigger.questionData = question;
    //     Debug.Log($"Added QuestionTrigger component to {question.id}");

    //     questionObj.gameObject.SetActive(true);

    //     // questionUIText.GetComponent<TextMeshProUGUI>().text = question.content;
    //     onTopUI.GetComponent<CanvasGroup>().alpha = 1;
    //     GameObject bg_FillInBlank = onTopUI.transform.Find("Bg/QABoard/bg_FillInBlank").gameObject;
    //     bg_FillInBlank.GetComponent<CanvasGroup>().alpha = 1;
    //     bg_FillInBlank.GetComponentInChildren<TextMeshProUGUI>().text = question.content;

    //     // Store the question data (you can add a component to store this if needed)
    //     // For now, just track the GameObject
    //     questionObjectsById[question.id] = questionObj;
    //     questions.Add(question);

    // }

    // private void RemoveQuestionObject(int id)
    // {
    //     if (questionObjectsById.TryGetValue(id, out var questionObj))
    //     {
    //         if (questionObj != null)
    //         {
    //             GameObject.Destroy(questionObj);
    //             Debug.Log($"[TowerGameController] Removed question GameObject for id={id}");
    //         }
    //         questionObjectsById.Remove(id);

    //         // Remove from list
    //         questions.RemoveAll(q => q.id == id);
    //     }
    // }

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
        Vector2 uiPosition = new Vector2(position.x, position.y);

        // Use RectTransform for UI positioning
        RectTransform rectTransform = answerObj.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = uiPosition;
        }
        else
        {
            // Fallback to world position if not a UI element
            answerObj.transform.position = new Vector3(uiPosition.x, uiPosition.y, 0f);
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
        // Find and update the answer in GameData
        if (WS_Client.Instance.GameData?.answers != null)
        {
            WS_Client.AnswerData answer = WS_Client.Instance.GameData.answers.Find(a => a.id == answerId);
            if (answer != null)
            {
                answer.isOnPlayer = 1;
                Debug.Log($"Set answer {answerId} isOnPlayer to 1");

                WS_Client.Instance.updateAnswerOnPlayer(answerId);
            }
            else
            {
                Debug.LogWarning($"Answer {answerId} not found in GameData.answers");
            }
        }
    }

    public void checkAnswerVisibility()
    {
        foreach (GameObject answerObj in answerObjectsById.Values) {
            AnswerTrigger answerTrigger = answerObj.GetComponent<AnswerTrigger>();
            if (answerTrigger != null)
            {
                answerTrigger.checkAnswerVisibility();
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
        Vector2 uiPosition = new Vector2(position.x, position.y);

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

        foreach (WS_Client.PlayerData player in WS_Client.Instance.GameData.players) {
            if (player.isAnswerVisible == 0) {
                CharacterController characterController = characterControllers.Find(c => c.UserId == player.uid);
                if (characterController != null) {
                    characterController.showAnswerBubble(0);
                }
            }
        }
    }

    public void loadRoomList() {
        WS_Client.Instance.JoinGameRoom(0);
        LoaderConfig.Instance?.changeScene(1);
    }

    public void quitGame() {
        #if UNITY_EDITOR
            // In Unity Editor, stop Play mode
            UnityEditor.EditorApplication.isPlaying = false;
        #elif UNITY_WEBGL
            // For WebGL builds, use JavaScript to close the browser tab
            Application.ExternalEval("window.close();");
        #else
            // For standalone builds, quit the application
            Application.Quit();
        #endif
    }

    public void ready(bool ready) {
        if (ready) {
            WS_Client.Instance.ready();
        } else {
            WS_Client.Instance.cancelReady();
        }
    }

}
