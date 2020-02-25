using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Photon.Pun;
using Photon.Realtime;

public class Particles : MonoBehaviour, IPooledObject
{
	[SerializeField] string tag;
	[SerializeField] ParticleSystem[] particles; //The Particle to Check if it is Done

	#region Interface Functions
	[PunRPC]
	public void OnCreateObject()
	{
		ObjectPooling.Pool pool = ObjectPooling.inst.GetPool(GetPoolTag());

		ObjectPooling.inst.poolDictionary[GetPoolTag()].Enqueue(gameObject);
		transform.parent = pool.parent;
		particles = GetComponentsInChildren<ParticleSystem>();
		gameObject.SetActive(false);
	}

	[PunRPC]
	public void OnObjectSpawn(int parentId)
	{
		//Execute Spawn Functions Here
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
		return tag;
	}
	#endregion

	void Update()
	{
		if (!particles[0].IsAlive(true)) ObjectPooling.inst.ReturnToPool(gameObject, GetPoolTag());
	}

	[PunRPC]
	public void SetParticleColor(int playerNo)
	{
		foreach (ParticleSystem particle in particles) particle.startColor = GameManager.GetCharacterColor(playerNo);
	}
}
