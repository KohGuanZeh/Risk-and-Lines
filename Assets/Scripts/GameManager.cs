﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class GameManager : MonoBehaviourPunCallbacks
{
    [Header("General Game Manager Properties")]
    public static GameManager inst;
    public Transform playerSpawnPos;

    [Header("Camera Items")]
    public Camera cam;
    public Vector3 moveDelta; //Stores Move Delta of the Camera;
    public Vector3 camPos; //Store Separately as this is the Reference Value that will be submitted to Server.
    public float camSpeed = 0, defaultCamSpeed = 5.0f; //Default is used to Set Values
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
        cam = Camera.main;

        //Only Master Client will handle Camera Movement Changes and Dot Spawning so only Master will need to Initialise this Value and Pass it to the rest
        if (PhotonNetwork.IsMasterClient) photonView.RPC("InitialiseValues", RpcTarget.AllBuffered);
    }

    void Start()
    {
        CreatePlayer();

        //Only Master Client will handle Dot Spawning
        if (PhotonNetwork.IsMasterClient) photonView.RPC("SpawnDots", RpcTarget.AllBuffered);
    }

    void Update()
    {
        //Only Master Client will handle Camera Movement Value Changes and Dot Spawning
        if (PhotonNetwork.IsMasterClient)
        {
            if (Input.GetKeyDown(KeyCode.A)) photonView.RPC("ToggleMoveCam", RpcTarget.AllBuffered, camSpeed != 0);
            photonView.RPC("MoveCamera", RpcTarget.AllBuffered);
            photonView.RPC("SpawnDots", RpcTarget.AllBuffered);
        }

        cam.transform.position = camPos; //Update Camera Position Locally for each Player
    }

    void CreatePlayer()
    {
        PhotonNetwork.Instantiate(System.IO.Path.Combine("PhotonPrefabs", "Player"), playerSpawnPos.position, Quaternion.identity);
    }

    #region Networking Functions

    #region For Initialisation
    [PunRPC]
    void InitialiseValues()
    {
        //Set Standard Cam Position
        camPos = cam.transform.position;

        //Set Spawn Dot Values
        minMaxY = new Vector2(camPos.y - cam.orthographicSize + yMargin, camPos.y + cam.orthographicSize - yMargin);
        
        xInterval = Random.Range(minMaxXInterval.x, minMaxXInterval.y);
        lastXSpawn = cam.transform.position.x - xInterval - 5;
        xRemainder = (cam.orthographicSize * 2) * cam.aspect;
    }
    #endregion

    #region For Camera Movement
    [PunRPC]
    void ToggleMoveCam(bool isMoving)
    {
        if (isMoving) camSpeed = 0;
        else camSpeed = defaultCamSpeed;
    }

    [PunRPC]
    void MoveCamera()
    {
        if (camSpeed == 0) return;
        moveDelta = Vector3.right * camSpeed * Time.deltaTime;
        camPos += moveDelta;
    }
    #endregion

    #region For Dot Spawning
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
    #endregion

    #endregion
}
