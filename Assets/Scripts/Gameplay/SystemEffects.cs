using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SystemEffects : MonoBehaviour
{
    #region HAPTICS
    /// <summary>
    /// Applies controller haptics to the player.
    /// </summary>
    /// <param name="playerInput">The player input component.</param>
    /// <param name="leftIntensity">The intensity of the left motor.</param>
    /// <param name="rightIntensity">The intensity of the right motor.</param>
    /// <param name="duration">The duration of the haptics effect.</param>
    public void ApplyControllerHaptics(PlayerInput playerInput, float leftIntensity, float rightIntensity, float duration)
    {
        Gamepad gamepad = playerInput.devices[0] as Gamepad;

        //Apply haptics if the player is using a Gamepad
        if (gamepad != null)
            StartCoroutine(PlayHapticsConstant(gamepad, leftIntensity, rightIntensity, duration));
    }

    /// <summary>
    /// Applies controller haptics to the player.
    /// </summary>
    /// <param name="playerInput">The player input component.</param>
    /// <param name="intensity">The intensity of both the left and right motors.</param>
    /// <param name="duration">The duration of the haptics effect.</param>
    public void ApplyControllerHaptics(PlayerInput playerInput, float intensity, float duration)
    {
        ApplyControllerHaptics(playerInput, intensity, intensity, duration);
    }

    /// <summary>
    /// Applies ramped controller haptics feedback to the player.
    /// </summary>
    /// <param name="playerInput">The player input component.</param>
    /// <param name="startLeftIntensity">The starting intensity of the left motor.</param>
    /// <param name="startRightIntensity">The starting intensity of the right motor.</param>
    /// <param name="endLeftIntensity">The target intensity of the left motor.</param>
    /// <param name="endRightIntensity">The target intensity of the right motor.</param>
    /// <param name="rampUpDuration">The time it takes to ramp from start to end intensity.</param>
    /// <param name="holdDuration">The time after the ramped duration to maintain the ending intensity.</param>
    /// <param name="rampDownDuration">The time it takes to ramp from end to start intensity.</param>
    public void ApplyRampedControllerHaptics(PlayerInput playerInput, float startLeftIntensity, float startRightIntensity, float endLeftIntensity, float endRightIntensity, float rampUpDuration, float holdDuration, float rampDownDuration)
    {
        Gamepad gamepad = playerInput.devices[0] as Gamepad;

        //Apply haptics if the player is using a Gamepad
        if (gamepad != null)
            StartCoroutine(PlayHapticsRamped(gamepad, startLeftIntensity, startRightIntensity, endLeftIntensity, endRightIntensity, rampUpDuration, holdDuration, rampDownDuration));
    }

    /// <summary>
    /// Applies ramped controller haptics feedback to the player.
    /// </summary>
    /// <param name="playerInput">The player input component.</param>
    /// <param name="startIntensity">The starting intensity of both the left and right motors.</param>
    /// <param name="endIntensity">The ending intensity of both the left and right motors.</param>
    /// <param name="rampUpDuration">The time it takes to ramp from start to end intensity.</param>
    /// <param name="holdDuration">The time after the ramped duration to maintain the ending intensity.</param>
    /// <param name="rampDownDuration">The time it takes to ramp from end to start intensity.</param>
    public void ApplyRampedControllerHaptics(PlayerInput playerInput, float startIntensity, float endIntensity, float rampUpDuration, float holdDuration, float rampDownDuration)
    {
        ApplyRampedControllerHaptics(playerInput, startIntensity, startIntensity, endIntensity, endIntensity, rampUpDuration, holdDuration, rampDownDuration);
    }

    /// <summary>
    /// Applies a constant haptics feedback to a Gamepad.
    /// </summary>
    /// <param name="gamepad">The Gamepad to apply haptics to.</param>
    /// <param name="leftIntensity">The intensity of the left motor.</param>
    /// <param name="rightIntensity">The intensity of the right motor.</param>
    /// <param name="duration">The duration of the haptics effect.</param>
    /// <returns></returns>
    private IEnumerator PlayHapticsConstant(Gamepad gamepad, float leftIntensity, float rightIntensity, float duration)
    {
        gamepad.SetMotorSpeeds(leftIntensity, rightIntensity);

        yield return new WaitForSecondsRealtime(duration);

        StopHaptics(gamepad);
    }

    /// <summary>
    /// Applies ramped haptics feedback to a Gamepad over time, holds the end intensity, and ramps back to the starting intensity.
    /// </summary>
    /// <param name="gamepad">The Gamepad to apply haptics to.</param>
    /// <param name="startLeftIntensity">The starting intensity of the left motor.</param>
    /// <param name="startRightIntensity">The starting intensity of the right motor.</param>
    /// <param name="endLeftIntensity">The target intensity of the left motor.</param>
    /// <param name="endRightIntensity">The target intensity of the right motor.</param>
    /// <param name="rampUpDuration">The time it takes to ramp from start to end intensity.</param>
    /// <param name="holdDuration">The time after the ramped duration to maintain the ending intensity.</param>
    /// <param name="rampDownDuration">The time it takes to ramp from end to start intensity.</param>
    /// <returns></returns>
    private IEnumerator PlayHapticsRamped(Gamepad gamepad, float startLeftIntensity, float startRightIntensity, float endLeftIntensity, float endRightIntensity, float rampUpDuration, float holdDuration, float rampDownDuration)
    {
        float elapsed = 0f;

        // Ramp up from start intensity to end intensity
        while (elapsed < rampUpDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / rampUpDuration;

            // Interpolate motor speeds
            float currentLeftIntensity = Mathf.Lerp(startLeftIntensity, endLeftIntensity, t);
            float currentRightIntensity = Mathf.Lerp(startRightIntensity, endRightIntensity, t);

            gamepad.SetMotorSpeeds(currentLeftIntensity, currentRightIntensity);

            yield return null;
        }

        // Hold the end intensity for the specified duration
        gamepad.SetMotorSpeeds(endLeftIntensity, endRightIntensity);
        yield return new WaitForSecondsRealtime(holdDuration);

        // Ramp down from end intensity back to start intensity
        elapsed = 0f;
        while (elapsed < rampUpDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / rampUpDuration;

            // Interpolate motor speeds back to the starting intensity
            float currentLeftIntensity = Mathf.Lerp(endLeftIntensity, startLeftIntensity, t);
            float currentRightIntensity = Mathf.Lerp(endRightIntensity, startRightIntensity, t);

            gamepad.SetMotorSpeeds(currentLeftIntensity, currentRightIntensity);

            yield return null;
        }

        StopHaptics(gamepad);
    }

    /// <summary>
    /// Stops haptics on a Gamepad.
    /// </summary>
    /// <param name="gamepad">The Gamepad to stop the haptics for.</param>
    public void StopHaptics(Gamepad gamepad)
    {
        gamepad.ResetHaptics();
    }
    #endregion
    #region SCREENSHAKE
    /// <summary>
    /// Shakes the Cinemachine camera given.
    /// </summary>
    /// <param name="currentCamera">The current camera to shake (requires a Perlin Noise Profile on the camera to work).</param>
    /// <param name="intensity">The intensity of the screen shake.</param>
    /// <param name="duration">The duration of the screen shake.</param>
    public void ShakeCamera(CinemachineVirtualCamera currentCamera, float intensity, float duration)
    {
        //If the users have Screenshake turned on
        if (PlayerPrefs.GetInt("Screenshake", 1) == 1)
            StartCoroutine(ShakeCameraAnimation(currentCamera, intensity, duration));
    }

    /// <summary>
    /// Coroutine that shakes the camera for a specified duration.
    /// </summary>
    /// <param name="currentCamera">The current camera to shake  (requires a Perlin Noise Profile on the camera to work).</param>
    /// <param name="intensity">The intensity of the screen shake.</param>
    /// <param name="duration">The duration of the screen shake.</param>
    /// <returns></returns>
    private IEnumerator ShakeCameraAnimation(CinemachineVirtualCamera currentCamera, float intensity, float duration)
    {
        // Get the Perlin Noise component
        CinemachineBasicMultiChannelPerlin noise = currentCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();

        if (noise != null)
        {
            // Set initial shake intensity
            noise.m_AmplitudeGain = intensity;
            yield return new WaitForSecondsRealtime(duration);

            // Reset the shake intensity after the shake duration
            noise.m_AmplitudeGain = 0f;
        }
    }
    #endregion
}
