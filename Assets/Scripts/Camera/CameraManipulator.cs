using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using System.Linq;
using UnityEngine.Rendering.Universal;
using Sirenix.OdinInspector;
using UnityEngine.UI;

public class CameraManipulator : MonoBehaviour
{
    //Enums, Classes & Structs:
    /// <summary>
    /// Describes a camera system which tracks an individual tank in the level scene.
    /// </summary>
    public class TankCamSystem
    {
        //Objects & Components:
        [Tooltip("The tank(s) this system is targeting.")]                   internal List<TankController> tanks = new List<TankController>();
        [Tooltip("Camera component which renders this specific tank.")]      internal Camera cam;
        [Tooltip("Virtual camera pointed at the tank.")]                     internal CinemachineVirtualCamera vcam;
        [Tooltip("Collider used to manage offscreen visualization system.")] private BoxCollider2D boundCollider;

        //Runtime variables:
        [Tooltip("True if this is the primary camera system for the current player tank.")] public bool isPlayerCam;
        [Tooltip("Value indicating whether this is Cam A, Cam B, etc.")]                    public int camNum;
        [Tooltip("Determines whether or not this system is active and rendering.")]         public bool enabled = true;

        /// <summary>
        /// Ganerates a camera setup to track given tank.
        /// </summary>
        /// <param name="tank">The primary tank which this system will be associated with.</param>
        /// <param name="startEnabled">Set false to spawn a cam system but leave it dormant.</param>
        public TankCamSystem(TankController tank)
        {
            //Validity checks:
            if (tank == null) { Debug.LogError("Tried to spawn tank camera system using null script!"); return; }

            //Initialize main camera:
            cam = new GameObject("CAM_" + tank.TankName.Replace(" ", ""), typeof(Camera), typeof(UniversalAdditionalCameraData), typeof(CinemachineBrain)).GetComponent<Camera>(); //Generate main camera object with full suite of components
            cam.transform.parent = main.transform; //Child camera to camera manipulator object (used as a bucket for camera stuff)
            cam.orthographic = true;               //Make cam orthographic
            if (main.camSystems.Count == 0) camNum = 0; //Assign first cam number designator if no other cameras are present
            else //Camera needs to find lowest un-taken number
            {
                List<int> availableNums = new List<int>();                                                 //Initialize list to store available camera numbers
                for (int x = 0; x < main.maxTankCams; x++) availableNums.Add(x);                           //Add one incrementing cam value for each available potential cam number (based on cam manipulator setting)
                int[] takenNums = main.camSystems.Select(camSystem => camSystem.camNum).ToArray();         //Get array of taken cam numbers
                foreach (int num in takenNums) if (availableNums.Contains(num)) availableNums.Remove(num); //Remove taken numbers from availability list
                if (availableNums.Count == 0) //There is a problem, this cam system should not have been generated because it would exceed the available number of camera slots
                {
                    Debug.LogError("CameraManipulator tried to generate a TankCamSystem for " + tank.TankName + ", however there were no available camera slots."); //Indicate nature of problem
                    return;                                                                                                                                         //Do nothing else
                }
                camNum = availableNums[0]; //Get lowest available number as confirmed by taken number elimination sequence
            }
            string camLayerName = "TankCam" + "ABCDEFGH"[camNum].ToString();                                  //Get name of layer this camera will be on (needs to already be in LayerManager)
            string[] excludedCamLayers = main.camLayers.Where(layer => layer != camLayerName).ToArray();      //Get list of all camera layers excluding the one used by this camera
            LayerMask camLayerMask = ~0;                                                                      //Since the camera needs to be able to see every layer except excluded cameras, start with a layermask which includes every layer
            foreach (string layer in excludedCamLayers) camLayerMask &= ~(1 << LayerMask.NameToLayer(layer)); //Systematically exclude each camera layer which is not designated for this tank cam from the layermask
            cam.cullingMask = camLayerMask;                                                                   //Set camera culling mask so that camera can only see vcam on designated layer

            //Initialize virtual camera:
            vcam = new GameObject("VCAM_" + tank.TankName.Replace(" ", "")).AddComponent<CinemachineVirtualCamera>(); //Generate object with virtual camera component and get a reference to it
            vcam.transform.parent = main.transform;                                                                   //Child camera to camera manipulator object (used as a bucket for camera stuff)
            vcam.gameObject.layer = LayerMask.NameToLayer(camLayerName);                                              //Assign layer to virtual camera so that only this system's camera is rendering from it
            vcam.m_Follow = tank.treadSystem.transform;                                                               //Have camera lock to centerpoint of tank treadSystem by default
            CinemachineFramingTransposer transposer = vcam.AddCinemachineComponent<CinemachineFramingTransposer>();   //Add framing transposer to camera so that it's behavior can be fine-tuned
            transposer.m_TrackedObjectOffset.z = -10;                                                                 //Set camera z value so it can actually see the tank
            transposer.m_XDamping = 0;
            transposer.m_YDamping = 0;
            transposer.m_ZDamping = 0;

            //Generate additional objects:
            boundCollider = new GameObject("BoundCollider").AddComponent<BoxCollider2D>(); //Generate the boundary collider used for detecting offscreen objects
            boundCollider.transform.parent = cam.transform;                                //Child collider to main camera
            boundCollider.transform.localPosition = Vector3.zero;                          //Zero out relative position of collider
            boundCollider.gameObject.layer = LayerMask.NameToLayer("Camera");              //Put collider on camera layer so it doesn't interfere with anything else

            //Cleanup:
            tanks.Add(tank);                                                 //Add given tank controller as the first instance in list of tracked tanks
            if (tank.tankType == TankId.TankType.PLAYER) isPlayerCam = true; //Mark whether or not this is the player tank's camera system
            UpdateEverything();                                              //Immediately initialize all camera stuff
        }

        //FUNCTIONALITY METHODS:
        /// <summary>
        /// Performs all updates that are part of the cam system.
        /// </summary>
        public void UpdateEverything()
        {
            UpdateEnabledStatus(); //Update status
            if (!enabled) return;  //Do nothing if disabled

            UpdateCameraZone();    //Position camera in target zone according to various gameplay settings
            UpdateCameraValues();  //Update camera properties
            UpdateBoundCollider(); //Update collider around camera
        }
        /// <summary>
        /// Checks whether or not this system should be enabled based on proximity to player tank.
        /// </summary>
        public void UpdateEnabledStatus()
        {
            if (isPlayerCam) return; //Player cam is always enabled

            float distanceFromPlayer = Mathf.Abs(tanks[0].treadSystem.transform.position.x - TankManager.instance.playerTank.treadSystem.transform.position.x); //Get flat horizontal distance from player tank
            if (distanceFromPlayer > main.engagementDistance) ToggleEnabled(false); //Disable camera if tank is too far away from player
            else ToggleEnabled(true);                                               //Enable camera if tank is close enough to player
        }
        /// <summary>
        /// Updates position of camera system based on that of other cameras.
        /// </summary>
        public void UpdateCameraZone()
        {
            if (!enabled) return; //Do nothing if disabled

            Rect camRect = main.normalizedEngagementArea; //Get copy of engagement area rect in case it needs to be modified
            if (main.EnabledCamCount() > 1) //Rect needs to be a different size because there are multiple engaged tanks in scene
            {
                float screenPortion = 1f / main.EnabledCamCount();                                                              //Get horizontal slice of the zone this camera will be able to occupy
                float xPosition = main.camSystems.Where(c => c.enabled).ToList().IndexOf(this) * screenPortion * camRect.width; //Get normalized x position of camera rect RELATIVE to x position of camRect, adjusted for total rect width
                camRect.width = camRect.width * screenPortion;                                                                  //Divide width of rect into sections based on how many systems are sharing the screen
                camRect.position += new Vector2 (xPosition, 0);                                                                 //Set position of camera rect depending on order system is in list
            }
            cam.rect = camRect; //Apply changes in cam rect
        }
        /// <summary>
        /// Updates properties in the camera component.
        /// </summary>
        public void UpdateCameraValues()
        {
            if (!enabled) return; //Do nothing if disabled

            //Update ortho size:
            float tallestTankHeight = tanks[0].treadSystem.transform.InverseTransformPoint(tanks[0].highestCell.transform.position).y + 0.5f; //Get height of tallest cell in tank above tread system (add half a unit to account for height of cell itself)
            if (tanks.Count > 1) //There are other tanks to consider
            {
                for (int x = 1; x < tanks.Count; x++) //Iterate through tanks in system
                {
                    float tankHeight = tanks[x].treadSystem.transform.InverseTransformPoint(tanks[x].highestCell.transform.position).y + 0.5f; //Get height of this tank
                    tallestTankHeight = Mathf.Max(tallestTankHeight, tankHeight);                                                              //Use highest tank
                }
                
            }
            float orthoSize = (tallestTankHeight + main.tankCamLowerBuffer + main.tankCamUpperBuffer) / 2; //Get ortho size by adding the exact height of the tank to that of both buffer zones and correcting for ortho scale
            vcam.m_Lens.OrthographicSize = orthoSize;                                                      //Set ortho size of camera
            
            //Update y offset:
            float yOffset = (main.tankCamUpperBuffer + tallestTankHeight -main.tankCamLowerBuffer) / 2;     //Get camera y offset by getting sum of all height modifiers and finding center by dividing by 2
            vcam.GetCinemachineComponent<CinemachineFramingTransposer>().m_TrackedObjectOffset.y = yOffset; //Apply y offset to camera component
        }
        /// <summary>
        /// Updates size of boundary collider to match size of camera frame.
        /// </summary>
        public void UpdateBoundCollider()
        {
            if (!enabled) return; //Do nothing if disabled

            float camHeight = cam.orthographicSize * 2;                                                          //Get real world height of camera frame
            float radiusBuffer = main.boundColliderEdgeRadius * 2;                                               //Get value to account for space taken up by radius of collider
            boundCollider.size = new Vector2((cam.aspect * camHeight) - radiusBuffer, camHeight - radiusBuffer); //Set size of collider
            boundCollider.edgeRadius = main.boundColliderEdgeRadius;                                             //Set size of edge radius
        }
        /// <summary>
        /// Deletes cam system elements.
        /// </summary>
        public void CleanUp()
        {
            Destroy(cam.gameObject);  //Destroy camera object (also destroys bound collider)
            Destroy(vcam.gameObject); //Destroy virtual camera object
        }
        /// <summary>
        /// Enables or disables this cam system.
        /// </summary>
        /// <param name="newStatus"></param>
        public void ToggleEnabled(bool newStatus)
        {
            if (newStatus == enabled) return; //Skip if action is redundant
            enabled = newStatus;              //Indicate whether or not system is now enabled
            vcam.enabled = newStatus;         //Set vcam status
            cam.enabled = newStatus;          //Set camera status
        }
    }

    //Objects & Components:
    [Tooltip("Singleton instance of camera manipulator in scene.")]                   public static CameraManipulator main;
    [Tooltip("List of camera systems being used to actively render tanks in scene.")] private List<TankCamSystem> camSystems = new List<TankCamSystem>();

    //Settings:
    [Header("General Settings:")]
    [SerializeField, Tooltip("Maximum possible number of concurrent tank cam systems (there need to be layers made in layerManager for these. Cam layers need to be in sequential indexes and the first must be named TankCamA)"), Min(1)] private int maxTankCams = 3;
    [Header("Camera Zone Positioning:")]
    [SerializeField, Tooltip("UI object used to position engagement zone camera setup in scene.")] private RectTransform engagementZoneTargeter;
    [SerializeField, Tooltip("UI object used to position radar zone camera in scene.")]            private RectTransform radarZoneTargeter;
    [Header("Tank Camera Settings:")]
    [SerializeField, Tooltip("Camera space to leave above top cell of tank (in world units).")] private float tankCamUpperBuffer;
    [SerializeField, Tooltip("Camera space to leave below treads of tank (in world units).")]   private float tankCamLowerBuffer;
    [Space]
    [SerializeField, Tooltip("Range (from player tank) at which an enemy tank's camera will become active."), Min(0)]       private float engagementDistance;
    [SerializeField, Tooltip("Range (from player tank) at which enemy tank camera will merge with player camera."), Min(0)] private float shareCamDistance;
    [Header("Offscreen Visualization Settings:")]
    [SerializeField, Tooltip("Roundness of collider corners around camera plane (smooths out edge UI)."), Min(0)] private float boundColliderEdgeRadius = 1;

    //Runtima Variables:
    private Canvas zoneVisCanvas;          //Generated canvas which contains visualizers for camera zones (used to position where cameras will render)
    private string[] camLayers;            //Array of all user-assigned camera layers in the game (used to make each separate cam exclusive of others)
    private Rect normalizedEngagementArea; //Normalized rectangle representing area on screen that engagement cameras are confined to and rendered in

    //UNITY METHODS:
    private void Awake()
    {
        //Initialization:
        main = this; //Make latest instance of this script main

        //Get runtime variables:
        zoneVisCanvas = GetComponentInChildren<Canvas>();                                                                                                      //Get canvas containing camera zone visualization boxes
        camLayers = Enumerable.Range(0, 32).Select(index => LayerMask.LayerToName(index)).Where(l => !string.IsNullOrEmpty(l) && l.Contains("Cam")).ToArray(); //Get all layers (by name) with "Cam" in the name

        //Hide visualizers:
        engagementZoneTargeter.GetComponent<Image>().enabled = false; //Disable image for engagement zone camera targeter
        radarZoneTargeter.GetComponent<Image>().enabled = false;      //Disable image for radar zone camera targeter
    }
    private void Start()
    {
        //Debug/demo stuff:
        //if (TankManager.instance != null) new TankCamSystem(TankManager.instance.playerTank); //Generate a camera for player tank

        //Setup:
        normalizedEngagementArea = GetNormalizedRect(engagementZoneTargeter, zoneVisCanvas); //Get area for engagement cameras to occupy
    }
    private void Update()
    {
        //Regular updates:
        if (camSystems.Count > 0) //Cam updater
        {
            foreach (TankCamSystem system in camSystems) system.UpdateEverything(); //Fully update all values in each camera system
        }
        if (Application.isEditor) //Editor-specific updates
        {
            normalizedEngagementArea = GetNormalizedRect(engagementZoneTargeter, zoneVisCanvas); //Re-locate engagement area in case it is being changed at runtime for debug purposes
        }
    }

    //FUNCTIONALITY METHODS:
    /// <summary>
    /// Returns given rect normalized relative to game resolution and scale factor of given canvas.
    /// </summary>
    /// <param name="target">RectTransform component to get normalized values for.</param>
    /// <returns></returns>
    private Rect GetNormalizedRect(RectTransform target, Canvas refCanvas)
    {
        Rect normalizedRect = target.rect;                                                                //Get current dimensions of engagementZoneTargeter so that they can be adjusted into normalized screen space
        Vector2 screenRes = new Vector2(Screen.currentResolution.width, Screen.currentResolution.height); //Get width and height (in pixels) of active screen
        Vector2 displayRes = new Vector2(Display.main.renderingWidth, Display.main.renderingHeight);      //Get width and height (in pixels) of display setting (may be different from screen)
        Vector2 resModFactor = screenRes / displayRes;                                                    //Get scale adjustment factor depending on discrepancy between resolution of display and resolution of game
        normalizedRect.size = (normalizedRect.size / screenRes) * refCanvas.scaleFactor;                  //Normalize world values of camera target size relative to screen size
        normalizedRect.position = target.position / screenRes;                                            //Normalize world values of camera target position relative to screen size
        normalizedRect.size *= resModFactor;                                                              //Factor resolution modifier into zone size
        normalizedRect.position *= resModFactor;                                                          //Factor resolution modifier into zone position
        return normalizedRect;                                                                            //Return calculated rect
    }
    private void GenerateCamSystem(TankController targetTank)
    {
        TankCamSystem newSystem = new TankCamSystem(targetTank); //Generate new system using constructor
        main.camSystems.Add(newSystem);                          //Add this to master list of tank camera systems
    }

    //EVENT METHODS:
    /// <summary>
    /// Called by TankManager when a new tank is spawned.
    /// </summary>
    /// <param name="tank"></param>
    public void OnTankSpawned(TankController tank)
    {
        foreach (TankCamSystem system in camSystems) if (system.tanks.Contains(tank)) return; //Skip if tank is already present in cam system
        GenerateCamSystem(tank); //Immediately generate a cam system for spawned tank
    }
    /// <summary>
    /// Called by TankManager when a tank is destroyed.
    /// </summary>
    /// <param name="tank"></param>
    public void OnTankDestroyed(TankController tank)
    {
        //Remove tank from camSystems:
        for (int x = 0; x < camSystems.Count;) //Iterate through all cam systems
        {
            if (camSystems[x].tanks.Contains(tank)) //Cam system needs to be cleaned up
            {
                if (camSystems[x].tanks.Count == 1) //Entire cam system needs to go
                {
                    camSystems[x].CleanUp(); //Destroy camera elements
                    camSystems.RemoveAt(x);  //Remove cam system from list, destroying it
                    continue;                //Skip everything else without incrementing x value (because an entry in list was removed)
                }
                else //Cam system list needs to be modified
                {
                    camSystems[x].tanks.Remove(tank); //Just remove tank from camSystem list
                }
            }
            x++; //Iterate if no tank has been removed
        }
    }

    //UTILITY METHODS:
    /// <summary>
    /// The number of currently-enabled cam systems.
    /// </summary>
    public int EnabledCamCount() { return camSystems.Where(c => c.enabled == true).Count(); }
}
