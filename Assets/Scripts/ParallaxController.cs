using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParallaxController : MonoBehaviour
{
    private float parallaxSpeed;
    private float currentParallaxSpeed;

    private Vector2 backgroundSize;

    //Tiles the width of the background to give it room to parallax
    private float tileMultiplier = 3;

    private PlayerTankController playerTankController;

    // Start is called before the first frame update
    void Start()
    {
        backgroundSize = GetComponent<SpriteRenderer>().bounds.size;
        GetComponent<SpriteRenderer>().size = new Vector2(backgroundSize.x * tileMultiplier, backgroundSize.y);

        //Get the speed of the player tank
        playerTankController = GameObject.FindGameObjectWithTag("PlayerTank").GetComponent<PlayerTankController>();
        parallaxSpeed = -(playerTankController.GetPlayerSpeed());
        currentParallaxSpeed = parallaxSpeed;
    }

    // Update is called once per frame
    void Update()
    {
        //Get the speed of the player tank
        if(GameObject.FindGameObjectWithTag("PlayerTank") != null)
        {
            parallaxSpeed = -(playerTankController.GetPlayerSpeed());
        }

        currentParallaxSpeed = parallaxSpeed * LevelManager.instance.gameSpeed;

        if (LevelManager.instance.hasFuel)
        {
            //Moving backwards
            if (currentParallaxSpeed > 0)
            {
                //Constantly move the background
                transform.position += new Vector3(currentParallaxSpeed * Time.deltaTime, transform.position.y);

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
                transform.position += new Vector3(currentParallaxSpeed * Time.deltaTime, transform.position.y);

                //If the background moves past the original background size, move the background back to its original position
                if (transform.position.x < -backgroundSize.x)
                {
                    transform.position = new Vector3(backgroundSize.x, transform.position.y, transform.position.z);
                }
            }
        }
    }
}
