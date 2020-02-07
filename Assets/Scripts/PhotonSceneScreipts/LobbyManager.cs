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


	// for quick play
	public GameObject quickPlay,quickPlayCancel;
	[SerializeField] private int roomSize;

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
}
