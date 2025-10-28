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

    // Map WS uid -> CharacterController (ensures one GameObject per ws player)
    private Dictionary<int, CharacterController> playerControllersByUid = new Dictionary<int, CharacterController>();

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
        var currentUids = new HashSet<int>();

        // Get local player's uid (if available)
        int localUid = -1;
        if (WS_Client.Instance != null && WS_Client.Instance.pulic_UserInfo != null)
        {
            localUid = WS_Client.Instance.pulic_UserInfo.uid;
        }

        // Create missing players and update positions for existing ones
        foreach (var player in players)
        {
            int uid = player.uid;
            currentUids.Add(uid);

            Vector3 otherPlayerPos = Vector3.zero;
            if (player.position != null && player.position.Length >= 2)
            {
                otherPlayerPos = new Vector3(player.position[0], player.position[1], 0f);
            }

            if (!playerControllersByUid.ContainsKey(uid))
            {
                // Create once; mark local player if uid matches
                CreatePlayerFromData(player, otherPlayerPos, uid == localUid);
            }
            else
            {
                Debug.Log($"FKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKK existing player uid={uid} to {otherPlayerPos}");
                // Update existing player's position
                var cc = playerControllersByUid[uid];
                if (cc != null)
                {
                   // cc.transform.localPosition = otherPlayerPos;
                }
            }
        }

        // Remove controllers for players who left
        var toRemove = new List<int>();
        foreach (var kv in playerControllersByUid)
        {
            if (!currentUids.Contains(kv.Key))
            {
                toRemove.Add(kv.Key);
            }
        }

        foreach (var uid in toRemove)
        {
            RemovePlayer(uid);
        }
    }

    private void CreatePlayerFromData(WS_Client.PlayerData player, Vector3 startPos, bool isLocal = false)
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
        characterController.gameObject.tag = "MainPlayer";
        this.characterControllers.Add(characterController);

        // set world-space start position
        characterController.transform.position = startPos;

        // mark local player for client-side control
        characterController.IsLocalPlayer = isLocal;
        if (isLocal)
        {
            Debug.Log($"Local player created for uid={uid}");
        }

        playerControllersByUid[uid] = characterController;

        // keep an incremental id for legacy naming if needed
        this.playerID = Mathf.Max(this.playerID, uid + 1);
        Debug.Log($"Created player GameObject for uid={uid} at {startPos} (isLocal={isLocal})");
    }

    private void RemovePlayer(int uid)
    {
        if (playerControllersByUid.TryGetValue(uid, out var cc))
        {
            if (cc != null)
            {
                this.characterControllers.Remove(cc);
                GameObject.Destroy(cc.gameObject);
                Debug.Log($"Removed player GameObject for uid={uid}");
            }
            playerControllersByUid.Remove(uid);
        }
    }
}
