using UnityEngine;
using Photon.Pun;
using TMPro;

public class RoomList : MonoBehaviour
{
    [Header("Object Components")]
    [SerializeField] TextMeshProUGUI roomNameTxt;
    [SerializeField] TextMeshProUGUI roomSizeTxt;

    [Header("Room List Info")]
    public string roomName;
    public int roomSize;
    public int playerCount = 0;

    public void JoinRoomOnClick()
    {
        PhotonNetwork.JoinRoom(roomName);
    }

    public void SetRoom(string name, int rmSize) // public function called in waitingroomcontroller
    {
        roomNameTxt.text = roomName = name;
        roomSize = rmSize;
        roomSizeTxt.text = string.Format("0/{0}", rmSize);
    }

    public void UpdatePlayerCount(int count)
    {
        playerCount = count;
        roomSizeTxt.text = string.Format("{0}/{1}", playerCount, roomSize);
    }
}
