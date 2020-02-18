using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using Photon.Pun;
using Photon.Realtime;

public class Matchmake : MonoBehaviourPunCallbacks
{
	public Matchmake inst;

	[Header("Panels")]
	[SerializeField] GameObject mainPanel;
	[SerializeField] GameObject lobbyPanel;
	[SerializeField] GameObject roomPanel;

	[Header("For Main")]
	[SerializeField] InputField nameInput;
	[SerializeField] GameObject lobbyConnectButton;

	[Header("For Lobby Panel")]
	[SerializeField] InputField rmName;
	public List<RoomInfo> rms;
	public Transform rmContainer;
	[SerializeField] RoomButton rmButtonPrefab;
	public List<RoomButton> rmButtons;

	[Header("For Room Panel")]
	[SerializeField] GameObject startButton;
	[SerializeField] Transform playersContainer; // used to display all the players in the current room
	[SerializeField] RoomButton playerListingPrefab; // Instantiate to display each player in the room
	[SerializeField] List<RoomButton> playerList;
	[SerializeField] Text roomNameDisplay; // display the name of the room

	private void Awake()
	{
		inst = this;
	}

	private void Start()
	{
		if (!PhotonNetwork.IsConnected) PhotonNetwork.ConnectUsingSettings();

		//Check for Player Prefs
		string name = PlayerPrefs.GetString("NickName", string.Empty);
		nameInput.text = name;

		rms = new List<RoomInfo>();
		rmButtons = new List<RoomButton>();
		playerList = new List<RoomButton>();

		UpdateSceneOnLobbyState();
		PhotonNetwork.AutomaticallySyncScene = true;
	}

	#region Pun Callback Functions
	public override void OnConnectedToMaster()
	{
		lobbyConnectButton.SetActive(true);
	}

	public override void OnCreateRoomFailed(short returnCode, string message)
	{
		base.OnCreateRoomFailed(returnCode, message);
	}

	public override void OnRoomListUpdate(List<RoomInfo> roomList)
	{   
		foreach (RoomInfo room in roomList)
		{
			int idx = rmButtons.FindIndex(x => x.roomName == room.Name);

			if (idx > -1)
			{
				if (!room.IsOpen || !room.IsVisible || room.PlayerCount == room.MaxPlayers)
				{
					RoomButton rmButton = rmButtons[idx];
					rmButtons.RemoveAt(idx);
					Destroy(rmButton);
				}
			}
			else if (room.IsOpen && room.IsVisible && room.PlayerCount < room.MaxPlayers) ListRoom(room);
		}
	}

	public override void OnJoinedRoom()
	{
		roomPanel.SetActive(true);
		lobbyPanel.SetActive(false);

		roomNameDisplay.text = PhotonNetwork.CurrentRoom.Name; //Display Room Name
		if (PhotonNetwork.IsMasterClient) startButton.SetActive(true);
		else startButton.SetActive(false);
	}

	public override void OnPlayerEnteredRoom(Player newPlayer)
	{
		ListPlayer(newPlayer);
	}

	public override void OnPlayerLeftRoom(Player otherPlayer)
	{
		RemovePlayerFromListing(otherPlayer.ActorNumber);
		if (PhotonNetwork.IsMasterClient) startButton.SetActive(true);
	}
	#endregion

	void UpdateSceneOnLobbyState()
	{
		switch (PlayerPrefs.GetInt("Lobby State", 0))
		{
			case 1: //Show Lobby Panel

				mainPanel.SetActive(false);
				lobbyPanel.SetActive(true);

				//Need to Update Room List?

				break;
			case 2: //Show Room Panel

				mainPanel.SetActive(false);
				lobbyPanel.SetActive(false);
				roomPanel.SetActive(true);

				roomNameDisplay.text = PhotonNetwork.CurrentRoom.Name; // update the room name display

				if (PhotonNetwork.IsMasterClient) startButton.SetActive(true);
				else startButton.SetActive(false);

				foreach (Player player in PhotonNetwork.PlayerList) ListPlayer(player);

				break;
		}
	}

	#region Join Quit Button Functions
	public void JoinNetworkLobby() //When you Click Connect Button
	{
		string name = nameInput.text;
		if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(name)) name = "Player_" + Random.Range(0, 100).ToString("000");

		PhotonNetwork.NickName = name;
		PlayerPrefs.SetString("NickName", name);

		mainPanel.SetActive(false);
		lobbyPanel.SetActive(true);

		PhotonNetwork.JoinLobby();
	}

	public void QuitNetworkLobby()
	{
		mainPanel.SetActive(true);
		lobbyPanel.SetActive(false);
		PhotonNetwork.LeaveLobby();
	}

	public void CreateRoom()
	{
		string name = rmName.text;
		if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(name)) name = "Player_" + Random.Range(0, 100).ToString("000");
		PhotonNetwork.CreateRoom(name, new RoomOptions() { IsVisible = true, IsOpen = true, MaxPlayers = (byte)4 });
		rmName.text = string.Empty;
	}

	public void LeaveRoom()
	{
		lobbyPanel.SetActive(true);
		roomPanel.SetActive(false);
	}
	#endregion

	#region Listing Functions
	void ListRoom(RoomInfo room)
	{
		RoomButton rmButton = Instantiate(rmButtonPrefab, rmContainer);
		rmButton.SetRoom(room.Name, room.MaxPlayers, room.PlayerCount);
		rmButtons.Add(rmButton);
	}

	void ListPlayer(Player player)
	{
		RoomButton playerListing = Instantiate(playerListingPrefab, playersContainer);
		playerListing.SetRoom(player.NickName, player.ActorNumber, 0);
		playerList.Add(playerListing);
	}

	void RemovePlayerFromListing(int actorId)
	{
		int idx = playerList.FindIndex(x => x.roomSize == actorId);
		if (idx < 0) return;

		RoomButton playerListing = playerList[idx];
		playerList.RemoveAt(idx);
		Destroy(playerListing);
	}
	#endregion
}
