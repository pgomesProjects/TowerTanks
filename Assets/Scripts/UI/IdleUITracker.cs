using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class IdleUITracker : MonoBehaviour
    {
        [SerializeField, Tooltip("The amount of time for the screen to idle before showing the idle screen (in seconds).")] private float idleLimit;

        public static System.Action OnIdleScreenActivated;
        public static System.Action OnIdleEnd;

        public static bool InputRecorded = false;

        private bool idleScreenActive;
        private float idleElapsed;

        private void OnEnable()
        {
            ResetIdle();
        }

        /// <summary>
        /// Resets the idle screen.
        /// </summary>
        private void ResetIdle()
        {
            //If the idle screen is active, call the ending action
            if (idleScreenActive)
            {
                idleScreenActive = false;
                OnIdleEnd?.Invoke();
            }

            //Reset the timer and boolean
            idleElapsed = 0f;
            InputRecorded = false;
        }

        /// <summary>
        /// Increments the idle timer.
        /// </summary>
        private void IncrementIdle()
        {
            //Increment the idle timer
            idleElapsed += Time.deltaTime;

            //If the timer has reached its limit, set the idle screen active to true and invoke
            if (idleElapsed >= idleLimit && !idleScreenActive)
            {
                idleScreenActive = true;
                OnIdleScreenActivated?.Invoke();
            }

            //If any input is recorded, reset
            if (InputRecorded)
                ResetIdle();
        }

        private void Update()
        {
            IncrementIdle();
        }
    }
}
