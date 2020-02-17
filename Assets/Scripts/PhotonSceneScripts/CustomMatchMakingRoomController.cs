using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Photon.Pun;
using Photon.Pun.UtilityScripts;
using Photon.Realtime;
using UnityEngine.UI;

public class CustomMatchMakingRoomController : MonoBehaviourPunCallbacks
{
	[SerializeField] private int gameLevelSceneIndex; // for loading the game level

	[SerializeField] private GameObject lobbyPanel;
	[SerializeField] private GameObject roomPanel;

	[SerializeField] private GameObject startButton;

	[SerializeField] private Transform playersContainer; // used to display all the players in the current room

	[SerializeField] private GameObject playerListingPrefab; // Instantiate to display each player in the room

	[SerializeField] private Text roomNameDisplay; // display the name of the room

	private void Start()
	{
		
	}

	void ClearPlayerListings()
	{
		for (int i = playersContainer.childCount - 1; i >= 0; i--) // loops through all the child object of the 
		{
			Destroy(playersContainer.GetChild(i).gameObject);
		}
	}
	void ListPlayers()
	{
		Player[] players = PhotonNetwork.PlayerList;
		for (int i = 0; i < players.Length; i++)
		{
			GameObject tempListing = Instantiate(playerListingPrefab, playersContainer);
			Text tempText = tempListing.transform.GetChild(0).GetComponent<Text>();
			tempText.text = players[i].NickName;
			players[i].SetPlayerNumber(i + 1);
		}
	}
	public override void OnJoinedRoom()
	{
		roomPanel.SetActive(true); // activate the display for being in a room
		lobbyPanel.SetActive(false); // hide the display for being in a lobby
		roomNameDisplay.text = PhotonNetwork.CurrentRoom.Name; // update the room name display
		if (PhotonNetwork.IsMasterClient)
		{
			startButton.SetActive(true);
		}
		else
		{
			startButton.SetActive(false);
		}
		ClearPlayerListings(); // remove all old player listings
		ListPlayers(); // relist all current player listings
	}
	public override void OnPlayerEnteredRoom(Player newPlayer)
	{
		ClearPlayerListings();
		ListPlayers();
	}
	public override void OnPlayerLeftRoom(Player otherPlayer)
	{
		ClearPlayerListings();
		ListPlayers();
		if (PhotonNetwork.IsMasterClient) // if the local player is now the new mater client then we activate the start button
		{
			startButton.SetActive(true);
		}
	}
	public void StartGame()
	{
		if (PhotonNetwork.IsMasterClient)
		{
			PhotonNetwork.CurrentRoom.IsOpen = false; // comment out if you wan the player to join after the game has started
			PhotonNetwork.LoadLevel(gameLevelSceneIndex);
		}
	}
	IEnumerator rejoinLobby()
	{
		yield return new WaitForSeconds(1);
		PhotonNetwork.JoinLobby();
	}
	public void BackOnClick() // to avoid the issue of the lobby not updating the rooms
	{
		lobbyPanel.SetActive(true);
		roomPanel.SetActive(false);
		PhotonNetwork.LeaveRoom();
		PhotonNetwork.LeaveLobby();
		StartCoroutine(rejoinLobby());
	}
}
	

