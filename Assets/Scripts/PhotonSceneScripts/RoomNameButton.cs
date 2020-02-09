using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RoomNameButton : MonoBehaviour
{
	public Text RoomName;

	// Use this for initialization
	void Start()
	{

		Button btn = GetComponent<Button>();

		btn.onClick.AddListener(() => JoinRoomByName(RoomName.text));
	}


	public void JoinRoomByName(string RoomName)
	{
		PhotonNetwork.JoinRoom(RoomName);
	}

}
