using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class GameManager : MonoBehaviourPunCallbacks
{
    [Header("General Game Manager Properties")]
    public static GameManager inst;
   
    [Header("Camera Items")]
    public Camera cam;
    public Vector3 moveDelta; //Stores Move Delta of the Camera;
    public Vector3 camPos; //Store Separately as this is the Reference Value that will be submitted to Server.
    public float camSpeed = 0, defaultCamSpeed = 5.0f; //Default is used to Set Values
    public float CamLeftBounds { get { return transform.position.x - cam.orthographicSize * cam.aspect; } }

    [Header("For Spawning Dots")]
    public float spawnFreq; //Frequency that Dots will Spawn
    [Range(0,1)] public float dotDensity; //Average Dot that should be on Screen
    public Vector2Int minMaxDotSpawn;

    public float yMargin; //Offset so that the Dot do not spawn at exactly the top of bottom of the Screen
    public Vector2 minMaxY; //Minimum and Maximum Y that Dot will Spawn

    public float xInterval; //How much X Dist Camera needs to cover before Spawning the next few dots
    public float xRemainder; //How much X Dist it exceeded from its X Interval
    public float lastXSpawn; //Last X Position that Spawned the Dots
    public Vector2 minMaxXInterval, minMaxXOffset; //Min and Max X Interval and Offset

	[Header ("For Spawning of player")]
	public Transform playerSpawnPos;
	public int currentPlayer;
    [SerializeField] private float spawnDistIntv; // spawn distance intervals between each player
	private void Awake()
    {
        inst = this;
        cam = GetComponent<Camera>();
        camPos = cam.transform.position;

        //Only Master Client will handle Camera Movement Changes and Dot Spawning so only Master will need to Initialise this Value and Pass it to the rest
        if (PhotonNetwork.IsMasterClient) InitialiseValues();
    }

    void Start()
    {
        CreatePlayer();

        //Only Master Client will handle Dot Spawning
        if (PhotonNetwork.IsMasterClient) SpawnDots();
    }

    void Update()
    {
        //Only Master Client will handle Camera Movement Value Changes and Dot Spawning
        if (PhotonNetwork.IsMasterClient)
        {
            if (Input.GetKeyDown(KeyCode.A)) photonView.RPC("ToggleMoveCam", RpcTarget.AllBuffered, camSpeed != 0);
            SpawnDots();
        }

        MoveCamera();
        cam.transform.position = camPos; //Update Camera Position Locally for each Player
		
        if (Input.GetKey(KeyCode.G)) PhotonNetwork.LeaveRoom();
    }

    #region For Spawning of player
    void CreatePlayer()
    {
        //Spawn Players Evenly
        float minY = cam.transform.position.y - cam.orthographicSize;
        float maxY = cam.transform.position.y + cam.orthographicSize;

		spawnDistIntv = (maxY - minY) / (PhotonNetwork.PlayerList.Length + 1);
		Vector3 spawnPos = new Vector3(playerSpawnPos.position.x, maxY - spawnDistIntv * (PhotonNetwork.LocalPlayer.ActorNumber), 0);
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

            /*int dotsToSpawn = Random.Range(minMaxDotSpawn.x, minMaxDotSpawn.y + 1);
            minMaxDotSpawn.x = Random.Range(0f, 100f) > 50 ? 2 : 1;
            minMaxDotSpawn.y = Random.Range(0f, 100f) > 50 ? 5 : 2;*/
            float dotDensity = Random.Range(this.dotDensity, 1); //Spawning of 1 dot should be the least frequent
            int dotsToSpawn = Mathf.RoundToInt(3 * Mathf.Cos(Mathf.PI * 0.5f * dotDensity) + 1); //Get the Y based off Dot Density
            Vector3[] dotPositions = new Vector3[dotsToSpawn];
            float yHeight = (cam.orthographicSize - yMargin) * 2;
            float maxY = yHeight / dotsToSpawn;

            for (int i = 0; i < dotsToSpawn; i++)
            {
                float xOffset = Random.Range(minMaxXOffset.x, minMaxXOffset.y);
                float x = lastXSpawn + xOffset;
                float y = Random.Range(maxY * i + 1, maxY * (i + 1)) + minMaxY.x; //maxY * i + 1 because 1 is the size of the Circle
                dotPositions[i] = new Vector3(x, y, 0);
                ObjectPooling.inst.SpawnFromPool("Dots", dotPositions[i], Quaternion.identity);
            }

            xInterval = Random.Range(minMaxXInterval.x, minMaxXInterval.y);
        }

        photonView.RPC("UpdateDotSpawnValues", RpcTarget.OthersBuffered, xRemainder, xInterval, lastXSpawn);
    }

    void AdjustSpawnAlgorithm (float dotDensity, float spawnFreq) 
    {
        minMaxDotSpawn.x = Random.Range(0f, 100f) > 50 ? 2 : 1;
        minMaxDotSpawn.y = Random.Range(0f, 100f) > 50 ? 5 : 2;
    }

    [PunRPC]
    void UpdateDotSpawnValues(float xRemainder, float xInterval, float lastXSpawn)
    {
        this.xRemainder = xRemainder;
        this.xInterval = xInterval;
        this.lastXSpawn = lastXSpawn;
    }
    #endregion

    #endregion
}
