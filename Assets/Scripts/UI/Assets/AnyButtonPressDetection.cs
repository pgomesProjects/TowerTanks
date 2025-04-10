using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace TowerTanks.Scripts
{
    public class AnyButtonPressDetection : MonoBehaviour
    {
        [SerializeField, Tooltip("The delay for when any button press starts being detected once the scene loads.")] private float buttonDetectionDelay;

        private System.IDisposable onAnyButtonPress;
        public static System.Action<InputControl> OnAnyButtonPressed;

        private bool detectionDelayActive;
        private float buttonDetectionElapsed;

        private void OnEnable() => detectionDelayActive = true;
        private void OnDisable() => onAnyButtonPress.Dispose();

        private void Update()
        {
            if (detectionDelayActive)
            {
                if (buttonDetectionElapsed >= buttonDetectionDelay)
                {
                    SubscribeToButtonPress();
                    detectionDelayActive = false;
                }
                else
                    buttonDetectionElapsed += Time.unscaledDeltaTime;
            }
        }

        /// <summary>
        /// Subscribes the IDisposable to the onAnyButtonPress call utility function.
        /// </summary>
        private void SubscribeToButtonPress() => onAnyButtonPress = InputSystem.onAnyButtonPress.Call(OnAnyButtonPress);

        /// <summary>
        /// The function called when any button is pressed.
        /// </summary>
        /// <param name="control">The input control that was pressed.</param>
        private void OnAnyButtonPress(InputControl control) => OnAnyButtonPressed?.Invoke(control);
    }
}
