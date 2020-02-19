using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using TMPro;

using Photon.Pun;

public class MainMenu : MonoBehaviourPunCallbacks
{
	[Header("General")]
	[SerializeField] Animator anim;

	[Header("For Start Game")]
	[SerializeField] string playerName;
	[SerializeField] TMP_InputField nameInput;

	[Header("For Screen Content")]
	[SerializeField] GameObject instructions;
	[SerializeField] GameObject options;
	[SerializeField] GameObject credits;

	[Header("For Setting Audio")]
	[SerializeField] Slider[] sliders;
	[SerializeField] AudioMixer mixer;

	//Always Delete Lobby State when Player goes to Main Menu
	public void Start()
	{
		PlayerPrefs.DeleteKey("Lobby State");
		nameInput.text = playerName = PlayerPrefs.GetString("NickName", string.Empty);

		sliders[0].value = PlayerPrefs.GetFloat("Music Volume", 1);
		sliders[1].value = PlayerPrefs.GetFloat("Sound Volume", 1);
	}

	public override void OnConnectedToMaster()
	{
		//If Player's Input Name is Empty, Create Random Name for it. Delete Previous Name Saved
		if (string.IsNullOrEmpty(playerName) || string.IsNullOrWhiteSpace(playerName))
		{
			playerName = "Player_" + Random.Range(0, 100).ToString("000");
			PlayerPrefs.DeleteKey("NickName");
		}
		//Only Save Name when Player Inputs a Valid Name
		else PlayerPrefs.SetString("NickName", playerName); 

		//Set Player's Input Name to Photon Player before going to Lobby
		PhotonNetwork.NickName = playerName;

		SceneManager.LoadScene(1);
	}

	public void StartGame(bool show)
	{
		anim.SetBool("Show Name Input", show);
	}

	public void OnNameSubmit()
	{
		//Connect to Master when Player Submits a Name. On Connected to Master, then Load Lobby Scene.
		playerName = nameInput.text;
		PhotonNetwork.ConnectUsingSettings();
	}

	public void Instructions()
	{
		instructions.SetActive(true);
		options.SetActive(false);
		credits.SetActive(false);

		ShowHideContentScreen(true);
	}

	public void Options()
	{
		options.SetActive(true);
		instructions.SetActive(false);
		credits.SetActive(false);

		ShowHideContentScreen(true);
	}

	public void Credits()
	{
		credits.SetActive(true);
		instructions.SetActive(false);
		options.SetActive(false);

		ShowHideContentScreen(true);
	}

	public void ShowHideContentScreen(bool show)
	{
		anim.SetBool("Show Content", show);
	}

	public void Quit()
	{
		PlayerPrefs.DeleteKey("Lobby State");
		Application.Quit();
	}

	public void SetMusicVolume(float val)
	{
		mixer.SetFloat("Music Volume", Mathf.Log10(val) * 20);
		PlayerPrefs.SetFloat("Music Volume", val);
	}

	public void SetSoundVolume(float val)
	{
		mixer.SetFloat("Sound Volume", Mathf.Log10(val) * 20);
		PlayerPrefs.SetFloat("Sound Volume", val);
	}
}
