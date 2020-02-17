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
    [SerializeField] TextMeshProUGUI leaderboard;

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
        for (int i = gm.playerInfos.Length - 1; i >= 0; i--)
        {
            if (gm.playerInfos[i].deathTime < 0) gm.playerInfos[i].deathTime = gm.playerInfos[0].deathTime + 1;
            else break;
        }

        System.Array.Sort(gm.playerInfos, (x, y) => y.deathTime.CompareTo(x.deathTime));

        string text = string.Empty;

        for (int i = 0; i < gm.playerInfos.Length; i++)
        {
            text += string.Format("{0}. {1} Time Survived: {2} <br>", i + 1, gm.playerInfos[i].playerName, gm.playerInfos[i].deathTime);
        }

        leaderboard.text = text;
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
