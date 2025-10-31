using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NativeWebSocket;
using System.Threading.Tasks;
using System.Linq; // 添加这一行

public class WS_Client : MonoBehaviour
{
    private static WS_Client _instance;
    public static WS_Client Instance
    {
        get
        {
            // 如果实例不存在，尝试在场景中查找
            if (_instance == null)
            {
                _instance = FindObjectOfType<WS_Client>();
                // 如果场景中也没有，就创建一个新的GameObject并挂载此组件
                if (_instance == null)
                {
                    GameObject singletonObject = new GameObject(typeof(WS_Client).Name);
                    _instance = singletonObject.AddComponent<WS_Client>();
                }
            }
            return _instance;
        }
    }
    WebSocket websocket;
    private string channelId = "towerGame";
    //uid = 543717
    // public string jwt = "eyJ0eXAiOiJqd3QiLCJhbGciOiJIUzI1NiJ9.eyJsb2dfZW5hYmxlZCI6IjEiLCJ0b2tlbiI6IjU0MzcxNy05ZjY3MjcwZDk1Zjc5NjEzMTMwNzU0MGEyNjUyMDdmN2Q0YWM5ZDU2OTM3OTBiMmNhNjhlNTQ5YzI5NjBkZmM5IiwiZXhwaXJlcyI6MTc2MjIyNDQ0NywicmVuZXdfZW5hYmxlZCI6MSwidGltZSI6IjIwMjUtMTAtMjggMDI6NDc6MjcgR01UIiwidWlkIjoiNTQzNzE3IiwidXNlcl9yb2xlIjoiMiIsInNjaG9vbF9pZCI6IjI3MiIsImlwIjoiMTY5LjI1NC4xMjkuNiIsInZlcnNpb24iOiIyLjguMzYiLCJkZXZpY2UiOiJ3aW5kb3dzIn0.tDcwbbY0OxfSCrrAMcneyvji2u5M7k5M8Moz7JQHiUU";
    public string jwt = "eyJ0eXAiOiJqd3QiLCJhbGciOiJIUzI1NiJ9.eyJsb2dfZW5hYmxlZCI6IjEiLCJ0b2tlbiI6IjUxMS00MzY0ZTlmYmE3NzA2M2Q4MjdjZWY0NjMzMGYwMjlhZmU2ZTIyNWZhOTk1MGMzMTRiMzRkNjAyNjY5NGUzYWIwIiwiZXhwaXJlcyI6MTc2MzYxMDE0NywidGltZSI6IjIwMjUtMTAtMjEgMTE6NDI6MjciLCJ1aWQiOiI1MTEiLCJ1c2VyX3JvbGUiOiIzIiwic2Nob29sX2lkIjoiMjcyIiwiaXAiOiIxNjkuMjU0LjEyOS40IiwidmVyc2lvbiI6IjIuOC4zNiIsImRldmljZSI6Im1hYyJ9.LT8f4UNEB3nnW6BY2FMPQXZVMUzQ-6NyCJT08gqSx1s";
    private string roomId = "";
    private string player_id = "";

    // const string WEBSHOCKET_URL = "wss://ws.openknowledge.hk:8084";//dev : "wss://ws.openknowledge.hk:8084";  // prod : "wss://ws.openknowledge.hk";
    public string localhostUrl = "ws://localhost:8000/";
    public string localhostUrl_copy = "ws://localhost:8000/";
    public string developmentUrl = "wss://ws.openknowledge.hk:8084";
    public string productionUrl = "wss://ws.openknowledge.hk";
    const string WS_API_BASE_URL = "https://ws.openknowledge.hk/api/towerGame";//"https://ws.openknowledge.hk:8084/api/metaverse";//"https://ws.openknowledge.hk/api/metaverse";
    public const int TIMEOUT_TIMELIMIT = 15;
    // flag set by WS thread when "test" message received
    public static volatile bool testReceived = false;
    public static volatile bool gameDataReceived = false;
    private bool isJoining = false;
    private float lastJoinTime = 0f;
    private const float JOIN_COOLDOWN = 1f; // 1秒冷却时间
    private UserInfo userInfo;
    private bool isSendingPosition = false;

    // 新增公共属性，作为访问私有字段的受控接口
    public UserInfo pulic_UserInfo
    {
        get { return userInfo; }
        set { userInfo = value; }
    }
    // 私有静态字段，用于实际存储数据
    private static RoomGameData _gameData;

    // 公共静态属性，供其他类访问
    public static RoomGameData GameData
    {
        get { return _gameData; }
        set { _gameData = value; }
    }

    [Serializable]
    public class UserInfo
    {
        public int uid;
        public string cname;
        public string ename;
        public int gender;
        public int user_role;
        public int school_id;
        public string classno;
        public string nickname;
        public string wsId;
        public string channelId;
        public string[] roomIds;
    }
    [Serializable]
    private class MessageContent
    {
        public string action;
        // public PositionData destination;
        // public PositionData position;
        public string position;
        public string destination;
        public string answer_id;
    }

    [Serializable]
    private class OutMessage
    {
        public string messageType;
        public MessageContent content;
        public string roomId;
    }

    [System.Serializable]
    public class WebSocketMessage
    {
        public string fromWsId;
        public int fromUid;
        public string messageType;
        public string roomId;
        // 可以根据需要添加其他字段
        public string data;
        public ServerMessageContent content;
        public long time;
    }

    public class PositionData
    {
        public float x;
        public float y;
    }

    [System.Serializable]
    public class ServerMessageContent
    {
        public RoomGameData roomGameData;
        // 如果需要，你也可以定义members字段
        // public List<RoomMember> members;
        public string roomId;
        public UserInfo userInfo;
        public string message;
        public List<RoomInfo> roomList;
    }

    [System.Serializable]
    public class RoomInfo
    {
        public string roomId;
        public int roomMembers;
    }

    [System.Serializable]
    public class RoomGameData
    {
        public List<PlayerData> players;
        public List<QuestionData> questions;
        public List<AnswerData> answers;
        public string status;
    }

    [System.Serializable]
    public class PlayerData
    {
        public string player_id;
        public int uid;
        public float[] position;
        public float[] destination;
        public int isAnswerVisible;
        public string answerContent;
    }

    [System.Serializable]
    public class QuestionData
    {
        public string id;
        public string content;
    }

    [System.Serializable]
    public class AnswerData
    {
        public string id;
        public string content;
        public string question_id;
        public float[] position;
        public int isOnPlayer;
        public int isSubmitted;
    }

    // Dummy data for testing (based on expected server format)
    private List<QuestionData> dummyQuestions = new List<QuestionData>
    {
        new QuestionData { id = "Q1", content = "Question 1:XXXXX" },
        new QuestionData { id = "Q2", content = "Question 2:XXXXX" },
        new QuestionData { id = "Q3", content = "Question 3:XXXXX" },
        new QuestionData { id = "Q4", content = "Question 4:XXXXX" }
    };

    private List<AnswerData> dummyAnswers = new List<AnswerData>
    {
        new AnswerData { id = "A1", content = "Answer 1", question_id = "Q1", position = new float[] { -1f, 0f }, isOnPlayer = 0, isSubmitted = 0 },
        new AnswerData { id = "A2", content = "Answer 2", question_id = "Q2", position = new float[] { 0f, 1f }, isOnPlayer = 0, isSubmitted = 0 },
        new AnswerData { id = "A3", content = "Answer 3", question_id = "Q3", position = new float[] { 1f, 0f }, isOnPlayer = 0, isSubmitted = 0 },
        new AnswerData { id = "A4", content = "Answer 4", question_id = "Q4", position = new float[] { 0f, -1f }, isOnPlayer = 0, isSubmitted = 0 }
    };

    // 如果需要处理members，可以定义此类
    [System.Serializable]
    public class RoomMember
    {
        public int uid;
        public string cname;
        public string ename;
        public int gender;
        public int user_role;
        public int school_id;
        public string classno;
        public string nickname;
        public string wsId;
        public string channelId;
        public string[] roomIds;
    }



    public static string GetCurrentDomainName
    {
        get
        {
            string absoluteUrl = Application.absoluteURL;
            Uri url = new Uri(absoluteUrl);
            // if (LogController.Instance != null) LogController.Instance.debug("Host Name:" + url.Host);
            Debug.Log("Host : " + url.Host);
            return url.Host;
        }
    }

    public string GetCurrentUrl()
    {
#if UNITY_EDITOR
        return localhostUrl;
#else
        string currentDomain = GetCurrentDomainName.ToLower();

        // localhost to localhost websocket
        if (currentDomain == "localhost")
        {
            Debug.Log($"Localhost environment detected: {currentDomain}");
            return localhostUrl;
        }

        // 环境检测逻辑：如果域名以"dev"开头，则使用开发服务器
        if (currentDomain.StartsWith("dev"))
        {
            Debug.Log($"Development environment detected: {currentDomain}");
            return developmentUrl;
        }
        else
        {
            Debug.Log($"Production environment detected: {currentDomain}");
            return productionUrl;
        }
#endif
    }

    void Start()
    {
        Debug.Log("Connecting to WebSocket...");

        // GameData.questions = dummyQuestions;
        // GameData.answers = dummyAnswers;
        Connect();
    }

    private void Awake()
    {
        Debug.Log("WS_Client Awake");
        // 确保单例在场景切换时不被销毁，且实例唯一
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(this.gameObject); // 可选：如需跨场景保持连接
        }

        // 在这里初始化WebSocket连接
        // InitializeWebSocket();
    }

    public async void Connect()
    {
        // var baseUrl = WEBSHOCKET_URL; // "wss://ws.openknowledge.hk"
        // // *********************************************
        // var baseUrl = "ws://localhost:8000/"; // comment when build and deploy
        // *********************************************
        var baseUrl = GetCurrentUrl();

        var query = "?channelId=" + Uri.EscapeDataString(channelId) + "&jwt=" + Uri.EscapeDataString(jwt);

        var fullUrl = baseUrl + query;
        Debug.Log("URL : " + fullUrl);
        websocket = new WebSocket(fullUrl);

        websocket.OnOpen += OnWebSocketOpen;

        websocket.OnError += (e) =>
        {
            Debug.Log("Error! " + e);
        };

        websocket.OnClose += (e) =>
        {
            Debug.Log("Connection closed!");
        };

        websocket.OnMessage += (bytes) =>
        {
            try
            {
                // 将字节数组转换为字符串
                var jsonString = System.Text.Encoding.UTF8.GetString(bytes);
                // debugLogPerSecond("OnMessage! " + jsonString, "debug");

                // 将JSON字符串反序列化为对象
                WebSocketMessage message = JsonUtility.FromJson<WebSocketMessage>(jsonString);

                // 现在可以安全地访问messageType属性
                switch (message.messageType)
                {
                    case "roomInfo":
                        roomId = message.roomId;
                        Debug.Log("current roomId : " + roomId);
                        break;
                    case "listGameRoom":
                        if (message.content.roomList != null)
                        {
                            Debug.Log("listGameRoom count: " + message.content.roomList.Count);
                            foreach (var room in message.content.roomList)
                            {
                                Debug.Log($"Room: {room.roomId}, Members: {room.roomMembers}");
                            }
                        }
                        else
                        {
                            Debug.LogError("roomList is null!");
                        }
                        break;
                    case "roomFull":
                        Debug.Log("roomFull : " + message.content.message + " / " + "current roomId : " + roomId);
                        break;
                    case "SyncRoomData":
                        // Debug.Log("OnMessage! " + jsonString);
                        GameData = message.content.roomGameData;
                        
                        if (GameData.players != null)
                        {
                            foreach (var player in GameData.players)
                            {
                                // 获取当前遍历玩家的位置坐标 [x, y]
                                int index = GameData.players.IndexOf(player);
                                float posX = player.position[0];
                                float posY = player.position[1];
                                float destX = player.destination[0];
                                float destY = player.destination[1];
                                if (player.uid == this.userInfo.uid)
                                {
                                    this.player_id = player_id.ToString();
                                }
                            }
                        }

                        gameDataReceived = true;
                        break;
                    case "ready":
                        this.userInfo = message.content.userInfo;
                        break;
                    case "test":
                        testReceived = true;
                        break;
                    default:
                        Debug.Log("Unhandled messageType: " + message.messageType);
                        break;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error processing message: " + e.Message);
            }
        };

        // Keep sending messages at every 5s
        InvokeRepeating("SendTest", 0.0f, 5f);
        // Keep sending game data at every 0.1s
        InvokeRepeating("ConstantSyncData", 0.0f, 0.1f);

        // waiting for messages\
        await websocket.Connect();
    }

    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        if (websocket == null) return;
        websocket.DispatchMessageQueue();
#endif
        if (Input.GetKeyDown(KeyCode.G))
        {
            updateAnswerOnPlayer("A1");
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            resetGame();
        }
    }

    public void JoinGameRoom()
    {
        if (websocket == null || websocket.State != WebSocketState.Open)
        {
            Debug.Log(websocket);
            Debug.LogWarning("WebSocket not connected! Cannot join room.");
            return;
        }
        if (!isJoining)
        {
            lastJoinTime = Time.time;
            isJoining = true;
            _ = JoinRoomAsync();
        }
    }

    // 连接打开后
    private async void OnWebSocketOpen()
    {
        Debug.Log("Connection open!");
        Debug.Log("WebSocket connection established! Attempting to join room...");
        try
        {
            // await JoinRoom(); // 调用一次 JoinRoom
            await ListGameRoom();
            // Automatically join game room after listing rooms
            JoinGameRoom();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to join room: {ex.Message}");
        }

    }

    public async Task ListGameRoom()
    {
        var msg = new OutMessage
        {
            messageType = "listGameRoom",
            content = new MessageContent { action = "listGameRoom" }
        };

        string jsonString = JsonUtility.ToJson(msg);
        await websocket.SendText(jsonString);
    }

    private async Task JoinRoomAsync()
    {
        try
        {
            Debug.Log("J key pressed - joining room...");
            // 这里替换为你的实际加入房间逻辑
            await JoinRoom();
            // Debug.Log("Room joined successfully!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Join room failed: {e.Message}");
        }
        finally
        {
            isJoining = false;
        }
    }

    public async Task JoinRoom()
    {
        var msg = new OutMessage
        {
            messageType = "joinRoom",
            content = new MessageContent { action = "joinRoom" },
            roomId = "room1"
        };

        string jsonString = JsonUtility.ToJson(msg);
        await websocket.SendText(jsonString);
    }

    async void SendTest()
    {
        var msg = new OutMessage
        {
            messageType = "handleMessage",
            content = new MessageContent { action = "test" }
        };

        string jsonString = JsonUtility.ToJson(msg);
        await websocket.SendText(jsonString);
    }

    async void ConstantSyncData()
    {
        if (isSendingPosition) return;
        // 检查WebSocket连接状态
        if (websocket?.State != WebSocketState.Open)
        {
            debugLogPerSecond("WebSocket未连接，无法发送位置同步数据！", "warning");
            return;
        }

        // 检查是否已加入房间
        if (string.IsNullOrEmpty(roomId) || roomId == "lobby")
        {
            // debugLogPerSecond("未加入有效房间，跳过位置同步", "debug");
            return;
        }

        try
        {
            // 1. 从本地GameData中获取当前玩家的数据
            if (GameData?.players == null)
            {
                debugLogPerSecond("GameData或players列表为空，无法同步位置", "warning");
                return;
            }

            // 获取当前玩家的UID
            int currentPlayerUid = this.userInfo.uid;

            // 在本地数据中查找当前玩家
            PlayerData myPlayer = GameData.players.FirstOrDefault(p => p.uid == currentPlayerUid);

            if (myPlayer == null)
            {
                debugLogPerSecond($"在GameData中未找到UID为{currentPlayerUid}的玩家, GameData: {GameData.players}", "warning");
                return;
            }

            // 2. 准备位置数据
            PositionData positionData = new PositionData
            {
                x = myPlayer.position[0],
                y = myPlayer.position[1]
            };

            PositionData destinationData = new PositionData
            {
                x = myPlayer.destination[0],
                y = myPlayer.destination[1]
            };

            // 3. 发送位置更新到服务器
            await UpdateServerPosition(positionData, destinationData);

            debugLogPerSecond($"位置同步发送: 位置({positionData.x:F2}, {positionData.y:F2}) -> 目的地({destinationData.x:F2}, {destinationData.y:F2})", "debug");
        }
        catch (System.Exception e)
        {
            debugLogPerSecond($"位置同步失败: {e.Message}", "error");
        }
        finally
        {
            isSendingPosition = false;
        }
    }

    public async Task UpdateServerPosition(PositionData position, PositionData destination)
    {
        isSendingPosition = true;
        if (websocket?.State == WebSocketState.Open)
        {
            var msg = new OutMessage
            {
                messageType = "UpdateServerPosition",
                content = new MessageContent
                {
                    action = "UpdateServerPosition",
                    // 将坐标转换为类似 "[x, y]" 的字符串格式
                    position = $"[{position.x}, {position.y}]",
                    destination = $"[{destination.x}, {destination.y}]"
                }
            };

            string jsonString = JsonUtility.ToJson(msg);
            await websocket.SendText(jsonString);
            debugLogPerSecond($"发送位置更新: {jsonString}", "debug");
        }
        else
        {
            debugLogPerSecond("WebSocket未连接！", "warning");
        }
    }

    public void UpdatePlayerPositionInGameData(int playerUid, float[] newPosition, float[] newDestination = null)
    {
        // 确保 GameData 和玩家列表不为空
        if (GameData?.players == null)
        {
            debugLogPerSecond("尝试更新玩家位置时，GameData 或 players 为 null。", "warning");
            return;
        }

        // 查找指定UID的玩家
        var playerToUpdate = GameData.players.FirstOrDefault(p => p.uid == playerUid);
        if (playerToUpdate != null)
        {
            // 更新玩家位置
            playerToUpdate.position = newPosition; // 例如 [1.5f, 2.3f]

            // 如果提供了新目的地，则一并更新
            if (newDestination != null)
            {
                playerToUpdate.destination = newDestination;
            }

            debugLogPerSecond($"已更新本地玩家数据: UID {playerUid} 位置 -> [{newPosition[0]}, {newPosition[1]}]", "debug");
        }
        else
        {
            debugLogPerSecond($"在 GameData 中未找到 UID 为 {playerUid} 的玩家。", "warning");
        }
    }

    public async Task updateAnswerOnPlayer(string answer_id)
    {
        isSendingPosition = true;
        if (websocket?.State == WebSocketState.Open)
        {
            var msg = new OutMessage
            {
                messageType = "UpdateAnswerOnPlayer",
                content = new MessageContent
                {
                    action = "UpdateAnswerOnPlayer",
                    answer_id = answer_id
                }
            };

            string jsonString = JsonUtility.ToJson(msg);
            await websocket.SendText(jsonString);
            Debug.Log($"玩家取得答案: {jsonString}");
        }
        else
        {
            Debug.LogWarning("WebSocket未连接！");
        }
    }

    public async Task resetGame()
    {
        isSendingPosition = true;
        if (websocket?.State == WebSocketState.Open)
        {
            var msg = new OutMessage
            {
                messageType = "resetGame",
                content = new MessageContent
                {
                    action = "resetGame"
                }
            };

            string jsonString = JsonUtility.ToJson(msg);
            await websocket.SendText(jsonString);
            Debug.Log($"重置游戏: {jsonString}");
        }
        else
        {
            Debug.LogWarning("WebSocket未连接！");
        }
    }

    async void SendWebSocketMessage()
    {
        if (websocket.State == WebSocketState.Open)
        {
            // Sending bytes
            await websocket.Send(new byte[] { 10, 20, 30 });

            // Sending plain text
            await websocket.SendText("plain text message");
        }
    }

    private async void OnApplicationQuit()
    {
        await websocket.Close();
    }

    private float lastLogTime = 0f;
    private void debugLogPerSecond(string message, string type)
    {
        if (Time.time - lastLogTime >= 1f)
        {
            switch (type)
            {
                case "debug":
                    Debug.Log(message);
                    break;
                case "warning":
                    Debug.LogWarning(message);
                    break;
                case "error":
                    Debug.LogError(message);
                    break;
                default:
                    Debug.Log(message);
                    break;
            }
            lastLogTime = Time.time;
        }
    }
}