using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Photon.Pun;

public class TravelDot : MonoBehaviour, IPooledObject
{
	[Header("Travel Dot Properties")]
	public bool locked; //Check if Dot is already being locked by a Player
	[SerializeField] PhotonView photonView;

	public void OnObjectSpawn()
	{
		locked = false;
		photonView.RPC("EnqueueOnSpawn", RpcTarget.All);
	}

	public void OnObjectDespawn()
	{
		
	}

	public string GetPoolTag()
	{
		return "Dots";	
	}

	[PunRPC] //Upon Dequeue, Enqueue. So that there is no need to Check Position every Update to Despawn Dots
	void EnqueueOnSpawn()
	{ 
		ObjectPooling.inst.poolDictionary[GetPoolTag()].Enqueue(gameObject);
	}
}
