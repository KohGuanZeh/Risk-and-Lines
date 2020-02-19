using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Photon.Pun.UtilityScripts;

[System.Serializable]
public struct PlayerInfo
{
	public int actorId;
	public string playerName;
	public float deathTime;

	public PlayerInfo(int id, string name, float time)
	{
		actorId = id;
		playerName = name;
		deathTime = time;
	}
}

public class GameManager : MonoBehaviourPunCallbacks
{
	[Header("General Variables")]
	public static GameManager inst;
	[SerializeField] UIManager gui;
   
	[Header("Camera Items")]
	public Camera cam;
	public Vector3 moveDelta; //Stores Move Delta of the Camera;
	public Vector3 camPos; //Store Separately as this is the Reference Value that will be submitted to Server.
	public float camSpeed = 0, defaultCamSpeed = 5.0f; //Default is used to Set Values
	public float CamLeftBounds { get { return transform.position.x - cam.orthographicSize * cam.aspect; } }

	[Header("For Spawning Dots")]
	[Range(0,1)] public float minSpawnIntervalCoeff; //> Coeff, > Interval Dist
	[Range(0,1)] public float maxSpawnIntervalCoeff; //> Coeff, > Interval Dist
	[Range(0,1)] public float minDotSpawnCoeff; //Coefficient that affects Dot Spawning. > Coeff, < Dots Spawn
	[Range(0,1)] public float maxDotSpawnCoeff; //Coefficient that affects Dot Spawning. > Coeff, < Dots Spawn
	//public Vector2Int minMaxDotSpawn;

	public float yMargin; //Offset so that the Dot do not spawn at exactly the top of bottom of the Screen
	public Vector2 minMaxY; //Minimum and Maximum Y that Dot will Spawn

	public float xInterval; //How much X Dist Camera needs to cover before Spawning the next few dots
	public float xRemainder; //How much X Dist it exceeded from its X Interval
	public float lastXSpawn; //Last X Position that Spawned the Dots
	public Vector2 minMaxXInterval, minMaxXOffset; //Min and Max X Interval and Offset //No Longer Used

	[Header("For Player Spawn")]
	public Transform playerSpawnPos;

	[Header("For Game Difficulty")]
	[SerializeField] int difficultyStage;
	[SerializeField] float timeStamp;
	[SerializeField] bool gameStarted;

	[Header("Test Different Game Difficulty")]
	[SerializeField] bool increaseAllDiff;
	[SerializeField] bool cohesive;

	[Header("For Game End")]
	public bool gameEnded;
	public int playersAlive;
	public PlayerInfo[] playerInfos;

	private void Awake()
	{
		inst = this;
		cam = GetComponent<Camera>();
		camPos = cam.transform.position;

		//Only Master Client will handle Camera Movement Changes and Dot Spawning so only Master will need to Initialise this Value and Pass it to the rest
		if (PhotonNetwork.IsMasterClient)
		{
			InitialiseValues();
			GetPlayerInfoAtStart();
		} 
	}

	void Start()
	{
		playersAlive = PhotonNetwork.PlayerList.Length;
		gui = UIManager.inst;
		CreatePlayer();

		//Only Master Client will handle Dot Spawning
		if (PhotonNetwork.IsMasterClient) SpawnDots();
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.A) && PhotonNetwork.IsMasterClient)
		{
			photonView.RPC("ToggleMoveCam", RpcTarget.AllBuffered, camSpeed != 0);

			//Temporary Check for when Game Started. Wait till we have a Coroutine or what not that registers when to move
			if (timeStamp <= 0) photonView.RPC("RegisterTimeStamp", RpcTarget.AllBuffered, 0f, true);
		}
		if (Input.GetKeyDown(KeyCode.G)) PhotonNetwork.LeaveRoom();
		if (Input.GetKeyDown(KeyCode.Q)) photonView.RPC("IncreaseDifficulty", RpcTarget.AllBuffered);
	}

	void FixedUpdate()
	{
		//Only Master Client will handle Camera Movement Value Changes and Dot Spawning
		if (PhotonNetwork.IsMasterClient)
		{
			#region Temp Difficulty Testing
			if (gameStarted)
			{
				timeStamp += Time.fixedDeltaTime;
				
				if (timeStamp >= 30)
				{
					if (increaseAllDiff) photonView.RPC("IncreaseDifficultyAll", RpcTarget.AllBuffered, cohesive);
					else
					{
						difficultyStage++;
						photonView.RPC("IncreaseDifficulty", RpcTarget.AllBuffered, difficultyStage, cohesive);
					}

					photonView.RPC("RegisterTimeStamp", RpcTarget.AllBuffered, 0f, false);
				}
				else photonView.RPC("RegisterTimeStamp", RpcTarget.OthersBuffered, timeStamp, false);
			}
			#endregion
			SpawnDots();
		}

		MoveCamera();
		cam.transform.position = camPos; //Update Camera Position Locally for each Player
	}

	#region For Spawning of player
	void CreatePlayer()
	{
		//Spawn Players Evenly
		float minY = cam.transform.position.y - cam.orthographicSize;
		float maxY = cam.transform.position.y + cam.orthographicSize;
		float interval = (maxY - minY) / (PhotonNetwork.PlayerList.Length + 1);
		Vector3 spawnPos = new Vector3(playerSpawnPos.position.x, maxY - interval * (PhotonNetwork.LocalPlayer.GetPlayerNumber()), 0);
		PhotonNetwork.Instantiate(System.IO.Path.Combine("PhotonPrefabs", "Player"), spawnPos , Quaternion.identity);
	}
	#endregion
	
	#region Networking Functions

	#region For Initialisation
	void InitialiseValues()
	{
		//Set Spawn Dot Values
		minMaxY = new Vector2(camPos.y - cam.orthographicSize + yMargin, camPos.y + cam.orthographicSize - yMargin);
		
		xInterval = Random.Range(minMaxXInterval.x, minMaxXInterval.y);
		lastXSpawn = cam.transform.position.x - xInterval - 5;
		xRemainder = (cam.orthographicSize * 2) * cam.aspect;

		photonView.RPC("SendInitValues", RpcTarget.OthersBuffered, minMaxY, xInterval, lastXSpawn, xRemainder);
	}

	[PunRPC]
	void SendInitValues(Vector2 minMaxY, float xInterval, float lastXSpawn, float xRemainder)
	{
		this.minMaxY = minMaxY;
		this.xInterval = xInterval;
		this.lastXSpawn = lastXSpawn;
		this.xRemainder = xRemainder;
	}

	void GetPlayerInfoAtStart()
	{
		Player[] players = PhotonNetwork.PlayerList;
		int[] ids = new int[players.Length];
		string[] names = new string[players.Length];
		float[] time = new float[players.Length];

		for (int i = 0; i < players.Length; i++)
		{
			ids[i] = players[i].ActorNumber;
			names[i] = players[i].NickName;
			time[i] = -1;
		}

		photonView.RPC("InitialiseLeaderboard", RpcTarget.AllBuffered, ids, names, time);
	}

	[PunRPC]
	void InitialiseLeaderboard(int[] ids, string[] names, float[] time)
	{
		playerInfos = new PlayerInfo[ids.Length];
		for (int i = 0; i < ids.Length; i++) playerInfos[i] = new PlayerInfo(ids[i], names[i], time[i]);
	}

	[PunRPC]
	void UpdateLeaderboard(int id, float time)
	{
		int idx = System.Array.IndexOf(playerInfos, playerInfos.Where(info => info.actorId == id).First());
		playerInfos[idx].deathTime = time;
		System.Array.Sort(playerInfos, (x,y) => y.deathTime.CompareTo(x.deathTime));
	}
	#endregion

	#region For Camera Movement
	[PunRPC]
	void ToggleMoveCam(bool isMoving)
	{
		if (isMoving)
		{
			camSpeed = 0;
			moveDelta = Vector2.zero;
		} 
		else camSpeed = defaultCamSpeed;
	}

	void MoveCamera()
	{
		if (camSpeed == 0) return;

		moveDelta = Vector3.right * camSpeed * Time.fixedDeltaTime;
		camPos += moveDelta;
	}

	[PunRPC]
	void SendNewCamValues(Vector3 moveDelta, Vector3 camPos)
	{
		this.moveDelta = moveDelta;
		this.camPos = camPos;
	}
	#endregion

	#region For Dot Spawning
	void SpawnDots()
	{
		xRemainder += moveDelta.x;
		while (xRemainder - xInterval > -0.25f)
		{
			xRemainder -= xInterval;
			lastXSpawn += xInterval;

			float dotSpawnCoeff = Random.Range(minDotSpawnCoeff, maxDotSpawnCoeff); //Spawning of 1 dot should be the least frequent
			int dotsToSpawn = Mathf.RoundToInt(3 * Mathf.Cos(Mathf.PI * 0.5f * dotSpawnCoeff) + 1); //Get the Y based off Dot Density
			Vector3[] dotPositions = new Vector3[dotsToSpawn];

			float spawnIntervalCoeff = Random.Range(minSpawnIntervalCoeff, maxSpawnIntervalCoeff);
			xInterval = (minMaxXInterval.y - minMaxXInterval.x) * Mathf.Sin(Mathf.PI * 0.5f * spawnIntervalCoeff) + minMaxXInterval.x; //Get next X Interval

			float yHeight = (cam.orthographicSize - yMargin) * 2;
			float maxY = yHeight / dotsToSpawn;

			for (int i = 0; i < dotsToSpawn; i++)
			{
				//Edit Min Max Offset Based on next Interval
				float xOffset = Random.Range(0, 0.666f) * xInterval; //Max Offset will be 2/3 of X Interval
				float x = lastXSpawn + xOffset;
				float y = Random.Range(maxY * i + 1, maxY * (i + 1)) + minMaxY.x; //maxY * i + 1 because 1 is the size of the Circle
				dotPositions[i] = new Vector3(x, y, 0);
				ObjectPooling.inst.SpawnFromPool("Dots", dotPositions[i], Quaternion.identity);
			}
		}

		photonView.RPC("UpdateDotSpawnValues", RpcTarget.OthersBuffered, xRemainder, xInterval, lastXSpawn);
	}

	[PunRPC]
	void UpdateDotSpawnValues(float xRemainder, float xInterval, float lastXSpawn)
	{
		this.xRemainder = xRemainder;
		this.xInterval = xInterval;
		this.lastXSpawn = lastXSpawn;
	}

	[PunRPC]
	void IncreaseDifficultyAll(bool cohesive)
	{
		if (cohesive)
		{
			//For Cohesive Increase
			maxDotSpawnCoeff = Mathf.Clamp(maxDotSpawnCoeff + 0.1f, 0, 1);
			minDotSpawnCoeff = Mathf.Clamp(minDotSpawnCoeff + 0.1f, 0, 1);
			//For Cohesive Increase
			maxSpawnIntervalCoeff = Mathf.Clamp(maxSpawnIntervalCoeff + 0.1f, 0, 1);
			minSpawnIntervalCoeff = Mathf.Clamp(minSpawnIntervalCoeff + 0.1f, 0, 1);
		}
		else
		{
			//For Affecting Separately
			if (maxDotSpawnCoeff != 1) maxDotSpawnCoeff = Mathf.Clamp(maxDotSpawnCoeff + 0.1f, 0, 1);
			else minDotSpawnCoeff = Mathf.Clamp(minDotSpawnCoeff + 0.1f, 0, 1);
			//For Affecting Separately
			if (maxSpawnIntervalCoeff != 1) maxSpawnIntervalCoeff = Mathf.Clamp(maxSpawnIntervalCoeff + 0.1f, 0, 1);
			else minSpawnIntervalCoeff = Mathf.Clamp(minSpawnIntervalCoeff + 0.1f, 0, 1);
		}

		defaultCamSpeed = Mathf.Clamp(defaultCamSpeed + 0.5f, defaultCamSpeed, 12.5f);
		camSpeed = defaultCamSpeed;
	}

	[PunRPC]
	void IncreaseDifficulty(int stage, bool cohesive)
	{
		if (stage % 2 != 0) //If Stage is Odd
		{
			if (cohesive)
			{
				//For Cohesive Increase
				maxDotSpawnCoeff = Mathf.Clamp(maxDotSpawnCoeff + 0.1f, 0, 1);
				minDotSpawnCoeff = Mathf.Clamp(minDotSpawnCoeff + 0.1f, 0, 1);
			}
			else
			{
				//For Affecting Separately
				if (maxDotSpawnCoeff != 1) maxDotSpawnCoeff = Mathf.Clamp(maxDotSpawnCoeff + 0.1f, 0, 1);
				else minDotSpawnCoeff = Mathf.Clamp(minDotSpawnCoeff + 0.1f, 0, 1);
			}
		}
		else
		{
			if (cohesive)
			{
				//For Cohesive Increase
				maxSpawnIntervalCoeff = Mathf.Clamp(maxSpawnIntervalCoeff + 0.1f, 0, 1);
				minSpawnIntervalCoeff = Mathf.Clamp(minSpawnIntervalCoeff + 0.1f, 0, 1);
			}
			else
			{
				//For Affecting Separately
				if (maxSpawnIntervalCoeff != 1) maxSpawnIntervalCoeff = Mathf.Clamp(maxSpawnIntervalCoeff + 0.1f, 0, 1);
				else minSpawnIntervalCoeff = Mathf.Clamp(minSpawnIntervalCoeff + 0.1f, 0, 1);
			}
		}

		defaultCamSpeed = Mathf.Clamp(defaultCamSpeed + 0.5f, defaultCamSpeed, 12.5f);
		camSpeed = defaultCamSpeed;
	}

	[PunRPC]
	void RegisterTimeStamp(float time, bool setGameStart)
	{
		timeStamp = time;
		if (setGameStart) gameStarted = true;
	}
	#endregion

	#endregion

	#region For Game End
	public void EndGame() //Called in RPC Function
	{
		gameEnded = true;
		ToggleMoveCam(false);
		gui.HideSpectateButton();
		gui.ShowHideEndScreen(true);
		gui.SwitchToSpectateMode(false); //In the case where we want to hide Spectate UI on End

		PhotonNetwork.CurrentRoom.IsOpen = true;
	}
	#endregion

	private void OnApplicationQuit()
	{
		PlayerPrefs.DeleteKey("Lobby State");
	}
}
