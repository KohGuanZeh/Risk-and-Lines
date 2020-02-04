using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TravelDot : MonoBehaviour
{
	[Header("Travel Dot Properties")]
	public bool locked; //Check if Dot is already being locked by a Player

	public Player currentPlayer;
	public int blinkCount;
}
