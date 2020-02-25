using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;

using Photon.Pun;
using Photon.Realtime;
using Photon.Pun.UtilityScripts;

public class Matchmake : MonoBehaviourPunCallbacks
{
	public static Matchmake inst;

	[Header("Panels")]
	[SerializeField] Animator anim;

	[Header("For Lobby Panel")]
	[SerializeField] TextMeshProUGUI connectedAs;
	[SerializeField] TMP_InputField newNickname;
	[SerializeField] TMP_InputField rmName;

	public string roomToJoin;
	public RectTransform rmContainer;
	public List<RoomList> rmButtons;
	[SerializeField] RoomList rmButtonPrefab;

	[Header("Options")]
	[SerializeField] AudioMixer mixer;
	[SerializeField] Slider[] sliders;

	[Header("For Room Panel")]
	[SerializeField] TextMeshProUGUI roomNameDisplay; // display the name of the room
	[SerializeField] ToggleGroup toggleGroup;
	[SerializeField] Toggle[] charSelectToggles; //For Character Select
	[SerializeField] Button startReadyButton;
	[SerializeField] TextMeshProUGUI startReadyButtonTxt;

	[SerializeField] RectTransform playerContainer; // used to display all the players in the current room
	[SerializeField] List<PlayerList> playerLists;
	[SerializeField] PlayerList playerListPrefab; // Instantiate to display each player in the room

	public Sprite[] masterAndClientIcon; //Set Sprites for Respective Icons to Identify Master and Ready Players

	[Header("For Game Start")]
	[SerializeField] List<int> playersReady;
	[SerializeField] bool isReady;

	private void Awake()
	{
		inst = this;
	}

	private void Start()
	{
		sliders[0].value = PlayerPrefs.GetFloat("Music Volume", 1);
		sliders[1].value = PlayerPrefs.GetFloat("Sound Volume", 1);

		rmButtons = new List<RoomList>();
		playerLists = new List<PlayerList>();

		connectedAs.text = string.Format("Connected as: [{0}]", PhotonNetwork.NickName);
		charSelectToggles[PlayerPrefs.GetInt("Preset", 0)].isOn = true; //Set According to Previous Selected Preset (If Any)
		UpdateSceneOnLobbyState();
	}

	#region Pun Callback Functions
	public override void OnConnectedToMaster()
	{
		PhotonNetwork.JoinLobby();
	}

	public override void OnCreateRoomFailed(short returnCode, string message)
	{
		base.OnCreateRoomFailed(returnCode, message);
	}

	public override void OnCreatedRoom()
	{
		PhotonNetwork.CurrentRoom.IsOpen = true;
	}

	public override void OnRoomListUpdate(List<RoomInfo> roomList)
	{
		bool sortList = false;

		foreach (RoomInfo room in roomList)
		{
			int idx = rmButtons.FindIndex(x => x.roomName == room.Name);

			if (idx > -1)
			{
				if (!room.IsOpen || !room.IsVisible || room.PlayerCount == room.MaxPlayers || room.PlayerCount < 1)
				{
					RoomList rmButton = rmButtons[idx];
					rmButtons.RemoveAt(idx);
					rmButton.anim.SetBool("Clear", true);
				}
				else rmButtons[idx].UpdatePlayerCount(room.PlayerCount);
			}
			else if (room.IsOpen && room.IsVisible && room.PlayerCount < room.MaxPlayers)
			{
				sortList = true;
				RoomList rmButton = ListRoom(room);
				rmButton.UpdatePlayerCount(room.PlayerCount);
			}
		}

		if (sortList) UpdateListingPosition(0);
	}

	public override void OnDisconnected(DisconnectCause cause)
	{
		if (PhotonNetwork.CurrentRoom != null) LeaveRoom();
		PhotonNetwork.Reconnect();
	}

	public override void OnJoinedRoom()
	{
		anim.SetInteger("Lobby State", 1);

		roomNameDisplay.text = PhotonNetwork.CurrentRoom.Name; //Display Room Name

		Player[] players = PhotonNetwork.PlayerList;
		PhotonNetwork.LocalPlayer.SetPlayerNumber(players.Length);
		foreach (Player player in players) ListPlayer(player);

		string msg = string.Format("{0} has joined the Room.", PhotonNetwork.LocalPlayer.NickName);
		ChatManager.inst.photonView.RPC("SendAutomatedMsg", RpcTarget.AllBuffered, msg, PhotonNetwork.LocalPlayer.GetPlayerNumber());

		SetStartReadyButton();
	}

	public override void OnJoinRandomFailed(short returnCode, string message)
	{
		CreateRoom();
	}

	public override void OnLeftRoom()
	{
		anim.SetInteger("Lobby State", 0);
		ChatManager.inst.ClearChat();

		foreach (PlayerList playerList in playerLists) Destroy(playerList.gameObject);
		playerLists.Clear();

		isReady = false;
		playersReady.Clear();
		PhotonNetwork.AutomaticallySyncScene = false;

		Debug.LogError("Left Room Correctly");
	}

	public override void OnPlayerEnteredRoom(Player newPlayer)
	{
		ListPlayer(newPlayer, true);
		if (PhotonNetwork.IsMasterClient)
		{
			photonView.RPC("SendCurrentReadyList", newPlayer, playersReady.ToArray());
			startReadyButton.interactable = false;
		}
	}

	public override void OnPlayerLeftRoom(Player otherPlayer)
	{
		Player[] players = PhotonNetwork.PlayerList;
		for (int i = 0; i < players.Length; i++) players[i].SetPlayerNumber(i + 1);

		RemovePlayerFromListing(otherPlayer.ActorNumber);
		foreach (PlayerList playerList in playerLists)
		{
			playerList.UpdateKickButtonDisplay();
			playerList.UpdatePlayerColor();
			//playerList.UpdatePlayerColor(PhotonNetwork.CurrentRoom.GetPlayer(playerList.playerId).GetPlayerNumber());
		}

		SetStartReadyButton();

		if (PhotonNetwork.IsMasterClient)
		{
			string msg = string.Format("{0} has left the Room.", otherPlayer.NickName);
			ChatManager.inst.photonView.RPC("SendAutomatedMsg", RpcTarget.AllBuffered, msg, otherPlayer.GetPlayerNumber());

			photonView.RPC("SendReadyUnready", RpcTarget.AllBuffered, false, otherPlayer.ActorNumber);
		} 
	}
	#endregion

	void UpdateSceneOnLobbyState()
	{
		switch (PlayerPrefs.GetInt("Lobby State", 0))
		{
			case 1: //Show Lobby Panel

				PhotonNetwork.LeaveRoom();
				PhotonNetwork.LeaveLobby();

				StartCoroutine(ReconnectBackToLobby());

				break;

			case 2: //Show Room Panel

				anim.SetInteger("Lobby State", 1);

				roomNameDisplay.text = PhotonNetwork.CurrentRoom.Name; // update the room name display
				SetStartReadyButton();

				foreach (Player player in PhotonNetwork.PlayerList) ListPlayer(player);

				break;

			default:

				if (!PhotonNetwork.IsConnected) PhotonNetwork.ConnectUsingSettings();
				else PhotonNetwork.JoinLobby();

				break;
		}
	}

	public void StartGameOrReady()
	{
		if (PhotonNetwork.IsMasterClient)
		{
			if (playersReady.Count != PhotonNetwork.CurrentRoom.PlayerCount - 1) return; //Master will not be registered under Ready
			PhotonNetwork.CurrentRoom.IsOpen = false;
			photonView.RPC("LoadSceneForAll", RpcTarget.All, 2); //LoadingScreen.inst.LoadScene(2, false);
			//PhotonNetwork.LoadLevel(2);
		}
		else
		{
			isReady = !isReady;
			startReadyButtonTxt.text = isReady ? "Cancel" : "Ready";
			photonView.RPC("SendReadyUnready", RpcTarget.AllBuffered, isReady, PhotonNetwork.LocalPlayer.ActorNumber);

			string msg = string.Format(isReady ? "{0} is ready." : "{0} needs more time to prepare.", PhotonNetwork.LocalPlayer.NickName);
			ChatManager.inst.photonView.RPC("SendAutomatedMsg", RpcTarget.AllBuffered, msg, PhotonNetwork.LocalPlayer.GetPlayerNumber());
		}
	}

	[PunRPC]
	public void SendReadyUnready(bool ready, int playerId)
	{
		if (ready) playersReady.Add(playerId);
		else playersReady.Remove(playerId);

		PlayerList playerList = playerLists.Find(x => x.playerId == playerId); 
		if (playerList) playerList.SetReadyUnreadyIcon(ready);

		if (playersReady.Count == PhotonNetwork.CurrentRoom.PlayerCount - 1 && playersReady.Count > 0)
		{
			if (PhotonNetwork.IsMasterClient) startReadyButton.interactable = true;
			PhotonNetwork.AutomaticallySyncScene = true;
		}
		else
		{
			if (PhotonNetwork.IsMasterClient) startReadyButton.interactable = false;
			PhotonNetwork.AutomaticallySyncScene = false;
		}
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

	[PunRPC]
	void LoadSceneForAll(int scene)
	{
		LoadingScreen.inst.LoadScene(scene, false);
	}

	#region Customisation Settings
	public void ChangeNickname() //When you Click Connect Button
	{
		string name = newNickname.text;
		if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(name)) return;

		PhotonNetwork.NickName = name;
		PlayerPrefs.SetString("NickName", name);
		connectedAs.text = string.Format("Connected as: [{0}]", name);
	}

	public void ChangeCharacter(int idx) //When Click on Any Toggle on Char Select
	{
		PlayerPrefs.SetInt("Preset", idx);
	}

	public void Options(bool show)
	{
		anim.SetBool("Options", show);
	}
	#endregion

	#region Join Quit Button Functions
	public void QuitNetworkLobby()
	{
		PhotonNetwork.LeaveLobby();
		LoadingScreen.inst.LoadScene(0);
	}

	public void CreateRoom()
	{
		string name = rmName.text;
		if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(name)) name = "Room_" + Random.Range(0, 100).ToString("000");
		PhotonNetwork.JoinOrCreateRoom(name, new RoomOptions() { IsVisible = true, IsOpen = false, MaxPlayers = (byte)4 }, TypedLobby.Default);
		rmName.text = string.Empty;
	}

	public void LeaveRoom()
	{
		PhotonNetwork.LeaveRoom();
		PhotonNetwork.LeaveLobby();

		//Have to Reconnect in order for Room List to Update...
		StartCoroutine(ReconnectBackToLobby());
	}

	public void QuickStart()
	{
		PhotonNetwork.JoinRandomRoom();
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

	void ListPlayer(Player player, bool newPlayer = false)
	{
		PlayerList playerList = Instantiate(playerListPrefab, playerContainer);
		playerList.SetPlayerInfo(player.NickName, player.ActorNumber);

		if (newPlayer) playerList.UpdatePlayerColor(playerLists.Count + 1); //Player Number is Array Index + 1. Hence use Count.
		else playerList.UpdatePlayerColor();

		playerList.SetMasterClientIcon(player.IsMasterClient);
		playerList.SetReadyUnreadyIcon(player.IsMasterClient); //If it is Master Client, always Show Icon
		playerLists.Add(playerList);

		UpdateListingPosition(1);
	}

	void RemovePlayerFromListing(int actorId)
	{
		int idx = playerLists.FindIndex(x => x.playerId == actorId);
		if (idx < 0) return;

		PlayerList playerList = playerLists[idx];
		playerLists.RemoveAt(idx);
		playerList.SetReadyUnreadyIcon(false);
		playerList.anim.SetBool("Clear", true);
	}

	public void UpdateListingPosition(int whichList)
	{
		switch (whichList)
		{
			case 0:
				for (int i = 0; i < rmButtons.Count; i++)
				{
					Vector2 anchoredPos = new Vector2(rmButtons[i].rect.anchoredPosition.x, -110 * i);
					rmButtons[i].rect.anchoredPosition = anchoredPos;
				}

				rmContainer.sizeDelta = new Vector2(rmContainer.sizeDelta.x, 110 * rmButtons.Count);

				break;
			case 1:
				for (int i = 0; i < playerLists.Count; i++)
				{
					Vector2 anchoredPos = new Vector2(playerLists[i].rect.anchoredPosition.x, -75 * i);
					playerLists[i].rect.anchoredPosition = anchoredPos;
				}

				playerContainer.sizeDelta = new Vector2(playerContainer.sizeDelta.x, 75 * playerLists.Count);

				break;
		}
	}
	#endregion

	#region Options
	public void SetMusicVolume(float val)
	{
		mixer.SetFloat("Music Volume", Mathf.Log10(val) * 20);
		PlayerPrefs.SetFloat("Music Volume", val);
	}

	public void SetSoundVolume(float val)
	{
		mixer.SetFloat("Sound Volume", Mathf.Log10(val) * 20);
		PlayerPrefs.SetFloat("Sound Volume", val);
	}
	#endregion

	private void OnApplicationQuit()
	{
		PlayerPrefs.DeleteKey("Lobby State");
	}
}
