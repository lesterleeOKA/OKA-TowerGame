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
    }

    void FixedUpdate()
    {
        if (WS_Client.Instance.RoomList != null)
        {
            room = WS_Client.Instance.RoomList.Find(r => r.roomId == roomId.ToString());
            if (room != null)
            {
                playerNoText.text = room.roomMembers.ToString() + "/6";
            }
        }
    }

    public void JoinRoom()
    {
        if (room != null)
        {
            WS_Client.Instance.JoinRoom(roomId);
            LoaderConfig.Instance?.changeScene(2);
        }
    }
}
