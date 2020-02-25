using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Photon.Pun;

public class TravelDot : MonoBehaviour, IPooledObject
{
	[Header("Travel Dot Properties")]
	[SerializeField] SpriteRenderer[] sprs;
	[SerializeField] float lockedAlpha;
	[SerializeField] Animator anim;
	public Color defaultColor;

	public bool locked; //Check if Dot is already being locked by a Player
	public PhotonView photonView;

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
		gameObject.SetActive(true);

		//Execute Spawn Functions Here
		/*foreach (SpriteRenderer spr in sprs) spr.color = defaultColor;
		locked = false;
		anim.Play("Travel Dot Unlock", 0, 1); //So that Animation wont Play when Dot first appears;
		anim.SetBool("Locked", locked);*/

		InstaLockUnlock(-1, false);
		ObjectPooling.inst.poolDictionary[GetPoolTag()].Enqueue(gameObject);

		if (parentId > 0) gameObject.transform.parent = PhotonNetwork.GetPhotonView(parentId).transform; //If Parent is not Null, Set New Parent
		else gameObject.transform.parent = ObjectPooling.inst.GetPool(GetPoolTag()).parent;
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

	[PunRPC]
	void LockTravelDot(int playerNo, bool lockDot)
	{
		locked = lockDot;
		anim.SetBool("Locked", locked);

		if (locked) foreach (SpriteRenderer spr in sprs) spr.color = GameManager.GetCharacterColor(playerNo, lockedAlpha);
		else foreach (SpriteRenderer spr in sprs) spr.color = defaultColor; //In case we do not want White
	}

	[PunRPC]
	void InstaLockUnlock(int playerNo, bool lockDot)
	{
		locked = lockDot;
		anim.SetBool("Locked", locked);
		anim.Play(locked ? "Travel Dot Lock" : "Travel Dot Unlock", 0 ,1);

		if (locked) foreach (SpriteRenderer spr in sprs) spr.color = GameManager.GetCharacterColor(playerNo, lockedAlpha);
		else foreach (SpriteRenderer spr in sprs) spr.color = defaultColor; //In case we do not want White
	}
}
