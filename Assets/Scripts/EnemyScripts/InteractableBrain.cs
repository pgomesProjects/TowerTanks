using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class InteractableBrain : MonoBehaviour //should be disabled in scene view on scene load, or won't work
    {
        private bool switchedOn = false;
        //will define common functionality for all interactable AI Brains. just here to use for polymorphism for now
        private void OnEnable()
        {
            switchedOn = true;
        }
        
        private void OnDisable()
        {
            switchedOn = false;
        }

        // private void OnDrawGizmos()
        // {
        //     Gizmos.color = Color.red;
        //     //draw the bounds of this monobehaviour
        //     Collider2D collider = GetComponent<Collider2D>();
        //     if (collider != null && switchedOn)
        //     {
        //         Bounds bounds = collider.bounds;
        //         Gizmos.color = Color.red;
        //         Gizmos.DrawWireCube(bounds.center, bounds.size);
        //     }
        // }
    }
}
