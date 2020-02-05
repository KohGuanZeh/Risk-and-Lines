using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Background : MonoBehaviour
{
    [SerializeField] float startPos, length, screenW, parallaxEffect;
    static float screenWidth;
    Camera cam;

    // Start is called before the first frame update
    void Start()
    {
        cam = GameManager.inst.cam;
        startPos = transform.position.x;
        length = GetComponent<SpriteRenderer>().bounds.size.x;
        
        if (screenWidth == 0) screenWidth = (2 * cam.orthographicSize) * cam.aspect; //2 * cam.orthographic = cam height. cam height * cam aspect = cam width
        screenW = screenWidth;
    }

    // Update is called once per frame
    void Update()
    {
        float offset = cam.transform.position.x * (1 - parallaxEffect); //How far Bg has moved relative to Camera
        float dist = cam.transform.position.x * parallaxEffect;
        transform.position = new Vector3(startPos + dist, transform.position.y, transform.position.z);

        //if (offset > startPos + length) startPos += length;
        //else if (offset < startPos - length) startPos -= length;
    }
}
