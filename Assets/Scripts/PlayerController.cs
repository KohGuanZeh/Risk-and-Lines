using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;
using TMPro;

using Photon.Pun;
using Photon.Pun.UtilityScripts;
using Photon.Realtime;

public class PlayerController : MonoBehaviourPunCallbacks 
{
	[Header("General Player Properties")]
	[SerializeField] GameManager gm;
	[SerializeField] UIManager gui;
	[SerializeField] int playerNo;
	[SerializeField] Rigidbody2D rb;
	[SerializeField] TravelLine linePreset; //Stores the Line Prefab that it will Instantiate each time Player travels

	[Header("Player Sprites")]
	[SerializeField] SpriteRenderer spr;

	[Header("Player Identifier")]
	[SerializeField] TextMeshProUGUI playerName;
	[SerializeField] Image arrow;
	[SerializeField] Animator identifierAnim;

	[Header("Player Movement Properties")]
	[SerializeField] float travelSpeed = 5; //Speed at which the Player Dot travels
	[SerializeField] TravelLine currentTravelLine; //The Line 
	[SerializeField] TravelDot targetDot, storedDot; //Target Dot is which Dot Player will travel to. Stored Dot is The Dot that is currently tapped
	[SerializeField] float doubleTapThreshold; //this is the time before travel and also the time to register a double tap
	[SerializeField] float distanceToStop;

	[Header("For Blink")]
	[SerializeField] int blinkCount = 3;
	[SerializeField] float blinkCd; //Timer used to check whether it should restore a Blink.
	[SerializeField] float maxCdTime = 5f; //Max Time for Blink to refill

	[Header("CamShake")]
	[SerializeField] float shakeDuration = 0f;
	[SerializeField] float setShakeDuration;
	[SerializeField] float shakeAmount = 0f;
	[SerializeField] float decreaseFactor = 1.0f;

	void Start() 
	{
		gm = GameManager.inst;
		gui = UIManager.inst;
		Debug.LogWarning(gui.name);
		gui.AssignPlayerController(this); //May need photonView.IsMine
		rb = GetComponent<Rigidbody2D>();

		blinkCd = maxCdTime;
		gm.photonView.RPC("QueueGameStart", RpcTarget.AllBuffered);

		photonView.RPC("SetIdentiferColor", RpcTarget.AllBuffered);

		shakeDuration = 0f;
		setShakeDuration = 0.2f;
		shakeAmount = 0.5f;
		decreaseFactor = 1.0f;
	}

	void Update() 
	{
		//Decrease the Wait Time every frame
		if (photonView.IsMine) 
		{
			CamShake();

			if (gm.gameStarted && !gm.gameEnded) {
				UpdateBlinkCd();

				doubleTapThreshold = Mathf.Max(doubleTapThreshold - Time.deltaTime, 0);
				if (Input.touchCount > 0) TouchControls();
				MouseControls();
				TravelControl();

				if (transform.position.x < gm.CamLeftBounds) Death();
			}
		}
	}

	#region Iput Controls
	void TouchControls() 
	{
		Touch touch = Input.GetTouch(0); // this is for the first finger that entered the screen

		// Sets the dot to move towards
		if (touch.phase == TouchPhase.Began) 
		{
			RaycastHit2D rayHit = Physics2D.GetRayIntersection(gm.cam.ScreenPointToRay(touch.position)); // this to get the direciton of the raycast

			//Getting the Dot
			if (!ReferenceEquals(rayHit.collider, null)) {
				TravelDot travelDot = rayHit.collider.GetComponent<TravelDot>();
				if (!ReferenceEquals(travelDot, null)) {
					bool blinked = false;

					if (ReferenceEquals(targetDot, null) && !travelDot.locked) {
						travelDot.photonView.RPC("LockTravelDot", RpcTarget.AllBuffered, playerNo, true);
						targetDot = travelDot;

						SfxManager.inst.PlaySfx(SfxManager.inst.Sfx.lockSound);// play the lock sound

						if (currentTravelLine) currentTravelLine.photonView.RPC("AddNewPoint", RpcTarget.AllBuffered, transform.position);
						else {
							currentTravelLine = ObjectPooling.inst.SpawnFromPool("Line", transform.position, Quaternion.identity).GetComponent<TravelLine>(); //Instantiate(linePreset, transform); //Will be changed to Object Pooling
							currentTravelLine.photonView.RPC("CreateNewLine", RpcTarget.AllBuffered, photonView.ViewID, playerNo, transform.position);
						}
					} else if (ReferenceEquals(travelDot, storedDot) && doubleTapThreshold > 0 && blinkCount > 0) //If there is already a Stored Dot, This is Considered a Double Tap
					  {
						blinked = true;
						if (travelDot.locked && targetDot != travelDot) {
							storedDot = null;
							return;
						}
						BlinkControl();
					}

					if (!blinked) {
						storedDot = travelDot;
						doubleTapThreshold = 0.5f;
					}
				}
			}
		}
	}

	void MouseControls() {
		if (Input.GetMouseButtonDown(0)) {
			RaycastHit2D rayHit = Physics2D.GetRayIntersection(gm.cam.ScreenPointToRay(Input.mousePosition)); // this to get the direciton of the raycast

			//Getting the Dot
			if (!ReferenceEquals(rayHit.collider, null)) {
				TravelDot travelDot = rayHit.collider.GetComponent<TravelDot>();
				if (!ReferenceEquals(travelDot, null)) {
					bool blinked = false;

					if (ReferenceEquals(targetDot, null) && !travelDot.locked) {
						travelDot.photonView.RPC("LockTravelDot", RpcTarget.AllBuffered, playerNo, true);
						targetDot = travelDot;

						SfxManager.inst.PlaySfx(SfxManager.inst.Sfx.lockSound);// play the lock sound

						transform.up = targetDot.transform.position - transform.position;

						if (currentTravelLine) currentTravelLine.photonView.RPC("AddNewPoint", RpcTarget.AllBuffered, transform.position);
						else {
							currentTravelLine = ObjectPooling.inst.SpawnFromPool("Line", transform.position, Quaternion.identity).GetComponent<TravelLine>(); //Instantiate(linePreset, transform); //Will be changed to Object Pooling
							currentTravelLine.playerRefId = photonView.ViewID;
							currentTravelLine.photonView.RPC("CreateNewLine", RpcTarget.AllBuffered, photonView.ViewID, playerNo, transform.position);
						}
					} else if (ReferenceEquals(travelDot, storedDot) && doubleTapThreshold > 0 && blinkCount > 0) //If there is already a Stored Dot, This is Considered a Double Tap
					  {
						transform.up = storedDot.transform.position - transform.position;
						blinked = true;
						if (travelDot.locked && targetDot != travelDot) {
							storedDot = null;
							return;
						}
						BlinkControl();
					}

					if (!blinked) {
						storedDot = travelDot;
						doubleTapThreshold = 0.5f;
					}
				}
			}
		}
	}
	#endregion

	#region For Travel and Blink
	// for Normal travel
	void TravelControl() 
	{
		if (ReferenceEquals(targetDot, null)) return;

		transform.position = Vector2.MoveTowards(transform.position, targetDot.transform.position, (gm.camSpeed + travelSpeed) * Time.deltaTime);
		currentTravelLine.photonView.RPC("UpdateLine", RpcTarget.AllBuffered, transform.position);

		//To remove the targetDot
		float distanceToDot = Vector2.Distance(transform.position, targetDot.transform.position);

		if (distanceToDot <= distanceToStop) {
			currentTravelLine.photonView.RPC("OnFinishedTravel", RpcTarget.AllBuffered, transform.position.x);
			targetDot = null;
		}
	}

	void BlinkControl() 
	{
		SetCamShakeDuration();
		CutLine(); //Need to be on top before Target Dot is Set to Null

		ObjectPooling.inst.SpawnFromPool("Blink Particles", transform.position, Quaternion.identity);

		transform.position = storedDot.transform.position;
		targetDot.photonView.RPC("LockTravelDot", RpcTarget.AllBuffered, -1, false);
		storedDot.photonView.RPC("InstaLockUnlock", RpcTarget.AllBuffered, playerNo, true);
		targetDot = null;
		storedDot = null;
		doubleTapThreshold = 0;

		blinkCount--;
		gui.UpdateBlinkCount(blinkCount);

		// for the calling of the blink sfx

		SfxManager.inst.PlaySfx(SfxManager.inst.Sfx.blink);
	}

	//Delete the last position and cut out the Travel Line
	void CutLine() {
		if (!ReferenceEquals(targetDot, null)) currentTravelLine.photonView.RPC("CutLine", RpcTarget.AllBuffered);
		currentTravelLine = null;
	}

	void UpdateBlinkCd() {
		if (blinkCount < 3) {
			blinkCd -= Time.fixedDeltaTime; //0.002f
			gui.UpdateBlinkCd(blinkCd / maxCdTime); //BlinkCd will always go back to MaxCdTime. Hence need to Update Cd here

			if (blinkCd <= 0) {
				blinkCount++;
				blinkCd = maxCdTime;
				gui.UpdateBlinkCount(blinkCount);
			}
		}
	}

	public void InstantFillBlink()
	{
		if (blinkCount >= 3) return;

		blinkCount++;
		if (blinkCount >= 3) blinkCd = maxCdTime;
		gui.UpdateBlinkCount(blinkCount);
	}
	#endregion

	#region For Character Creation
	public void SetUpCharacter()
	{
		int presetIdx = PlayerPrefs.GetInt("Preset", 0);
		playerNo = PhotonNetwork.LocalPlayer.GetPlayerNumber();
		photonView.RPC("SetCharacterDisplay", RpcTarget.AllBuffered, playerNo, presetIdx);
	}

	[PunRPC]
	void SetCharacterDisplay(int playerNo, int presetIdx)
	{
		if (ReferenceEquals(gm, null)) gm = GameManager.inst;

		spr.sprite = gm.presets[presetIdx];
		spr.color = GameManager.GetCharacterColor(playerNo);
	}
	#endregion

	#region For Death
	public void Death(bool ignoreGameEnd = false) 
	{
		// to play the death sound of the player

		SfxManager.inst.PlaySfx(SfxManager.inst.Sfx.deathSound);

		Debug.LogError("Death is Called");

		SetCamShakeDuration();
		gameObject.SetActive(false);

		//Check Player Count. If Player Count <= 1. Trigger End Screen
		gm.playersAlive--;

		//Update Dead Players
		float timeSurvived = gm.totalTime;

		UIManager.inst.ShowPersonalResult(gm.playersAlive, timeSurvived); //Getting Prefix takes Array Index hence 1st = 0;
		UIManager.inst.SwitchToSpectateMode();

		gm.photonView.RPC("UpdateLeaderboard", RpcTarget.AllBuffered, PhotonNetwork.LocalPlayer.ActorNumber, timeSurvived);
		photonView.RPC("SendDeathEvent", RpcTarget.OthersBuffered, gm.playersAlive, ignoreGameEnd);

		ObjectPooling.inst.SpawnFromPool("Death Particles", transform.position, Quaternion.identity);

		if (ignoreGameEnd || gm.playersAlive > 1) return;

		gm.EndGame();
		UIManager.inst.UpdateLeaderboard();
	}

	[PunRPC]
	public void SendDeathEvent(int playersAlive, bool ignoreGameEnd) 
	{
		SfxManager.inst.PlaySfx(SfxManager.inst.Sfx.deathSound);
		gameObject.SetActive(false);
		gm.playersAlive = playersAlive;

		if (ignoreGameEnd || gm.playersAlive > 1) return;

		gm.EndGame();
		UIManager.inst.UpdateLeaderboard(); //Update Rankings each time Player dies
	}
	#endregion

	#region Cam Shake
	public void SetCamShakeDuration()
	{
		shakeDuration = setShakeDuration;
	}
	void CamShake()
	{
		if (shakeDuration >= 0)
		{
			gm.cam.transform.position += Random.insideUnitSphere * shakeAmount;
			shakeDuration -= Time.deltaTime * decreaseFactor;
		}
		else gm.cam.transform.position = gm.camPos;
	}
	#endregion

	#region For Player Identification
	[PunRPC]
	public void SetIdentiferColor()
	{
		//Assigning of Identifier at Start
		playerName.text = photonView.Owner.NickName;
		playerName.color = arrow.color = GameManager.GetCharacterColor(photonView.Owner.GetPlayerNumber());
	}

	[PunRPC]
	public void HideIdentifierAnim()
	{
		identifierAnim.SetTrigger("Hide");
	}

	void StopIdentifierAnim()
	{
		identifierAnim.SetBool("Hidden", true);
	}
	#endregion

	void OnTriggerEnter2D(Collider2D other) 
	{
		if (other.CompareTag("Player")) 
		{
			PlayerController detectedPlayer = other.GetComponent<PlayerController>();

			if (!ReferenceEquals(detectedPlayer, null)) {
				detectedPlayer.Death(true);
				Death();
			}
		}
	}

	public override void OnDisconnected(DisconnectCause cause)
	{
		if (!GameManager.inst.gameEnded)
		{
			CutLine();
			Death();
			Debug.LogError("Stuff Happened");
		}
	}
}
