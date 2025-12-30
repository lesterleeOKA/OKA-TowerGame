using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameSetting : MonoBehaviour
{
    public HostName currentHostName = HostName.dev;
    private string _currentHostName;
    public string currentURL;
    public GameSetup gameSetup;
    public APIManager apiManager;
    public delegate void ParameterHandler(string value);
    protected private Dictionary<string, ParameterHandler> customHandlers = new Dictionary<string, ParameterHandler>();
    public string unitKey = string.Empty;
    public string testURL = string.Empty;
    public bool skipAudioPanel = false;
    
    // Loading screen references
    private GameObject loadingScreenObj;
    private CanvasGroup loadingScreenCanvas;
    private Slider loadingProgressBar;
    private Text loadingText;

    protected virtual void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
        Application.runInBackground = true;
        DontDestroyOnLoad(this);
    }

    public string GetCurrentDomainName(string Url="")
    {
        if(string.IsNullOrEmpty(Url))
            return null;
        else
        {
            Uri url = new Uri(Url);
            return url.Host;
        }
    }

    protected virtual void GetParseURLParams()
    {
        this.CurrentURL = string.IsNullOrEmpty(Application.absoluteURL) ? this.testURL : Application.absoluteURL;
        string hostName = this.GetCurrentDomainName(this.CurrentURL);
        LogController.Instance?.debug("Current hostName: " + hostName);
        switch (hostName)
        {
            case "dev.openknowledge.hk":
            case "devapp.openknowledge.hk":
            case "dev.starwishparty.com":
                this.currentHostName = HostName.dev;
                LogController.Instance?.UpdateVersion("dev");
                break;
            case "uat.openknowledge.hk":
            case "uat.starwishparty.com":
                this.currentHostName = HostName.uat;
                LogController.Instance?.UpdateVersion("uat");
                break;
            case "pre.openknowledge.hk":
            case "pre.starwishparty.com":
                this.currentHostName = HostName.preprod;
                LogController.Instance?.UpdateVersion("preprod");
                break;
            case "www.rainbowone.app":
            case "rainbowone.app":
            case "api.openknowledge.hk":
            case "www.starwishparty.com":
            case "starwishparty.com":
                this.currentHostName = HostName.prod;
                LogController.Instance?.UpdateVersion("prod");
                break;
            default:
                LogController.Instance?.UpdateVersion("dev");
                break;
        }
        this.CurrentHostName = hostName;

        string[] urlParts = this.CurrentURL.Split('?');
        if (urlParts.Length > 1)
        {
            string queryString = urlParts[1];
            string[] parameters = queryString.Split('&');

            foreach (string parameter in parameters)
            {
                string[] keyValue = parameter.Split('=');
                if (keyValue.Length == 2)
                {
                    string key = keyValue[0];
                    string value = keyValue[1];

                    if (!string.IsNullOrEmpty(value))
                    {

                        switch (key)
                        {
                            case "jwt":
                                this.apiManager.jwt = value;
                                break;
                            case "id":
                                this.apiManager.appId = value;
                                break;
                            case "unit":
                                this.unitKey = value;
                                break;
                            case "gameTime":
                                this.GameTime = float.Parse(value);
                                this.ShowFPS = true;
                                break;
                            case "playerNumbers":
                                this.PlayerNumbers = int.Parse(value);
                                break;
                            case "lang":
                                if (value == "tc" || value == "sc")
                                {
                                    this.Lang = 1;
                                }
                                else if (value == "en")
                                {
                                    this.Lang = 0;
                                }
                                else
                                {
                                    this.Lang = int.Parse(value);
                                }
                                LogController.Instance?.debug("Current Language: " + this.Lang);
                                break;
                            case "returnUrl":
                                this.ReturnUrl = UnityWebRequest.UnEscapeURL(value);
                                LogController.Instance?.debug("ReturnUrl: " + this.ReturnUrl);
                                ExternalCaller.RemoveReturnUrlFromAddressBar();
                                break;
                            case "dataKey":
                                this.RoAppDataKey = UnityWebRequest.UnEscapeURL(value);
                                LogController.Instance?.debug("DataKey: " + this.RoAppDataKey);
                                break;
                            default:
                                if (this.customHandlers.TryGetValue(key, out ParameterHandler handler))
                                {
                                    handler(value);
                                }
                                break;
                        }
                    }
                }
            }
        }
    }

    public void RegisterCustomHandler(string key, ParameterHandler handler)
    {
        if (!this.customHandlers.ContainsKey(key))
        {
            this.customHandlers[key] = handler;
        }
    }

    protected virtual void Start()
    {
        this.apiManager.Init();
    }

    protected virtual void Update()
    {
        this.apiManager.controlDebugLayer();
    }

    public void InitialGameImages(Action onCompleted = null)
    {
        if (this.apiManager.IsLogined)
        {
            this.initialGameImagesByAPI(onCompleted);
        }
        else
        {
            this.initialGameImagesByLocal(onCompleted);
        }
    }

    private void initialGameImagesByLocal(Action onCompleted = null)
    {
        //Download game background image from local streaming assets
        this.gameSetup.loadImageMethod = LoadImageMethod.StreamingAssets;
        StartCoroutine(this.gameSetup.Load("GameUI", "bg", _bgTexture =>
        {
            LogController.Instance?.debug($"Downloaded bg Image!!");
            ExternalCaller.UpdateLoadBarStatus("Loading Bg");
            if(_bgTexture != null) this.gameSetup.bgTexture = _bgTexture;

            StartCoroutine(this.gameSetup.Load("GameUI", "preview", _previewTexture =>
            {
                LogController.Instance?.debug($"Downloaded preview Image!!");
                ExternalCaller.UpdateLoadBarStatus("Loaded UI");
                if(_previewTexture != null) this.gameSetup.previewTexture = _previewTexture;
                onCompleted?.Invoke();
            }));
        }));
    }

    private void initialGameImagesByAPI(Action onCompleted = null)
    {
        //Download game background image from api
        this.gameSetup.loadImageMethod = LoadImageMethod.Url;
        var imageUrls = new List<string>
        {
            this.apiManager.settings.backgroundImageUrl,
            this.apiManager.settings.previewGameImageUrl,
        };
        imageUrls = imageUrls.Where(url => !string.IsNullOrEmpty(url)).ToList();

        string[] objectItemImages = this.apiManager.settings.object_item_images;

        if (objectItemImages != null)
        {
            imageUrls.AddRange(objectItemImages.Where(url => !string.IsNullOrEmpty(url)));
        }

        if (imageUrls.Count > 0)
        {
            StartCoroutine(LoadImages(imageUrls, onCompleted));
        }
        else
        {
            LogController.Instance?.debug($"No valid image URLs found!!");
            onCompleted?.Invoke();
        }
    }

    private IEnumerator LoadImages(List<string> imageUrls, Action onCompleted)
    {
        foreach (var url in imageUrls)
        {
            Texture texture = null;
            // Load each image
            yield return StartCoroutine(this.gameSetup.Load("", url, _texture =>
            {
                texture = _texture;
                LogController.Instance?.debug($"Downloaded image from: {url}");
                ExternalCaller.UpdateLoadBarStatus($"Loaded UI");
            }));

            // Assign textures based on their URL
            if (url == this.apiManager.settings.backgroundImageUrl)
            {
                this.gameSetup.bgTexture = texture != null ? texture : null;
            }
            else if (url == this.apiManager.settings.previewGameImageUrl)
            {
                this.gameSetup.previewTexture = texture != null ? texture : null;
            }
            else if (this.apiManager.settings.object_item_images.Contains(url))
            {
                if (this.gameSetup.object_item_images == null)
                {
                    this.gameSetup.object_item_images = new List<Texture>();
                }
                this.gameSetup.object_item_images.Add(texture != null ? texture : null);
            }
        }

        onCompleted?.Invoke();
    }

    public void InitialGameSetup()
    {
        this.gameSetup.setBackground();
        /*var content =  this.apiManager.IsLogined ? this.apiManager.settings.instructionContent :  QuestionManager.Instance.questionData.instruction;
        this.gameSetup.setInstruction(content);*/
    }
    public string CurrentURL
    {
        set { this.currentURL = value; }
        get { return this.currentURL; }
    }

    public float GameTime
    {
        get { return this.gameSetup.gameTime; }
        set { this.gameSetup.gameTime = value; }
    }

    public bool ShowFPS
    {
        get { return this.gameSetup.showFPS; }
        set { this.gameSetup.showFPS = value; }
    }

    public int PlayerNumbers
    {
        get { return this.gameSetup.playerNumber; }
        set { this.gameSetup.playerNumber = value; }
    }

    public int Lang
    {
        get { return this.gameSetup.lang; }
        set { this.gameSetup.lang = value; }
    }

    public string ReturnUrl
    {
        get { return this.gameSetup.returnUrl; }
        set { this.gameSetup.returnUrl = value; }
    }

    public string RoAppDataKey
    {
        get { return this.gameSetup.roAppDataKey; }
        set { this.gameSetup.roAppDataKey = value; }
    }

    public string CurrentHostName
    {
        set
        {
            this._currentHostName = value;
        }
        get
        {
            return this._currentHostName;
            /*return currentHostName switch
            {
                HostName.dev => "https://dev.openknowledge.hk",
                HostName.prod => "https://www.rainbowone.app",
                _ => throw new NotImplementedException()
            };*/
        }
    }

    public void Reload()
    {
        ExternalCaller.ReLoadCurrentPage();
    }

    public void changeScene(int sceneId)
    {
        if (sceneId != 2) {
            SceneManager.LoadScene(sceneId);
        } else {
            StartCoroutine(LoadSceneAsync(sceneId));
        }
        // StartCoroutine(LoadSceneAsync(sceneId));
    }

    private IEnumerator LoadSceneAsync(int sceneId)
    {
        LogController.Instance?.debug($"Starting to load scene {sceneId}...");
        
        // Create loading screen
        CreateLoadingScreen();
        
        // Show loading screen immediately
        if (loadingScreenCanvas != null)
        {
            LogController.Instance?.debug("Setting loading screen visible...");
            loadingScreenCanvas.alpha = 1f;
            loadingScreenCanvas.interactable = true;
            loadingScreenCanvas.blocksRaycasts = true;
        }
        else
        {
            LogController.Instance?.debug("ERROR: loadingScreenCanvas is null!");
        }
        
        // Wait one frame to ensure UI is rendered
        yield return new WaitForEndOfFrame();
        
        // Start loading the scene
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneId);
        asyncLoad.allowSceneActivation = false;
        
        // Minimum loading time to ensure visibility
        float minimumLoadTime = 0.5f;
        float startTime = Time.time;
        
        // Update loading progress
        while (!asyncLoad.isDone)
        {
            // Progress goes from 0 to 0.9 while loading
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            
            // Update progress bar if it exists
            if (loadingProgressBar != null)
            {
                loadingProgressBar.value = progress;
            }
            
            // Update loading text if it exists
            if (loadingText != null)
            {
                loadingText.text = $"Loading... {(progress * 100f):0}%";
            }
            
            // Scene is ready to activate
            if (asyncLoad.progress >= 0.9f)
            {
                // Ensure minimum loading time has passed
                float elapsedTime = Time.time - startTime;
                if (elapsedTime < minimumLoadTime)
                {
                    yield return new WaitForSeconds(minimumLoadTime - elapsedTime);
                }
                
                // Show 100% for a brief moment
                if (loadingProgressBar != null)
                {
                    loadingProgressBar.value = 1f;
                }
                if (loadingText != null)
                {
                    loadingText.text = "Loading... 100%";
                }
                
                yield return new WaitForSeconds(0.3f);
                
                // Fade out loading screen
                if (loadingScreenCanvas != null)
                {
                    float fadeOutTime = 0.3f;
                    float fadeOutElapsed = 0f;
                    
                    while (fadeOutElapsed < fadeOutTime)
                    {
                        fadeOutElapsed += Time.deltaTime;
                        loadingScreenCanvas.alpha = Mathf.Lerp(1f, 0f, fadeOutElapsed / fadeOutTime);
                        yield return null;
                    }
                    
                    loadingScreenCanvas.alpha = 0f;
                    loadingScreenCanvas.interactable = false;
                    loadingScreenCanvas.blocksRaycasts = false;
                }
                
                // Activate the scene
                LogController.Instance?.debug($"Scene {sceneId} loaded successfully!");
                asyncLoad.allowSceneActivation = true;
            }
            
            yield return null;
        }
    }

    private void CreateLoadingScreen()
    {
        // Check if loading screen already exists
        if (loadingScreenObj != null)
        {
            LogController.Instance?.debug("Loading screen already exists, reusing it.");
            return;
        }
        
        LogController.Instance?.debug("Creating loading screen...");
        
        // Create loading screen canvas
        loadingScreenObj = new GameObject("LoadingScreen");
        DontDestroyOnLoad(loadingScreenObj);
        
        Canvas canvas = loadingScreenObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999; // Ensure it's on top
        
        CanvasScaler scaler = loadingScreenObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        loadingScreenObj.AddComponent<GraphicRaycaster>();
        
        loadingScreenCanvas = loadingScreenObj.AddComponent<CanvasGroup>();
        loadingScreenCanvas.alpha = 0;
        
        // Create background panel
        GameObject bgPanel = new GameObject("Background");
        bgPanel.transform.SetParent(loadingScreenObj.transform, false);
        
        RectTransform bgRect = bgPanel.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        
        Image bgImage = bgPanel.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.9f); // Dark background
        
        // Create loading text
        GameObject textObj = new GameObject("LoadingText");
        textObj.transform.SetParent(loadingScreenObj.transform, false);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchoredPosition = new Vector2(0, 100);
        textRect.sizeDelta = new Vector2(600, 100);
        
        loadingText = textObj.AddComponent<Text>();
        loadingText.text = "Loading... 0%";
        // Try to get LegacyRuntime font for newer Unity versions
        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (defaultFont == null)
        {
            // Fallback to Arial for older Unity versions
            defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        if (defaultFont != null)
        {
            loadingText.font = defaultFont;
        }
        loadingText.fontSize = 40;
        loadingText.alignment = TextAnchor.MiddleCenter;
        loadingText.color = Color.white;
        
        // Create progress bar background
        GameObject progressBg = new GameObject("ProgressBarBackground");
        progressBg.transform.SetParent(loadingScreenObj.transform, false);
        
        RectTransform progressBgRect = progressBg.AddComponent<RectTransform>();
        progressBgRect.anchoredPosition = Vector2.zero;
        progressBgRect.sizeDelta = new Vector2(800, 50);
        
        Image progressBgImage = progressBg.AddComponent<Image>();
        progressBgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        
        // Create progress bar slider
        GameObject sliderObj = new GameObject("ProgressBar");
        sliderObj.transform.SetParent(progressBg.transform, false);
        
        RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
        sliderRect.anchorMin = Vector2.zero;
        sliderRect.anchorMax = Vector2.one;
        sliderRect.sizeDelta = Vector2.zero;
        
        loadingProgressBar = sliderObj.AddComponent<Slider>();
        loadingProgressBar.minValue = 0f;
        loadingProgressBar.maxValue = 1f;
        loadingProgressBar.value = 0f;
        
        // Create fill area for slider
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.sizeDelta = new Vector2(-10, -10);
        
        // Create fill
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        
        RectTransform fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0.2f, 0.8f, 0.2f, 1f); // Green fill
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        
        loadingProgressBar.fillRect = fillRect;
        loadingProgressBar.targetGraphic = fillImage;
        
        LogController.Instance?.debug("Loading screen created successfully!");
    }
}

[Serializable]
public class GameSetup : LoadImage
{
    [Tooltip("RainbowOne book question dataKey")]
    public string roAppDataKey = string.Empty;
    [Tooltip("Game Page Name")]
    public string gamePageName = "";
    [Tooltip("Default Game Background Texture")]
    public Texture bgTexture;
    [Tooltip("Default Game Preview Texture")]
    public Texture previewTexture;
    [Tooltip("Object item images array from default game settings")]
    public List<Texture> object_item_images = new List<Texture>();
    [Tooltip("Find Tag name of GameBackground in different scene")]
    public RawImage gameBackground;
    [Tooltip("Instruction Preview Image")]
    public RawImage gamePreview;
    [Tooltip("Game Exit Method, 0 is for demo page; 1 is back to roWeb; 2 is restart game")]
    public int gameExitType = 0;
    public InstructionText instructions;
    public float gameTime;
    public Inventory inventory;
    public float playersMovingSpeed = 1f;
    public int retry_times = 3;
    public int helpItemTypeOfId = 3;
    public string returnUrl = "";
    public bool showFPS = false;
    public int qa_font_alignment = 1; // 0: left, 1: center, 2: right
    public int playerNumber = 1;
    [Range(0, 1)]
    public int lang = 0;
    public int gameSettingScore = -1;
    public int gameTotalStars = 3;

    public void setBackground()
    {
        if (this.gameBackground == null)
        {
            var tex = GameObject.FindGameObjectWithTag("GameBackground");
            this.gameBackground = tex.GetComponent<RawImage>();
        }

        if (this.gameBackground != null)
        {
            this.gameBackground.texture = this.bgTexture;
        }
    }

    public void setInstruction(string content = "")
    {
        if (!string.IsNullOrEmpty(content) && this.instructions == null)
        {
            var instructionText = GameObject.FindGameObjectWithTag("Instruction");
            this.instructions = instructionText != null ? instructionText.GetComponent<InstructionText>() : null;
            if (instructionText != null) this.instructions.setContent(content);
        }

        if (this.gamePreview == null)
        {
            var preview = GameObject.FindGameObjectWithTag("GamePreview");

            if (preview != null)
            {
                var aspectRatio = preview.GetComponent<AspectRatioFitter>();
                this.gamePreview = preview.GetComponent<RawImage>();

                if (this.gamePreview != null) this.gamePreview.texture = this.previewTexture;

                if (aspectRatio != null)
                {
                    aspectRatio.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
                    aspectRatio.aspectRatio = (float)this.previewTexture.width / this.previewTexture.height;
                }
            }
        }
    }
}

[Serializable]
public class StarwishPartyAccountResponse
{
    public string status;
    public AccountData data;
}

[Serializable]
public class AccountData
{
    public string id;
    public string currency;
    public string equipped_costume;
    public string[] owned_costume;
    public string created_at;
    public string updated_at;
    public object deleted_at; // Use 'object' for nullable fields
    public EquippedCostumeData equipped_costume_data;
}

[Serializable]
public class EquippedCostumeData
{
    public string costume_id;
    public string costume_name;
    public string description;
    public string price;
    public string img_src_wholebody;
    public string img_src_head;
    public string created_at;
    public string updated_at;
    public object deleted_at; // Use 'object' for nullable fields
}

[Serializable]
public class CostumeData
{
    public string costume_id;
    public string costume_name;
    public string description;
    public string price;
    public string img_src_wholebody;
    public string img_src_head;
    public string img_src_stand;
    public string img_src_walk;
    public string img_src_jump;
    public string created_at;
    public string updated_at;
    public object deleted_at; // Use 'object' for nullable fields
}

[Serializable]
public class CostumeListResponse
{
    public string status;
    public CostumeData[] data;
}

[Serializable]
public class HelpToolInventory
{
    public int help_tool_id;
    public string help_tool_name;
    public string description;
    public int amount;
}

[Serializable]
public class Inventory
{
    public string status;
    public HelpToolInventory[] data;
}

[Serializable]
public class HelpToolRequest
{
    public int help_tool_id;
    public int amount;
}


public enum HostName
{
    dev,
    uat,
    preprod,
    prod
}