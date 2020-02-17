using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

public class CustomMatchMakingLobbyController : MonoBehaviourPunCallbacks {

	//static instance
	public static CustomMatchMakingLobbyController instance;

	[SerializeField] private GameObject lobbyConnectButton; // button used for joining the lobby
	[SerializeField] private GameObject lobbyPanel; // planel for displaying lobby
	[SerializeField] private GameObject mainPanel; // panel for displaying the main menu

	public InputField playerNameInput; // Inpu field so player can change their NickName

	private string roomName; // string for saving a room name
	private int roomSize; // int for saving a room size

	public List<RoomInfo> roomListings;
	public Transform roomsContainer;
	[SerializeField] private GameObject roomListingPrefab;

	// to reset the room listing
	public List<RoomButton> roomPrefabs;

	private void Awake() 
	{
		if (instance != null) Destroy(this.gameObject);
		else instance = this;

		//Checking Lobby State
		int lobbyState = PlayerPrefs.GetInt("Lobby State", 0);

		if (lobbyState == 1)
		{
			mainPanel.SetActive(false);
			lobbyPanel.SetActive(true);
		}
	}

	private void Start() {
		PhotonNetwork.ConnectUsingSettings(); // connects to photon master servers
		roomPrefabs = new List<RoomButton>();
	}

	public override void OnConnectedToMaster() {
		PhotonNetwork.AutomaticallySyncScene = true;
		lobbyConnectButton.SetActive(true);
		roomListings = new List<RoomInfo>();

		// check for player name saved to player prefs
		if (PlayerPrefs.HasKey("NickName")) {
			if (PlayerPrefs.GetString("NickName") == "") {
				PhotonNetwork.NickName = "Player" + Random.Range(0, 1000); // random player name when not specified
			} else {
				PhotonNetwork.NickName = PlayerPrefs.GetString("NickName"); // get saved player name
			}
		} else {
			PhotonNetwork.NickName = "Player" + Random.Range(0, 1000);// random player name when not specified
		}
		playerNameInput.text = PhotonNetwork.NickName; // update 
	}

	public void PlayerNameUpdate(string nameInput) {
		PhotonNetwork.NickName = nameInput;
		PlayerPrefs.SetString("NickName", nameInput);
	}
	public void JoinLobbyOnClick() {
		mainPanel.SetActive(false);
		lobbyPanel.SetActive(true);
		PhotonNetwork.JoinLobby(); // first tries to join an existing room
	}
	public override void OnRoomListUpdate(List<RoomInfo> roomList) {
		int tempIndex;
		
		foreach (RoomInfo room in roomList) {
			if (roomListings != null) // try to find an existing room
			{
				tempIndex = roomListings.FindIndex(ByName(room.Name));
			} else {
				tempIndex = -1;
			}
			if (tempIndex != -1) // remove listing because it has been closed
			{
				roomListings.RemoveAt(tempIndex);
				Destroy(roomsContainer.GetChild(tempIndex).gameObject);
			}
			if (room.PlayerCount > 0)// add room listing because it is new
			{
				resetLobby();
				roomListings.Add(room);
				ListRoom(room);
			}
		}
		//roomPrefabs = FindObjectsOfType<RoomButton>(); //sets all the rooms to be in the list
	}

	static System.Predicate<RoomInfo> ByName(string name) // predicate function for seach through toom
	{
		return delegate (RoomInfo room) {
			return room.Name == name;
		};
	}
	void ListRoom(RoomInfo room) // displays new room listing for the current room
	{
		if (room.IsOpen && room.IsVisible) {
			GameObject tempListing = Instantiate(roomListingPrefab, roomsContainer);
			RoomButton tempButton = tempListing.GetComponent<RoomButton>();
			tempButton.SetRoom(room.Name, room.MaxPlayers, room.PlayerCount);
		}
	}
	public void OnRoomNameChanged(string nameIn) {
		roomName = nameIn;
	}
	public void OnRoomSizeChanged(string sizeIn) {
		roomSize = int.Parse(sizeIn);
	}
	public void CreateRoom() {
		Debug.Log("Creating room now");
		RoomOptions roomOps = new RoomOptions() { IsVisible = true, IsOpen = true, MaxPlayers = (byte)roomSize };
		PhotonNetwork.CreateRoom(roomName, roomOps);
		//resetLobby();
	}
	public override void OnCreateRoomFailed(short returnCode, string message) {
		Debug.Log("tried to create a new rooom but failed");
	}
	public void MatchMakingCancel() {
		//	resetLobby();
		mainPanel.SetActive(true);
		lobbyPanel.SetActive(false);
		PhotonNetwork.LeaveLobby();
	}
	void roomPrefabUpdate() {
		roomPrefabs.Clear();
		roomPrefabs.AddRange(FindObjectsOfType<RoomButton>());
	}
	public void resetLobby() {
		roomPrefabUpdate();
		foreach (RoomButton rm in roomPrefabs) {
			Destroy(rm.gameObject);
		}
		roomPrefabs.Clear();
	}

	private void OnApplicationQuit()
	{
		PlayerPrefs.DeleteKey("Lobby State");
	}
}
