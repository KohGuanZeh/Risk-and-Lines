using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Photon.Pun;

public class TravelDot : MonoBehaviour, IPooledObject
{
	[Header("Travel Dot Properties")]
	public bool locked; //Check if Dot is already being locked by a Player

	[PunRPC]
	public void OnCreateObject()
	{
		ObjectPooling.Pool pool = ObjectPooling.inst.GetPool(GetPoolTag());

		ObjectPooling.inst.poolDictionary[GetPoolTag()].Enqueue(gameObject);
		transform.parent = pool.parent;
		print(ObjectPooling.inst.poolDictionary[GetPoolTag()].Count);
		gameObject.SetActive(false);
	}

	[PunRPC]
	public void OnObjectSpawn(int parentId)
	{
		//Execute Spawn Functions Here
		locked = false;

		if (parentId > 0) gameObject.transform.parent = PhotonNetwork.GetPhotonView(parentId).transform; //If Parent is not Null, Set New Parent
		else gameObject.transform.parent = ObjectPooling.inst.GetPool(GetPoolTag()).parent;

		gameObject.SetActive(true);
	}

	[PunRPC]
	public void OnObjectDespawn()
	{
		//Execute Despawn Functions Here

		ObjectPooling.inst.poolDictionary[GetPoolTag()].Enqueue(gameObject);
		gameObject.SetActive(false);
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
