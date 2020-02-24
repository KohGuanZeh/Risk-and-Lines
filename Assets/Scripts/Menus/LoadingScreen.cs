using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Photon.Pun;
using Photon.Realtime;

public class LoadingScreen : MonoBehaviour
{
	public static LoadingScreen inst;

	[Header("Loading Screen Properties")]
	[SerializeField] GameObject loadingScreen;
	[SerializeField] int sceneToLoad;
	[SerializeField] Animator anim;

	public bool fadingInProgress;
	public bool isLoading;
	public bool canFadeOut;
	public delegate void FadeEvent();
	public FadeEvent OnFadeIn, OnFadeOut;

	private void Awake()
	{
		if (inst) Destroy(gameObject);
		else
		{
			inst = this;
			DontDestroyOnLoad(gameObject);
		}
	}

	private void Update()
	{
		if (isLoading && PhotonNetwork.LevelLoadingProgress >= 1f && canFadeOut)
		{
			anim.SetBool("Fade In", false);
			isLoading = false;
			canFadeOut = false;
		}
	}

	public void LoadScene(int sceneIdx, bool fadeOutOnLoaded = true)
	{
		sceneToLoad = sceneIdx;
		isLoading = true;
		fadingInProgress = true;
		loadingScreen.SetActive(true);
		anim.SetBool("Fade In", true);
		anim.SetBool("Loading", true);

		if (fadeOutOnLoaded) StartCoroutine(AllowReceiveAsyncProgress());
	}

	//Problem is that Level Loading Progress Remains at 1 when Level Loading is done and cannot be Set Manually
	IEnumerator AllowReceiveAsyncProgress()
	{
		yield return new WaitForSeconds(0.5f);
		canFadeOut = true;
	}

	public void OnFadeInAnimationEvent()
	{
		if (PhotonNetwork.CurrentRoom != null && PhotonNetwork.IsMasterClient) PhotonNetwork.LoadLevel(sceneToLoad);
		else PhotonNetwork.LoadLevel(sceneToLoad);

		if (OnFadeIn != null) OnFadeIn();
	}

	public void OnFadeOutAnimationEvent()
	{
		fadingInProgress = false;
		loadingScreen.SetActive(false);
		anim.SetBool("Loading", false);
		if (OnFadeOut != null) OnFadeOut();
	}
}
