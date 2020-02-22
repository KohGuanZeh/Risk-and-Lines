using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using TMPro;

using Photon.Pun;
using Photon.Realtime;

[System.Serializable]
public struct RankCardItems
{
    public Image playerSpr;
    public TextMeshProUGUI playerName;
    public TextMeshProUGUI rank;
    public TextMeshProUGUI time;
}

public class UIManager : MonoBehaviour
{
    [Header("General Variables")]
    public static UIManager inst;
    [SerializeField] GameManager gm;
    [SerializeField] PlayerController player;
    [SerializeField] Animator anim;

    [Header("GUI")]
    [SerializeField] Image blinkIconOverlay;
    [SerializeField] TextMeshProUGUI blinkCount;

    [Header("Spectate Mode")]
    [SerializeField] GameObject spectatorUI;
    [SerializeField] TextMeshProUGUI spectatingTxt;

    [Header("End Screen")]
    [SerializeField] GameObject endScreen;
    [SerializeField] Sprite[] charSprites;
    [SerializeField] RankCardItems[] rankCards;

    private void Awake()
    {
        inst = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        for (int i = 3; i >= PhotonNetwork.CurrentRoom.PlayerCount - 1; i--)
        {
            rankCards[i].playerSpr.gameObject.SetActive(false);
            rankCards[i].playerName.text = "Player: -";
            rankCards[i].rank.text = "Rank: -";
            rankCards[i].time.text = "Time: -";
        }

        gm = GameManager.inst;
        PhotonNetwork.AutomaticallySyncScene = false;
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
    public void SwitchToSpectateMode()
    {
        anim.SetBool("Spectate", true);
        anim.SetBool("Show Button", true);
    }

    public void UpdateLeaderboard()
    {
        for (int i = gm.playerInfos.Length - 1; i >= 0; i--)
        {
            if (gm.playerInfos[i].deathTime < 0) gm.playerInfos[i].deathTime = gm.playerInfos[0].deathTime + 1;
            else break;
        }

        System.Array.Sort(gm.playerInfos, (x, y) => y.deathTime.CompareTo(x.deathTime));

        for (int i = 0; i < gm.playerInfos.Length; i++)
        {
            rankCards[i].playerSpr.sprite = charSprites[gm.playerInfos[i].charSpr];
            rankCards[i].playerName.text = string.Format("Player: {0}", gm.playerInfos[i].playerName);
            rankCards[i].rank.text = string.Format("Rank: {0}", GetRankPrefix(i));
            rankCards[i].time.text = string.Format("Time: {0}", i == 0 ? gm.playerInfos[i + 1].deathTime.ToString("0.00") + "++" : gm.playerInfos[i].deathTime.ToString("0.00"));
        }
    }

    public void ShowEndScreen()
    {
        anim.SetBool("Game Ended", true);
        anim.SetBool("Show Button", true);
    }

    //Initially Planned to have Rematch but Proved to be too Difficult
    public void BackToWaitRoom()
    {
        PlayerPrefs.SetInt("Lobby State", 2);
        PhotonNetwork.LoadLevel(0);
    }

    public void BackToLobby()
    {
        PlayerPrefs.SetInt("Lobby State", 1);
        PhotonNetwork.LoadLevel(0);
    }
    #endregion

    public string GetRankPrefix(int arrIdx)
    {
        switch (arrIdx)
        {
            case 0:
                return "1st";
            case 1:
                return "2nd";
            case 2:
                return "3rd";
            default:
                return string.Format("{0}th", arrIdx + 1);
        }
    }
}
