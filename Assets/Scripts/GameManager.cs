using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class GameManager : MonoBehaviour
{
    [Header("General Game Manager Properties")]
    public static GameManager inst;
    [SerializeField] PhotonView photonView;
    public Transform playerSpawnPos;

    [Header("Camera Items")]
    public Camera cam;
    public Vector3 moveDelta; //Stores Move Delta of the Camera;
    public Vector3 camPos; //Store Separately as this is the Reference Value that will be submitted to Server.
    public float camSpeed = 3.0f;
    public bool moveCam; //If true, Cam will move. Else Cam will stop moving
    public float CamLeftBounds { get { return transform.position.x - cam.orthographicSize * cam.aspect; } }

    [Header("For Spawning Dots")]
    public int dotDensity;
    public Vector2Int minMaxDotSpawn;

    public float yMargin; //Offset so that the Dot do not spawn at exactly the top of bottom of the Screen
    public Vector2 minMaxY; //Minimum and Maximum Y that Dot will Spawn

    public float xInterval; //How much X Dist Camera needs to cover before Spawning the next few dots
    public float xRemainder; //How much X Dist it exceeded from its X Interval
    public float lastXSpawn; //Last X Position that Spawned the Dots
    public Vector2 minMaxXInterval, minMaxXOffset; //Min and Max X Interval and Offset

    private void Awake()
    {
        inst = this;
        photonView = GetComponent<PhotonView>();
        cam = GetComponent<Camera>();
        camPos = cam.transform.position;

        if (!PhotonNetwork.IsConnected) PhotonNetwork.OfflineMode = true;
        minMaxY = new Vector2(camPos.y - cam.orthographicSize + yMargin, camPos.y + cam.orthographicSize - yMargin);
    }

    void Start()
    {
        CreatePlayer();
        lastXSpawn = cam.transform.position.x;
        xInterval = Random.Range(minMaxXInterval.x, minMaxXInterval.y);
        xRemainder = (cam.orthographicSize * 2) * cam.aspect;

        if (PhotonNetwork.IsConnected) photonView.RPC("SpawnDots", RpcTarget.AllBuffered);
        else SpawnDots(); 
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A)) moveCam = !moveCam;

        if (moveCam && PhotonNetwork.IsMasterClient) MoveCamera();
        photonView.RPC("SpawnDots", RpcTarget.AllBuffered);

        /*if (PhotonNetwork.IsConnected)
        {
            if (moveCam && PhotonNetwork.IsMasterClient) MoveCamera();
            photonView.RPC("SpawnDots", RpcTarget.AllBuffered);
        }
        else //Offline Mode
        {
            if (moveCam) MoveCamera();
            SpawnDots();
        }*/
    }

    void CreatePlayer()
    {
        PhotonNetwork.Instantiate(System.IO.Path.Combine("PhotonPrefabs", "Player"), playerSpawnPos.position, Quaternion.identity);
        /*for (int i = 0; i < PhotonNetwork.PlayerList.Length; i++)
        {
            Vector3 offset = Vector3.up * 0.5f * i;
            PhotonNetwork.Instantiate(System.IO.Path.Combine("PhotonPrefabs", "Player"), playerSpawnPos.position + offset, Quaternion.identity);
        }*/
    }

    void MoveCamera()
    {
        moveDelta = Vector3.right * camSpeed * Time.deltaTime;
        camPos += moveDelta;
        cam.transform.position = camPos;
    }

    [PunRPC]
    void SpawnDots()
    {
        xRemainder += moveDelta.x;
        while (xRemainder - xInterval > -0.25f)
        {
            xRemainder -= xInterval;
            lastXSpawn += xInterval;

            int dotsToSpawn = Random.Range(minMaxDotSpawn.x, minMaxDotSpawn.y + 1);
            minMaxDotSpawn.x = Random.Range(0f, 100f) > 50 ? 2 : 1;
            minMaxDotSpawn.y = Random.Range(0f, 100f) > 50 ? 5 : 2;
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
    }
}
