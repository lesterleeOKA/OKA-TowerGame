using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class reconnectRoom : MonoBehaviour
{
    public GameObject reconnectRoomUI;
    public int reconnectRoomId;

    void Update() {
        // Check if there's a pending reconnect room ID from WS_Client
        if (WS_Client.Instance != null && !string.IsNullOrEmpty(WS_Client.Instance.pendingReconnectRoomId))
        {
            showReconnectRoomUI(WS_Client.Instance.pendingReconnectRoomId);
            WS_Client.Instance.pendingReconnectRoomId = ""; // Clear after using
        }
    }

    public void showReconnectRoomUI(string roomId) {
        reconnectRoomUI.SetActive(true);
        reconnectRoomId = int.Parse(roomId.Replace("room", ""));
    }

    public void ReconnectRoom()
    {
        WS_Client.Instance.JoinGameRoom(reconnectRoomId);
        reconnectRoomUI.SetActive(false);
        reconnectRoomId = 0;
        LoaderConfig.Instance?.changeScene(2);
    }
}
