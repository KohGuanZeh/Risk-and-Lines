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
	[SerializeField] bool canReceiveAsyncProgress;
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
		if (isLoading && PhotonNetwork.LevelLoadingProgress >= 1f && canReceiveAsyncProgress)
		{
			anim.SetBool("Fade In", false);
			isLoading = false;
			canReceiveAsyncProgress = false;
		}
	}

	public void LoadScene(int sceneIdx)
	{
		sceneToLoad = sceneIdx;
		isLoading = true;
		fadingInProgress = true;
		loadingScreen.SetActive(true);
		anim.SetBool("Fade In", true);
		anim.SetBool("Loading", true);

		StartCoroutine(AllowReceiveAsyncProgress());
	}

	//Problem is that Level Loading Progress Remains at 1 when Level Loading is done and cannot be Set Manually
	IEnumerator AllowReceiveAsyncProgress()
	{
		yield return new WaitForSeconds(0.5f);
		canReceiveAsyncProgress = true;
	}

	public void OnFadeInAnimationEvent()
	{
		PhotonNetwork.LoadLevel(sceneToLoad);
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
