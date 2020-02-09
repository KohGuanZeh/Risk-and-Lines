using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamShake : MonoBehaviour
{
	public Animator camAnim;
	
	void Shake()
	{
		camAnim.SetTrigger("Shake");
	}
}
