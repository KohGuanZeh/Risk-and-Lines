using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviourPunCallbacks
{
	[SerializeField]
	private GameObject UserNameScreen, ConnectScreen;

	[SerializeField]
	private InputField UserNameInput, CreateRoomInput, JoinRoomInput;

	[SerializeField]
	private GameObject CreateUserNameButton;

	[SerializeField] GameObject characterSelect;


	public GameObject quickPlay,quickPlayCancel;
	
	// for the lobby panel
	[SerializeField] private int roomSize;
	private string roomName;

	private List<RoomInfo> roomListings; // list of current rooms
	[SerializeField] Transform roomsContainer; // container for holding all the romm listing
	[SerializeField] GameObject roomListingPrefab; // prefav for displayer each room in the lobby

	// for the waiting room 
	public int waitSceneIndex;

	private void Start()
	{
		//Connect to server using the PhotonServerSettings
		PhotonNetwork.ConnectUsingSettings();
	}

	// Called when the client is connected to the Master Server and ready for matchmaking and other tasks.
	public override void OnConnectedToMaster()
	{
		//Debug.Log("Connected to Master Server!!!");
		//PhotonNetwork.JoinLobby(TypedLobby.Default);
		PhotonNetwork.AutomaticallySyncScene = true;
		quickPlay.SetActive(true);
		roomListings = new List<RoomInfo>(); // initializing roomListing
	}

	// this is to quick start the game 
	public void QuickStart()
	{
		quickPlay.SetActive(false);
		quickPlayCancel.SetActive(true);
		PhotonNetwork.JoinRandomRoom();
		print("quickStarted");
	}
	public void QuickCancel()
	{
		quickPlayCancel.SetActive(false);
		quickPlay.SetActive(true);
		PhotonNetwork.LeaveRoom();
	}
	public override void OnJoinRandomFailed(short returnCode, string message)
	{
		//base.OnJoinRandomFailed(returnCode, message);
		CreateRoom();
	}
	void CreateRoom()
	{
		int randomRoomNummber = Random.Range(0, 1000);//creating a random room name for the room 
		RoomOptions roomOps = new RoomOptions() { IsVisible = true, IsOpen = true, MaxPlayers = (byte)roomSize };
		PhotonNetwork.CreateRoom("Room" + randomRoomNummber, roomOps);
		print(randomRoomNummber);
	}

	public override void OnCreateRoomFailed(short returnCode, string message)
	{
		Debug.Log("Failed to create room... trying again");
		CreateRoom();
	}

	// to allow the server to send information to the client
	public override void OnEnable()
	{
		PhotonNetwork.AddCallbackTarget(this);
	}

	public override void OnDisable()
	{
		PhotonNetwork.RemoveCallbackTarget(this);
	}
	public override void OnJoinedRoom()
	{
		//base.OnJoinedRoom();
		PhotonNetwork.LoadLevel(waitSceneIndex);
	}

	private void StartGame()
	{
		if (PhotonNetwork.IsMasterClient)
		{
			Debug.Log("starting game");
			PhotonNetwork.LoadLevel(waitSceneIndex);
		}
	}

	// for joining the lobby and updating all the rooms in the lobby
	public void JoinLobbyOnClick()
	{
		PhotonNetwork.JoinLobby();
	}

	public override void OnRoomListUpdate(List<RoomInfo> roomList)
	{
		int tempIndex;
		foreach(RoomInfo room in roomList)
		{
			if (roomListings != null) // try to find an existing room
			{
				tempIndex = roomListings.FindIndex(ByName(room.Name));
			}
			else
			{
				tempIndex = -1;
			}
			if(tempIndex != -1) // remove listing because it has been closed
			{
				roomListings.RemoveAt(tempIndex);
				Destroy(roomsContainer.GetChild(tempIndex).gameObject);
			}
			if(room.PlayerCount>0)// add room listing because it is new
			{
				roomListings.Add(room);
				ListRoom(room);
			}
		}
	}

	static System.Predicate<RoomInfo> ByName(string name) // predicate function for seach through toom
	{
		return delegate (RoomInfo room)
		{
			return room.Name == name;
		};
	}
	void ListRoom(RoomInfo room) // displays new room listing for the current room
	{
		if(room.IsOpen && room.IsVisible)
		{
			GameObject tempListing = Instantiate(roomListingPrefab, roomsContainer);
			RoomButton tempButton = tempListing.GetComponent<RoomButton>();
			tempButton.SetRoom(room.Name, room.MaxPlayers, room.PlayerCount);
		}
	}
	public void OnRoomNameChanged(string nameIn)
	{
		roomName = nameIn; 
	}
	public void OnRoomSizeChanged(string sizeIn)
	{
		roomSize = int.Parse(sizeIn);
	}
	
}
