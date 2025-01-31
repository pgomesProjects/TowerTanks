using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    [System.Serializable]
    public class ParallaxLayer
    {
        public enum LayerType { TILE, CHUNK };

        [Header("Layer Settings")]
        [Tooltip("The type of parallax layer.")] public LayerType layerType;
        [Tooltip("The SpriteRenderer for the background.")] public SpriteRenderer parallaxSpriteRenderer;
        [Tooltip("The speed of the parallax movement.")] public Vector2 parallaxSpeed;
        [Tooltip("The innate speed of the parallax movement.")] public Vector2 automaticSpeed;
        [Tooltip("If true, the background infinitely scrolls horizontally.")] public bool infiniteHorizontal;
        [Tooltip("If true, the background infinitely scrolls vertically.")] public bool infiniteVertical;
        [Space()]
        [Header("Chunk Settings")]
        [Tooltip("The parent for the chunk layers.")]public Transform chunkParent;
        [Tooltip("Prefab to spawn for chunk layers.")] public GameObject chunkPrefab;
        [Tooltip("The width for each chunk.")] public float chunkWidth;
        [Tooltip("Y-position for each chunk.")] public Vector2 yPosition = new Vector2(-1f, 1f);
        [Tooltip("Frequency at which chunks are spawned.")] public Vector2 spawnFrequency = new Vector2(3f, 5f);
        [Tooltip("The pool size of the background layer.")] public int poolSize;

        private Vector2 textureUnitSize;
        private float nextSpawnTime;
        private float currentDistanceTraveled;
        private List<GameObject> chunkPool = new List<GameObject>();

        public Vector2 GetTextureUnitSize() => textureUnitSize;
        public void SetTextureUnitSize(Vector2 textureUnitSize) => this.textureUnitSize = textureUnitSize;

        public void ResetSpawnTimer()
        {
            nextSpawnTime = Random.Range(spawnFrequency.x, spawnFrequency.y);
            currentDistanceTraveled = 0f;
        }

        public float GetCurrentDistanceTraveled() => currentDistanceTraveled;
        public void SetDistanceTraveled(float distanceTraveled) => currentDistanceTraveled = distanceTraveled;
        public bool CanSpawnChunk() => currentDistanceTraveled >= nextSpawnTime;
        public GameObject GetNextChunk(Vector2 position)
        {
            GameObject newChunk = GameObject.Instantiate(chunkPrefab);
            newChunk.transform.position = position;
            newChunk.transform.parent = chunkParent;
            newChunk.layer = chunkParent.gameObject.layer;
            chunkPool.Add(newChunk);
            return newChunk;
        }

        public void ReturnChunkToPool(GameObject chunk)
        {
            //Set inactive
            chunk.SetActive(false);
        }
    }

    public class MultiCameraParallaxController : MonoBehaviour
    {
        [SerializeField, Tooltip("The camera to follow.")] private Camera followCamera;
        [SerializeField, Tooltip("The layers of the parallax background.")] private List<ParallaxLayer> parallaxLayers = new List<ParallaxLayer>();
        [SerializeField, Tooltip("How far away from the tank a chunk is allowed to render from.")] public float RENDER_DISTANCE = 300f;

        private Vector3 lastCameraPosition;
        private bool camInitialized;

        public Color[] desertColorPalette;
        public bool useDesertPalette;

        private void Start()
        {
            foreach(ParallaxLayer layer in parallaxLayers)
            {
                switch (layer.layerType)
                {
                    case ParallaxLayer.LayerType.TILE:
                        //Set the texture unit size of all the background pieces
                        Sprite sprite = layer.parallaxSpriteRenderer.sprite;
                        Texture2D texture = sprite.texture;
                        layer.SetTextureUnitSize(new Vector2(texture.width / sprite.pixelsPerUnit, texture.height / sprite.pixelsPerUnit) * layer.parallaxSpriteRenderer.transform.localScale);
                        break;
                    case ParallaxLayer.LayerType.CHUNK:

                        layer.chunkParent.transform.position = Vector2.zero;
                        Vector2 chunkPosition = Vector2.zero;

                        layer.GetNextChunk(chunkPosition);

                        for (int i = 1; i < layer.poolSize / 2; i++)
                        {
                            chunkPosition.x = layer.chunkWidth * Random.Range(layer.spawnFrequency.x, layer.spawnFrequency.y) * i;
                            layer.GetNextChunk(new Vector2(chunkPosition.x, Random.Range(layer.yPosition.x, layer.yPosition.y)));
                            layer.GetNextChunk(new Vector2(-chunkPosition.x, Random.Range(layer.yPosition.x, layer.yPosition.y)));
                        }

                        layer.ResetSpawnTimer();
                        break;
                }
            }

            for (int i = 0; i < parallaxLayers.Count; i++)
            {
                if (useDesertPalette)
                {
                    SpriteRenderer renderer = parallaxLayers[i].parallaxSpriteRenderer;
                    if (renderer != null) renderer.color = desertColorPalette[i];
                }
            }

            InitCamera();
        }

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

        private void LateUpdate()
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
            foreach (ParallaxLayer layer in parallaxLayers)
            {
                switch (layer.layerType)
                {
                    case ParallaxLayer.LayerType.TILE:
                        //Move the layer
                        layer.parallaxSpriteRenderer.transform.position += new Vector3((deltaMovement.x * layer.parallaxSpeed.x) + (layer.automaticSpeed.x * Time.deltaTime), (deltaMovement.y * layer.parallaxSpeed.y) + (layer.automaticSpeed.y * Time.deltaTime), 0);

                        //If the layer scrolls infinitely horizontally and has reached the end of the texture, offset the position
                        if (layer.infiniteHorizontal && Mathf.Abs(followCamera.transform.position.x - layer.parallaxSpriteRenderer.transform.position.x) >= layer.GetTextureUnitSize().x)
                        {
                            float offsetPositionX = (followCamera.transform.position.x - layer.parallaxSpriteRenderer.transform.position.x) % layer.GetTextureUnitSize().x;
                            layer.parallaxSpriteRenderer.transform.position = new Vector3(followCamera.transform.position.x + offsetPositionX, layer.parallaxSpriteRenderer.transform.position.y);
                        }

                        //If the layer scrolls infinitely vertically and has reached the end of the texture, offset the position
                        if (layer.infiniteVertical && Mathf.Abs(followCamera.transform.position.y - layer.parallaxSpriteRenderer.transform.position.y) >= layer.GetTextureUnitSize().y)
                        {
                            float offsetPositionY = (followCamera.transform.position.y - layer.parallaxSpriteRenderer.transform.position.y) % layer.GetTextureUnitSize().y;
                            layer.parallaxSpriteRenderer.transform.position = new Vector3(layer.parallaxSpriteRenderer.transform.position.x, layer.parallaxSpriteRenderer.transform.position.y + offsetPositionY);
                        }
                        break;
                    case ParallaxLayer.LayerType.CHUNK:
                        //Move the layer
                        Vector3 distanceMoved = new Vector3((deltaMovement.x * layer.parallaxSpeed.x) + (layer.automaticSpeed.x * Time.deltaTime), (deltaMovement.y * layer.parallaxSpeed.y) + (layer.automaticSpeed.y * Time.deltaTime), 0);
                        layer.chunkParent.transform.position += distanceMoved;

                        layer.SetDistanceTraveled(layer.GetCurrentDistanceTraveled() + distanceMoved.x);

                        //If the layer can spawn a new chunk, spawn one
                        if (layer.CanSpawnChunk())
                        {
                            layer.ResetSpawnTimer();
                        }

                        List<Transform> chunksToRecycle = new List<Transform>();
                        //If there are chunks outside of the layer's render distance, add them to a list to recycle
                        foreach (Transform chunk in layer.chunkParent)
                        {
                            float chunkDistance = Vector3.Distance(followCamera.transform.position, chunk.position);

                            if (chunkDistance <= RENDER_DISTANCE)
                            {
                                chunk.gameObject.SetActive(true);
                            }

                            if (chunkDistance > RENDER_DISTANCE)
                                chunksToRecycle.Add(chunk);
                        }

                        //Return all chunks outside of the render distance to the chunk pool
                        foreach (Transform chunk in chunksToRecycle)
                            layer.ReturnChunkToPool(chunk.gameObject);

                        break;
                }
            }
        }

        public void AddCameraToParallax(Camera newCamera)
        {
            followCamera = newCamera;
        }

        private void OnDrawGizmos()
        {
#if UNITY_EDITOR
            if (followCamera == null)
                return;

            if (followCamera.tag == "RadarCam")
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireCube(followCamera.transform.position, Vector3.one * RENDER_DISTANCE * 2);

                foreach (ParallaxLayer layer in parallaxLayers)
                {
                    if (layer.layerType == ParallaxLayer.LayerType.CHUNK)
                    {
                        foreach (Transform chunk in layer.chunkParent)
                        {
                            if (chunk.gameObject.activeInHierarchy)
                                Gizmos.color = Color.green;
                            else
                                Gizmos.color = Color.red;
                            Gizmos.DrawWireCube(chunk.position, Vector3.one * 10f);
                        }
                    }
                }
            }
#endif
        }
    }
}
