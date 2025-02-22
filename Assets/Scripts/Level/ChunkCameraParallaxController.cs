using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TowerTanks.Scripts
{
    [System.Serializable]
    public class ChunkParallaxLayer : ParallaxLayer
    {
        [Tooltip("The parent for the chunk pieces.")] public Transform chunkParent;
        [Tooltip("Prefab to spawn for chunk piece layers.")] public GameObject piecePrefab;
        [Tooltip("The width for each chunk piece.")] public float pieceWidth;
        [Tooltip("Y-position for each chunk piece.")] public Vector2 yPosition = new Vector2(-1f, 1f);
        [Tooltip("Frequency at which chunk pieces are spawned.")] public Vector2 spawnFrequency = new Vector2(3f, 5f);
        [Tooltip("The pool size of the chunk piece layer.")] public int poolSize;

        private float nextSpawnTime;
        private float currentDistanceTraveled;
        private List<GameObject> piecePool = new List<GameObject>();

        public float GetCurrentDistanceTraveled() => currentDistanceTraveled;
        public void SetDistanceTraveled(float distanceTraveled) => currentDistanceTraveled = distanceTraveled;
        public GameObject GetNextChunkPiece(Vector2 position)
        {
            GameObject newChunk = GameObject.Instantiate(piecePrefab);
            newChunk.transform.position = position;
            newChunk.transform.parent = chunkParent;
            piecePool.Add(newChunk);
            return newChunk;
        }

        public void ReturnChunkPieceToPool(GameObject chunk)
        {
            //Set inactive
            chunk.SetActive(false);
        }
    }

    public class ChunkCameraParallaxController : MultiCameraParallaxController
    {
        [SerializeField, Tooltip("How far away from the tank a chunk is allowed to render from.")] private float RENDER_DISTANCE = 100f;
        [SerializeField, Tooltip("The layers of the parallax background")] private List<ChunkParallaxLayer> chunkParallaxLayers = new List<ChunkParallaxLayer>();
        protected override List<ParallaxLayer> parallaxLayers => chunkParallaxLayers.Cast<ParallaxLayer>().ToList();

        protected override void Awake()
        {
            base.Awake();
        }

        protected override void SetupLayer(int index)
        {
            ChunkParallaxLayer chunkLayer = chunkParallaxLayers[index];

            chunkLayer.chunkParent.transform.position = Vector2.zero;
            //Create the chunks for the pool
            for (int i = 0; i < chunkLayer.poolSize; i++)
                chunkLayer.GetNextChunkPiece(Vector2.zero);
        }

        public void PositionLayers(List<List<Vector2>> positions)
        {
            //Get all of the layers in the chunk controller
            for (int i = 0; i < chunkParallaxLayers.Count; i++)
            {
                ChunkParallaxLayer currentLayer = chunkParallaxLayers[i];
                //Place all of the positions of the chunk pieces
                for(int j = 0; j < currentLayer.poolSize; j++)
                {
                    currentLayer.chunkParent.GetChild(j).localPosition = positions[i][j];
                    currentLayer.chunkParent.GetChild(j).gameObject.layer = currentLayer.chunkParent.gameObject.layer;

                    //Color the Chunk
                    if (useDesertPalette) currentLayer.chunkParent.GetChild(j).gameObject.GetComponent<SpriteRenderer>().color = desertColorPalette[i];
                }
            }
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

            List<Transform> piecesToRecycle = new List<Transform>();
            //If there are chunks outside of the layer's render distance, add them to a list to recycle
            foreach (Transform piece in chunkLayer.chunkParent)
            {
                float pieceDistance = Vector3.Distance(followCamera.transform.position, piece.position);

                //If a piece is within the render distance, show it
                if (pieceDistance <= RENDER_DISTANCE)
                {
                    piece.gameObject.SetActive(true);
                }

                //If not, add it to the pile to hide
                if (pieceDistance > RENDER_DISTANCE)
                    piecesToRecycle.Add(piece);
            }

            //Return all chunk pieces outside of the render distance to the chunk pool
            foreach (Transform piece in piecesToRecycle)
                chunkLayer.ReturnChunkPieceToPool(piece.gameObject);
        }

        public List<ChunkParallaxLayer> GetParallaxLayers() => chunkParallaxLayers;

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
