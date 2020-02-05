using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TravelLine : MonoBehaviour, IPooledObject
{
	[SerializeField] LineRenderer line;
	[SerializeField] PolygonCollider2D collider;

	public void OnObjectSpawn()
	{

	}

	public void OnObjectDespawn()
	{

	}

	public string GetPoolTag()
	{
		return "Line";
	}
}
