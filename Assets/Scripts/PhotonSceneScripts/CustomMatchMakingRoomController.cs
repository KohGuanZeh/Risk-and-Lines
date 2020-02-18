using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Photon.Pun;
using Photon.Pun.UtilityScripts;
using Photon.Realtime;


public class CustomMatchMakingRoomController : MonoBehaviourPunCallbacks {
	[SerializeField] private int gameLevelSceneIndex; // for loading the game level

	public GameObject lobbyPanel;
	public GameObject roomPanel;

	[SerializeField] private GameObject startButton;

	[SerializeField] private Transform playersContainer; // used to display all the players in the current room

	[SerializeField] private GameObject playerListingPrefab; // Instantiate to display each player in the room

	[SerializeField] private Text roomNameDisplay; // display the name of the room

	public List<int> readyPlayers;
	bool ready;

	private void Awake() {
		int lobbyState = PlayerPrefs.GetInt("Lobby State", 0);

		if (lobbyState == 2) {
			lobbyPanel.SetActive(false);
			roomPanel.SetActive(true);

			roomNameDisplay.text = PhotonNetwork.CurrentRoom.Name; // update the room name display

			ListPlayers(); // relist all current player listings
		}
	}

	void ClearPlayerListings() {
		for (int i = playersContainer.childCount - 1; i >= 0; i--) // loops through all the child object of the 
		{
			Destroy(playersContainer.GetChild(i).gameObject);
		}
	}
	void ListPlayers() {
		Player[] players = PhotonNetwork.PlayerList;
		for (int i = 0; i < players.Length; i++) {
			GameObject tempListing = Instantiate(playerListingPrefab, playersContainer);
			tempListing.GetComponent<RoomButton>().currentPlayer = players[i];

			// to ensure that only the master client can kick players
			if (players[i].IsMasterClient) tempListing.GetComponent<RoomButton>().kickButton.SetActive(false);
			if (!PhotonNetwork.IsMasterClient) tempListing.GetComponent<RoomButton>().kickButton.SetActive(false);
			Text tempText = tempListing.transform.GetChild(0).GetComponent<Text>();
			tempText.text = players[i].NickName;
			players[i].SetPlayerNumber(i + 1);
		}
	}
	public override void OnJoinedRoom() {
		roomPanel.SetActive(true); // activate the display for being in a room
		lobbyPanel.SetActive(false); // hide the display for being in a lobby
		roomNameDisplay.text = PhotonNetwork.CurrentRoom.Name; // update the room name display

		if (PhotonNetwork.IsMasterClient) startButton.GetComponentInChildren<Text>().text = "Start";
		else startButton.GetComponentInChildren<Text>().text = "Ready";

		ClearPlayerListings(); // remove all old player listings
		ListPlayers(); // relist all current player listings
	}
	public override void OnPlayerEnteredRoom(Player newPlayer) {
		ClearPlayerListings();
		ListPlayers();
	}
	public override void OnPlayerLeftRoom(Player otherPlayer) {
		ClearPlayerListings();
		ListPlayers();
		if (PhotonNetwork.IsMasterClient) // if the local player is now the new mater client then we activate the start button
		{
			startButton.SetActive(true);
		}

		// to remove a ready player that has left the room
		for (int i = 0; i < readyPlayers.Count; i++) {
			print(readyPlayers[i]);
			if (readyPlayers[i] == otherPlayer.GetPlayerNumber()) {
				readyPlayers.RemoveAt(i);
			}
		}

		if (PhotonNetwork.IsMasterClient) startButton.GetComponentInChildren<Text>().text = "Start";
		else startButton.GetComponentInChildren<Text>().text = "Ready";
	}
	public void StartGame() {
		if (PhotonNetwork.IsMasterClient && readyPlayers.Count >= PhotonNetwork.PlayerList.Length - 1) {
			PhotonNetwork.CurrentRoom.IsOpen = false; // comment out if you wan the player to join after the game has started
			PhotonNetwork.LoadLevel(gameLevelSceneIndex);
		} else {
			if (!PhotonNetwork.IsMasterClient) Ready();
		}
	}
	IEnumerator rejoinLobby() {
		yield return new WaitForSeconds(1);
		PhotonNetwork.JoinLobby();
	}
	public void BackOnClick() // to avoid the issue of the lobby not updating the rooms
	{
		if (PhotonNetwork.IsMasterClient) {
			photonView.RPC("UnReady", RpcTarget.All);
		}
		ready = false;
		readyPlayers.Clear();
		lobbyPanel.SetActive(true);
		roomPanel.SetActive(false);
		PhotonNetwork.LeaveRoom();
		PhotonNetwork.LeaveLobby();
		StartCoroutine(rejoinLobby());
	}
	void Ready() {
		if (!ready) {
			photonView.RPC("SetReady", RpcTarget.All, PhotonNetwork.LocalPlayer.GetPlayerNumber());
			ready = true;
		}
	}
	[PunRPC]
	void UnReady() {
		// unready everyone when the master leaves
		ready = false;
		readyPlayers.Clear();
	}
	[PunRPC]
	void SetReady(int playerNum) {
		readyPlayers.Add(playerNum);
	}
}


