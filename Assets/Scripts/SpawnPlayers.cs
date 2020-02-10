using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class SpawnPlayers : MonoBehaviourPunCallbacks
{
	private PhotonView pv;
	public List<Player> playerInts;
	private void Start()
	{
		pv = GetComponent<PhotonView>();
	//	playerInts = new List<int>();
		print(pv.ViewID);
	//	playerInts = PhotonNetwork.PlayerList;
	}

	public override void OnJoinedLobby()
	{
	//	playerInts.Add(GetComponent<PhotonView>().ViewID);
	}
}
