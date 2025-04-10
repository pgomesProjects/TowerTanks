using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    /// <summary>
    /// Used to translate positional and spatial information about a variety of potential camera targets into generic data useable by a CamSystem. This goes on the object which the camera will target, and is capable of recognizing and dealing with certain specialized object types (such as tanks).
    /// </summary>
    public class CamTarget : MonoBehaviour
    {
        //Static Stuff:
        [Tooltip("List of all cam targets in scene.")] public static List<CamTarget> sceneTargets = new List<CamTarget>();

        //Objects & Components:
        [Tooltip("The camera system this target is currently associated with.")] internal CombatCameraController.CamSystem system;

        //Settings:
        [Header("Targeting Settings:")]
        [Tooltip("Transform which camera will actively focus on. Leave null if same as object this script is on")] public Transform centerTransform;

        //Runtime Variables:
        private TankController tank; //If not null, indicates that this target refers to a tank

        //RUNTIME METHODS:
        private void Awake()
        {
            sceneTargets.Add(this); //Add to static list of cam targets upon instantiation

            tank = GetComponent<TankController>();                             //Try to get a tank controller from current object
            if (tank == null) tank = GetComponentInChildren<TankController>(); //Try to get a tank controller from childed objects
        }
        private void Start()
        {
            if (tank != null) //This target is for tank
            {
                if (tank.tankType == TankId.TankType.PLAYER) //This target is the player tank
                {
                    CombatCameraController.main.playerTankCam.AttachTarget(this); //Attach target to main camera feed
                    CombatCameraController.main.radarCam.AttachTarget(this);      //Attach target to radar camera feed
                }
                else //This target is a non-player tank
                {
                    CombatCameraController.main.opponentTankCam.AttachTarget(this); //Attach target to opponent viewer camera feed
                }
            }
        }
        private void OnDestroy()
        {
            if (system != null) system.DetachTarget(); //Detach this target from given system
            sceneTargets.Remove(this);                 //Remove from static list of cam targets upon destruction
        }
        private void OnDrawGizmos()
        {
            
        }

        //FUNCTIONALITY METHODS:
        /// <summary>
        /// Gets bounding area of target object (which camera needs to minimally keep in frame), using specialized methods for certain objects such as tanks.
        /// </summary>
        public Bounds GetTargetBounds()
        {
            //Initialize:
            Bounds bounds = new(); //Initialize container to store acquired bounds

            //Tank-specific bounds calculation:
            if (tank != null) //Target is a tank
            {
                //Get size from tank values and center from current tank bounds (accounts for orientation):
                Vector2 size = new Vector2(tank.tankSizeValues.y + tank.tankSizeValues.w, tank.tankSizeValues.x + tank.tankSizeValues.z); //Get size of tank (different from current bounds because it doesn't account for rotation) from tank size values chart
                Vector2 position = tank.treadSystem.GetTankBounds().center;                                                               //Use real current bounds to get center mass of tank (so that correct positional adjustments will be made

                //Expand bounds to account for potential rotation:
                //NOTE: This feature needs to be added but can for now be substituted by adding a buffer
                //bounds = new Bounds(position, size); //Return bounds of calculated position and size
            }

            //Cleanup:
            return bounds; //Return calculated bounds
        }
        /// <summary>
        /// Returns raw transform component this target is tracking.
        /// </summary>
        public Transform GetTargetTransform() { if (centerTransform == null) return transform; else return centerTransform; }
    }
}
