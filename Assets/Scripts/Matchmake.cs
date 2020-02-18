using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using TMPro;

using Photon.Pun;
using Photon.Realtime;
using Photon.Pun.UtilityScripts;

public class Matchmake : MonoBehaviourPunCallbacks
{
	public static Matchmake inst;

	[Header("Panels")]
	[SerializeField] GameObject mainPanel;
	[SerializeField] GameObject lobbyPanel;
	[SerializeField] GameObject roomPanel;

	[Header("For Main")]
	[SerializeField] TMP_InputField nameInput;
	[SerializeField] GameObject lobbyConnectButton;

	[Header("For Lobby Panel")]
	[SerializeField] TMP_InputField rmName;

	public Transform rmContainer;
	public List<RoomList> rmButtons;
	[SerializeField] RoomList rmButtonPrefab;

	[Header("For Room Panel")]
	[SerializeField] TextMeshProUGUI roomNameDisplay; // display the name of the room
	[SerializeField] Button startReadyButton;
	[SerializeField] TextMeshProUGUI startReadyButtonTxt;

	[SerializeField] Transform playerContainer; // used to display all the players in the current room
	[SerializeField] List<PlayerList> playerLists;
	[SerializeField] PlayerList playerListPrefab; // Instantiate to display each player in the room

	[Header("For Game Start")]
	[SerializeField] List<int> playersReady;
	[SerializeField] bool isReady;

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

		rmButtons = new List<RoomList>();
		playerLists = new List<PlayerList>();

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
		print("Room List Updated");
		foreach (RoomInfo room in roomList)
		{
			int idx = rmButtons.FindIndex(x => x.roomName == room.Name);

			if (idx > -1)
			{
				if (!room.IsOpen || !room.IsVisible || room.PlayerCount == room.MaxPlayers || room.PlayerCount < 1)
				{
					RoomList rmButton = rmButtons[idx];
					rmButtons.RemoveAt(idx);
					Destroy(rmButton.gameObject);
				}
				else rmButtons[idx].UpdatePlayerCount(room.PlayerCount);
			}
			else if (room.IsOpen && room.IsVisible && room.PlayerCount < room.MaxPlayers)
			{
				RoomList rmButton = ListRoom(room);
				rmButton.UpdatePlayerCount(room.PlayerCount);
			}
		}
	}

	public override void OnDisconnected(DisconnectCause cause)
	{
		LeaveRoom();
		PhotonNetwork.Reconnect();
	}

	public override void OnJoinedRoom()
	{
		roomPanel.SetActive(true);
		lobbyPanel.SetActive(false);

		roomNameDisplay.text = PhotonNetwork.CurrentRoom.Name; //Display Room Name

		Player[] players = PhotonNetwork.PlayerList;
		foreach (Player player in players) ListPlayer(player);
		PhotonNetwork.LocalPlayer.SetPlayerNumber(players.Length);

		SetStartReadyButton();
	}

	public override void OnLeftRoom()
	{
		lobbyPanel.SetActive(true);
		roomPanel.SetActive(false);

		foreach (PlayerList playerList in playerLists) Destroy(playerList.gameObject);
		playerLists.Clear();

		isReady = false;
		playersReady.Clear();
	}

	public override void OnPlayerEnteredRoom(Player newPlayer)
	{
		ListPlayer(newPlayer);
		if (PhotonNetwork.IsMasterClient) photonView.RPC("SendCurrentReadyList", newPlayer, playersReady.ToArray());
	}

	public override void OnPlayerLeftRoom(Player otherPlayer)
	{
		Player[] players = PhotonNetwork.PlayerList;
		for (int i = 0; i < players.Length; i++) players[i].SetPlayerNumber(i + 1);

		RemovePlayerFromListing(otherPlayer.ActorNumber);
		foreach (PlayerList playerList in playerLists) playerList.UpdateKickButtonDisplay();

		SetStartReadyButton();

		if (PhotonNetwork.IsMasterClient)
		{
			photonView.RPC("SendReadyUnready", RpcTarget.AllBuffered, false, otherPlayer.ActorNumber);
			if (playersReady.Count == PhotonNetwork.CurrentRoom.PlayerCount - 1 && playersReady.Count > 0) startReadyButton.interactable = true;
		} 
	}
	#endregion

	void UpdateSceneOnLobbyState()
	{
		switch (PlayerPrefs.GetInt("Lobby State", 0))
		{
			case 1: //Show Lobby Panel

				mainPanel.SetActive(false);
				lobbyPanel.SetActive(true);

				break;

			case 2: //Show Room Panel

				mainPanel.SetActive(false);
				lobbyPanel.SetActive(false);
				roomPanel.SetActive(true);

				roomNameDisplay.text = PhotonNetwork.CurrentRoom.Name; // update the room name display
				SetStartReadyButton();

				foreach (Player player in PhotonNetwork.PlayerList) ListPlayer(player);

				break;
		}
	}

	public void StartGameOrReady()
	{
		if (PhotonNetwork.IsMasterClient)
		{
			if (playersReady.Count != PhotonNetwork.CurrentRoom.PlayerCount - 1) return; //Master will not be registered under Ready
			PhotonNetwork.CurrentRoom.IsOpen = false;
			PhotonNetwork.LoadLevel(2);
		}
		else
		{
			isReady = !isReady;
			startReadyButtonTxt.text = isReady ? "Cancel" : "Ready";
			photonView.RPC("SendReadyUnready", RpcTarget.AllBuffered, isReady, PhotonNetwork.LocalPlayer.ActorNumber);
		}
	}

	[PunRPC]
	public void SendReadyUnready(bool ready, int playerId)
	{
		if (ready) playersReady.Add(playerId);
		else playersReady.Remove(playerId);

		if (PhotonNetwork.IsMasterClient && playersReady.Count == PhotonNetwork.CurrentRoom.PlayerCount - 1 && playersReady.Count > 0) startReadyButton.interactable = true;
	}

	[PunRPC]
	public void SendCurrentReadyList(int[] readyPlayers)
	{
		playersReady.AddRange(readyPlayers);
	}

	void SetStartReadyButton()
	{
		if (PhotonNetwork.IsMasterClient)
		{
			startReadyButton.interactable = false;
			startReadyButtonTxt.text = "Start Game";
		}
		else
		{
			startReadyButton.interactable = true;
			startReadyButtonTxt.text = "Ready";
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
		if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(name)) name = "Room_" + Random.Range(0, 100).ToString("000");
		PhotonNetwork.CreateRoom(name, new RoomOptions() { IsVisible = true, IsOpen = true, MaxPlayers = (byte)4 });
		rmName.text = string.Empty;
	}

	public void LeaveRoom()
	{
		PhotonNetwork.LeaveRoom();
		PhotonNetwork.LeaveLobby();

		//Have to Reconnect in order for Room List to Update...
		StartCoroutine(ReconnectBackToLobby());
	}

	IEnumerator ReconnectBackToLobby()
	{
		//When Leave Lobby, State Becomes Authenticating. Hence just wait till its connected to Master Server 
		yield return new WaitUntil(() => PhotonNetwork.NetworkClientState == ClientState.ConnectedToMasterServer);
		PhotonNetwork.JoinLobby();
		print("Joined Lobby");
	}
	#endregion

	#region Listing Functions
	RoomList ListRoom(RoomInfo room) //return the Room List so I can Update the Player Count in OnRoomListUpdate
	{
		RoomList rmButton = Instantiate(rmButtonPrefab, rmContainer);
		rmButton.SetRoom(room.Name, room.MaxPlayers);
		rmButtons.Add(rmButton);
		return rmButton;
	}

	void ListPlayer(Player player)
	{
		PlayerList playerList = Instantiate(playerListPrefab, playerContainer);
		playerList.SetPlayerInfo(player.NickName, player.ActorNumber);
		playerLists.Add(playerList);
	}

	void RemovePlayerFromListing(int actorId)
	{
		int idx = playerLists.FindIndex(x => x.playerId == actorId);
		if (idx < 0) return;

		PlayerList playerList = playerLists[idx];
		playerLists.RemoveAt(idx);
		Destroy(playerList.gameObject);
	}
	#endregion

	private void OnApplicationQuit()
	{
		PlayerPrefs.DeleteKey("Lobby State");
	}
}
