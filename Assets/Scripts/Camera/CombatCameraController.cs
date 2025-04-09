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
            [Tooltip("The primary object which this camera system will be following.")]    public CamTarget target;
            [Tooltip("Camera component which renders this specific target.")]              internal Camera cam;
            [Tooltip("Virtual camera which points at the target")]                         internal CinemachineVirtualCamera vcam;
            [Tooltip("Scripts controlling the parallax background sfor this cam system.")] private List<MultiCameraParallaxController> parallaxControllers = new List<MultiCameraParallaxController>();

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
                vcam = new GameObject("VCAM_" + type.ToString()).AddComponent<CinemachineVirtualCamera>();              //Generate object with virtual camera component and get a reference to it
                vcam.transform.parent = main.transform;                                                                 //Child camera to camera container
                vcam.gameObject.layer = LayerMask.NameToLayer(camLayerName);                                            //Assign layer to virtual camera so that only this system's camera is rendering from it
                CinemachineFramingTransposer transposer = vcam.AddCinemachineComponent<CinemachineFramingTransposer>(); //Use a framing transposer to finely adjust camera settings
                transposer.m_TrackedObjectOffset.z = -10;                                                               //Set camera z value so it can actually see the tank
                transposer.m_XDamping = 0; transposer.m_YDamping = 0; transposer.m_ZDamping = 0;                        //Turn off all camera damping

                //Radar setup:
                if (type == CamSystemType.Radar) //Only perform the following setup for the radar cam
                {
                    //Extra camera setup:
                    cam.transform.tag = "RadarCam";
                    cam.depth = 1;                                                                                    //Change the radar's priority
                    cam.clearFlags = CameraClearFlags.SolidColor;                                                     //Change the background type to only show a solid color
                    cam.backgroundColor = Color.black;                                                                //Set the background color to black
                    cam.cullingMask = 1 << LayerMask.NameToLayer("TankCamC") | 1 << LayerMask.NameToLayer("Minimap"); //Set the camera to only render the radar cam and the minimap cam

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
                //Modify viewport rect:
                Rect camRect = new(); //Initialize container to store final camera rect
                switch (type)
                {
                    case CamSystemType.Player: //The player camera output always takes up the entire screen
                        camRect = new(Vector2.zero, Vector2.one); //Always use a rect which fills the entire screen
                        break;
                    case CamSystemType.Radar: //The radar camera is usually fixed in place
                        camRect = main.radarRect; //Always use the preset radar rect
                        break;
                    case CamSystemType.Opponent: //The opponent camera rect position changes depending on certain factors

                        break;
                }
                cam.rect = camRect; //Apply changes in cam rect

                //Frame targets:
                Bounds targetBounds = target.GetTargetBounds(); //Get bounds from target
                targetBounds.size += new Vector3(main.bufferValues.y + main.bufferValues.w, main.bufferValues.x + main.bufferValues.z);               //Add universal buffer size to bounds
                targetBounds.center += new Vector3((main.bufferValues.y - main.bufferValues.w) / 2, (main.bufferValues.x - main.bufferValues.z) / 2); //Adjust centerpoint of bounds so that asymmetrical settings do not offset bounding box
                
                if (type == CamSystemType.Radar) //Radar framing:
                {
                    vcam.m_Lens.OrthographicSize = (main.radarRange / cam.aspect) / 2; //Adjust ortho size of camera so that radar is precisely rendering designated range at all times
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

        //Settings:
        [Header("Camera Zone Positioning:")]
        [SerializeField, Tooltip("UI object used to position radar zone camera in scene.")]                      private RectTransform radarCamTargeter;
        [SerializeField, Tooltip("UI object used to position smallest version of opponent subcamera in scene.")] private RectTransform minOpponentCamTargeter;
        [SerializeField, Tooltip("UI object used to position largest version of opponent subcamera in scene.")]  private RectTransform maxOpponentCamTargeter;
        [Header("Main Tank Camera Settings:")]
        [SerializeField, Tooltip("Use this to add hard space (in units) between extremities of framed objects and the frame itself.")] private Vector4 bufferValues;
        [Header("Radar Settings:")]
        [SerializeField, Tooltip("How far ahead of the player tank the radar can see."), Min(0)]                                                                      private float radarRange;
        [SerializeField, Tooltip("Percentage of vertical space on radar taken up by terrain (all visible topography will be squashed to fit in this area)."), Min(0)] private float radarTerrainCrossSectionalHeight;

        //Runtime Variables:
        private List<List<List<Vector2>>> chunkCameraPositions = new List<List<List<Vector2>>>(); //List of all positions for the chunk parallax layers to spawn objects on
        private string[] camLayers;   //Array of all user-assigned camera layers in the game (used to make each separate cam exclusive of others)
        private Canvas zoneVisCanvas; //Generated canvas which contains visualizers for camera zones (used to position where cameras will render)

        private Rect radarRect;       //Normalized rectangle representing area on screen that radar camera is confined to and rendered in
        private Rect minOpponentRect; //Normalized rectangle representing area on screen where opponent tracker subcam will render when opponent tank is most distant
        private Rect maxOpponentRect; //Normalized rectangle representing area on screen where opponent tracker subcam will render when opponent tank is ready to merge with player camera

        //RUNTIME METHODS:
        private void Awake()
        {
            //Initialization:
            if (main != null) { Destroy(main); Debug.LogError("A second instance of CombatCameraController has been loaded in scene. This should not happen. Earlier instance has been destroyed."); } main = this; //Singleton-ize script instance

            //Get runtime variables:
            camLayers = Enumerable.Range(0, 32).Select(index => LayerMask.LayerToName(index)).Where(l => !string.IsNullOrEmpty(l) && l.Contains("Cam")).ToArray(); //Get all layers (by name) with "Cam" in the name
            zoneVisCanvas = GetComponentInChildren<Canvas>();                                                                                                      //Get canvas containing camera zone visualization boxes
        
            //Hide visualizers:
            radarCamTargeter.GetComponent<Image>().enabled = false; //Disable image for engagement zone camera targeter

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
            playerTankCam = new CamSystem(CamSystemType.Player); //Generate a main camera system
            radarCam = new CamSystem(CamSystemType.Radar);       //Generate a camera system for the radar
        }
        private void Update()
        {
            playerTankCam.Update(Time.deltaTime);
            radarCam.Update(Time.deltaTime);

            //Debug:
            if (Application.isEditor) //Editor-specific updates
            {
                UpdateOutputRects(); //Update rects while in editor so they can be changed on the fly if need be
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
