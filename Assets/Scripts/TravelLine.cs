using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Photon.Pun;
using Photon.Pun.UtilityScripts;

public class TravelLine : MonoBehaviour, IPooledObject
{
	public int playerRefId;
	public PhotonView photonView;
	[SerializeField] LineRenderer line;
	[SerializeField] EdgeCollider2D collider;
	[SerializeField] Vector2[] dotPos;
	[SerializeField] float greatestX;

	#region Interface Functions
	[PunRPC]
	public void OnCreateObject()
	{
		ObjectPooling.Pool pool = ObjectPooling.inst.GetPool(GetPoolTag());
		transform.parent = pool.parent;
		gameObject.SetActive(false);
	}

	[PunRPC]
	public void OnObjectSpawn(int parentId)
	{
		playerRefId = -1;

		//Not Setting Parent when Travel Line Spawns
		//if (parentId > 0) gameObject.transform.parent = PhotonNetwork.GetPhotonView(parentId).transform; //If Parent is not Null, Set New Parent
		//else gameObject.transform.parent = ObjectPooling.inst.GetPool(GetPoolTag()).parent;

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
		return "Line";
	}
	#endregion

	#region Line Renderer Related Functions
	[PunRPC]
	public void CreateNewLine(int viewId, int playerNo, Vector3 pos)
	{
		playerRefId = viewId;

		line.positionCount = 2;
		line.startColor = line.endColor = GameManager.GetCharacterColor(playerNo , 0.8f);
		for (int i = 0; i < line.positionCount; i++) line.SetPosition(i, pos);
		greatestX = pos.x;
		UpdateDotPosArray();
	}

	[PunRPC]
	public void AddNewPoint(Vector3 pos)
	{
		line.positionCount++;
		line.SetPosition(line.positionCount - 1, pos);
		UpdateDotPosArray();
	}

	[PunRPC]
	public void UpdateLine(Vector3 pos)
	{
		line.SetPosition(line.positionCount - 1, pos);
		if (pos.x > greatestX) greatestX = pos.x;
		UpdateDotPosArray();
		//collider.points[line.positionCount - 1] = transform.InverseTransformPoint(pos); //Doesnt Work
	}

	[PunRPC]
	public void OnFinishedTravel(float xPos)
	{
		if (xPos > greatestX) greatestX = xPos;
	}

	[PunRPC]
	public void CutLine()
	{
		line.positionCount--;
		UpdateDotPosArray();
		if (line.positionCount == 1) ObjectPooling.inst.ReturnToPool(gameObject, GetPoolTag());
	}

	[PunRPC]
	void UpdateDotPosArray()
	{
		dotPos = new Vector2[line.positionCount];
		for (int i = 0; i < dotPos.Length; i++) dotPos[i] = transform.InverseTransformPoint(line.GetPosition(i));
		collider.points = dotPos;
		//foreach (Vector2 pt in collider.points) Debug.LogError(pt);
	}
	#endregion

	#region Update and Trigger Functions
	void Update()
	{
		if (GameManager.inst.CamLeftBounds > greatestX) ObjectPooling.inst.ReturnToPool(gameObject, GetPoolTag());
	}

	void OnTriggerEnter2D(Collider2D other)
	{
		if (other.CompareTag("Player"))
		{
			PlayerController detectedPlayer = other.GetComponent<PlayerController>();
			if (!ReferenceEquals(detectedPlayer, null) && playerRefId != detectedPlayer.photonView.ViewID)
			{
				detectedPlayer.Death();
				Debug.LogError(string.Format("Detected View ID and Name: {0}, {1}. PlayerRefId: {2}.", detectedPlayer.photonView.ViewID.ToString(), detectedPlayer.photonView.Owner.NickName, playerRefId));
			} 
		}
	}
	#endregion
}
