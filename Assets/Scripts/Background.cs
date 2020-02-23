using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Special Thanks to Code Monkey for Tutorial
public class Background : MonoBehaviour
{
    GameManager gm;
    [SerializeField] Camera cam;

    [SerializeField] Vector2 parallaxMult;
    [SerializeField] Vector2 textureUnitSize;

    // Start is called before the first frame update
    void Start()
    {
        gm = GameManager.inst;
        cam = gm.cam;

        Sprite spr = GetComponent<SpriteRenderer>().sprite;
        Texture2D tex = spr.texture;
        //Gets the Original Sprite Size and how much 1 Tile of that Sprite takes up in Unity Units.
        textureUnitSize = new Vector2(tex.width / spr.pixelsPerUnit, tex.height / spr.pixelsPerUnit); 
    }

    // Update is called once per frame
    void LateUpdate()
    {
        //Vector3 moveDelta = cam.transform.position - lastCamPos; //Check how much the Cam Moved
        transform.position += new Vector3(gm.moveDelta.x * parallaxMult.x, gm.moveDelta.y * parallaxMult.y, 0); //Add Dist Cam Moved * Parallax to create Parallax Effect. Update new BG Position
        
        if (Mathf.Abs(cam.transform.position.x - transform.position.x) >= textureUnitSize.x) //If the Position has passed = that of 1 tile of the sprite, Teleport the Sprite
        {
            //Requires offset so the teleportation of Sprite Looks Seamless. Remainder gives how much more you need to move the Sprite in order for it to look seamless.
            float offset = (cam.transform.position.x - transform.position.x) % textureUnitSize.x; 
            transform.position = new Vector3(cam.transform.position.x + offset, transform.position.y, transform.position.z);
        }

        //Know that we are not changing Y axis but still good to add as Ref to Learn
        /*if (Mathf.Abs(cam.transform.position.y - transform.position.y) >= textureUnitSize.y)
        {
            float offset = (cam.transform.position.y - transform.position.y) % textureUnitSize.y;
            transform.position = new Vector3(transform.position.x, cam.transform.position.y + offset, transform.position.z);
        }*/
    }
}
