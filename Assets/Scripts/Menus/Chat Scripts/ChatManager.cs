using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
public class ChatManager : MonoBehaviourPun {

	// variables for the chat
	public static ChatManager inst;
	public TMP_InputField chatInput;
	public RectTransform chatContainer; // to hold all the chat messages

	[Header("For Chat Size")]
	[SerializeField] List<TextMeshProUGUI> texts;
	public float chatSize;

	private void Awake()
	{
		inst = this;
		chatSize = 0;
	}

	private void Update() 
	{
		if (Input.GetKeyDown(KeyCode.KeypadEnter)) DeliverMsg();
	}

	public void DeliverMsg() 
	{
		photonView.RPC("SendMsg", RpcTarget.All, PhotonNetwork.NickName, chatInput.text); // to send the others this message
		chatInput.text = string.Empty; // clears the text inputtext after the message has been sent
	}

	[PunRPC]
	//Sends the message to the Chat Box
	void SendMsg(string nickname, string msg) 
	{
		if (!string.IsNullOrEmpty(msg) && !string.IsNullOrWhiteSpace(msg)) 
		{
			GameObject textObj = PhotonNetwork.Instantiate(System.IO.Path.Combine("PhotonPrefabs", "Message Prefab"), Vector2.zero, Quaternion.identity);

			TextMeshProUGUI text = textObj.GetComponent<TextMeshProUGUI>(); // sets the text of the prefab and acts as a message
			text.text = string.Format("{0}: {1}", nickname, msg);

			textObj.transform.parent = chatContainer;
			textObj.transform.localScale = Vector3.one;

			texts.Add(text);

			StartCoroutine(ChangeChatSize(text));
		}
	}

	[PunRPC]
	void SendAutomatedMsg(string msg, int playerNo)
	{
		if (!string.IsNullOrEmpty(msg) && !string.IsNullOrWhiteSpace(msg))
		{
			GameObject textObj = PhotonNetwork.Instantiate(System.IO.Path.Combine("PhotonPrefabs", "Message Prefab"), Vector2.zero, Quaternion.identity);

			TextMeshProUGUI text = textObj.GetComponent<TextMeshProUGUI>(); // sets the text of the prefab and acts as a message
			text.text = msg;
			text.color = GameManager.GetCharacterColor(playerNo);

			textObj.transform.SetParent(chatContainer);
			textObj.transform.localScale = Vector3.one;

			texts.Add(text);

			StartCoroutine(ChangeChatSize(text));
		}
	}

	void EditChatSize(TextMeshProUGUI text) 
	{
		chatSize += text.rectTransform.sizeDelta.y;
		if (chatSize >= chatContainer.sizeDelta.y) chatContainer.sizeDelta = new Vector2(chatContainer.sizeDelta.x, chatSize + 10);
		if (chatSize > 495) chatContainer.anchoredPosition = new Vector2(chatContainer.anchoredPosition.x, chatSize - 495);
	}

	IEnumerator ChangeChatSize(TextMeshProUGUI text) 
	{
		yield return new WaitForSeconds(.1f);
		EditChatSize(text);
	}

	public void ClearChat()
	{
		chatSize = 0;
		foreach (TextMeshProUGUI text in texts) Destroy(text.gameObject);
	}
}
