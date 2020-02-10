using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnSetup : MonoBehaviour
{
	public static SpawnSetup instance;

	public Transform[] spawnPoint;

	public void OnEnable()
	{
		instance = this;
	}
}
