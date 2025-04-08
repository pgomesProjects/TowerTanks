using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class PointAndStretch : MonoBehaviour
    {
        //Objects & Components:
        [SerializeField, Tooltip("Transform which this object will point its Transform.Up toward.")] private Transform target;

        //Runtime variables:
        private Vector3 origScale; //Original scale of object

        private void Update()
        {
            if (target != null) //Only do process if a target is present
            {
                //Rotate toward target:
                Vector2 direction = (target.position - transform.position).normalized; //Get direction from this position to target
                transform.up = direction;                                              //Point in given direction

                //Stretch to target:
                float distance = Vector2.Distance(target.position, transform.position); //Get distance between this object and target
                transform.localScale = new Vector3(1, distance, 1);                     //Stretch object to given distance
            }
            
        }
    }
}
