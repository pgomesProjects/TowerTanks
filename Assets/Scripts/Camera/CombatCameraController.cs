using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using System.Linq;
using UnityEngine.Rendering.Universal;
using Sirenix.OdinInspector;
using UnityEngine.UI;

namespace TowerTanks.Scripts
{
    public class CombatCameraController : MonoBehaviour
    {
        //Classes, Enums & Structs:
        /// <summary>
        /// Indicates what the primary function and target of a given CamSystem is.
        /// </summary>
        public enum CamSystemType { Player, Opponent, Radar }
        /// <summary>
        /// Describes a generic camera system which can be configured to contain a target or set of targets, shake, use the combat scene parallax background, etc.
        /// </summary>
        [System.Serializable]
        public class CamSystem
        {
            //Objects & Components:
            [Tooltip("The primary object which this camera system will be following.")]       public CamTarget target;
            [Tooltip("List of objects which are temporarily targeted by the camera system.")] public List<CamTarget> subTargets = new List<CamTarget>();
            [Tooltip("Camera component which renders this specific target.")]                 internal Camera cam;
            [Tooltip("Virtual camera which points at the target")]                            internal CinemachineVirtualCamera vcam;
            [Tooltip("Scripts controlling the parallax background sfor this cam system.")]    private List<MultiCameraParallaxController> parallaxControllers = new List<MultiCameraParallaxController>();

            //Runtime Variables:
            [Tooltip("Indicate what this camera system is for.")] public CamSystemType type;

            //RUNTIME METHODS:
            /// <summary>
            /// Generate a generic camera system which tracks the given target.
            /// </summary>
            /// <param name="name">Designates which camera this is (can be PlayerTank, OpponentTank, or Radar).</param>
            public CamSystem(CamSystemType camType)
            {
                //Initialization:
                type = camType; //Store designated type

                //Initialize camera:
                cam = new GameObject("CAM_" + type.ToString(), typeof(Camera), typeof(UniversalAdditionalCameraData), typeof(CinemachineBrain)).GetComponent<Camera>(); //Generate main camera object with a full suite of components for hooking up to a cinemachine vcam
                cam.transform.parent = main.transform;                                                                                                                  //Child camera to camera container object
                cam.orthographic = true;                                                                                                                                //Make cam orthographic (because it is a 2D game)
                string camLayerName = "TankCam" + "ABC"[(int)camType].ToString();                                                                                       //Find layer name to match to this camera, designed to be backwards compatible with layers made for old CameraManipulator
                cam.cullingMask = main.MakeCamMask(camLayerName);                                                                                                       //Set camera culling mask so that camera can only see vcam on designated layer

                //Initialize virtual camera:
                vcam = new GameObject("VCAM_" + type.ToString()).AddComponent<CinemachineVirtualCamera>(); //Generate object with virtual camera component and get a reference to it
                vcam.transform.parent = main.transform;                                                    //Child camera to camera container
                vcam.gameObject.layer = LayerMask.NameToLayer(camLayerName);                               //Assign layer to virtual camera so that only this system's camera is rendering from it
                vcam.m_Lens.NearClipPlane = -10;                                                           //Bring near clip plane forward so that level is visible
                CinemachineTransposer transposer = vcam.AddCinemachineComponent<CinemachineTransposer>();  //Use a transposer to finely adjust camera settings
                transposer.m_XDamping = 0; transposer.m_YDamping = 0; transposer.m_ZDamping = 0;           //Turn off all camera damping

                //Type-specific setup:
                vcam.Priority = type == CamSystemType.Opponent || type == CamSystemType.Radar ? 1 : 0; //Raise camera priority if it needs to be rendered over player camera
                if (type == CamSystemType.Radar) //Only perform the following setup for the radar cam
                {
                    //Extra camera setup:
                    cam.transform.tag = "RadarCam";                                                                   //Tag the camera radar so stuff knows what it is
                    cam.clearFlags = CameraClearFlags.SolidColor;                                                     //Change the background type to only show a solid color
                    cam.backgroundColor = Color.black;                                                                //Set the background color to black
                    cam.cullingMask = 1 << LayerMask.NameToLayer("TankCamC") | 1 << LayerMask.NameToLayer("Minimap") | 1 << LayerMask.NameToLayer("RadarCam"); //Set the camera to only render the radar cam and the minimap cam
                    //NOTE: RADARCAM IN THE ABOVE LINE IS TEMPORARY

                    //Extra elements:
                    if (main.radarGrid != null) Instantiate(main.radarGrid, vcam.transform); //Add a grid visual to the radar
                }
                else //Setup for non-radar cameras only
                {
                    //Extra camera setup:
                    cam.cullingMask &= ~(1 << LayerMask.NameToLayer("Minimap")); //Remove the minimap layer from the camera

                    //Screenshake setup:
                    CinemachineBasicMultiChannelPerlin perlin = vcam.AddCinemachineComponent<CinemachineBasicMultiChannelPerlin>(); //Add perlin noise component to camera
                    perlin.m_NoiseProfile = main.shakeNoiseProfile;                                                                 //Set noise profile to predetermined value
                    perlin.m_AmplitudeGain = 0;                                                                                     //Have camera default to no noise
                    perlin.m_FrequencyGain = 1;                                                                                     //Have camera default to no noise

                    //Parallax setup:
                    int chunkLayerCounter = 0;
                    for (int i = 0; i < main.parallaxPrefabs.Length; i++)
                    {
                        parallaxControllers.Add(Instantiate(main.parallaxPrefabs[i]).GetComponent<MultiCameraParallaxController>());                //Instantiate a parallax background for this camera and get a reference
                        parallaxControllers[i].transform.parent = main.transform;                                                                   //Child parallax object to camera container
                        parallaxControllers[i].gameObject.name = "Parallax_" + cam.name + "_[" + i + "]";                                           //Rename parallax object for clarity
                        parallaxControllers[i].AddCameraToParallax(cam);                                                                            //Add this camera to controller so it is tracked properly
                        parallaxControllers[i].transform.position = Vector3.zero;                                                                   //Zero out position of parallax system
                        if (parallaxControllers[i].useDesertPalette) cam.backgroundColor = parallaxControllers[i].desertColorPalette[0];            //Use alternate background color if bool is checked
                        foreach (Transform child in parallaxControllers[i].transform) child.gameObject.layer = LayerMask.NameToLayer(camLayerName); //Put each parallax layer on a layer which can only be seen by this camera

                        if (parallaxControllers[i].GetType() == typeof(ChunkCameraParallaxController))                                               //Check if the parallax background is a chunk parallax
                        {
                            ChunkCameraParallaxController chunkParallaxController = (ChunkCameraParallaxController)parallaxControllers[i];          //Cast the parallax controller to the chunk parallax controller
                            chunkParallaxController.PositionLayers(main.chunkCameraPositions[chunkLayerCounter]);                                   //Reposition all of the chunk pieces
                            chunkLayerCounter++;                                                                                                    //Iterate on the chunk layer counter
                        }
                    }
                }
            }

            /// <summary>
            /// Updates all camera values and behavior
            /// </summary>
            /// <param name="deltaTime">Time (in seconds) since last update.</param>
            public void Update(float deltaTime)
            {
                //Check enabled status:
                if (type == CamSystemType.Opponent) //Opponent camera may be enabled or disabled depending on conditions
                {
                    if (target != null) //Target is present
                    {
                        float targetDistance = Mathf.Abs(main.playerTankCam.target.GetTargetTransform().position.x - target.GetTargetTransform().position.x); //Get distance between player cam target and opponent
                        if (targetDistance <= main.opponentVisDistance) //Target is within visibility range
                        {
                            if (!cam.enabled) cam.enabled = true; //Enable camera if it isn't already
                        }
                        else if (cam.enabled) //Target is outside visibility range and cam is enabled
                        {
                            cam.enabled = false; //Disable camera
                        }
                    }
                    else if (cam.enabled) cam.enabled = false; //Disable camera if there is no current target
                }
                if (!cam.enabled) return; //Do nothing else if camera is not enabled

                //Modify viewport rect:
                Rect camRect = new(); //Initialize container to store final camera rect
                if (type == CamSystemType.Radar) //The player camera output always takes up the entire screen
                {
                    camRect = main.radarRect; //Always use the preset radar rect
                }
                else if (type == CamSystemType.Player) //The radar camera is usually fixed in place
                {
                    camRect = new(Vector2.zero, Vector2.one); //Always use a rect which fills the entire screen
                }
                else if (type == CamSystemType.Opponent) //The opponent camera rect position changes depending on certain factors
                {
                    camRect = main.minOpponentRect; //MODIFY THIS LATER
                }
                cam.rect = camRect; //Apply changes in cam rect

                //Get minimum bounds for target:
                Bounds targetBounds = target.GetTargetBounds();                                                    //Get bounds from target
                foreach (CamTarget subTarget in subTargets) targetBounds.Encapsulate(subTarget.GetTargetBounds()); //Encapsulate other subtargets camera is following
                targetBounds.size += new Vector3(main.bufferValues.y + main.bufferValues.w, main.bufferValues.x + main.bufferValues.z);               //Add universal buffer size to bounds
                targetBounds.center += new Vector3((main.bufferValues.y - main.bufferValues.w) / 2, (main.bufferValues.x - main.bufferValues.z) / 2); //Adjust centerpoint of bounds so that asymmetrical settings do not offset bounding box

                //Frame target:
                CinemachineTransposer transposer = vcam.GetCinemachineComponent<CinemachineTransposer>(); //Get transposer component so that offset values can be modified
                if (type == CamSystemType.Radar) //Framing for the radar camera
                {
                    //Adjust zoom:
                    vcam.m_Lens.OrthographicSize = (main.radarRange / cam.aspect) / 2; //Adjust ortho size of camera so that radar is precisely rendering designated range at all times

                    //Edit position of tank in frame:
                    Vector2 followOffset = new Vector2((cam.aspect * cam.orthographicSize) - targetBounds.extents.x, 0); //Calibrate follow offset so tank is glued to center of far left of radar
                    transposer.m_FollowOffset = new Vector3(followOffset.x, followOffset.y, 0);                        //Move tank to target position in frame, keeping camera distance at consistent level
                }
                else if (type == CamSystemType.Player) //Framing for the main player camera
                {
                    //Adjust zoom:
                    Rect confines = main.GetNormalizedRect(main.mainCamConfiner, main.zoneVisCanvas);       //Get rectangle in screen defining area where target is allowed to be present (so it is not obscured by UI)
                    float widthOrthoSize = ((targetBounds.size.x / 2) / cam.aspect) * (1 / confines.width); //Determine orthographic size as would be defined by width of bounds (factor in confine width inversely because making the confines larger means the camera can zoom in (smaller ortho size))
                    float heightOrthoSize = (targetBounds.size.y / 2) * (1 / confines.height);              //Determine orthographic size as would be defined by height of bounds (factor in confine height inversely)
                    float targetOrthoSize = Mathf.Max(widthOrthoSize, heightOrthoSize);                     //Pick whichever dimension needs more space as the determinant of ortho size
                    vcam.m_Lens.OrthographicSize = targetOrthoSize;                                         //Set ortho size

                    //Edit position of tank in frame:
                    Vector2 frameDimensions = new Vector2(cam.aspect * (cam.orthographicSize * 2), cam.orthographicSize * 2);                                  //Determine dimensions of frame in units based on aspect ratio and orthographic size
                    Vector2 followOffset = (frameDimensions / 2) - (confines.min * frameDimensions);                                                           //Get base follow offset that puts target center on lower left corner of allowed confines (finding actual world position of confines min by multiplying it by frame dimensions)
                    followOffset += (Vector2)(targetBounds.min - target.GetTargetTransform().position);                                                        //Move inward from base follow offset so that entire target bounds are accounted for
                    transposer.m_FollowOffset = Vector2.Lerp(transposer.m_FollowOffset, new Vector3(followOffset.x, followOffset.y, 0), main.cameraSmoothing); //Move tank to target position in frame, keeping camera distance at consistent level (smooth camera to reduce jitter)
                }
                else if (type == CamSystemType.Opponent) //Framing for the opponent subcamera
                {
                    //Adjust zoom:
                    float widthOrthoSize = (targetBounds.size.x / 2) / cam.aspect;      //Determine ortho size as would be defined by width of bounds
                    float heightOrthoSize = targetBounds.size.y / 2;                    //Determine ortho size as would be defined by height of bounds
                    float targetOrthoSize = Mathf.Max(widthOrthoSize, heightOrthoSize); //Pick whichever dimension needs more space as the determinant of ortho size
                    vcam.m_Lens.OrthographicSize = targetOrthoSize;                     //Set ortho size

                    //Edit position of tank in frame:
                    float followOffsetY = cam.orthographicSize + (targetBounds.min.y - target.GetTargetTransform().position.y);                              //Get vertical component of follow offset seperately
                    float followOffsetX = (targetBounds.center.x - target.GetTargetTransform().position.x);                                                  //Just get the horizontal distance between followpoint and actual center of bounds
                    transposer.m_FollowOffset = Vector2.Lerp(transposer.m_FollowOffset, new Vector3(followOffsetX, followOffsetY, 0), main.cameraSmoothing); //Move tank to position pinned to bottom of frame but centered horizontally (smooth camera to reduce jitter)
                }
            }

            //FUNCTIONALITY METHODS:
            /// <summary>
            /// Attaches given target to this system.
            /// </summary>
            public void AttachTarget(CamTarget newTarget)
            {
                target = newTarget;                                                                         //Get reference to new target
                vcam.m_Follow = target.centerTransform == null ? target.transform : target.centerTransform; //Get transform of target, defaulting to transform on same object as target component if no center is designated
                target.system = this;                                                                       //Indicate to target which system is referencing it
            }
            /// <summary>
            /// Disconnects this cam system from its target.
            /// </summary>
            public void DetachTarget()
            {
                if (target == null) { Debug.LogWarning("Detach target was called on a cam system with no target."); return; } //Do nothing if system has no target

                target = null;        //Clear reference to target
                vcam.m_Follow = null; //Clear follow target for VCam
            }
        }

        //Objects & Components:
        [Tooltip("Singleton instancce of camera controller in scene.")]                              public static CombatCameraController main;
        [Tooltip("Camera system which displays the player tank.")]                                   internal CamSystem playerTankCam;
        [Tooltip("Camera system which displays the currently-focused enemy tank.")]                  internal CamSystem opponentTankCam;
        [Tooltip("Camera system which displays the player tank as a radar screen.")]                 internal CamSystem radarCam;
        [SerializeField, Tooltip("Noise profile asset describing behavior of camera shake events.")] private NoiseSettings shakeNoiseProfile;
        [SerializeField, Tooltip("Prefabs for parallax systems instantiated for each camera.")]      private GameObject[] parallaxPrefabs;
        [SerializeField, Tooltip("The grid for the radar camera.")]                                  private GameObject radarGrid;
        [Space()]
        [SerializeField, Tooltip("UI object used to position radar zone camera in scene.")]                           private RectTransform radarCamTargeter;
        [SerializeField, Tooltip("UI object used to position smallest version of opponent subcamera in scene.")]      private RectTransform minOpponentCamTargeter;
        [SerializeField, Tooltip("UI object used to position largest version of opponent subcamera in scene.")]       private RectTransform maxOpponentCamTargeter;
        [SerializeField, Tooltip("Dynamic UI object used to determine where player tank is allowed to be in frame.")] private RectTransform mainCamConfiner;

        //Settings:
        [Header("General Settings:")]
        [SerializeField, Tooltip(""), Range(0,1)] private float cameraSmoothing;
        [SerializeField, Tooltip("Target FPS for all active cameras"), Min(1)]                                                         private float fps;
        [SerializeField, Tooltip("Use this to add hard space (in units) between extremities of framed objects and the frame itself.")] private Vector4 bufferValues;
        [Header("Main Tank Camera Settings:")]
        [SerializeField, Tooltip("The minimum-allowed size (relative to screen size) that cells are allowed to be on screen.")]                        private float minCellSizeOnScreen;
        [SerializeField, Tooltip("When the player tank is traveling at this speed, lead values will be maxxed out (zoom and position).")]              private float maxLeadSpeed;
        [SerializeField, Tooltip("Curve describing affect of lead zoom and positional offset depending on percentage of maxLeadSpeed tank is going.")] private AnimationCurve leadCurve;
        [Header("Radar Settings:")]
        [SerializeField, Tooltip("How far ahead of the player tank the radar can see."), Min(0)]                                                                      private float radarRange;
        [SerializeField, Tooltip("Percentage of vertical space on radar taken up by terrain (all visible topography will be squashed to fit in this area)."), Min(0)] private float radarTerrainCrossSectionalHeight;
        [Header("Opponent Viewer Settings:")]
        [SerializeField, Tooltip("Distance from player at which opponent viewer cam becomes enabled.")] private float opponentVisDistance;

        //Runtime Variables:
        private List<List<List<Vector2>>> chunkCameraPositions = new List<List<List<Vector2>>>(); //List of all positions for the chunk parallax layers to spawn objects on
        private string[] camLayers;   //Array of all user-assigned camera layers in the game (used to make each separate cam exclusive of others)
        private Canvas zoneVisCanvas; //Generated canvas which contains visualizers for camera zones (used to position where cameras will render)

        private Rect radarRect;       //Normalized rectangle representing area on screen that radar camera is confined to and rendered in
        private Rect minOpponentRect; //Normalized rectangle representing area on screen where opponent tracker subcam will render when opponent tank is most distant
        private Rect maxOpponentRect; //Normalized rectangle representing area on screen where opponent tracker subcam will render when opponent tank is ready to merge with player camera

        private float mainCamleadValue; //Value between -1 and 1 determining how far forward or backward (and how far zoomed out) camera should get depending on tank speed over time

        //RUNTIME METHODS:
        private void Awake()
        {
            //Initialization:
            if (main != null) { Destroy(main); Debug.LogError("A second instance of CombatCameraController has been loaded in scene. This should not happen. Earlier instance has been destroyed."); } main = this; //Singleton-ize script instance

            //Get runtime variables:
            camLayers = Enumerable.Range(0, 32).Select(index => LayerMask.LayerToName(index)).Where(l => !string.IsNullOrEmpty(l) && l.Contains("Cam")).ToArray(); //Get all layers (by name) with "Cam" in the name
            zoneVisCanvas = GetComponentInChildren<Canvas>();                                                                                                      //Get canvas containing camera zone visualization boxes
            
            //Hide visualizers:
            radarCamTargeter.GetComponent<Image>().enabled = false; //Disable image for radar zone camera targeter
            //mainCamConfiner.GetComponent<Image>().enabled = false;  //Disable image for confinement zone camera targeter
            minOpponentCamTargeter.GetComponent<Image>().enabled = false; //Disable image for opponent camera targeter
            maxOpponentCamTargeter.GetComponent<Image>().enabled = false; //Disable image for opponent camera targeter

            //Set up parallax infrastructure:
            int chunkLayerCounter = 0;
            for (int i = 0; i < parallaxPrefabs.Length; i++)
            {
                if (parallaxPrefabs[i].TryGetComponent(out ChunkCameraParallaxController chunkController))                                                                                                                          //Check to see if the current parallax layer has a chunk parallax component
                {
                    chunkCameraPositions.Add(new List<List<Vector2>>());                                                                                                                                                            //Create a new list for the chunk controller
                    List<ChunkParallaxLayer> chunkLayers = chunkController.GetParallaxLayers();                                                                                                                                     //Get the parallax layers from the controller
                    for (int j = 0; j < chunkLayers.Count; j++)
                    {
                        chunkCameraPositions[chunkCameraPositions.Count - 1].Add(new List<Vector2>());                                                                                                                              //Create a new list for the positions
                        Vector2 chunkPosition = 
                            new Vector2(chunkLayers[j].pieceWidth, Random.Range(chunkLayers[j].yPosition.x, chunkLayers[j].yPosition.y));          //Create a tracker for the chunk piece positions

                        Vector2 chunkCounters = new Vector2(Random.Range(chunkLayers[j].spawnFrequency.x, chunkLayers[j].spawnFrequency.y), Random.Range(chunkLayers[j].spawnFrequency.x, chunkLayers[j].spawnFrequency.y));

                        for (int k = 0; k < chunkLayers[j].poolSize; k++)                                                                                                                                                           //Iterate through the chunk piece pool
                        {
                            Vector2 newChunkPos = new Vector2(k % 2 == 1 ? chunkPosition.x * chunkCounters.y : -chunkPosition.x * chunkCounters.x, Random.Range(chunkLayers[j].yPosition.x, chunkLayers[j].yPosition.y));
                            chunkCameraPositions[chunkLayerCounter][j].Add(newChunkPos);                            //Alternate between the right and left of the starting position

                            if (k % 2 == 1)
                            {
                                chunkCounters.x += Random.Range(chunkLayers[j].spawnFrequency.x, chunkLayers[j].spawnFrequency.y);
                                chunkCounters.y += Random.Range(chunkLayers[j].spawnFrequency.x, chunkLayers[j].spawnFrequency.y);
                            }

                            //Debug.Log("Chunk Pos: " + newChunkPos);
                        }
                    }
                    chunkLayerCounter++;                                                                                                                                                                                            //Iterate the chunk layer counter
                }
            }

            //Generate camera systems:
            playerTankCam = new CamSystem(CamSystemType.Player);     //Generate a main camera system
            radarCam = new CamSystem(CamSystemType.Radar);           //Generate a camera system for the radar
            opponentTankCam = new CamSystem(CamSystemType.Opponent); //Generate a camera system for the opponent viewer
        }
        private void Update()
        {
            //Update cameras:
            playerTankCam.Update(Time.deltaTime);
            radarCam.Update(Time.deltaTime);
            opponentTankCam.Update(Time.deltaTime);

            //Debug:
            if (Application.isEditor) //Editor-specific updates
            {
                UpdateOutputRects(); //Update rects while in editor so they can be changed on the fly if need be
            }
        }
        private void OnDrawGizmos()
        {
            if (Application.isPlaying && Application.isEditor) //Only show gizmos when playing in editor
            {
                //Visualize buffer rect:
                Bounds targetBounds = playerTankCam.target.GetTargetBounds(); //Store bounds so they can be drawn without having to be re-calculated
                targetBounds.size += new Vector3(main.bufferValues.y + main.bufferValues.w, main.bufferValues.x + main.bufferValues.z);               //Add universal buffer size to bounds
                targetBounds.center += new Vector3((main.bufferValues.y - main.bufferValues.w) / 2, (main.bufferValues.x - main.bufferValues.z) / 2); //Adjust centerpoint of bounds so that asymmetrical settings do not offset bounding box
                Gizmos.color = Color.yellow;                                  //Draw bounds in yellow
                Gizmos.DrawWireCube(targetBounds.center, targetBounds.size);  //Draw a wire cube representing acquired bounds
            }
        }
        
        //FUNCTIONALITY METHODS:
        /// <summary>
        /// Shakes given camera system according to given settings.
        /// </summary>
        public void Shake(CamSystemType targetCamera, ScreenshakeSettings settings)
        {
            Shake(targetCamera, settings.intensity, settings.duration); //Get data out of settings and send it to other method.
        }
        /// <summary>
        /// Shakes given camera system according to given values.
        /// </summary>
        public void Shake(CamSystemType targetCamera, float intensity, float duration)
        {
            switch (targetCamera) //Determine target camera based on type given
            {
                case CamSystemType.Player:
                    GameManager.Instance.SystemEffects.ShakeCamera(playerTankCam.vcam, intensity, duration); //Shake player camera using given settings
                    break;
                case CamSystemType.Opponent:
                    GameManager.Instance.SystemEffects.ShakeCamera(opponentTankCam.vcam, intensity, duration); //Shake opponent camera using given settings
                    break;
                case CamSystemType.Radar:
                    Debug.LogError("Tried to shake radar for some reason.");
                    break;
            }
        }

        //UTILITY METHODS:
        /// <summary>
        /// Creates a layermask which excludes all camera layers except the given one.
        /// </summary>
        /// <param name="camLayerName">The name of the layer used by this camera to render exclusively to its target VCam.</param>
        /// <returns></returns>
        private LayerMask MakeCamMask(string camLayerName)
        {
            string[] excludedCamLayers = main.camLayers.Where(layer => layer != camLayerName).ToArray();      //Get list of all camera layers excluding the one used by this camera
            LayerMask camLayerMask = ~0;                                                                      //Since the camera needs to be able to see every layer except excluded cameras, start with a layermask which includes every layer
            foreach (string layer in excludedCamLayers) camLayerMask &= ~(1 << LayerMask.NameToLayer(layer)); //Systematically exclude each camera layer which is not designated for this tank cam from the layermask
            return camLayerMask;                                                                              //Set camera culling mask so that camera can only see vcam on designated layer
        }
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
        private void UpdateOutputRects()
        {
            if (radarCamTargeter != null) radarRect = GetNormalizedRect(radarCamTargeter, zoneVisCanvas);                   //Update radar zone
            if (minOpponentCamTargeter != null) minOpponentRect = GetNormalizedRect(minOpponentCamTargeter, zoneVisCanvas); //Update minimum opponent cam zone
            if (maxOpponentCamTargeter != null) maxOpponentRect = GetNormalizedRect(maxOpponentCamTargeter, zoneVisCanvas); //Update maximum opponent cam zone
        }
    }
}
