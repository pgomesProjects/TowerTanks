using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class ParallaxSpawner : MonoBehaviour
    {
        public GameObject[] spawnerPrefabs;
        public List<GameObject> objects = new List<GameObject>();
        public Transform startPos;
        public Transform endPos;

        public Vector2 frequency;
        public Vector2 yOffsetRange;
        public Vector2 scaleRange;

        private float spawnerTimer;

        private void Start()
        {
            foreach(GameObject child in objects)
            {
                child.transform.localScale *= RandomizeScale();
            }

            SpawnObject();
            spawnerTimer = RollFrequency();
        }


        private void FixedUpdate()
        {
            spawnerTimer -= Time.fixedDeltaTime;
            if (spawnerTimer <= 0)
            {
                SpawnObject();
                spawnerTimer = RollFrequency();
            }

            foreach(GameObject chunk in objects)
            {
                if (chunk.transform.position.x <= endPos.position.x)
                {
                    objects.Remove(chunk);
                    Destroy(chunk);
                    break;
                }
            }
        }

        void SpawnObject()
        {
            if (spawnerPrefabs.Length <= 0) return;
            int random = Random.Range(0, spawnerPrefabs.Length);

            GameObject chunk = Instantiate(spawnerPrefabs[random], this.transform);
            chunk.transform.position = startPos.position;

            Vector2 offset = new Vector2(chunk.transform.position.x, chunk.transform.position.y + RollYOffset());
            chunk.transform.position = offset;

            objects.Add(chunk);
            chunk.transform.localScale *= RandomizeScale();
        }

        float RollFrequency()
        {
            float f = Random.Range(frequency.x, frequency.y);

            return f;
        }

        float RollYOffset()
        {
            float y = Random.Range(yOffsetRange.x, yOffsetRange.y);

            return y;
        }

        float RandomizeScale()
        {
            float s = Random.Range(scaleRange.x, scaleRange.y);

            return s;
        }
    }
}
