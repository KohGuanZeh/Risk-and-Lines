using UnityEngine;

public class GameManagerSetup : MonoBehaviour
{
	[SerializeField] string gmPrefabName;

	private void Awake()
	{
		//Only Instantiate if it is Master Client
		if (GameManager.inst == null && Photon.Pun.PhotonNetwork.IsMasterClient)
		{
			GameObject gm = Photon.Pun.PhotonNetwork.InstantiateSceneObject(System.IO.Path.Combine("PhotonPrefabs", gmPrefabName), transform.position, Quaternion.identity);
			gm.transform.SetAsFirstSibling();
		}
	}
}
