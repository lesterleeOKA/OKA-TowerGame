using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms;

public class TowerGameController : GameBaseController
{
    public static TowerGameController Instance = null;
    public GameObject playerPrefab;
    public Transform parent;
    public List<CharacterController> characterControllers = new List<CharacterController>();
    private int playerID = 0;

    // Map WS player key (string) -> CharacterController (ensures one GameObject per ws player)
    private Dictionary<string, CharacterController> playerControllersByKey = new Dictionary<string, CharacterController>();


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
        Debug.Log("Current Players in join room " + this.playerNumber);

        // Track which uids are currently present this frame
        var currentKeys = new HashSet<string>();

        // Get local player's uid (if available)
        int localUid = -1;
        if (WS_Client.Instance != null && WS_Client.Instance.pulic_UserInfo != null)
        {
            localUid = WS_Client.Instance.pulic_UserInfo.uid;
        }

        // Create missing players and update positions for existing ones
        foreach (var player in players)
        {
            string key = !string.IsNullOrEmpty(player.player_id) ? player.player_id : player.uid.ToString();
            currentKeys.Add(key);

            Vector3 otherPlayerPos = Vector3.zero;
            if (player.position != null && player.position.Length >= 2)
            {
                otherPlayerPos = new Vector3(player.position[0], player.position[1], 0f);
            }

            if (!playerControllersByKey.ContainsKey(key))
            {
                bool isLocal = (player.uid == localUid);
                CreatePlayerFromData(player, otherPlayerPos, key, isLocal);
            }
            else
            {
                var cc = playerControllersByKey[key];
                if (cc != null)
                {
                    // don't override local player's client-controlled transform
                    if (!(player.uid == localUid && cc.IsLocalPlayer))
                    {
                        //cc.transform.position = otherPlayerPos;
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
}
