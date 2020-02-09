using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public class WaitingRoomController : MonoBehaviourPunCallbacks
{
	// this photon view for sending rpc that updates the timer
	private PhotonView myPhotonView;

	//scene navigation indexes
	[SerializeField] private int gameLevelIndex; // to load the gameLevel 
	[SerializeField] private int menuSceneIndex;

	// number of players in the room out of the total room size
	private int playerCount;
	private int roomSize;

	[SerializeField] private int minPlayersToStart;

	// text variavles for holding the displats for the countdown options
	[SerializeField] private Text playerCountDisplay;
	[SerializeField] private Text timerToStartDisplay;

	// bool values for if the timer can count down
	private bool readyToCountDown;
	private bool readyToStart;
	private bool startingGame;

	// Countdown timer variables
	private float timerToStartGame;
	private float notFullGameTimer;
	private float fullGameTimer;

	// to show the players in the room
	[SerializeField] private Transform playersContainer; // used to display all the players in the room
	[SerializeField] private GameObject playersListingPrefab;// instantiate to display each player in the room

	[SerializeField] private Text roomNameDisplay; // display the name of the room


	// countdown timer reset vatiables
	[SerializeField] private float maxWaitTime, maxFullGameWaitTime;

	private void Start()
	{
		myPhotonView = GetComponent<PhotonView>();
		fullGameTimer = maxFullGameWaitTime;
		notFullGameTimer = maxWaitTime;
		timerToStartGame = maxWaitTime;

		PlayerCountUpdate();
	}

	void PlayerCountUpdate()
	{
		playerCount = PhotonNetwork.PlayerList.Length;
		roomSize = PhotonNetwork.CurrentRoom.MaxPlayers;
		playerCountDisplay.text = playerCount + ":" + roomSize;

		if (playerCount == roomSize)
		{
			readyToStart = true;
		}
		else if(playerCount >= minPlayersToStart)
		{
			readyToCountDown = true;
		}
		else
		{
			readyToStart = false;
			readyToCountDown = false;
		}
	}

	[PunRPC]
	private void RPC_SendTimer(float timeIn)
	{
		// rpc for syncing the countdown timer to thouse that join after it has started the countdown
		timerToStartGame = timeIn;
		notFullGameTimer = timeIn;
		if(timeIn < fullGameTimer)
		{
			fullGameTimer = timeIn;
		}
	}

	public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
	{
		PlayerCountUpdate();
		ClearPlayerListings();
		ListPlayers();
	}
	private void Update()
	{
		WaitingForMorePlayers();
	}

	void WaitingForMorePlayers()
	{
		if (playerCount <= 1)
		{
			ResetTimer();
		}
		if (readyToStart)
		{
			fullGameTimer -= Time.deltaTime;
			timerToStartGame = fullGameTimer;
		}
		else if (readyToCountDown)
		{
			notFullGameTimer -= Time.deltaTime;
			timerToStartGame = notFullGameTimer;
		}

		string tempTimer = string.Format("{0:00}", timerToStartGame);
		timerToStartDisplay.text = tempTimer;

		if(timerToStartGame <= 0f)
		{
			if (startingGame) return;
			StartGame();
		}
	}

	void ResetTimer()
	{
		//resets the countdown timer
		timerToStartGame = maxWaitTime;
		notFullGameTimer = maxWaitTime;
		fullGameTimer = maxFullGameWaitTime;
	}
	void StartGame()
	{
		startingGame = true;
		if (!PhotonNetwork.IsMasterClient) return;

		PhotonNetwork.CurrentRoom.IsOpen = false; // closes the room so that no more players can join
		PhotonNetwork.LoadLevel(gameLevelIndex);
	}

	public void Cancel()
	{
		PhotonNetwork.LeaveRoom();
		SceneManager.LoadScene(menuSceneIndex);
	}

	void ClearPlayerListings()
	{
		for(int i = playersContainer.childCount - 1; i >= 0; i--) // loops through all the child object of the 
		{
			Destroy(playersContainer.GetChild(i).gameObject);
		}
	}
	void ListPlayers()
	{
		foreach(Player player in PhotonNetwork.PlayerList)
		{
			GameObject tempListing = Instantiate(playersListingPrefab, playersContainer);
			Text tempText = tempListing.transform.GetChild(0).GetComponent<Text>();
			tempText.text = player.NickName;
		}
	}

	public override void OnJoinedRoom() // called when the local player joins the room
	{
		ClearPlayerListings();
		ListPlayers();
	}
	public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer) // called whenever a new player enters the room
	{
		PlayerCountUpdate();
		ClearPlayerListings();
		ListPlayers();
		if (PhotonNetwork.IsMasterClient) myPhotonView.RPC("RPC_SendTimer", RpcTarget.Others, timerToStartGame);
	}
	IEnumerator rejoinLobby()
	{
		yield return new WaitForSeconds(1);
		PhotonNetwork.JoinLobby();
	}
	public void BackOnClick() // to avoid the issue of the lobby not updating the rooms
	{
		PhotonNetwork.LeaveRoom();
		PhotonNetwork.LeaveLobby();
		StartCoroutine(rejoinLobby());
	}
}
