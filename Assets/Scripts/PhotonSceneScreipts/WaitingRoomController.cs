using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public class WaitingRoomController : MonoBehaviourPunCallbacks
{
	// this photon view for sending rpc that updates the timer
	private PhotonView myPhotonView;

	//scene navigation indexes
	[SerializeField] private int gameLevelIndex;
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

	public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
	{
		PlayerCountUpdate();

		if (PhotonNetwork.IsMasterClient) myPhotonView.RPC("RPC_SendTimer", RpcTarget.Others, timerToStartGame);
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
}
