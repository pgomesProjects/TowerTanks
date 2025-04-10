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
                Vector4 tsv = tank.tankSizeValues; //Get shorthand for tank size value vector (because we will be referencing it a lot)
                Vector2 size = new Vector2(tsv.y + tsv.w, tsv.x + tsv.z); //Get size of tank (different from current bounds because it doesn't account for rotation) from tank size values chart
                Vector2 position = tank.treadSystem.GetTankBounds().center;

                /*
                Vector2 sampleCorner = tank.rooms[0].cells[0].corners[0].position;
                Vector2[] extremeCorners = { sampleCorner, sampleCorner, sampleCorner, sampleCorner};
                foreach (Room room in tank.rooms) //Iterate through every room in tank
                {
                    foreach (Cell cell in room.cells) //Iterate through every cell in room
                    {
                        foreach (Transform corner in cell.corners) //Iterate through all four corners of each cell
                        {
                            if (corner.position.y > extremeCorners[0].x) extremeCorners[0] = corner.position; //Store highest position
                            if (corner.position.x > extremeCorners[1].y) extremeCorners[1] = corner.position; //Store rightmost position
                            if (corner.position.y < extremeCorners[2].x) extremeCorners[2] = corner.position; //Store lowest position
                            if (corner.position.x < extremeCorners[3].y) extremeCorners[3] = corner.position; //Store leftmost position
                        }
                    }
                }
                Bounds tankBounds = new Bounds(sampleCorner, Vector2.zero);
                foreach (Vector2 corner in extremeCorners) tankBounds.Encapsulate(corner);
                Vector2 position = tankBounds.center - tank.treadSystem.transform.position;
                */

                //float posX = (Mathf.Abs(currentSizeValues.w) - (Mathf.Abs(currentSizeValues.y)) / 2); //Get offset center X value from angle-adjusted tank size values (center will not necessarily align with tank transform position)
                //float posY = (Mathf.Abs(currentSizeValues.z) - (Mathf.Abs(currentSizeValues.x)) / 2); //Get offset center Y value from angle-adjusted tank size values
                //Vector2 position = new Vector2(posX, posY); //Create vector to store position
                //position += (Vector2)tank.treadSystem.transform.position; //Place position in world space using treadsystem position (because this is what the tankSizeValues are relative to)

                /*Vector2[] corners = { new(-tsv.w, tsv.x), new(tsv.y, tsv.x), new(tsv.y, -tsv.z), new(-tsv.w, -tsv.z) }; //Create a matrix representing the four corners of the tank bounds rectangle
                for (int c = 0; c < 4; c++) corners[c] = Quaternion.AngleAxis(-tank.treadSystem.transform.eulerAngles.z, Vector3.back) * corners[c]; //Rotate corner skewed central axis of the tank based on current tank rotation
                Bounds rotBounds = new Bounds(Vector3.zero, Vector3.zero);
                foreach (Vector2 corner in corners) rotBounds.Encapsulate(corner);
                Vector2 position = rotBounds.center;*/

                /*
                if (Mathf.Abs(corners[2].x - corners[0].x) > Mathf.Abs(corners[3].x - corners[1].x)) //Tank is rotated counter-clockwise (pair of opposite corners with the greatest X difference decides this)
                {
                    //NOTE: All this garbage is done to avoid having to use actual bounding boxes, the physics of which messes with cameras
                    position.x = (corners[0].x + corners[2].x) / 2; //Get offset center X value from rotated AABB based on tank size values (because tank can be lopsided, and rotation needs to account for the CORNERS of the bounding box)
                    position.y = (corners[1].y + corners[3].y) / 2; //Get offset center Y value from rotated AABB based on tank size values
                }
                else //Tank is rotated clockwise (or is perfectly vertical)
                {
                    position.x = (corners[1].x + corners[3].x) / 2; //Get offset center X value from rotated AABB based on tank size values
                    position.y = (corners[0].y + corners[2].y) / 2; //Get offset center Y value from rotated AABB based on tank size values
                }*/

                /*
                float posX = ((tank.tankSizeValues.y - tank.tankSizeValues.w) / 2);               //Get offset center X value from tank size values (center will not necessarily align with tank transform position)
                float posY = ((tank.tankSizeValues.x - tank.tankSizeValues.z) / 2);               //Get offset center Y value from tank size values
                Vector2 position = new Vector2(); //Create vector to store position
                position += (Vector2)(Quaternion.AngleAxis(-tank.treadSystem.transform.eulerAngles.z, Vector3.back) * new Vector3(posX, posY, 0));
                position += (Vector2)tank.treadSystem.transform.position; //Place position in world space using treadsystem position (because this is what the tankSizeValues are relative to)
                */

                //Expand bounds to account for potential rotation:
                //NOTE: This feature needs to be added but can for now be substituted by adding a buffer
                bounds = new Bounds(position, size); //Return bounds of calculated position and size
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
