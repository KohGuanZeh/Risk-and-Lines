using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SfxManager : MonoBehaviour
{
	[System.Serializable]
	public struct SFXSounds
	{
		public AudioClip blink;
		public AudioClip deathSound;
		public AudioClip lockSound;
		public AudioClip unlockSound;
		public AudioClip travelSound;
	}

	public static SfxManager inst;

	public SFXSounds Sfx;
	public AudioSource myAudio;

	private void Start()
	{
		//setting of the singleton
		if (inst != null) Destroy(this.gameObject);
		else
		{
			inst = this;
		}

		// accessing components
		myAudio = GetComponent<AudioSource>();
	}

	// to play the clip needd
	public void PlaySfx(AudioClip clipToPlay)
	{
		myAudio.PlayOneShot(clipToPlay, 1.0f);
	}

}
