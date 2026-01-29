using System.Text;
using TMPro;
using UnityEngine;

public class reconnectRoom : MonoBehaviour
{
    public CanvasGroup reconnectRoomUI;
    public TextMeshProUGUI reconnectRoomName;
    public int reconnectRoomId;

    void Update() {
        // Check if there's a pending reconnect room ID from WS_Client
        if (WS_Client.Instance != null && !string.IsNullOrEmpty(WS_Client.Instance.pendingReconnectRoomId))
        {
            this.showReconnectRoomUI(WS_Client.Instance.pendingReconnectRoomId);
            WS_Client.Instance.pendingReconnectRoomId = ""; // Clear after using
        }
    }

    public void showReconnectRoomUI(string roomId) {
        SetUI.Set(this.reconnectRoomUI, true);
        reconnectRoomId = int.Parse(roomId.Replace("room", ""));
        if(this.reconnectRoomName != null) {
            StringBuilder roomName =  new StringBuilder();
            roomName.Append("Re-Join ");
            roomName.Append(roomId);
            this.reconnectRoomName.text = roomName.ToString();
        }
    }

    public void ReconnectRoom()
    {
        WS_Client.Instance.JoinGameRoom(reconnectRoomId);
        SetUI.Set(this.reconnectRoomUI, false);
        reconnectRoomId = 0;
        LoaderConfig.Instance?.changeScene(2);
    }
}
