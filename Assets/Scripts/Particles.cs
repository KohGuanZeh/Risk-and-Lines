﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Photon.Pun;
using Photon.Realtime;
using Photon.Pun.UtilityScripts;

public class Particles : MonoBehaviour, IPooledObject
{
	[SerializeField] string tag;
	[SerializeField] ParticleSystem[] particles; //The Particle to Check if it is Done
	[SerializeField] PhotonView photonView;

	#region Interface Functions
	[PunRPC]
	public void OnCreateObject()
	{
		ObjectPooling.Pool pool = ObjectPooling.inst.GetPool(GetPoolTag());
		transform.parent = pool.parent;
		gameObject.SetActive(false);
		particles = GetComponentsInChildren<ParticleSystem>();
	}

	[PunRPC]
	public void OnObjectSpawn(int parentId)
	{
		//Execute Spawn Functions Here
		//if (parentId > 0) gameObject.transform.parent = PhotonNetwork.GetPhotonView(parentId).transform; //If Parent is not Null, Set New Parent
		//else gameObject.transform.parent = ObjectPooling.inst.GetPool(GetPoolTag()).parent;

		foreach (ParticleSystem particle in particles)
		{
			ParticleSystem.MainModule module = particle.main;
			module.startColor = GameManager.GetCharacterColor(photonView.Owner.GetPlayerNumber());
			particle.gameObject.SetActive(true);
			particle.Play();
		}
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
		if (particles.Length > 0 && !particles[0].IsAlive(true) && photonView.IsMine) ObjectPooling.inst.ReturnToPool(gameObject, GetPoolTag());
	}

	/*public void SetParticleColor(int playerNo)
	{
		photonView.RPC("SendParticleColorChanges", RpcTarget.AllBuffered, playerNo);
	}

	[PunRPC]
	public void SendParticleColorChanges(int playerNo)
	{
		foreach (ParticleSystem particle in particles)
		{
			ParticleSystem.MainModule module = particle.main;
			module.startColor = GameManager.GetCharacterColor(playerNo);
			particle.gameObject.SetActive(true);
			particle.Play();
		}
	}*/
}
