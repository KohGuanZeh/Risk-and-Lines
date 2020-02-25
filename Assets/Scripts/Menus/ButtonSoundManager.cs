using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonSoundManager : MonoBehaviour
{
	public static ButtonSoundManager inst;

	public AudioClip buttonSound, startGameSound;
	AudioSource myAudio;

	private void Awake()
	{
		if (inst != null)
		{
			Destroy(this.gameObject);
		}
		else
		{
			inst = this;
			DontDestroyOnLoad(this.gameObject);
		}
	}

	private void Start()
	{
		myAudio = GetComponent<AudioSource>();
	}

	// to play the clip needed
	public void PlaySound(AudioClip clipToPlay)
	{
		myAudio.clip = clipToPlay;
		myAudio.Play();
	}
}
