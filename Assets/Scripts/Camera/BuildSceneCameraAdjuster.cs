using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

namespace TowerTanks.Scripts
{
    public class BuildSceneCameraAdjuster : MonoBehaviour
    {
        [SerializeField, Tooltip("The minimum orthographic size for the camera.")] private float minimumOrthoSize;
        [SerializeField, Tooltip("The virtual camera component.")] private CinemachineVirtualCamera buildCam;
        [SerializeField, Tooltip("Padding to add around the tank.")] private float padding;

        [SerializeField, Tooltip("The duration for the camera to adjust its ortho size.")] private float cameraAdjustDuration;
        [SerializeField, Tooltip("The animation curve for the camera adjustment.")] private AnimationCurve cameraAniCurve = new AnimationCurve(new Keyframe(0f, 0f, 0f, 5f), new Keyframe(1f, 1f, 0f, 0f));

        private TankController buildTank;
        private float highestYValue;

        private bool isAdjusted;
        private float startingOrthoSize;
        private float desiredOrthoSize;
        private float currentCameraDuration;

        private void Awake()
        {
            buildTank = FindObjectOfType<TankController>();
        }

        private void Start()
        {
            isAdjusted = true;
            AdjustCamera();
        }

        private void OnEnable()
        {
            TankController.OnPlayerTankSizeAdjusted += AdjustCamera;
        }

        private void OnDisable()
        {
            TankController.OnPlayerTankSizeAdjusted -= AdjustCamera;
        }

        /// <summary>
        /// Adjusts the camera's orthographic size based on the highest point found in the tank.
        /// </summary>
        private void AdjustCamera()
        {
            highestYValue = buildTank.GetHighestPoint() + padding;

            startingOrthoSize = buildCam.m_Lens.OrthographicSize;

            // Calculate the orthographic size based on the y-value (Orthosize = 0.5 [slope] * y + 0.75 [y-intercept])
            desiredOrthoSize = Mathf.Max(0.5f * highestYValue + 0.75f, minimumOrthoSize);

            if (startingOrthoSize != desiredOrthoSize)
            {
                isAdjusted = false;
                currentCameraDuration = 0f;
            }
        }

        private void Update()
        {
            if (!isAdjusted)
            {
                currentCameraDuration += Time.deltaTime;

                if (currentCameraDuration >= cameraAdjustDuration)
                {
                    buildCam.m_Lens.OrthographicSize = desiredOrthoSize;
                    isAdjusted = true;
                }
                else
                {
                    //Lerp the orthographic size of the camera
                    float t = cameraAniCurve.Evaluate(currentCameraDuration / cameraAdjustDuration);
                    buildCam.m_Lens.OrthographicSize = Mathf.Lerp(startingOrthoSize, desiredOrthoSize, t);
                }
            }
        }

        private void OnDrawGizmos()
        {
            //Draw the highest y-value of the tank
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(new Vector3(0, highestYValue, 0), 0.5f);
        }
    }
}
