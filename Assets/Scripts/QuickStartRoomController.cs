using Photon.Pun;
using UnityEngine;

public class QuickStartRoomController : MonoBehaviourPunCallbacks
{
	//public Transform Grid;
	//public GameObject RoomNamePrefab;

	[SerializeField]
	private int multiplayerSceneIndex;

	public override void OnEnable()
	{
		PhotonNetwork.AddCallbackTarget(this);
	}

	public override void OnDisable()
	{
		PhotonNetwork.RemoveCallbackTarget(this);
	}

	public override void OnJoinedRoom()
	{
		//base.OnJoinedRoom();
		StartGame();
	}
	
	private void StartGame()
	{
		if (PhotonNetwork.IsMasterClient)
		{
			Debug.Log("starting game");
			PhotonNetwork.LoadLevel(multiplayerSceneIndex);  
		}
	}

}
