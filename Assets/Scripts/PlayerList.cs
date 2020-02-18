using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class PlayerList : MonoBehaviour
{
    [Header("Object Components")]
    [SerializeField] TextMeshProUGUI playerNameTxt;
    public GameObject kickButton;

    [Header("Player List Info")]
    public string playerName;
    public int playerId;

    public void SetPlayerInfo(string name, int id)
    {
        playerNameTxt.text = playerName = name;
        playerId = id;

        UpdateKickButtonDisplay();
    }

    public void UpdateKickButtonDisplay()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            if (PhotonNetwork.MasterClient.ActorNumber != playerId) kickButton.SetActive(true);
            else kickButton.SetActive(false);
        }
        else kickButton.SetActive(false);
    }

    public void KickPlayer() //To be in Button Function
    {
        if (PhotonNetwork.IsMasterClient) PhotonNetwork.CloseConnection(PhotonNetwork.CurrentRoom.GetPlayer(playerId));
    }
}
