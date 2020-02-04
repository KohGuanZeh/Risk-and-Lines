using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBackup : MonoBehaviour
{
	[Header("General Player Properties")]
	[SerializeField] LineRenderer linePreset; //Stores the Line Prefab that it will Instantiate each time Player travels

	[Header("Player Movement Properties")]
	[SerializeField] bool isTravelling; //Check if Player is Travelling
	[SerializeField] float travelSpeed; //Speed at which the Player Dot travels
	[SerializeField] Vector2 travelDir; //Direction at which Player should travel
	[SerializeField] TravelDot targetDot, storedDot, blinkDot; //Target Dot at which Player would go to || storedDot is for the blinking || blinnk dot is for the finalised blinking

	[SerializeField] float doubleTapThreshold; //this is the time before trave and also the time to register a double tap
	[SerializeField] float distanceToStop;

	// Start is called before the first frame update
	void Start()
	{

	}

	// Update is called once per frame
	void Update()
	{
		// decrease th waitTime every frame
		doubleTapThreshold -= Time.deltaTime;

		if (Input.touchCount > 0)
		{
			TouchControls();
		}
		TravelControl();
	}

	void TouchControls()
	{
		Touch touch = Input.GetTouch(0); // this is for the first finger that entered the screen

		// sets the dot to move towards
		if (touch.phase == TouchPhase.Ended && targetDot == null)
		{
			RaycastHit2D rayHit = Physics2D.GetRayIntersection(Camera.main.ScreenPointToRay(touch.position)); // this to get the direciton of the raycast

			// getting the dot
			if (rayHit.collider.GetComponent<TravelDot>() != null && rayHit.collider.GetComponent<TravelDot>().currentPlayer == null) // ensures that only one player can use it
			{
				Debug.Log(rayHit.collider.name);
				targetDot = rayHit.collider.GetComponent<TravelDot>();
				//targetDot.currentPlayer = this;
			}
		}
		//// to blink and cancel the touchphase
		if (touch.phase == TouchPhase.Ended)
		{
			RaycastHit2D rayHit = Physics2D.GetRayIntersection(Camera.main.ScreenPointToRay(touch.position)); // this to get the direciton of the raycast

			// getting the dot
			if ((rayHit.collider.GetComponent<TravelDot>() != null) && rayHit.collider.GetComponent<TravelDot>().currentPlayer == null || rayHit.collider.GetComponent<TravelDot>().currentPlayer == this) // ensures that only one player can use it
			{
				storedDot = rayHit.collider.GetComponent<TravelDot>();

				if (doubleTapThreshold < 0)
				{
					blinkDot = rayHit.collider.GetComponent<TravelDot>();
					doubleTapThreshold = 0.15f;
				}

				else if (doubleTapThreshold > 0)
				{
					BlinkControl();
				}
			}
		}
	}

	// for normal travel
	void TravelControl()
	{
		if (targetDot != null)
		{
			transform.position = Vector3.MoveTowards(transform.position, targetDot.transform.position, travelSpeed * Time.deltaTime);

			// to remove the targetDot
			float distanceToDot = Vector3.Distance(transform.position, targetDot.transform.position);
			if (distanceToDot <= distanceToStop)
			{
				targetDot = null;
			}
		}
	}

	void BlinkControl()
	{
		if (storedDot == blinkDot)
		{
			transform.position = storedDot.transform.position;
			doubleTapThreshold = 0;
			blinkDot = null;
			targetDot = null;
		}
		else
		{
			doubleTapThreshold = 0;
			blinkDot = null;
		}
	}

	void StartTravelTo(TravelDot dot)
	{
		if (dot.locked) return;
		isTravelling = true;
	}

	void BlinkTo(TravelDot dot)
	{
		if (dot.locked) return;
		transform.position = dot.transform.position;
	}
}
