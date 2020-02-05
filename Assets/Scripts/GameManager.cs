using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("General Game Manager Properties")]
    public static GameManager inst;

    [Header("Camera Items")]
    public Camera cam;
    public Vector3 camPos; //Store Separately as this is the Reference Value that will be submitted to Server.
    public float camSpeed = 3.0f;
    public bool moveCam; //If true, Cam will move. Else Cam will stop moving

    [Header("For Spawning Dots")]
    public int dotDensity;

    private void Awake()
    {
        inst = this;
        cam = GetComponent<Camera>();
    }

    void Start()
    {
        camPos = cam.transform.position;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A)) moveCam = !moveCam;
        if (moveCam) MoveCamera();
    }

    void MoveCamera()
    {
        camPos += Vector3.right * camSpeed * Time.deltaTime;
        cam.transform.position = camPos;
    }
}
