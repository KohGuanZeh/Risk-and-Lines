using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using Photon.Pun.UtilityScripts;

public class RoomButton : MonoBehaviourPunCallbacks {
	[SerializeField] private Text nameText; // display roomName
	[SerializeField] private Text sizeText; // display roomSize

	private string roomName; // string for saving the room name
	private int roomSize; // int for saving the room size
	private int playerCount;

	[Header("KickPlayer")]
	public Player currentPlayer; // stores the player so that the player can be kicked
	public GameObject kickButton;
	[SerializeField] CustomMatchMakingRoomController roomController;
	 private void Start() {
		roomController = FindObjectOfType<CustomMatchMakingRoomController>();
	}
	public void JoinRoomOnClick() {
		PhotonNetwork.JoinRoom(roomName);
		CustomMatchMakingLobbyController.instance.resetLobby();
	}
	public void SetRoom(string nameInput, int sizeInput, int countInput) // public function called in waitingroomcontroller
	{
		roomName = nameInput;
		roomSize = sizeInput;
		playerCount = countInput;
		nameText.text = nameInput;
		sizeText.text = countInput + "/" + sizeInput;
	}
	public void KickPlayer() {
		photonView.RPC("CloseRoom",currentPlayer);
	}
	[PunRPC]
	void CloseRoom(){
		print("Kappa");
		roomController.lobbyPanel.SetActive(true);
		roomController.roomPanel.SetActive(false);
		PhotonNetwork.LeaveRoom();
		PhotonNetwork.LeaveLobby();
	}
}
