using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class CollisionTransmitter : MonoBehaviour
    {
        //Objects & Components:
        /// <summary>
        /// Object which this transmitter is sending data to, done in case some collision system needs more data from that object.
        /// </summary>
        public GameObject target;

        //Delegates & Events:
        public delegate void CollisionEvent(Collision2D collision);
        public event CollisionEvent collisionEnter;
        public event CollisionEvent collisionStay;
        public event CollisionEvent collisionExit;

        //UNITY METHODS:
        private void Awake()
        {
            //Subscribe to default methods: 
            collisionEnter += EmptyMethod;
            collisionStay += EmptyMethod;
            collisionExit += EmptyMethod;
        }
        private void OnDestroy()
        {
            //Unsubscribe from default methods:
            collisionEnter -= EmptyMethod;
            collisionStay -= EmptyMethod;
            collisionExit -= EmptyMethod;
        }
        private void OnCollisionEnter2D(Collision2D collision)
        {
            collisionEnter.Invoke(collision); //Invoke event to send it to relevant scripts
        }
        private void OnCollisionStay2D(Collision2D collision)
        {
            collisionStay.Invoke(collision); //Invoke event to send it to relevant scripts
        }
        private void OnCollisionExit2D(Collision2D collision)
        {
            collisionExit.Invoke(collision); //Invoke event to send it to relevant scripts
        }
        private void EmptyMethod(Collision2D collision) { } //This is here so we have something to subscribe delegates to so they don't throw empty invoke errors
    }
}
