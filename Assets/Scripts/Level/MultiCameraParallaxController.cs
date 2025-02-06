using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    [System.Serializable]
    public class ParallaxLayer
    {
        [Header("Layer Settings")]
        [Tooltip("The speed of the parallax movement.")] public Vector2 parallaxSpeed;
        [Tooltip("The innate speed of the parallax movement.")] public Vector2 automaticSpeed;
        [Tooltip("If true, the background infinitely scrolls horizontally.")] public bool infiniteHorizontal;
        [Tooltip("If true, the background infinitely scrolls vertically.")] public bool infiniteVertical;
    }

    public abstract class MultiCameraParallaxController : MonoBehaviour
    {
        [SerializeField, Tooltip("The camera to follow.")] protected Camera followCamera;
        [SerializeField, Tooltip("The colors used in the desert color palette")] internal Color[] desertColorPalette;
        [SerializeField, Tooltip("If true, the desert palette is used.")] internal bool useDesertPalette;

        protected abstract List<ParallaxLayer> parallaxLayers { get; }

        private Vector3 lastCameraPosition;
        private bool camInitialized;

        protected virtual void Awake()
        {
            for (int i = 0; i < parallaxLayers.Count; i++)
            {
                //Set up the layer
                SetupLayer(i);

                //If the desert palette is used, color the layer
                if (useDesertPalette)
                    ColorLayer(i, desertColorPalette[i]);
            }

            InitCamera();
        }

        protected abstract void ColorLayer(int index, Color newColor);
        protected abstract void SetupLayer(int index);

        /// <summary>
        /// Initializes the camera information.
        /// </summary>
        private void InitCamera()
        {
            if (followCamera == null)
            {
                camInitialized = false;
                return;
            }

            lastCameraPosition = followCamera.transform.position;
            camInitialized = true;
        }

        protected virtual void LateUpdate()
        {
            //If there is no camera to follow, return
            if (followCamera == null)
            {
                if (camInitialized)
                    camInitialized = false;
                return;
            }

            //If the camera is not initialized, initialize it
            else if (!camInitialized)
                InitCamera();

            //Get the movement of the camera
            Vector3 deltaMovement = followCamera.transform.position - lastCameraPosition;
            lastCameraPosition = followCamera.transform.position;

            //Move all of the layers accordingly
            for (int i = 0; i < parallaxLayers.Count; i++)
                MoveLayer(i, deltaMovement);
        }

        public abstract void MoveLayer(int index, Vector3 deltaMovement);
        
        public void AddCameraToParallax(Camera newCamera) => followCamera = newCamera;
    }
}
