using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    [Header ("General Player Properties")]
    [SerializeField] LineRenderer linePreset; //Stores the Line Prefab that it will Instantiate each time Player travels

    [Header("Player Movement Properties")]
    [SerializeField] bool isTravelling; //Check if Player is Travelling
    [SerializeField] float travelSpeed; //Speed at which the Player Dot travels
    [SerializeField] Vector2 travelDir; //Direction at which Player should travel
    [SerializeField] TravelDot targetDot; //Target Dot at which Player would go to

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.touchCount > 0)
        {
            
        }
    }

    void StartTravelTo(TravelDot dot)
    {
        if (dot.locked) return;
        isTravelling = true;
    }

    void BlinkTo(TravelDot dot)
    {
        if (dot.locked) return;
        transform.position = dot.transform.position;
    }
}
