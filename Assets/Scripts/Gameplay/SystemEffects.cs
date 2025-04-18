using Cinemachine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TowerTanks.Scripts
{
    public class SystemEffects : MonoBehaviour
    {
        private void Start()
        {
            //Ease Out Circ Ease Type
            startSlowMotionCurve = new AnimationCurve();
            startSlowMotionCurve.AddKey(new Keyframe(0f, 0f));
            startSlowMotionCurve.AddKey(new Keyframe(0.25f, 0.55f));
            startSlowMotionCurve.AddKey(new Keyframe(0.5f, 0.85f));
            startSlowMotionCurve.AddKey(new Keyframe(1f, 1f));
            startSlowMotionCurve.SmoothTangents(0, 0.5f);
            startSlowMotionCurve.SmoothTangents(1, 0.5f);
            startSlowMotionCurve.SmoothTangents(2, 0.5f);
            startSlowMotionCurve.SmoothTangents(3, 0f);

            //Ease Out Back Ease Type
            endSlowMotionCurve = new AnimationCurve();
            endSlowMotionCurve.AddKey(new Keyframe(0f, 0f));
            endSlowMotionCurve.AddKey(new Keyframe(0.7f, 1.2f));
            endSlowMotionCurve.AddKey(new Keyframe(1f, 1f));
            endSlowMotionCurve.SmoothTangents(0, 0f);
            endSlowMotionCurve.SmoothTangents(1, 0.5f);
            endSlowMotionCurve.SmoothTangents(2, 0f);
        }

        private void LateUpdate()
        {
            foreach (ActiveCamera camera in activeCameras)
            {
                float totalAmpGain = 0;
                List<ActiveEffect> effectsToRemove = new List<ActiveEffect>();

                //Get total amp gain of all active effects
                if (camera.currentEffects.Count > 0)
                {
                    foreach(ActiveEffect effect in camera.currentEffects)
                    {
                        float ampGain = Mathf.Lerp(0, effect.amplitudeGain, effect.currentDuration / effect.duration);
                        effect.currentDuration -= Time.unscaledDeltaTime; //tick down each effect's duration
                        if (effect.currentDuration <= 0)
                        {
                            ampGain = 0;
                            effectsToRemove.Add(effect);
                        }
                        totalAmpGain += ampGain;
                        Mathf.Clamp(totalAmpGain, 0, maxAmplitude); //clamp maximum applied amplitude on camera
                    }
                }

                //Cleanup Ended Effects
                if (effectsToRemove.Count > 0)
                {
                    foreach (ActiveEffect effect in effectsToRemove)
                    {
                        camera.currentEffects.Remove(effect);
                    }
                }

                //Apply total amp gain to camera
                camera.noisePerlin.m_AmplitudeGain = totalAmpGain;
            }
        }

        #region TIME
        private bool isFrozen = false;

        private AnimationCurve startSlowMotionCurve;
        private AnimationCurve endSlowMotionCurve;
        private bool inSlowMotion = false;

        /// <summary>
        /// Freezes the screen for a certain amount of time.
        /// </summary>
        /// <param name="duration">The duration for the screen freeze.</param>
        public void ScreenFreeze(float duration)
        {
            if (!isFrozen)
                StartCoroutine(FreezeTimer(duration));
        }

        /// <summary>
        /// Freezes the screen, and then returns the game to the original time scale.
        /// </summary>
        /// <param name="duration">The duration for the screen freeze.</param>
        /// <returns></returns>
        private IEnumerator FreezeTimer(float duration)
        {
            isFrozen = true;

            float originalTimeScale = GameManager.gameTimeScale;
            GameManager.gameTimeScale = 0f;
            Time.timeScale = GameManager.gameTimeScale;

            yield return new WaitForSecondsRealtime(duration);

            GameManager.gameTimeScale = originalTimeScale;
            Time.timeScale = GameManager.gameTimeScale;

            isFrozen = false;
        }

        /// <summary>
        /// Activates a slow motion time scale.
        /// </summary>
        /// <param name="timeScale">The time scale to slow down to.</param>
        /// <param name="startTime">The time it takes to reach the slow motion time.</param>
        /// <param name="duration">The duration of the time scale.</param>
        /// <param name="returnTime">The amount of time it takes to return to the normal time scale.</param>
        public void ActivateSlowMotion(float timeScale, float startTime, float duration, float returnTime)
        {
            if (!inSlowMotion && !isFrozen)
                StartCoroutine(SlowMotionTimer(timeScale, startTime, duration, returnTime));
        }

        /// <summary>
        /// Slows the time scale, waits for a specified amount of time, and then quickly transitions back into real time.
        /// </summary>
        /// <param name="timeScale">The time scale to slow down to.</param>
        /// <param name="startTime">The time it takes to reach the slow motion time.</param>
        /// <param name="duration">The duration of the time scale.</param>
        /// <param name="returnTime">The amount of time it takes to return to the normal time scale.</param>
        /// <returns></returns>
        private IEnumerator SlowMotionTimer(float timeScale, float startTime, float duration, float returnTime)
        {
            inSlowMotion = true;

            float elapsedTime = 0f;
            float normalTimeScale = 1.0f;

            while (elapsedTime < startTime)
            {
                elapsedTime += Time.unscaledDeltaTime;

                float t = endSlowMotionCurve.Evaluate(elapsedTime / startTime);

                GameManager.gameTimeScale = Mathf.Lerp(normalTimeScale, timeScale, t);
                Time.timeScale = GameManager.gameTimeScale;
                GameManager.Instance.AudioManager.UpdateSFXPitch(GameManager.gameTimeScale);
                yield return null;
            }

            GameManager.gameTimeScale = timeScale;
            Time.timeScale = GameManager.gameTimeScale;
            GameManager.Instance.AudioManager.UpdateSFXPitch(timeScale);

            yield return new WaitForSecondsRealtime(duration);

            elapsedTime = 0f;

            while (elapsedTime < returnTime)
            {
                elapsedTime += Time.unscaledDeltaTime;

                float t = endSlowMotionCurve.Evaluate(elapsedTime / returnTime);

                GameManager.gameTimeScale = Mathf.Lerp(timeScale, normalTimeScale, t);
                Time.timeScale = GameManager.gameTimeScale;
                GameManager.Instance.AudioManager.UpdateSFXPitch(GameManager.gameTimeScale);
                yield return null;
            }

            GameManager.gameTimeScale = normalTimeScale;
            Time.timeScale = GameManager.gameTimeScale;
            GameManager.Instance.AudioManager.UpdateSFXPitch(normalTimeScale);

            inSlowMotion = false;
        }

        #endregion
        #region HAPTICS

        public HapticsSettings[] hapticsOptions;

        public HapticsSettings GetHapticsSetting(string id)
        {
            HapticsSettings setting = null;

            foreach (HapticsSettings settings in hapticsOptions)
            {
                if (settings.name == id) setting = settings;
            }

            return setting;
        }

        /// <summary>
        /// Applies controller haptics to all players.
        /// </summary>
        /// <param name="hapticsSettings">The settings for the haptics event.</param>
        public void ApplyControllerHaptics(HapticsSettings hapticsSettings)
        {
            //Goes through each player and applies haptics
            foreach (PlayerData player in GameManager.Instance.MultiplayerManager.GetAllPlayers())
                ApplyControllerHaptics(player.playerInput, hapticsSettings);
        }

        /// <summary>
        /// Applies controller haptics to the player.
        /// </summary>
        /// <param name="playerInput">The player input component.</param>
        /// <param name="hapticsSettings">The settings for the haptics event.</param>
        public void ApplyControllerHaptics(PlayerInput playerInput, HapticsSettings hapticsSettings)
        {
            //If the player has rumble turned off, return
            if (GameSettings.currentSettings.rumbleOn == 0)
                return;
            
            Gamepad gamepad = playerInput.devices[0] as Gamepad;

            //Return if the player does not have a gamepad or there are no settings
            if (gamepad == null || hapticsSettings == null)
                return;

            //Start a coroutine based on the type of haptics
            switch (hapticsSettings.hapticsType)
            {
                case HapticsType.STANDARD:
                    StartCoroutine(PlayHapticsConstant(gamepad, hapticsSettings.leftMotorIntensity, hapticsSettings.rightMotorIntensity, hapticsSettings.duration));
                    break;
                case HapticsType.RAMPED:
                    StartCoroutine(PlayHapticsRamped(gamepad, hapticsSettings.leftStartIntensity, hapticsSettings.rightStartIntensity, hapticsSettings.leftEndIntensity, hapticsSettings.rightEndIntensity, hapticsSettings.rampUpDuration, hapticsSettings.holdDuration, hapticsSettings.rampDownDuration));
                    break;
            }
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
                elapsed += Time.unscaledDeltaTime;
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
                elapsed += Time.unscaledDeltaTime;
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
        public class ActiveCamera : SystemEffects
        {
            public CinemachineVirtualCamera cam; //assigned camera
            public CinemachineBasicMultiChannelPerlin noisePerlin; //perlin assigned to this camera
            public List<ActiveEffect> currentEffects = new List<ActiveEffect>();
        }

        public class ActiveEffect : SystemEffects
        {
            public float amplitudeGain;
            public float duration;
            public float currentDuration;
        }

        private List<ActiveCamera> activeCameras = new List<ActiveCamera>();
        private float maxAmplitude = 50f;

        public ScreenshakeSettings[] screenshakeOptions;

        public ScreenshakeSettings GetScreenShakeSetting(string id)
        {
            ScreenshakeSettings setting = null;

            foreach (ScreenshakeSettings settings in screenshakeOptions)
            {
                if (settings.name == id) setting = settings;
            }

            return setting;
        }

        /// <summary>
        /// Shakes the Cinemachine camera given.
        /// </summary>
        /// <param name="currentCamera">The current camera to shake (requires a Perlin Noise Profile on the camera to work).</param>
        /// <param name="screenshakeSettings">The settings for the screenshake event.</param>
        public void ShakeCamera(CinemachineVirtualCamera currentCamera, ScreenshakeSettings screenshakeSettings)
        {
            //If the users have Screenshake turned off or there are no settings, return
            if (GameSettings.currentSettings.screenshakeOn == 0 || screenshakeSettings == null)
                return;

            bool camExists = false;
            foreach (ActiveCamera camera in activeCameras)
            {
                if (camera.cam == currentCamera)
                {
                    camExists = true;

                    //Add a new effect
                    ActiveEffect effect = new ActiveEffect();
                    effect.amplitudeGain = screenshakeSettings.intensity;
                    effect.duration = screenshakeSettings.duration;
                    effect.currentDuration = effect.duration;
                    camera.currentEffects.Add(effect);
                }
            }

            if (!camExists)
            {
                //Create a new Cam
                ActiveCamera newCam = new ActiveCamera();
                newCam.cam = currentCamera;
                newCam.noisePerlin = currentCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
                activeCameras.Add(newCam);

                //Add it's first effect
                ActiveEffect effect = new ActiveEffect();
                effect.amplitudeGain = screenshakeSettings.intensity;
                effect.duration = screenshakeSettings.duration;
                effect.currentDuration = effect.duration;
                newCam.currentEffects.Add(effect);
            }
            //StartCoroutine(ShakeCameraAnimation(currentCamera, screenshakeSettings.intensity, screenshakeSettings.duration));
        }
        /// <summary>
        /// Override for ShakeCamera that accepts raw values instead of a scriptable object.
        /// </summary>
        /// <param name="currentCamera">The current camera to shake (requires a Perlin Noise Profile on the camera to work).</param>
        public void ShakeCamera(CinemachineVirtualCamera currentCamera, float intensity, float duration)
        {
            //If the users have Screenshake turned off or there are no settings, return
            if (GameSettings.currentSettings.screenshakeOn == 0)
                return;

            bool camExists = false;
            foreach (ActiveCamera camera in activeCameras)
            {
                if (camera.cam == currentCamera)
                {
                    camExists = true;

                    //Add a new effect
                    ActiveEffect effect = new ActiveEffect();
                    effect.amplitudeGain = intensity;
                    effect.duration = duration;
                    effect.currentDuration = effect.duration;
                    camera.currentEffects.Add(effect);
                }
            }

            if (!camExists)
            {
                //Create a new Cam
                ActiveCamera newCam = new ActiveCamera();
                newCam.cam = currentCamera;
                newCam.noisePerlin = currentCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
                activeCameras.Add(newCam);

                //Add it's first effect
                ActiveEffect effect = new ActiveEffect();
                effect.amplitudeGain = intensity;
                effect.duration = duration;
                effect.currentDuration = effect.duration;
                newCam.currentEffects.Add(effect);
            }
            //StartCoroutine(ShakeCameraAnimation(currentCamera, intensity, duration));
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
        #region SCREENSHOT

        public void TakeScreenshot()
        {
            string fileName = "TowerTanks-Screenshot-" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".png";

            ScreenCapture.CaptureScreenshot(Application.dataPath + "/Resources/Screenshots/" + fileName);
            Debug.Log("Screenshot Successfully Taken. Saved as " + fileName);
        }

        #endregion
    }
}
