using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class TitlescreenParallax : MonoBehaviour
    {
        [SerializeField] private float parallaxSpeed = 1;

        private Vector2 backgroundSize;

        //Tiles the width of the background to give it room to parallax
        private float tileMultiplier = 3;

        // Start is called before the first frame update
        void Start()
        {
            backgroundSize = GetComponent<SpriteRenderer>().bounds.size;
            //Debug.Log("Background Size: " + backgroundSize);
            GetComponent<SpriteRenderer>().size = new Vector2(backgroundSize.x * tileMultiplier, GetComponent<SpriteRenderer>().size.y);
        }

        // Update is called once per frame
        void Update()
        {
            //Moving backwards
            if (parallaxSpeed > 0)
            {
                //Constantly move the background
                transform.position += new Vector3(parallaxSpeed * Time.deltaTime, 0);

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
                transform.position += new Vector3(parallaxSpeed * Time.deltaTime, 0);

                //If the background moves past the original background size, move the background back to its original position
                if (transform.position.x < -backgroundSize.x)
                {
                    transform.position = new Vector3(backgroundSize.x, transform.position.y, transform.position.z);
                }
            }
        }
    }
}
