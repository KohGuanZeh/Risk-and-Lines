using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

using Photon.Pun;
using Photon.Realtime;

public class UIManager : MonoBehaviour
{
    [Header("General Variables")]
    public static UIManager inst;
    [SerializeField] GameManager gm;
    [SerializeField] PlayerController player;

    [Header("GUI")]
    [SerializeField] Image blinkIconOverlay;
    [SerializeField] TextMeshProUGUI blinkCount;

    [Header("Spectate Mode")]
    [SerializeField] GameObject spectatorUI;
    [SerializeField] TextMeshProUGUI spectatingTxt;

    [Header("End Screen")]
    [SerializeField] GameObject endScreen;
    [SerializeField] GameObject spectateButton;

    private void Awake()
    {
        inst = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        gm = GameManager.inst;
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void AssignPlayerController(PlayerController player)
    {
        this.player = player;
    }

	#region For Blink
	public void UpdateBlinkCd(float cdRatio)
    {
        if (cdRatio < 0) cdRatio = 0;
        blinkIconOverlay.fillAmount = cdRatio;
    }

    public void UpdateBlinkCount(int count)
    {
        blinkCount.text = count.ToString();
    }
    #endregion

    #region For Spectating and End
    public void SwitchToSpectateMode(bool spectate)
    {
        spectatorUI.SetActive(spectate);
    }

    public void UpdateLeaderboard()
    {
        print("Updated");
    }

    public void ShowHideEndScreen(bool show)
    {
        endScreen.SetActive(show);
    }

    public void HideSpectateButton()
    {
        spectateButton.SetActive(false);
    }

    public void BackToWaitRoom()
    {
        SceneManager.LoadScene(0);
    }

    public void BackToLobby()
    {
        PhotonNetwork.LeaveRoom();
        SceneManager.LoadScene(0);
    }
	#endregion
}
