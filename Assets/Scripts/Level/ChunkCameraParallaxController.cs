using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TowerTanks.Scripts
{
    [System.Serializable]
    public class ChunkParallaxLayer : ParallaxLayer
    {
        [Tooltip("The parent for the chunk layers.")] public Transform chunkParent;
        [Tooltip("Prefab to spawn for chunk layers.")] public GameObject chunkPrefab;
        [Tooltip("The width for each chunk.")] public float chunkWidth;
        [Tooltip("Y-position for each chunk.")] public Vector2 yPosition = new Vector2(-1f, 1f);
        [Tooltip("Frequency at which chunks are spawned.")] public Vector2 spawnFrequency = new Vector2(3f, 5f);
        [Tooltip("The pool size of the background layer.")] public int poolSize;

        private float nextSpawnTime;
        private float currentDistanceTraveled;
        private List<GameObject> chunkPool = new List<GameObject>();

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

    public class ChunkCameraParallaxController : MultiCameraParallaxController
    {
        [SerializeField, Tooltip("How far away from the tank a chunk is allowed to render from.")] private float RENDER_DISTANCE = 300f;
        [SerializeField, Tooltip("The layers of the parallax background")] private List<ChunkParallaxLayer> chunkParallaxLayers = new List<ChunkParallaxLayer>();
        protected override List<ParallaxLayer> parallaxLayers => chunkParallaxLayers.Cast<ParallaxLayer>().ToList();


        protected override void Start()
        {
            base.Start();
        }

        protected override void SetupLayer(int index)
        {
            ChunkParallaxLayer chunkLayer = chunkParallaxLayers[index];

            chunkLayer.chunkParent.transform.position = Vector2.zero;
            Vector2 chunkPosition = Vector2.zero;

            chunkLayer.GetNextChunk(chunkPosition);

            for (int i = 1; i < chunkLayer.poolSize / 2; i++)
            {
                chunkPosition.x = chunkLayer.chunkWidth * Random.Range(chunkLayer.spawnFrequency.x, chunkLayer.spawnFrequency.y) * i;
                chunkLayer.GetNextChunk(new Vector2(chunkPosition.x, Random.Range(chunkLayer.yPosition.x, chunkLayer.yPosition.y)));
                chunkLayer.GetNextChunk(new Vector2(-chunkPosition.x, Random.Range(chunkLayer.yPosition.x, chunkLayer.yPosition.y)));
            }

            chunkLayer.ResetSpawnTimer();
        }

        protected override void ColorLayer(int index, Color newColor)
        {
            //Color layer function logic
        }

        protected override void LateUpdate()
        {
            base.LateUpdate();
        }

        public override void MoveLayer(int index, Vector3 deltaMovement)
        {
            ChunkParallaxLayer chunkLayer = chunkParallaxLayers[index];

            //Move the layer
            Vector3 distanceMoved = new Vector3((deltaMovement.x * chunkLayer.parallaxSpeed.x) + (chunkLayer.automaticSpeed.x * Time.deltaTime), (deltaMovement.y * chunkLayer.parallaxSpeed.y) + (chunkLayer.automaticSpeed.y * Time.deltaTime), 0);
            chunkLayer.chunkParent.transform.position += distanceMoved;

            chunkLayer.SetDistanceTraveled(chunkLayer.GetCurrentDistanceTraveled() + distanceMoved.x);

            //If the layer can spawn a new chunk, spawn one
            if (chunkLayer.CanSpawnChunk())
            {
                chunkLayer.ResetSpawnTimer();
            }

            List<Transform> chunksToRecycle = new List<Transform>();
            //If there are chunks outside of the layer's render distance, add them to a list to recycle
            foreach (Transform chunk in chunkLayer.chunkParent)
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
                chunkLayer.ReturnChunkToPool(chunk.gameObject);
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
                    //Upcast to chunk parallax layer
                    ChunkParallaxLayer chunkLayer = (ChunkParallaxLayer)layer;

                    foreach (Transform chunk in chunkLayer.chunkParent)
                    {
                        if (chunk.gameObject.activeInHierarchy)
                            Gizmos.color = Color.green;
                        else
                            Gizmos.color = Color.red;
                        Gizmos.DrawWireCube(chunk.position, Vector3.one * 10f);
                    }
                }
            }
#endif
        }
    }
}
