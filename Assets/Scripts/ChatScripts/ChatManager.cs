using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// photon namespaces
using Photon.Pun;
using Photon.Realtime;
public class ChatManager : MonoBehaviourPun {
	// variables for the chat
	Player player;
	public InputField chatInput;
	public Transform chatContainer; // to hold all the chat messages
	public GameObject textPrefab;
	public RectTransform chatRect;

	private void Update() {
		if (Input.GetKeyDown(KeyCode.KeypadEnter)) {
			deliverMsg();
		}
	}
	public void deliverMsg() {
		photonView.RPC("SendMsg", RpcTarget.All, PhotonNetwork.NickName + ": " + chatInput.text); // to send the others this message
	}

	[PunRPC]
	//sends the message to the chat box
	void SendMsg(string msg) {
		if (msg != PhotonNetwork.NickName + ": " + "" && msg.Length > 1) {
			GameObject textObj = PhotonNetwork.Instantiate(System.IO.Path.Combine("PhotonPrefabs", "MessagePrefab"), Vector2.zero, Quaternion.identity);
			//GameObject textObj = Instantiate(textPrefab,chatContainer);
			textObj.GetComponent<Text>().text = msg; // sets the text of the prefab and acts as a message
			textObj.transform.parent = chatContainer;
			textObj.transform.localScale = new Vector3(1, 1, 1);

			chatRect.sizeDelta = new Vector2 (chatRect.sizeDelta.x,chatRect.sizeDelta.y + textObj.GetComponent<RectTransform>().sizeDelta.y);
			chatInput.text = ""; // clears the text inputtext after the message has been sent
		}
	}

}
