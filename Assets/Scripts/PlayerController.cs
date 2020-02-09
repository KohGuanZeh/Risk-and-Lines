using System.Collections;
using System.Collections.Generic;
using UnityEngine;

<<<<<<< HEAD:Assets/Scripts/PlayerController.cs
public class PlayerController : MonoBehaviour
=======
using Photon.Pun;

public class Player : MonoBehaviour
>>>>>>> 821bbfe32f70e45003960c6eb4c4b89e9a673fd3:Assets/Scripts/Player.cs
{
	[Header("General Player Properties")]
	[SerializeField] GameManager gm;
    [SerializeField] TravelLine linePreset; //Stores the Line Prefab that it will Instantiate each time Player travels

    [Header("Player Movement Properties")]
    [SerializeField] float travelSpeed = 5; //Speed at which the Player Dot travels
	[SerializeField] TravelLine currentTravelLine; //The Line 
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
		if (touch.phase == TouchPhase.Began)
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
						print("Detected Touch");
						travelDot.locked = true;
						targetDot = travelDot;

						if (currentTravelLine) currentTravelLine.AddNewPoint(transform.position);
						else
						{
							currentTravelLine = ObjectPooling.inst.SpawnFromPool("Line", transform.position, Quaternion.identity).GetComponent<TravelLine>(); //Instantiate(linePreset, transform); //Will be changed to Object Pooling
							currentTravelLine.CreateNewLine(transform.position);
						}
					} 
					else if (travelDot == storedDot && doubleTapThreshold > 0) //If there is already a Stored Dot, This is Considered a Double Tap
					{
						blinked = true;
						if (travelDot.locked && targetDot != travelDot)
						{
							storedDot = null;
							return;
						}
						BlinkControl();
					}

					if (!blinked)
					{
						storedDot = travelDot;
						doubleTapThreshold = 0.5f;
					}
				}
			}
		}
	}

	// for normal travel
	void TravelControl()
	{
		if (targetDot == null) return;

		transform.position = Vector2.MoveTowards(transform.position, targetDot.transform.position, travelSpeed * Time.deltaTime);
		currentTravelLine.UpdateLine(transform.position);

		//To remove the targetDot
		float distanceToDot = Vector2.Distance(transform.position, targetDot.transform.position);
		
		if (distanceToDot <= distanceToStop)
		{
			currentTravelLine.OnFinishedTravel(transform.position.x);
			targetDot = null;
		} 
	}

	void BlinkControl()
	{
		//Insert Camera Shake Effect

		CutLine(); //Need to be on top before Target Dot is Set to Null

		transform.position = storedDot.transform.position;
		targetDot.locked = false;
		targetDot = null;
		storedDot = null;
		doubleTapThreshold = 0;
	}

	//Delete the last position and cut out the Travel Line
	void CutLine()
	{
		if (targetDot != null) currentTravelLine.CutLine();
		currentTravelLine = null;
	}

	public void Death()
	{
		print("Player is Dead");
		//gameObject.SetActive(false);
		//Check Player Count. If Player Count is 1. Trigger Win Screen
	}
}
