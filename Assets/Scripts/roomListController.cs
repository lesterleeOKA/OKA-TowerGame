using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using NativeWebSocket;

public class roomListController : MonoBehaviour
{
    public TextMeshProUGUI roomNoText;
    public TextMeshProUGUI playerNoText;
    public GameObject indicator;
    public Button joinButton;
    public int roomId;

    private WS_Client.RoomInfo room;

    void Start()
    {
        joinButton.onClick.AddListener(JoinRoom);
        roomNoText.text = "Room " + roomId.ToString();
        if (this.indicator == null) this.indicator.SetActive(true);
        roomListRefresh();
    }

    void Update()
    {
        roomListRefresh();
        if (this.indicator == null) return;
        if (WS_Client.Instance.websocket == null || WS_Client.Instance.websocket.State != WebSocketState.Open) {
            playerNoText.text = "<color=#FF000000>0</color>/6";
            joinButton.interactable = false;
            return;
        }
        else
        {
            if (WS_Client.Instance.RoomList != null)
            {
                if (room == null)
                {
                    playerNoText.text = "<color=#FF000000>0</color>/6";
                    joinButton.interactable = false;
                }
                else
                {
                    playerNoText.text = room.roomMembers.ToString() + "/6";
                    if (room.roomMembers >= 6 || room.roomStatus == "playing")
                    {
                        joinButton.interactable = false;
                    }
                    else
                    {
                        joinButton.interactable = true;
                    }
                    joinButton.interactable = true;
                    this.indicator.SetActive(false);
                }
            }
        }
    }

    private void roomListRefresh()
    {
        if (WS_Client.Instance != null && WS_Client.Instance.RoomList != null)
        {
            room = WS_Client.Instance.RoomList.Find(r => r.roomId == "room" + roomId.ToString());
            if (room != null)
            {
                playerNoText.text = room.roomMembers.ToString() + "/6";
                return;
            }
        }
        playerNoText.text = "<color=#FF000000>0</color>/6";
        StartCoroutine(RetryFindRoom());
    }

    private IEnumerator RetryFindRoom()
    {
        yield return new WaitForSeconds(0.1f);
        roomListRefresh();
    }

    public void JoinRoom()
    {
        if (room != null)
        {
            WS_Client.Instance.JoinGameRoom(roomId);
            LoaderConfig.Instance?.changeScene(2);
        }
    }
}
