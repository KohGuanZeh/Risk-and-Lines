using UnityEngine;
using UnityEngine.UI;
using TMPro;

using Photon.Pun;
using Photon.Pun.UtilityScripts;
using Photon.Realtime;

public class PlayerList : Listable
{
    [Header("Object Components")]
    [SerializeField] TextMeshProUGUI playerNameTxt;
    public GameObject kickButton;
    public Image readyIcon;

    [Header("Player List Info")]
    public string playerName;
    public int playerId;

    public void SetPlayerInfo(string name, int id)
    {
        playerNameTxt.text = playerName = name;
        playerId = id;

        UpdateKickButtonDisplay();
    }

    public void UpdatePlayerColor(int playerNo)
    {
        playerNameTxt.color = GameManager.GetCharacterColor(playerNo);
    }

    public void UpdatePlayerColor()
    {
        int playerNo = PhotonNetwork.CurrentRoom.GetPlayer(playerId).GetPlayerNumber();
        playerNameTxt.color = GameManager.GetCharacterColor(playerNo);
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
        readyIcon.gameObject.SetActive(false);
        if (PhotonNetwork.IsMasterClient)
        {
            Player player = PhotonNetwork.CurrentRoom.GetPlayer(playerId);

            string msg = string.Format("{0} has been Kicked form the Room.", player.NickName);
            ChatManager.inst.photonView.RPC("SendAutomatedMsg", RpcTarget.AllBuffered, msg, player.GetPlayerNumber());
            PhotonNetwork.CloseConnection(player);
        } 
    }

    public void SetMasterClientIcon(bool isMaster)
    {
        readyIcon.sprite = Matchmake.inst.masterAndClientIcon[isMaster ? 0 : 1];
    }

    public void SetReadyUnreadyIcon(bool isReady)
    {
        readyIcon.gameObject.SetActive(isReady);
    }

    public override void OnListRemove()
    {
        Matchmake.inst.UpdateListingPosition(1);
        Destroy(gameObject);
    }
}
