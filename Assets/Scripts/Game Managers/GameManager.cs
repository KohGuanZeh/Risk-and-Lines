﻿using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Photon.Pun.UtilityScripts;

[System.Serializable]
public struct PlayerInfo {
	public int actorId;
	public int charSpr;
	public string playerName;
	public float deathTime;

	public PlayerInfo(int id, int spr, string name, float time) {
		actorId = id;
		charSpr = spr;
		playerName = name;
		deathTime = time;
	}
}

public class GameManager : MonoBehaviourPunCallbacks {
	[Header("General Variables")]
	public static GameManager inst;
	[SerializeField] UIManager gui;
	public float totalTime;
	public int playersLoaded;

	[Header("Camera Items")]
	public Camera cam;
	public bool moveCam;
	public float accelDecelMult; //Multiplier to Increase/Decrease the Cam Speed to Default Speed/0 in 0.5s
	public Vector3 moveDelta; //Stores Move Delta of the Camera;
	public Vector3 camPos; //Store Separately as Reference Value for Camera
	public float camSpeed = 0, defaultCamSpeed = 5.0f; //Default is used to Set Values
	public float CamLeftBounds { get { return camPos.x - cam.orthographicSize * cam.aspect; } }

	[Header("For Spawning Dots")]
	[Range(0, 1)] public float minSpawnIntervalCoeff; //> Coeff, > Interval Dist
	[Range(0, 1)] public float maxSpawnIntervalCoeff; //> Coeff, > Interval Dist
	[Range(0, 1)] public float minDotSpawnCoeff; //Coefficient that affects Dot Spawning. > Coeff, < Dots Spawn
	[Range(0, 1)] public float maxDotSpawnCoeff; //Coefficient that affects Dot Spawning. > Coeff, < Dots Spawn
	//public Vector2Int minMaxDotSpawn;

	public float yMargin; //Offset so that the Dot do not spawn at exactly the top of bottom of the Screen
	public Vector2 minMaxY; //Minimum and Maximum Y that Dot will Spawn

	public float xInterval; //How much X Dist Camera needs to cover before Spawning the next few dots
	public float xRemainder; //How much X Dist it exceeded from its X Interval
	public float lastXSpawn; //Last X Position that Spawned the Dots
	public Vector2 minMaxXInterval, minMaxXOffset; //Min and Max X Interval and Offset

	[Header("For Spawning Interactables")]
	public float nextSpawnTime;

	[Header("For Player Spawn")]
	public Transform playerSpawnPos;
	public Sprite[] presets;

	[Header("For Game Difficulty")]
	public bool reachedMaxDifficulty;
	[SerializeField] int difficultyStage;
	[SerializeField] float timeStamp;
	public bool gameStarted;

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
		gui = UIManager.inst;

		playersAlive = PhotonNetwork.PlayerList.Length;
		CreatePlayer();

		//Only Master Client will handle Dot Spawning
		if (PhotonNetwork.IsMasterClient)
		{
			nextSpawnTime = 30;
			photonView.RPC("UpdateNextSpawnTime", RpcTarget.OthersBuffered, nextSpawnTime);
			SpawnDots();
		} 
	}

	void FixedUpdate() {

		if (gameStarted && !gameEnded)
		{
			totalTime += Time.fixedDeltaTime;

			//Only Master Client will handle Camera Movement Value Changes and Dot Spawning
			if (PhotonNetwork.IsMasterClient)
			{
				if (gameStarted && !reachedMaxDifficulty)
				{
					timeStamp += Time.fixedDeltaTime;

					if (timeStamp >= 15)
					{
						IncreaseDifficulty();
						photonView.RPC("RegisterTimeStamp", RpcTarget.AllBuffered, 0f, false);
					}
					else photonView.RPC("RegisterTimeStamp", RpcTarget.OthersBuffered, timeStamp, false);
				}
				SpawnDots();
			}
		}

		AccelerateDecelerateCam();
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
		GameObject playerObj = PhotonNetwork.Instantiate(System.IO.Path.Combine("PhotonPrefabs", "Player"), spawnPos, Quaternion.Euler(0, 0, -90));
		playerObj.GetComponent<PlayerController>().SetUpCharacter();

	}
	#endregion

	#region Networking Functions

	#region For Initialisation
	void InitialiseValues() {
		//Set Spawn Dot Values
		minMaxY = new Vector2(camPos.y - cam.orthographicSize + yMargin, camPos.y + cam.orthographicSize - yMargin);

		xInterval = Random.Range(minMaxXInterval.x, minMaxXInterval.y);
		lastXSpawn = cam.transform.position.x - xInterval - 5;
		xRemainder = (cam.orthographicSize * 2) * cam.aspect;

		photonView.RPC("SendInitValues", RpcTarget.OthersBuffered, minMaxY, xInterval, lastXSpawn, xRemainder);
	}

	[PunRPC]
	void SendInitValues(Vector2 minMaxY, float xInterval, float lastXSpawn, float xRemainder) {
		this.minMaxY = minMaxY;
		this.xInterval = xInterval;
		this.lastXSpawn = lastXSpawn;
		this.xRemainder = xRemainder;
	}

	[PunRPC]
	public void QueueGameStart()
	{
		playersLoaded++;
		if (playersLoaded != PhotonNetwork.PlayerList.Length) return;

		LoadingScreen.inst.canFadeOut = true;
		PhotonNetwork.AutomaticallySyncScene = false;
		UIManager.inst.TriggerGameStart();
	}

	public void StartGame()
	{
		photonView.RPC("ToggleMoveCam", RpcTarget.AllBuffered, true);
		photonView.RPC("RegisterTimeStamp", RpcTarget.AllBuffered, 0f, true);
	}

	//Called in Animation Event
	void SetGameModeToStart()
	{
		gameStarted = true;
	}

	void GetPlayerInfoAtStart() {
		Player[] players = PhotonNetwork.PlayerList;
		int[] ids = new int[players.Length];
		int[] charSprs = new int[players.Length];
		string[] names = new string[players.Length];
		float[] time = new float[players.Length];

		for (int i = 0; i < players.Length; i++) {
			ids[i] = players[i].ActorNumber;
			charSprs[i] = 0; //Set only in Character Creation
			names[i] = players[i].NickName;
			time[i] = -1;
		}

		photonView.RPC("InitialiseLeaderboard", RpcTarget.AllBuffered, ids, charSprs, names, time);
	}

	[PunRPC]
	void InitialiseLeaderboard(int[] ids, int[] charSprs, string[] names, float[] time) 
	{
		playerInfos = new PlayerInfo[ids.Length];
		for (int i = 0; i < ids.Length; i++) playerInfos[i] = new PlayerInfo(ids[i], charSprs[i], names[i], time[i]);

		//Send Another RPC to get the Preset Value for Leaderboard Display
		photonView.RPC("SetPlayersCharPreset", RpcTarget.AllBuffered, PhotonNetwork.LocalPlayer.ActorNumber, PlayerPrefs.GetInt("Preset", 0));
	}

	[PunRPC]
	void SetPlayersCharPreset(int id, int presetIdx)
	{
		int idx = System.Array.IndexOf(playerInfos, playerInfos.Where(info => info.actorId == id).First());
		playerInfos[idx].charSpr = presetIdx;
	}

	[PunRPC]
	void UpdateLeaderboard(int id, float time) {
		int idx = System.Array.IndexOf(playerInfos, playerInfos.Where(info => info.actorId == id).First());
		playerInfos[idx].deathTime = time;
		System.Array.Sort(playerInfos, (x, y) => y.deathTime.CompareTo(x.deathTime));
	}
	#endregion

	#region For Camera Movement
	[PunRPC]
	void ToggleMoveCam(bool move) 
	{
		moveCam = move;

		if (moveCam) accelDecelMult = (defaultCamSpeed - camSpeed) / (Time.fixedDeltaTime * 0.5f);
		else accelDecelMult = (camSpeed - 0) / 0.5f;
	}

	void AccelerateDecelerateCam()
	{
		if ((moveCam && camSpeed >= defaultCamSpeed) || (!moveCam && camSpeed <= 0)) return;
		float change = Time.fixedDeltaTime * accelDecelMult;
		camSpeed = moveCam ? Mathf.Min(camSpeed + change, defaultCamSpeed) : Mathf.Max(camSpeed - change, 0);
	}

	void MoveCamera() 
	{
		if (camSpeed == 0 && !moveCam && moveDelta.x == 0) return;

		moveDelta = Vector3.right * camSpeed * Time.fixedDeltaTime;
		camPos += moveDelta;
	}

	[PunRPC]
	void SendNewCamValues(Vector3 moveDelta, Vector3 camPos) {
		this.moveDelta = moveDelta;
		this.camPos = camPos;
	}
	#endregion

	#region For Dot Spawning
	void SpawnDots() {
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

			for (int i = 0; i < dotsToSpawn; i++) {
				//Edit Min Max Offset Based on next Interval
				float xOffset = Random.Range(0, 0.666f) * xInterval; //Max Offset will be 2/3 of X Interval
				float x = lastXSpawn + xOffset;
				float y = Random.Range(maxY * i + 1, maxY * (i + 1)) + minMaxY.x; //maxY * i + 1 because 1 is the size of the Circle
				dotPositions[i] = new Vector3(x, y, 0);
				ObjectPooling.inst.SpawnFromPool("Dots", dotPositions[i], Quaternion.identity);
			}

			//Spawn Interactables
			if (totalTime >= nextSpawnTime)
			{
				int noToSpawn = GetNumberToSpawn();
				for (int i = 1; i < noToSpawn; i++)
				{
					float xOffset = Random.Range(0, 1.0f) * xInterval;
					float x = lastXSpawn + xOffset;
					float y = Random.Range(0, 1.0f) * yHeight + minMaxY.x;
					Vector3 spawnPos = new Vector3(x, y, 0);
					ObjectPooling.inst.SpawnFromPool(GetSpawnObjTag(), spawnPos, Quaternion.identity);
				}
				nextSpawnTime = totalTime + 30;
				photonView.RPC("UpdateNextSpawnTime", RpcTarget.OthersBuffered, nextSpawnTime);
			}
		}

		photonView.RPC("UpdateDotSpawnValues", RpcTarget.OthersBuffered, xRemainder, xInterval, lastXSpawn);
	}

	[PunRPC]
	void UpdateDotSpawnValues(float xRemainder, float xInterval, float lastXSpawn) {
		this.xRemainder = xRemainder;
		this.xInterval = xInterval;
		this.lastXSpawn = lastXSpawn;
	}

	//Testing Different Difficulty Algorithms
	public void IncreaseDifficulty()
	{
		if (increaseAllDiff) photonView.RPC("IncreaseDifficultyAll", RpcTarget.AllBuffered, cohesive);
		else
		{
			difficultyStage++;
			photonView.RPC("IncreaseDifficultySep", RpcTarget.AllBuffered, difficultyStage, cohesive);
		}
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
		accelDecelMult = (defaultCamSpeed - camSpeed) / 0.5f;
		//camSpeed = defaultCamSpeed;

		gui.ShowDiffIncreased();

		if (defaultCamSpeed >= 12.5f && minDotSpawnCoeff >= 1 && minSpawnIntervalCoeff >= 1) reachedMaxDifficulty = true;
	}

	[PunRPC]
	void IncreaseDifficultySep(int stage, bool cohesive) 
	{
		if (stage % 2 != 0) //If Stage is Odd
		{
			if (cohesive) {
				//For Cohesive Increase
				maxDotSpawnCoeff = Mathf.Clamp(maxDotSpawnCoeff + 0.1f, 0, 1);
				minDotSpawnCoeff = Mathf.Clamp(minDotSpawnCoeff + 0.1f, 0, 1);
			} else {
				//For Affecting Separately
				if (maxDotSpawnCoeff != 1) maxDotSpawnCoeff = Mathf.Clamp(maxDotSpawnCoeff + 0.1f, 0, 1);
				else minDotSpawnCoeff = Mathf.Clamp(minDotSpawnCoeff + 0.1f, 0, 1);
			}
		} else {
			if (cohesive) {
				//For Cohesive Increase
				maxSpawnIntervalCoeff = Mathf.Clamp(maxSpawnIntervalCoeff + 0.1f, 0, 1);
				minSpawnIntervalCoeff = Mathf.Clamp(minSpawnIntervalCoeff + 0.1f, 0, 1);
			} else {
				//For Affecting Separately
				if (maxSpawnIntervalCoeff != 1) maxSpawnIntervalCoeff = Mathf.Clamp(maxSpawnIntervalCoeff + 0.1f, 0, 1);
				else minSpawnIntervalCoeff = Mathf.Clamp(minSpawnIntervalCoeff + 0.1f, 0, 1);
			}
		}

		defaultCamSpeed = Mathf.Clamp(defaultCamSpeed + 0.5f, defaultCamSpeed, 12.5f);
		accelDecelMult = (defaultCamSpeed - camSpeed) / 0.5f;
		//camSpeed = defaultCamSpeed;

		gui.ShowDiffIncreased();

		if (defaultCamSpeed >= 12.5f && minDotSpawnCoeff >= 1 && minSpawnIntervalCoeff >= 1) reachedMaxDifficulty = true;
	}

	[PunRPC]
	void RegisterTimeStamp(float time, bool setGameStart) {
		timeStamp = time;
		if (setGameStart) gameStarted = true;
	}
	#endregion

	#endregion

	#region For Interactable Spawn
	[PunRPC]
	void UpdateNextSpawnTime(float val)
	{
		nextSpawnTime = val;
	}

	int GetNumberToSpawn()
	{
		float pct = UnityEngine.Random.Range(0, 100);
		if (pct <= 20) return 0; //20% to spawn 0
		else if (pct <= 60) return 1; //40% to spawn 1
		else if (pct <= 90) return 2; //30% to spawn 2
		else return 3; //10% to spawn 3
	}

	string GetSpawnObjTag()
	{
		float pct = UnityEngine.Random.Range(0, 100);
		if (pct <= 70) return "Blink";
		else return "Difficulty";
	}
	#endregion

	#region For Game End
	public void EndGame() //Called in RPC Function
	{
		gameEnded = true;
		ToggleMoveCam(false); //True is Stop Moving. Bool = isMoving. Therefore, If Moving, Stop Moving.
		gui.ShowEndScreen();
	}
	#endregion

	private void OnApplicationQuit() 
	{
		PlayerPrefs.DeleteKey("Lobby State");
	}

	#region ExtraUtilities
	public static Color GetCharacterColor(int playerNo, float alpha = 1)
	{
		switch (playerNo)
		{
			case 1:
				return new Color(1, 0.37f, 0.68f, alpha);
			case 2:
				return new Color(0.37f, 0.68f, 1, alpha);
			case 3:
				return new Color(0.21f, 1, 0.60f, alpha);
			case 4:
				return new Color(1, 1, 0.24f, alpha);
			default:
				return new Color(1, 1, 1, alpha);
		}
	}
	#endregion
}
