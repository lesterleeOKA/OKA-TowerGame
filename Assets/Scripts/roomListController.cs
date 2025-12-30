using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class roomListController : MonoBehaviour
{
    public TextMeshProUGUI roomNoText;
    public TextMeshProUGUI playerNoText;
    public Button joinButton;
    public int roomId;

    private WS_Client.RoomInfo room;

    void Start()
    {
        joinButton.onClick.AddListener(JoinRoom);
        roomNoText.text = "Room " + roomId.ToString();
        roomListRefresh();
    }

    void FixedUpdate()
    {
        roomListRefresh();
        if (WS_Client.Instance.RoomList != null)
        {
            
            if (room != null)
            {
                playerNoText.text = room.roomMembers.ToString() + "/6";
                if (room.roomMembers >= 6 || room.roomStatus == "playing") {
                    joinButton.interactable = false;
                } else {
                    joinButton.interactable = true;
                }
            } else {
                playerNoText.text = "-/6";
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
        playerNoText.text = "-/6";
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
