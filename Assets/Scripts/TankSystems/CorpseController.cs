using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class CorpseController : MonoBehaviour
    {
        public class DummyObject : CorpseController
        {
            public GameObject dummyObject;
            public float lifetime;
        }

        public List<DummyObject> objects = new List<DummyObject>();
        public bool countDown = false;
        private float shrinkTime = 1.0f;

        // Start is called before the first frame update
        void Start()
        {
            ApplyForces();
            //StartCountdown(4f);
        }

        private void FixedUpdate()
        {
            if (countDown == true)
            {
                foreach(DummyObject _object in objects)
                {
                    if (_object.dummyObject != null)
                    {
                        _object.lifetime -= Time.fixedDeltaTime;

                        if (_object.lifetime < shrinkTime)
                        {
                            float rate = 0.8f * Time.deltaTime;
                            _object.dummyObject.transform.localScale -= new Vector3(rate, rate, 0);
                            if (_object.dummyObject.transform.localScale.x < 0) _object.dummyObject.transform.localScale = new Vector3(0, 0, 0);
                        }
                        if (_object.lifetime <= 0) Destroy(_object.dummyObject);
                    }
                }
            }
        }

        public void StartCountdown(float duration)
        {
            countDown = true;
            foreach(DummyObject _object in objects)
            {
                _object.lifetime = Random.Range(5f + duration, 7f + duration);
            }
        }

        public void ApplyForces()
        {
            foreach(DummyObject _object in objects)
            {
                Rigidbody2D rb = _object.dummyObject.GetComponent<Rigidbody2D>();

                //Calculate Direction based on relative position
                Vector2 direction = _object.dummyObject.transform.position - transform.position;
                float force = 1f;

                //Calculate Torque
                int sign = 1;
                float randomSign = Random.Range(0, 1f);
                if (randomSign < 0.5f) sign = -1;

                float randomRotation = Random.Range(5f, 10f);
                randomRotation *= sign;

                //Launch the Object
                rb.AddForce(direction * force, ForceMode2D.Impulse);
                rb.AddTorque(randomRotation, ForceMode2D.Impulse);
            }
        }
    }
}
