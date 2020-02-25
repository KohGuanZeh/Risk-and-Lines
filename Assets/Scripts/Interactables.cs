using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Photon.Pun;
using Photon.Realtime;

public class Interactables : MonoBehaviour, IPooledObject
{
	[SerializeField] string tag;

	#region Interface Functions
	[PunRPC]
	public void OnCreateObject()
	{
		ObjectPooling.Pool pool = ObjectPooling.inst.GetPool(GetPoolTag());

		ObjectPooling.inst.poolDictionary[GetPoolTag()].Enqueue(gameObject);
		transform.parent = pool.parent;
		gameObject.SetActive(false);
	}

	[PunRPC]
	public void OnObjectSpawn(int parentId)
	{
		//Execute Spawn Functions Here
		if (parentId > 0) gameObject.transform.parent = PhotonNetwork.GetPhotonView(parentId).transform; //If Parent is not Null, Set New Parent
		else gameObject.transform.parent = ObjectPooling.inst.GetPool(GetPoolTag()).parent;
		ObjectPooling.inst.poolDictionary[GetPoolTag()].Enqueue(gameObject);

		gameObject.SetActive(true);
	}

	[PunRPC]
	public void OnObjectDespawn()
	{
		gameObject.SetActive(false);
	}

	public string GetPoolTag()
	{
		return tag;
	}
	#endregion

	public void OnTriggerEnter2D(Collider2D other)
	{
		if (other.CompareTag("Player"))
		{
			switch (tag)
			{
				case "Difficulty":
					GameManager.inst.IncreaseDifficulty();
					break;
				case "Blink":
					other.GetComponent<PlayerController>().InstantFillBlink();
					break;
			}
		}

		ObjectPooling.inst.ReturnToPool(gameObject, tag);
	}
}
