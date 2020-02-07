
using UnityEngine;
using Photon.Pun;
using System.IO;

public class GameSetUpController : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
		CreatePlayer();
    }

   private void CreatePlayer()
	{
		Debug.Log("Creating Player");
		PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "Player"), Vector3.zero, Quaternion.identity);
	}
}
