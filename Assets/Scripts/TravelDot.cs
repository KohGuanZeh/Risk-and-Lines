using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TravelDot : MonoBehaviour, IPooledObject
{
	[Header("Travel Dot Properties")]
	public bool locked; //Check if Dot is already being locked by a Player

	public void OnObjectSpawn()
	{
		locked = false;
		ObjectPooling.inst.poolDictionary[GetPoolTag()].Enqueue(gameObject); //Upon Dequeue, Enqueue. So that there is no need to Check Position every Update to Despawn Dots
	}

	public void OnObjectDespawn()
	{
		
	}

	public string GetPoolTag()
	{
		return "Dots";	
	}
}
