using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParallaxController : MonoBehaviour
{
    [SerializeField, Tooltip("The speed of the parallax background when the player moves.")] private float parallaxSpeed = 1;
    [SerializeField, Tooltip("The speed for parallax layer to move without player movement.")] private float automaticSpeed = 0;

    private float currentParallaxSpeed;
    private Vector2 backgroundSize;

    //Tiles the width of the background to give it room to parallax
    private float tileMultiplier = 3;

    private TankController playerTank;

    // Start is called before the first frame update
    void Start()
    {
        playerTank = LevelManager.Instance.GetPlayerTank();
        backgroundSize = GetComponent<SpriteRenderer>().bounds.size;
        GetComponent<SpriteRenderer>().size = new Vector2(backgroundSize.x * tileMultiplier, GetComponent<SpriteRenderer>().size.y);
    }

    // Update is called once per frame
    void Update()
    {
        //NEED TO ADJUST THIS FOR NEW TANK SPEED VALUES
        //Get the speed of the player tank moving and adjusts the parallax speed by it's stored speed value
        if (playerTank != null)
            currentParallaxSpeed = 0;  //((-playerTank.GetBaseTankSpeed() * playerTank.GetThrottleMultiplier()) + automaticSpeed) * parallaxSpeed; NEED TO ADJUST THIS

        MoveBackground();
    }

    /// <summary>
    /// Moves the background's position.
    /// </summary>
    private void MoveBackground()
    {
        //Moving backwards
        if (currentParallaxSpeed > 0)
        {
            //Constantly move the background
            transform.localPosition += new Vector3(currentParallaxSpeed * Time.deltaTime, 0);

            //If the background moves past the original background size, move the background back to its original position
            if (transform.localPosition.x > backgroundSize.x)
                transform.localPosition = new Vector3(-backgroundSize.x, transform.localPosition.y, transform.localPosition.z);
        }

        //Moving forwards
        else
        {
            //Constantly move the background
            transform.localPosition += new Vector3(currentParallaxSpeed * Time.deltaTime, 0);

            //If the background moves past the original background size, move the background back to its original position
            if (transform.localPosition.x < -backgroundSize.x)
                transform.localPosition = new Vector3(backgroundSize.x, transform.localPosition.y, transform.localPosition.z);
        }
    }
}
