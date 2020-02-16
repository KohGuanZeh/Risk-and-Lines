using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class RoomButton : MonoBehaviour
{
	[SerializeField] private Text nameText; // display roomName
	[SerializeField] private Text sizeText; // display roomSize

	private string roomName; // string for saving the room name
	private int roomSize; // int for saving the room size
	private int playerCount;

	public void JoinRoomOnClick()
	{
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
}
