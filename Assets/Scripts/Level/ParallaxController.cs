using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParallaxController : MonoBehaviour
{
    [SerializeField] private float parallaxSpeed = 1;
    private float relativeParallaxSpeed;
    private float currentParallaxSpeed;

    private Vector2 backgroundSize;

    //Tiles the width of the background to give it room to parallax
    private float tileMultiplier = 3;

    private PlayerTankController playerTank;

    // Start is called before the first frame update
    void Start()
    {
        backgroundSize = GetComponent<SpriteRenderer>().bounds.size;
        //Debug.Log("Background Size: " + backgroundSize);
        GetComponent<SpriteRenderer>().size = new Vector2(backgroundSize.x * tileMultiplier, GetComponent<SpriteRenderer>().size.y);

        //Get the speed of the player tank
        playerTank = GameObject.FindGameObjectWithTag("PlayerTank").GetComponent<PlayerTankController>();
        relativeParallaxSpeed = -(playerTank.GetBasePlayerSpeed()) * parallaxSpeed;
        currentParallaxSpeed = relativeParallaxSpeed;
    }

    // Update is called once per frame
    void Update()
    {
        //Get the speed of the player tank
        if(GameObject.FindGameObjectWithTag("PlayerTank") != null)
        {
            relativeParallaxSpeed = -(playerTank.GetBasePlayerSpeed()) * parallaxSpeed;
        }

        currentParallaxSpeed = relativeParallaxSpeed * playerTank.GetThrottleMultiplier();

        //Moving backwards
        if (currentParallaxSpeed > 0)
        {
            //Constantly move the background
            transform.position += new Vector3(currentParallaxSpeed * Time.deltaTime, 0);

            //If the background moves past the original background size, move the background back to its original position
            if (transform.position.x > backgroundSize.x)
            {
                transform.position = new Vector3(-backgroundSize.x, transform.position.y, transform.position.z);
            }
        }

        //Moving forwards
        else
        {
            //Constantly move the background
            transform.position += new Vector3(currentParallaxSpeed * Time.deltaTime, 0);

            //If the background moves past the original background size, move the background back to its original position
            if (transform.position.x < -backgroundSize.x)
            {
                transform.position = new Vector3(backgroundSize.x, transform.position.y, transform.position.z);
            }
        }
    }
}
