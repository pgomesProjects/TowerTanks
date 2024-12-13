using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TowerTanks.Scripts
{
    [ExecuteInEditMode]
    public class CaptureImage : MonoBehaviour
    {
        [SerializeField, Tooltip("The folder to take pictures of prefabs from.")] private string folderName;
        [SerializeField, Tooltip("The resolution of the image.")] private float imageRes = 100f;
        private Camera captureCamera;

        private float originalCameraOrthoSize;
        private float originalCameraFOV;

        [Button]
        public void CaptureImages()
        {
            captureCamera = Camera.main;
            CapturePrefabsFromFolder();
        }

        /// <summary>
        /// Captures images of all prefabs in a defined folder.
        /// </summary>
        public void CapturePrefabsFromFolder()
        {
            string folderPath = "Assets/" + folderName;

            //Get all file paths of type Prefab from the folder
            string[] prefabPaths = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });

            foreach (string prefabPath in prefabPaths)
            {
                //Load the current prefab
                string assetPath = AssetDatabase.GUIDToAssetPath(prefabPath);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

                //Instantiate it into the scene to capture it
                GameObject instantiatedObject = Instantiate(prefab);
                instantiatedObject.transform.position = Vector3.zero;
                Capture(instantiatedObject);

                //Cleanup
                DestroyImmediate(instantiatedObject);
            }
        }

        /// <summary>
        /// Captures an image of a GameObject.
        /// </summary>
        /// <param name="targetObject">The GameObject to capture an image of.</param>
        public void Capture(GameObject targetObject)
        {
            // Calculate the bounds of the target object
            Bounds bounds = CalculateBounds(targetObject);

            // Create a RenderTexture with the appropriate size
            int width = Mathf.RoundToInt(bounds.size.x * imageRes);
            int height = Mathf.RoundToInt(bounds.size.y * imageRes);
            RenderTexture renderTexture = new RenderTexture(width, height, 24);
            captureCamera.targetTexture = renderTexture;

            // Adjust the camera to fit the bounds of the object
            AdjustCamera(bounds);

            // Create a Texture2D to hold the RenderTexture image
            RenderTexture.active = renderTexture;
            Texture2D texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.ARGB32, false);
            texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            texture.Apply();

            //Turn the Texture2D into a PNG
            byte[] bytes = texture.EncodeToPNG();
            string path = Path.Combine(Application.dataPath, "Pictures/" + targetObject.name.Replace("(Clone)", "") + ".png");
            File.WriteAllBytes(path, bytes);
            Debug.Log("Image saved to " + path + " successfully.");

            // Cleanup
            RenderTexture.active = null;
            captureCamera.targetTexture = null;
            captureCamera.orthographicSize = originalCameraOrthoSize;
            captureCamera.fieldOfView = originalCameraFOV;
            DestroyImmediate(texture);
            DestroyImmediate(renderTexture);
        }

        /// <summary>
        /// Gets the bounds of the target object.
        /// </summary>
        /// <param name="targetObject">The GameObject to get the bounds from.</param>
        /// <returns>Returns the bounds of the GameObject.</returns>
        private Bounds CalculateBounds(GameObject targetObject)
        {
            Renderer[] renderers = targetObject.GetComponentsInChildren<Renderer>();

            //If there are no renderers in the object, return null
            if (renderers.Length == 0)
                return new Bounds();

            Bounds bounds = renderers[0].bounds;

            foreach (var renderer in renderers)
            {
                //Increases the bounds of the main renderer to include all renderers in the GameObject
                bounds.Encapsulate(renderer.bounds);
            }
            return bounds;
        }

        /// <summary>
        /// Adjusts the camera orthosize to include the entire bounds.
        /// </summary>
        /// <param name="bounds">The bounds to capture.</param>
        private void AdjustCamera(Bounds bounds)
        {
            //Look at the center of the bounds
            Vector3 center = bounds.center;
            captureCamera.transform.position = new Vector3(center.x, center.y, captureCamera.transform.position.z);
            captureCamera.transform.LookAt(center);

            // Adjust the orthographic size of the camera
            originalCameraOrthoSize = captureCamera.orthographicSize;
            captureCamera.orthographicSize = bounds.extents.y;

            //Update the camera
            captureCamera.Render();
        }
    }
}
