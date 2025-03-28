using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public static class RuntimePolaroid // this is for any snapshots we want to be able to take of game objects while the game is running (used for rand hatch rooms)
    {
        private static float cameraSize;
        static void AdjustCameraToFitRoom(Room room, Camera captureCamera)
        {
            Bounds bounds = room.GetRoomBounds();
            captureCamera.transform.position = new Vector3(bounds.center.x, bounds.center.y, -10);
            
            float objectHeight = bounds.size.y;
            float objectWidth = bounds.size.x;

            //make sure the camera encompasses the entire object
            float cameraHeight = objectHeight / 2f + .5f;
            float cameraWidth = objectWidth / 2f + .5f;
            cameraSize = Mathf.Max(cameraHeight, cameraWidth);

            captureCamera.orthographicSize = cameraSize;
            UnityEngine.Debug.Log($"Camera position: {captureCamera.transform.position}");
        }
        public static Sprite CaptureSpriteFromObject(GameObject obj)
        {
            Camera captureCamera = new GameObject("CaptureCamera").AddComponent<Camera>();
            
            //captureCamera.backgroundColor = Color.clear;  
            captureCamera.orthographic = true;  

            if (obj.TryGetComponent(out Room room))
            {
                //captureCamera.transform.position = obj.transform.position;
                //captureCamera.orthographicSize = 2;
                AdjustCameraToFitRoom(room, captureCamera); // Adjust camera position and size for room
            }
            else
            {
                captureCamera.transform.position = obj.transform.position;
                captureCamera.orthographicSize = 2;
            }
            
            RenderTexture renderTexture = new RenderTexture(1000, 1000, 24);
            //renderTexture.depthStencilFormat = GraphicsFormat.None;
            captureCamera.targetTexture = renderTexture;

            int originalLayer = obj.layer;

            captureCamera.Render();  // Render the camera view to the RenderTexture
            
            Texture2D texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);
            RenderTexture.active = renderTexture;
            
            texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            texture.Apply();
            
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));

            // Clean up
            captureCamera.targetTexture = null;
            RenderTexture.active = null;
            obj.layer = originalLayer;
            GameObject.Destroy(captureCamera);
    
            return sprite;
        }
    }
}

