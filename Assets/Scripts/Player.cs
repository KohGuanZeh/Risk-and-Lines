﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
	[Header("General Player Properties")]
	[SerializeField] GameManager gm;
    [SerializeField] LineRenderer linePreset; //Stores the Line Prefab that it will Instantiate each time Player travels

    [Header("Player Movement Properties")]
    [SerializeField] float travelSpeed; //Speed at which the Player Dot travels
	[SerializeField] TravelDot targetDot, storedDot; //Target Dot is which Dot Player will travel to. Stored Dot is The Dot that is currently tapped

	[SerializeField] float doubleTapThreshold; //this is the time before travel and also the time to register a double tap
	[SerializeField] float distanceToStop;

    void Start()
    {
		gm = GameManager.inst;
    }

    void Update()
    {
		//Decrease the Wait Time every frame
		doubleTapThreshold = Mathf.Max(doubleTapThreshold - Time.deltaTime, 0);
        if (Input.touchCount > 0) TouchControls();
		TravelControl();
    }

	void TouchControls()
	{
		Touch touch = Input.GetTouch(0); // this is for the first finger that entered the screen

		// Sets the dot to move towards
		if (touch.phase == TouchPhase.Ended)
		{
			RaycastHit2D rayHit = Physics2D.GetRayIntersection(gm.cam.ScreenPointToRay(touch.position)); // this to get the direciton of the raycast

			//Getting the Dot
			if (rayHit.collider != null)
			{
				TravelDot travelDot = rayHit.collider.GetComponent<TravelDot>();
				if (travelDot != null)
				{
					bool blinked = false;

					if (targetDot == null && !travelDot.locked)
					{
						travelDot.locked = true;
						targetDot = travelDot;
						doubleTapThreshold = 0.15f;
					} 
					else if (travelDot == storedDot && doubleTapThreshold > 0) //If there is already a Stored Dot, This is Considered a Double Tap
					{
						blinked = true;
						storedDot = null;
						doubleTapThreshold = 0;
						BlinkControl();
					}

					if (!blinked && !travelDot.locked) storedDot = travelDot;
				}
			}
		}
	}

	// for normal travel
	void TravelControl()
	{
		if (targetDot != null)
		{
			transform.position = Vector2.MoveTowards(transform.position, targetDot.transform.position, travelSpeed * Time.deltaTime);

			//To remove the targetDot
			float distanceToDot = Vector2.Distance(transform.position, targetDot.transform.position);
			if (distanceToDot <= distanceToStop) targetDot = null;
		}
	}

	void BlinkControl()
	{
		//if (targetDot != null) //If Player is Travelling. Rmb Delete the Line Renderer
		//Insert Camera Shake Effect

		transform.position = storedDot.transform.position;
		targetDot = null;
		storedDot = null;
		doubleTapThreshold = 0;
	}
}
