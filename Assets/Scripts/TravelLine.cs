using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TravelLine : MonoBehaviour, IPooledObject
{
	[SerializeField] Player playerRef;
	[SerializeField] LineRenderer line;
	[SerializeField] EdgeCollider2D collider;
	[SerializeField] Vector2[] dotPos;

	#region Interface Functions
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
	#endregion

	#region Line Renderer Related Functions
	public void CreateNewLine(Vector3 pos)
	{
		line.positionCount = 2;
		for (int i = 0; i < line.positionCount; i++) line.SetPosition(i, pos);
		UpdateDotPosArray();
	}

	public void AddNewPoint(Vector3 pos)
	{
		line.positionCount++;
		line.SetPosition(line.positionCount - 1, pos);
		UpdateDotPosArray();
	}

	public void UpdateLine(Vector3 pos)
	{
		line.SetPosition(line.positionCount - 1, pos);
		collider.points[line.positionCount - 1] = transform.InverseTransformPoint(pos);
	}

	public void CutLine()
	{
		line.positionCount--;
		UpdateDotPosArray();
		if (line.positionCount == 1) ObjectPooling.inst.ReturnToPool(gameObject, GetPoolTag());
	}

	void UpdateDotPosArray()
	{
		dotPos = new Vector2[line.positionCount];
		for (int i = 0; i < dotPos.Length; i++) dotPos[i] = transform.InverseTransformPoint(line.GetPosition(i));
		collider.points = dotPos;
	}
	#endregion

	#region Trigger Functions
	void OnTriggerEnter2D(Collider2D other)
	{
		if (other.tag == "Player")
		{
			Player detectedPlayer = GetComponent<Player>();
			if (detectedPlayer != playerRef) detectedPlayer.Death();
		}
	}
	#endregion
}
