using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class OffscreenTracker : MonoBehaviour
    {
        [SerializeField, Tooltip("The camera to use to track whether the player is offscreen.")] private Camera playerCam;
        [SerializeField, Tooltip("The buffer around the screen to allow players to enter before being considered offscreen.")] private float screenBuffer = 0.01f;

        private float offscreenDuration = 3f;
        private float currentOffscreenTimer = 0f;
        private bool isOffscreen = false;
        private Character currentCharacter;

        private void Awake()
        {
            //If there is a character attached to this script, use their settings for the offscreen time
            currentCharacter = GetComponent<Character>();
            if (currentCharacter != null)
                offscreenDuration = currentCharacter.GetCharacterSettings().offscreenTime;
        }

        /// <summary>
        /// Assigns a camera to the offscreen tracker.
        /// </summary>
        /// <param name="newCamera">The camera for the player to be onscreen in.</param>
        public void AssignCamera(Camera newCamera)
        {
            playerCam = newCamera;
        }

        void Update()
        {
            //If there is no camera or player, return
            if (playerCam == null || currentCharacter == null)
                return;

            //If the character is dead, ignore this
            if (currentCharacter.IsDead())
                return;

            OffscreenCheck();
        }

        /// <summary>
        /// Checks to see if the player is offscreen. If so, a timer is incremented.
        /// </summary>
        private void OffscreenCheck()
        {
            //Check to see if the player is currently offscreen
            Vector3 cameraViewport = playerCam.WorldToViewportPoint(transform.position);
            bool isCurrentlyOffScreen =
                cameraViewport.x < -screenBuffer || cameraViewport.x > 1 + screenBuffer ||
                cameraViewport.y < -screenBuffer || cameraViewport.y > 1 + screenBuffer;

            if (isCurrentlyOffScreen)
            {
                //If this is the first frame that they are offscreen, turn on and reset the offscreen timer
                if (!isOffscreen)
                {
                    isOffscreen = true;
                    currentOffscreenTimer = 0f;
                    Debug.Log("Player Is Offscreen.");
                }

                currentOffscreenTimer += Time.deltaTime;

                if (currentOffscreenTimer >= offscreenDuration)
                {
                    currentCharacter.KillCharacterImmediate();
                    isOffscreen = false;
                }
            }
            else
            {
                //If the player is on screen, stop the timer and reset it
                isOffscreen = false;
                currentOffscreenTimer = 0f;
            }
        }

        public bool IsOffscreen() => isOffscreen;
    }
}
