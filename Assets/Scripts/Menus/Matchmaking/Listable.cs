using UnityEngine;
using UnityEngine.UI;

public class Listable : MonoBehaviour
{
	[Header ("Listable Items")]
	public RectTransform rect;
	public Animator anim;

	public virtual void OnListRemove()
	{
		
	}
}
