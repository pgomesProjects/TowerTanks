using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using System.Linq;
using UnityEngine.Rendering.Universal;
using Sirenix.OdinInspector;
using UnityEngine.UI;
using UnityEngine.Audio;

namespace TowerTanks.Scripts
{
    public class CameraManipulator : MonoBehaviour
    {
        //Enums, Classes & Structs:
        /// <summary>
        /// Describes a camera system which tracks an individual tank in the level scene.
        /// </summary>
        [System.Serializable]
        public class TankCamSystem
        {
            //Objects & Components:
            [Tooltip("The tank(s) this system is targeting.")]                                                  public List<TankController> tanks = new List<TankController>();
            [Tooltip("Bounds representing tanks which have been destroyed while encapsulated by this system.")] public List<BoxCollider2D> simulatedTanks = new List<BoxCollider2D>();
            [Tooltip("Camera component which renders this specific tank.")]                                     internal Camera cam;
            [Tooltip("Virtual camera pointed at the tank.")]                                                    internal CinemachineVirtualCamera vcam;
            [Tooltip("Collider used to manage offscreen visualization system.")]                                private BoxCollider2D boundCollider;
            [Tooltip("Generated transform used to point camera when following multiple tanks.")]                private Transform followDummy;
            [Tooltip("Script controlling the parallax background for this cam system.")]                        private MultiCameraParallaxController parallaxController;

            //Runtime variables:
            [Tooltip("True if this is the primary camera system for the current player tank.")]       public bool isPlayerCam;
            [Tooltip("Value indicating whether this is Cam A, Cam B, etc.")]                          public int camNum;
            [Tooltip("Determines whether or not this system is active and rendering.")]               public bool enabled = true;
            [Tooltip("True if this is an enemy tank which is within engagement distance of player.")] public bool engaged = false;
            [Tooltip("True if this camera system is for the player tank radar.")]                     public bool radar = false;

            [Tooltip("Offset width at last camera update, used to smooth out jittering.")] private float prevOffsetWidth;

            private bool firstEngagement = true;
            internal float timeUntilDeath = -1; //Used to clean up camera after a certain amount of time, -1 = not in use, -2 = infinite, 0 = marked for destruction

            /// <summary>
            /// Ganerates a camera setup to track given tank.
            /// </summary>
            /// <param name="tank">The primary tank which this system will be associated with.</param>
            /// <param name="tankCamBackgroundColor">The background color of the tank camera.</param>
            /// <param name="isRadar">Will initialize this system as the radar. There can only be one radar and it behaves differently than normal tankCamSystems. Only initialize with the player tank.</param>
            public TankCamSystem(TankController tank, Color tankCamBackgroundColor, bool isRadar = false)
            {
                //Validity checks:
                if (tank == null) { Debug.LogError("Tried to spawn tank camera system using null script!"); return; }

                //Initialize main camera:
                cam = new GameObject("CAM_" + (isRadar ? "Radar" : tank.TankName.Replace(" ", "")), typeof(Camera), typeof(UniversalAdditionalCameraData), typeof(CinemachineBrain)).GetComponent<Camera>(); //Generate main camera object with full suite of components
                cam.transform.parent = main.transform; //Child camera to camera manipulator object (used as a bucket for camera stuff)
                cam.orthographic = true;               //Make cam orthographic
                cam.backgroundColor = tankCamBackgroundColor;   //Change the background color of the camera
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
                string camLayerName = isRadar ? "RadarCam" : "TankCam" + "ABCDEFGH"[camNum].ToString(); //Get name of layer this camera will be on (needs to already be in LayerManager)
                cam.cullingMask = main.MakeCamMask(camLayerName);                                       //Set camera culling mask so that camera can only see vcam on designated layer

                //Initialize virtual camera:
                vcam = new GameObject("VCAM_" + (isRadar ? "Radar" : tank.TankName.Replace(" ", ""))).AddComponent<CinemachineVirtualCamera>(); //Generate object with virtual camera component and get a reference to it
                vcam.transform.parent = main.transform;                                                                                         //Child camera to camera manipulator object (used as a bucket for camera stuff)
                vcam.gameObject.layer = LayerMask.NameToLayer(camLayerName);                                                                    //Assign layer to virtual camera so that only this system's camera is rendering from it
                vcam.m_Follow = tank.treadSystem.transform;                                                                                     //Have camera lock to centerpoint of tank treadSystem by default

                //Setup specific vcam settings:
                if (!isRadar) //Settings for engagement cams
                {
                    CinemachineFramingTransposer transposer = vcam.AddCinemachineComponent<CinemachineFramingTransposer>(); //Add framing transposer to camera so that it's behavior can be fine-tuned
                    transposer.m_TrackedObjectOffset.z = -10;                                                               //Set camera z value so it can actually see the tank
                    transposer.m_XDamping = 0; transposer.m_YDamping = 0; transposer.m_ZDamping = 0;                        //Turn off all camera dampingqaqsdad eeeeeeeeeeeeeeeeeeeedd
                }
                else //Settings for radar cam
                {
                    radar = true;
                    cam.transform.tag = "RadarCam";
                    CinemachineTransposer transposer = vcam.AddCinemachineComponent<CinemachineTransposer>(); //Use a simpler transposer to track tank in radar screen
                    transposer.m_XDamping = 0; transposer.m_YDamping = 0; transposer.m_ZDamping = 0;          //Turn off all camera damping
                }

                //Generate additional objects:
                boundCollider = new GameObject("BoundCollider").AddComponent<BoxCollider2D>(); //Generate the boundary collider used for detecting offscreen objects
                boundCollider.transform.parent = cam.transform;                                //Child collider to main camera
                boundCollider.transform.localPosition = Vector3.zero;                          //Zero out relative position of collider
                boundCollider.gameObject.layer = LayerMask.NameToLayer("Camera");              //Put collider on camera layer so it doesn't interfere with anything else

                //Audio setup:
                if (tank.tankType == TankId.TankType.PLAYER && !radar) //Set up audio on this camera if it is the player's
                {
                    //AkSoundEngine.AddDefaultListener(cam.gameObject);
                }

                //Parallax setup:
                parallaxController = Instantiate(main.parallaxPrefab).GetComponent<MultiCameraParallaxController>();                    //Instantiate a parallax background for this camera and get a reference
                parallaxController.transform.parent = main.transform;                                                                   //Child parallax object to camera container
                parallaxController.gameObject.name = "Parallax_" + cam.name;                                                            //Rename parallax object for clarity
                parallaxController.AddCameraToParallax(cam);                                                                            //Add this camera to controller so it is tracked properly
                parallaxController.transform.position = Vector3.zero;                                                                   //Zero out position of parallax system
                if (parallaxController.useDesertPalette) cam.backgroundColor = parallaxController.desertColorPalette[0];                //Use alternate background color if bool is checked
                foreach (Transform child in parallaxController.transform) child.gameObject.layer = LayerMask.NameToLayer(camLayerName); //Put each parallax layer on a layer which can only be seen by this camera

                //Perlin setup:
                if (!radar) //The radar does not need to shake
                {
                    CinemachineBasicMultiChannelPerlin perlin = vcam.AddCinemachineComponent<CinemachineBasicMultiChannelPerlin>(); //Add perlin noise component to camera
                    perlin.m_NoiseProfile = main.shakeNoiseProfile;                                                                 //Set noise profile to predetermined value
                    perlin.m_AmplitudeGain = 0;                                                                                     //Have camera default to no noise
                    perlin.m_FrequencyGain = 1;                                                                                     //Have camera default to no noise
                }

                //Cleanup:
                tanks.Add(tank);                                                 //Add given tank controller as the first instance in list of tracked tanks
                if (tank.tankType == TankId.TankType.PLAYER) isPlayerCam = true; //Mark whether or not this is the player tank's camera system
                radar = isRadar;                                                 //Store value indicating whether or not this is the radar system
                UpdateEverything(0);                                             //Immediately initialize all camera stuff
            }

            //FUNCTIONALITY METHODS:
            /// <summary>
            /// Performs all updates that are part of the cam system.
            /// </summary>
            public void UpdateEverything(float deltaTime)
            {
                //Camera death updates:
                if (timeUntilDeath == -2) //Time until death is infinite
                {
                    return; //Camera is following dead tank but is set to NEVER go away
                }
                else if (timeUntilDeath > 0) //Target tank is dead
                {
                    timeUntilDeath = Mathf.Max(timeUntilDeath - deltaTime, 0); //Decrement death time tracker
                    if (timeUntilDeath == 0) CleanUp();                        //Fully clean up system once death time has been reached
                    return;                                                    //Do not do normal camera updates while waiting for death
                }

                //Universal camera updates:
                UpdateEnabledStatus(); //Update status
                if (!enabled) return;  //Do nothing if disabled

                //Enabled camera updates:
                UpdateCameraZone();    //Position camera in target zone according to various gameplay settings
                UpdateCameraValues();  //Update camera properties
                UpdateBoundCollider(); //Update collider around camera
            }
            /// <summary>
            /// Checks whether or not this system should be enabled based on proximity to player tank.
            /// </summary>
            public void UpdateEnabledStatus()
            {
                if (isPlayerCam) return;                             //Player cam is always enabled
                if (TankManager.instance.playerTank == null) return; //Do nothing when the player tank is destroyed (prevents errors upon player death)x

                float distanceFromPlayer = Mathf.Abs(tanks[0].treadSystem.transform.position.x - TankManager.instance.playerTank.treadSystem.transform.position.x); //Get flat horizontal distance from player tank
                if (distanceFromPlayer > main.engagementDistance) //Tank is outside engagement distance
                {
                    ToggleEnabled(false);       //Disable camera
                    engaged = false;            //Indicate that system is no longer engaged
                    main.CheckIfStillEngaged(); //Check if disengaging this system results in the player tank no longer being engaged
                }
                else if (distanceFromPlayer < main.shareCamDistance) //Tank is close enough to combine with player tank camera system
                {
                    if (!main.PlayerCamSystem().tanks.Contains(tanks[0]))
                    {
                        ToggleEnabled(false);                       //Disable camera
                        main.PlayerCamSystem().tanks.Add(tanks[0]); //Add tank from this system to player's cam system
                    }
                }
                else if (!enabled) //Tank is within normal engagement distance and is switching from a disabled status
                {
                    if (main.PlayerCamSystem().tanks.Contains(tanks[0])) //Tank was previously sharing an engagment camera with player
                    {
                        if (distanceFromPlayer < main.shareCamStickDistance) return; //Do not disengage from player camera until stick distance has been exceeded
                        main.PlayerCamSystem().tanks.Remove(tanks[0]); //Stop sharing camera with player tank if stick distance has been exceeded
                    }

                    ToggleEnabled(true); //Enable camera
                    engaged = true;      //Indicate that system is engaged
                    main.engaged = true; //Make sure camera manipulator knows engagement is occurring

                    if (firstEngagement) //First time tank has engaged with the camera
                    {
                        FindObjectOfType<CombatHUD>()?.DisplayEnemyTankInformation(tanks[0]); //Display the enemy information in the CombatHUD
                        firstEngagement = false;                                              //Make sure that the system does not call this logic again
                    }
                }
            }
            /// <summary>
            /// Updates position of camera system based on that of other cameras.
            /// </summary>
            public void UpdateCameraZone()
            {
                //Validity checks:
                if (!enabled) return; //Do nothing if disabled

                //Modify viewport rect:
                Rect camRect = radar ? main.normalizedRadarArea : main.normalizedEngagementArea; //Get copy of area rect in case it needs to be modified
                int camCount = main.EnabledCamCount();                                           //Get number of currently-enabled camera systems
                if (!radar && camCount > 1) //Rect needs to be a different size because there are multiple engaged tanks in scene
                {
                    //Get screen portion:
                    int camIndex = main.EnabledCamIndex(this);                  //Get index of this system within version of camSystem list containing only enabled cameras
                    float screenPortion = 1f / camCount;                        //Get horizontal slice of the zone this camera will be able to occupy
                    float xPosition = camIndex * screenPortion * camRect.width; //Get normalized x position of camera rect RELATIVE to x position of camRect, adjusted for total rect width
                    camRect.width = camRect.width * screenPortion;              //Divide width of rect into sections based on how many systems are sharing the screen
                    camRect.position += new Vector2(xPosition, 0);             //Set position of camera rect depending on order system is in list

                    //Create separator bar between engagement cams:
                    float separation = main.GetNormalizedVector(new Vector2(main.engagementCamSeparation, 0), main.zoneVisCanvas).x; //Get value for distance between camera frames normalized for viewport space
                    if (camIndex < camCount - 1) //Right side of separator bar needs to be allocated to this camera frame
                    {
                        camRect.width -= (separation / 2); //Reduce width of frame by half of separation to account for space taken up by bar
                    }
                    if (camIndex > 0) //Left side of separator bar needs to be allocated to this camera frame
                    {
                        camRect.width -= (separation / 2);                  //Reduce width of frame by half of separation to account for space taken up by bar (this may be the second time this happens and that's okay)
                        camRect.position += new Vector2(separation / 2, 0); //Move frame to the right so that only the left side is effectively moved
                    }
                }

                //Cleanup:
                cam.rect = camRect; //Apply changes in cam rect
            }
            /// <summary>
            /// Updates properties in the camera component.
            /// </summary>
            public void UpdateCameraValues()
            {
                //Pre-flight checks:
                if (!enabled) return; //Do nothing if disabled

                if (!radar) //Updates for engagement cameras
                {
                    //Find tank extremities:
                    Bounds[] allTankBounds = tanks.Select(t => GetTankAsBounds(t)).Concat(simulatedTanks.Select(s => s.bounds)).ToArray(); //Get an array of bounds representing all tanks in view (simulated and real)
                    Bounds highestTank = allTankBounds[0]; //Make container for storing the uppermost tank onscreen and default to system's base tank (container used is bounds because it may be a simulacrum tank)
                    Bounds lowestTank = highestTank;       //Make container for storing the lowermost tank onscreen and default to system's base tank (container used is bounds because it may be a simulacrum tank)
                    Bounds leftMostTank = highestTank;     //Make container for storing the leftmost tank onscreen and default to system's base tank (container used is bounds because it may be a simulacrum tank)
                    Bounds rightMostTank = highestTank;    //Make container for storing the rightmost tank onscreen and default to system's base tank (container used is bounds because it may be a simulacrum tank)
                    foreach (Bounds tankBounds in allTankBounds) //Iterate through tanks (and simulacrum stanks) in camera system (other than base tank)
                    {
                        //Compare extents:
                        if (tankBounds == allTankBounds[0]) continue;                                                                                  //Skip first entry because it does not need to be compared to itself
                        if (tankBounds.center.y + tankBounds.extents.y > highestTank.center.y + highestTank.extents.y) highestTank = tankBounds;       //Factor in both physical tank position and tank height when looking for tallest tank
                        if (tankBounds.center.y - tankBounds.extents.y < lowestTank.center.y - lowestTank.extents.y) lowestTank = tankBounds;          //Factor in both physical tank position and tank depth when looking for lowest tank
                        if (tankBounds.center.x - tankBounds.extents.x < leftMostTank.center.x - leftMostTank.extents.x) leftMostTank = tankBounds;    //Factor in both physical tank position and left side length of tank when looking for leftmost tank
                        if (tankBounds.center.x + tankBounds.extents.x > rightMostTank.center.x + rightMostTank.extents.x) rightMostTank = tankBounds; //Factor in both physical tank position and right side length of tank when looking for rightmost tank
                    }
                    Bounds[] allActualTankBounds = tanks.Select(t => t.treadSystem.GetTankBounds()).Concat(simulatedTanks.Select(s => s.bounds)).ToArray(); //Get an array of bounds representing actual cross section of all tanks in view (simulated and real)
                    Bounds actualLeftMostTank = allActualTankBounds[0];  //Container specifically for REAL CURRENT bounds of leftmost tank (as opposed to tank extents)
                    Bounds actualRightMostTank = actualLeftMostTank;     //Container specifically for REAL CURRENT bounds of rightmost tank (as opposed to tank extents)
                    foreach (Bounds tankBounds in allActualTankBounds) //Iterate through tanks (and simulacrum tanks) in camera system
                    {
                        //Compare real extents:
                        if (tankBounds == allTankBounds[0]) continue;                                                                                                    //Skip first entry because it does not need to be compared to itself
                        if (tankBounds.center.x - tankBounds.extents.x < actualLeftMostTank.center.x - actualLeftMostTank.extents.x) actualLeftMostTank = tankBounds;    //If bounds are farther left than current leftmost bounds, indicate that they are now the new leftmost bounds
                        if (tankBounds.center.x + tankBounds.extents.x > actualRightMostTank.center.x + actualRightMostTank.extents.x) actualRightMostTank = tankBounds; //If bounds are farther right than current rightmost bounds, indicate that they are now the new rightmost bounds
                    }

                    //Update ortho size:
                    float heightOrthoSize = highestTank.extents.y + lowestTank.extents.y + Mathf.Abs(highestTank.center.y - lowestTank.center.y); //Get combined height and depth of tallest and lowest tank, plus the vertical difference in position between the two
                    heightOrthoSize = (heightOrthoSize + main.tankCamUpperBuffer + main.tankCamLowerBuffer) / 2; //Get final orthographic size (as defined by tank heights) by adding vertical buffers and dividing by two

                    float widthOrthoSize = 0; //Because the value used for width ortho size depends on how many tanks are in system, create an empty container here
                    if (tanks.Count == 1 && simulatedTanks.Count == 0) //System is tracking a single tank, and because it needs to track the center of that tank, it has to decide which side is longer and base the ortho size off of that
                    {
                        //NOTE: This is done as such because otherwise, the camera will not lock to the center of given tank as desired
                        float leftWidthOrthoSize = (((tanks[0].tankSizeValues.w * 2) + (2 * main.tankCamSideBuffer)) / 2) / cam.aspect;  //Get ortho size as defined by tank width (measuring from middle to left)
                        float rightWidthOrthoSize = (((tanks[0].tankSizeValues.y * 2) + (2 * main.tankCamSideBuffer)) / 2) / cam.aspect; //Get ortho size as defined by tank width (measuring from middle to right)
                        widthOrthoSize = Mathf.Max(leftWidthOrthoSize, rightWidthOrthoSize);                                             //Get highest width-defined orthographic size
                    }
                    else //With multiple tanks, the system needs to combine the respective extremities of the two outermost tanks to get effective width
                    {
                        //Determine orthographic size:
                        widthOrthoSize = leftMostTank.extents.x + rightMostTank.extents.x + Mathf.Abs(leftMostTank.center.x - rightMostTank.center.x); //Get leftward width of leftmost tank and rightmost width of rightmost tank, plus the horizontal difference in position between the two
                        widthOrthoSize = ((widthOrthoSize + (main.tankCamSideBuffer * 2)) / 2) / cam.aspect; //Get final orthographic size (as defined by tank widths) by adding horizontal buffers and dividing by the cam aspect ratio
                    }
                    vcam.m_Lens.OrthographicSize = Mathf.Max(heightOrthoSize, widthOrthoSize); //Use whichever value is larger as the final orthographic size

                    //Get horizontal extents of frame:
                    float actualLeftMostPoint = actualLeftMostTank.center.x - actualLeftMostTank.extents.x;
                    float actualRightMostPoint = actualRightMostTank.center.x + actualRightMostTank.extents.x;

                    //Position camera:
                    if (tanks.Count == 1 && simulatedTanks.Count == 0) //When tracking a single tank, the camera system uses the tracked pose offset value to position the tank at the center of the screen
                    {
                        //Despawn target dummy:
                        if (followDummy != null) //System is switching to single-tank mode
                        {
                            vcam.Follow = tanks[0].treadSystem.transform; //Point vcam at base tank's treadsystem
                            Destroy(followDummy.gameObject);              //Destroy dummy object
                            followDummy = null;                           //Clear reference to destroyed dummy transform
                        }

                        //Get x offset:
                        Vector2 offset = new Vector2();                                                                                                            //Create container to apply offsets to
                        float offsetWidth = tanks[0].treadSystem.transform.position.x - ((actualLeftMostPoint + actualRightMostPoint) / 2);                        //Get x distance to offset camera by by finding the world center between both extreme horizontal points in tank and getting the difference between that and the tank x position
                        offsetWidth = Mathf.Lerp(prevOffsetWidth, offsetWidth, main.horizontalOffsetSmoothing * Time.deltaTime);                                   //Use a lerp to smooth out erratic changes in found offset width
                        prevOffsetWidth = offsetWidth;                                                                                                             //Store offset width value for later
                        offset -= (Vector2)(Quaternion.AngleAxis(-tanks[0].treadSystem.transform.eulerAngles.z, Vector3.forward) * (Vector3.right * offsetWidth)); //Apply value to offset, compensating for current rotation of tank

                        //Get y offset:
                        float offsetHeight = (vcam.m_Lens.OrthographicSize - tanks[0].tankSizeValues.z) - main.tankCamLowerBuffer;                               //Always adjust height of camera frame so it is lined up with lower camera buffer
                        offset += (Vector2)(Quaternion.AngleAxis(-tanks[0].treadSystem.transform.eulerAngles.z, Vector3.forward) * (Vector3.up * offsetHeight)); //Apply value to offset, compensating for current rotation of tank
                        vcam.GetCinemachineComponent<CinemachineFramingTransposer>().m_TrackedObjectOffset = offset;                                             //Apply final offset to vcam component
                    }
                    else //When tracking multiple tanks (simulated or otherwise), the camera system uses a generated transform to position the tank at the center of the screen
                    {
                        //Spawn target dummy:
                        if (followDummy == null) //System is switching to multi-tank mode
                        {
                            followDummy = new GameObject(vcam.name + "_Target").transform;                                     //Generate transform for system to follow
                            followDummy.parent = main.transform;                                                               //Child dummy to camera suite object
                            vcam.Follow = followDummy;                                                                         //Point vcam at dummy
                            vcam.GetCinemachineComponent<CinemachineFramingTransposer>().m_TrackedObjectOffset = Vector2.zero; //Clear vcam follow offset so that dummy is tracked precisely
                        }

                        //Move target:
                        Vector2 newPosition = new Vector2();                                                                                                                    //Create container to store new position for follow dummy
                        newPosition.x = (actualLeftMostPoint + actualRightMostPoint) / 2;                                                                                       //Position dummy at exact center between found tank extremities
                        //newPosition.y = ((lowestTank.treadSystem.transform.position.y - lowestTank.tankSizeValues.z) - main.tankCamLowerBuffer) + vcam.m_Lens.OrthographicSize; //Get position by finding bottom of lowest followed tank (plus buffer) then moving halfway up the screen from there
                        newPosition.y = ((lowestTank.center.y - lowestTank.extents.y) - main.tankCamLowerBuffer) + vcam.m_Lens.OrthographicSize; //Get position by finding bottom of lowest followed tank (plus buffer) then moving halfway up the screen from there
                        followDummy.position = newPosition;                                                                                                                     //Move dummy to calculated position
                    }
                }
                else //Updates for the radar
                {
                    //Update ortho size:
                    CinemachineTransposer transposer = vcam.GetCinemachineComponent<CinemachineTransposer>(); //Get reference to transposer component
                    vcam.m_Lens.OrthographicSize = (main.radarRange / cam.aspect) / 2;                        //Adjust ortho size of camera so that radar is precisely rendering designated range at all times
                    float frameWidth = cam.aspect * (cam.orthographicSize * 2);                               //Get width of radar frame

                    //Find tanks in camera:
                    TankController lowestTank = tanks[0]; //Create container to store lowest found tank in radar, defaulting to player tank
                    foreach (TankCamSystem system in main.camSystems) //Iterate through active cam systems
                    {
                        if (system.tanks.Count == 0 || system.tanks[0] == null) continue;                                                                   //Early disqualification of tanks/systems which should not be considered as contenders
                        float distanceFromPlayer = Mathf.Abs(system.tanks[0].treadSystem.transform.position.x - tanks[0].treadSystem.transform.position.x); //Get flat horizontal distance between this system's tank and radar tank
                        if (distanceFromPlayer > frameWidth) continue;                                                                                      //Consider tanks within radar range
                        if (lowestTank == null || system != this && system.tanks[0].treadSystem.transform.position.y - system.tanks[0].tankSizeValues.z < lowestTank.treadSystem.transform.position.y - lowestTank.tankSizeValues.z) lowestTank = system.tanks[0]; //If tank is lower than current lowest tank, store it
                    }

                    //Update offset:
                    float leftEdgeBuffer = ((frameWidth / 2) - main.radarEdgeBuffer.x) - tanks[0].tankSizeValues.y; //Get half of frame width so that tank is pinned to edge of screen, then adjust based on edge buffer (also apply tank left side width so that full tank is in frame by default)
                    float bottomEdgeBuffer = cam.orthographicSize - main.radarEdgeBuffer.y; //Get vertical follow offset so that tank is pinned to bottom of screen (offset by entire ortho size) and apply edge buffer for more control
                    bottomEdgeBuffer -= Mathf.Abs(tanks[0].treadSystem.transform.position.y - lowestTank.treadSystem.transform.position.y) + lowestTank.tankSizeValues.z; //Offset by difference between lowest tank and player tank and apply tank depth value so that all engaged tanks are visible in radar
                    transposer.m_FollowOffset.x = leftEdgeBuffer;                           //Set horizontal follow offset so tank is pinned to side of radar screen
                    transposer.m_FollowOffset.y = bottomEdgeBuffer;                         //Set vertical follow offset so tank is pinned to bottom of radar screen
                }
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
                if (vcam.m_Follow != null) //Vcam has a follow target which still needs to be destroyed
                {
                    foreach (TankCamSystem otherSystem in main.camSystems) //Iterate through camera systems
                    {
                        if (otherSystem.simulatedTanks.Select(b => b.transform).ToArray().Contains(vcam.m_Follow)) //System contains a simulacrum of this system's dead tank
                        {
                            otherSystem.simulatedTanks.Remove(vcam.m_Follow.GetComponent<BoxCollider2D>()); //Remove simulacrum from tank list
                        }
                    }
                    Destroy(vcam.m_Follow.gameObject); //Destroy deathMannequin if it exists
                }
                if (cam.gameObject != null) Destroy(cam.gameObject);                    //Destroy camera object (also destroys bound collider)
                if (vcam.gameObject != null) Destroy(vcam.gameObject);                  //Destroy virtual camera object
                if (parallaxController != null) Destroy(parallaxController.gameObject); //Destroy parallax system when destroying camera
                engaged = false; main.CheckIfStillEngaged();                  //Have camera manipulator check if destroying this cam system ends engagement
            }
            /// <summary>
            /// Sets camera system to clean itself up after a certain amount of time (cameraDisappearTime, or indefinite if camera is for player tank).
            /// </summary>
            public void CleanUpLater()
            {
                //Create dummy of destroyed tank:
                timeUntilDeath = isPlayerCam ? -2 : main.cameraDisappearTime;                      //Begin countdown timer to death
                Transform tankDummy = new GameObject(tanks[0].name + "_DeathMannequin").transform; //Create new stationary transform for camera to follow
                tankDummy.parent = main.transform;                                                 //Child mannequin to camera container
                tankDummy.position = tanks[0].treadSystem.transform.position;                      //Match position to that of tank tread system
                if (vcam.m_Follow == tanks[0].treadSystem.transform) vcam.m_Follow = tankDummy;    //Have camera follow tank dummy (unless it is already following a tank dummy)

                //Set up tank bounds simulation:
                Bounds tankBounds = tanks[0].treadSystem.GetTankBounds();                         //Get bounds of tank
                BoxCollider2D boundCollider = tankDummy.gameObject.AddComponent<BoxCollider2D>(); //Add a box collider to store the bounds of the tank dummy
                boundCollider.gameObject.layer = LayerMask.NameToLayer("Ghost");                  //Put collider on layer that makes it collide with nothing
                boundCollider.size = tankBounds.size;                                             //Store bounds size in collider
                boundCollider.offset = tankBounds.center - boundCollider.transform.position;      //Offset collider so that its center matches that of bounds

                //Cleanup instances on other camsystems and populate with mannequins:
                simulatedTanks.Add(boundCollider); //Make sure simulacrum tank is being tracked by this camera
                foreach (TankCamSystem system in main.camSystems) //Iterate through tankCams in camera manipulator
                {
                    if (system != this && system.tanks.Contains(tanks[0])) //Other cam system currently encapsulates this tank
                    {
                        system.tanks.Remove(tanks[0]);            //Remove tank from all other camSystems (because it has been destroyed and will cause nullrefs otherwise)
                        system.simulatedTanks.Add(boundCollider); //Add simulation of tank's bounds to cam system's simulated tanks list
                    }
                }
                tanks.RemoveAt(0); //Lose reference to destroyed tank
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

            //UTILITY METHODS:
            /// <summary>
            /// Checks given tank's size values and position and returns a bounds which corresponds to that (NOTE: will give different answer than treadSystem.GetTankBounds()).
            /// </summary>
            private Bounds GetTankAsBounds(TankController tank)
            {
                Vector2 size = new Vector2(tank.tankSizeValues.y + tank.tankSizeValues.w, tank.tankSizeValues.x + tank.tankSizeValues.z); //Get size of tank (different from current bounds because it doesn't account for rotation) from tank size values chart
                Vector2 position = tank.treadSystem.transform.position;                                                                   //Initialize bounds position at base position of tank treadsystem
                position.x = (position.x - tank.tankSizeValues.y) + (size.x / 2);                                                         //Re-position bounds to actually encapsulate tank vertically by getting the leftmost side of tank and then moving half the width of the bounds to the right
                position.y = (position.y - tank.tankSizeValues.z) + (size.y / 2);                                                         //Re-position bounds to actually encapsulate tank horizontally by getting the lowermost side of tank and then moving half the height of the bounds upward
                return new Bounds(position, size);                                                                                        //Return bounds of calculated position and size
            }
        }

        //Objects & Components:
        [Tooltip("Singleton instance of camera manipulator in scene.")]                                   public static CameraManipulator main;
        [SerializeField, Tooltip("List of camera systems being used to actively render tanks in scene.")] private List<TankCamSystem> camSystems = new List<TankCamSystem>();
        [Tooltip("Cam system used to control the radar camera.")]                                         private TankCamSystem radarSystem;
        [SerializeField, Tooltip("Prefab for parallax system instantiated for each camera.")]             private GameObject parallaxPrefab;
        [SerializeField, Tooltip("Noise profile asset describing behavior of camera shake events.")]      private NoiseSettings shakeNoiseProfile;

        //Settings:
        [Header("General Settings:")]
        [SerializeField, Tooltip("Maximum possible number of concurrent tank cam systems (there need to be layers made in layerManager for these. Cam layers need to be in sequential indexes and the first must be named TankCamA)"), Min(1)] private int maxTankCams = 3;
        [SerializeField, Tooltip("Determines whether or not a radar camera system will be spawned in this scene.")]                                                                                                                            private bool useRadar = true;
        [Header("Camera Zone Positioning:")]
        [SerializeField, Tooltip("UI object used to position engagement zone camera setup in scene.")] private RectTransform engagementZoneTargeter;
        [SerializeField, Tooltip("UI object used to position radar zone camera in scene.")]            private RectTransform radarZoneTargeter;
        [Header("Tank Camera Settings:")]
        [SerializeField, Tooltip("Camera space to leave above top cell of tank (in world units)."), Min(0)]                                 private float tankCamUpperBuffer;
        [SerializeField, Tooltip("Camera space to leave below treads of tank (in world units)."), Min(0)]                                   private float tankCamLowerBuffer;
        [SerializeField, Tooltip("Camera space to leave beside each side of tank (in world units)."), Min(0)]                               private float tankCamSideBuffer;
        [SerializeField, Tooltip("Distance (in canvas space units) between engagement camera frames when multiple are on screen."), Min(0)] private float engagementCamSeparation;
        [SerializeField, Tooltip("Lerp factor to apply to changes in horizontal offset for reducing jitter."), Min(0.001f)]                 private float horizontalOffsetSmoothing;
        [SerializeField, Tooltip("The background color of the tank cameras.")]                                                              private Color tankCameraColor;
        [Space]
        [SerializeField, Tooltip("Range (from player tank) at which an enemy tank's camera will become active."), Min(0)]                                                                                         private float engagementDistance;
        [SerializeField, Tooltip("Range (from player tank) at which enemy tank camera will merge with player camera."), Min(0)]                                                                                   private float shareCamDistance;
        [SerializeField, Tooltip("Once enemy tank camera and player cameras are merged, this is the distance at which they will uncouple (should always be equal to or greater than shareCamDistance)."), Min(0)] private float shareCamStickDistance;
        [Space()]
        [SerializeField, Tooltip("Once a tank is killed, its camera will stick around for this number of seconds."), Min(0)] private float cameraDisappearTime;
        [Header("Radar Settings:")]
        [SerializeField, Tooltip("Distance from the lower left corner of the radar field at which tank will be kept.")] private Vector2 radarEdgeBuffer;
        [SerializeField, Tooltip("How far ahead of the player tank the radar can see."), Min(0)]                        private float radarRange;
        [Header("Offscreen Visualization Settings:")]
        [SerializeField, Tooltip("Roundness of collider corners around camera plane (smooths out edge UI)."), Min(0)] private float boundColliderEdgeRadius = 1;
        [Header("Test Features:")]
        [SerializeField] private float testShakeIntensity;
        [SerializeField] private float testShakeDuration;
        [Button("ShakeCamera", Icon = SdfIconType.PhoneVibrate)] private void TestShakeCam() { ShakeTankCamera(camSystems[0].tanks[0], testShakeIntensity, testShakeDuration); }

        //Runtime Variables:
        private Canvas zoneVisCanvas;          //Generated canvas which contains visualizers for camera zones (used to position where cameras will render)
        private string[] camLayers;            //Array of all user-assigned camera layers in the game (used to make each separate cam exclusive of others)
        private Rect normalizedEngagementArea; //Normalized rectangle representing area on screen that engagement cameras are confined to and rendered in
        private Rect normalizedRadarArea;      //Normalized rectangle representing area on screen that radar camera is confined to and rendered in
        private bool engaged;                  //True if player tank is within engagement range of another tank

        //UNITY METHODS:
        private void Awake()
        {
            //Initialization:
            main = this; //Make latest instance of this script main

            //Get runtime variables:
            zoneVisCanvas = GetComponentInChildren<Canvas>();                                                                                                      //Get canvas containing camera zone visualization boxes
            camLayers = Enumerable.Range(0, 32).Select(index => LayerMask.LayerToName(index)).Where(l => !string.IsNullOrEmpty(l) && l.Contains("Cam")).ToArray(); //Get all layers (by name) with "Cam" in the name

            //Hide visualizers:
            engagementZoneTargeter.GetComponent<Image>().enabled = false;          //Disable image for engagement zone camera targeter
            if (useRadar) radarZoneTargeter.GetComponent<Image>().enabled = false; //Disable image for radar zone camera targeter
        }
        private void Start()
        {
            //Late generation:
            if (useRadar) radarSystem = new TankCamSystem(TankManager.instance.playerTank, tankCameraColor, true); //Initialize radar system

            //Setup:
            normalizedEngagementArea = GetNormalizedRect(engagementZoneTargeter, zoneVisCanvas); //Get area for engagement cameras to occupy
            normalizedRadarArea = GetNormalizedRect(radarZoneTargeter, zoneVisCanvas);           //Get area for radar camera to occupy
        }
        private void Update()
        {
            //Cam system updates:
            if (useRadar) radarSystem.UpdateEverything(Time.deltaTime); //Fully update radar system
            if (camSystems.Count > 0) foreach (TankCamSystem system in camSystems) system.UpdateEverything(Time.deltaTime); //Fully update all values in each camera system
            for (int x = 0; x < camSystems.Count;) //Iterate manually through camsystems list (destruction check)
            {
                TankCamSystem currentSystem = camSystems[x]; //Get current system
                if (currentSystem.timeUntilDeath == 0) camSystems.Remove(currentSystem); //Remove (destroy) a cam system once it is dead and has been cleaned up
                else x++; //Increment to next system if not dead
            }

            //Debug:
            if (Application.isEditor) //Editor-specific updates
            {
                normalizedEngagementArea = GetNormalizedRect(engagementZoneTargeter, zoneVisCanvas); //Re-locate engagement area in case it is being changed at runtime for debug purposes
                normalizedRadarArea = GetNormalizedRect(radarZoneTargeter, zoneVisCanvas);           //Re-locate radar area in case it is being changed at runtime for debug purposes
            }
        }
        private void OnDrawGizmos()
        {
            if (Application.isEditor && Application.isPlaying)
            {

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
        /// <summary>
        /// Returns given position normalized relative to screen space canvas.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        private Vector2 GetNormalizedVector(Vector2 position, Canvas refCanvas)
        {
            Vector2 screenRes = new Vector2(Screen.currentResolution.width, Screen.currentResolution.height); //Get width and height (in pixels) of active screen
            Vector2 displayRes = new Vector2(Display.main.renderingWidth, Display.main.renderingHeight);      //Get width and height (in pixels) of display setting (may be different from screen)
            Vector2 resModFactor = screenRes / displayRes;                                                    //Get scale adjustment factor depending on discrepancy between resolution of display and resolution of game
            return (position / screenRes) * resModFactor * refCanvas.scaleFactor;                             //Return position adjusted for resolution of the screen and discrepancy between game and screen resolution
        }
        private void GenerateCamSystem(TankController targetTank)
        {
            TankCamSystem newSystem = new TankCamSystem(targetTank, tankCameraColor); //Generate new system using constructor
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
            if (tank.tankType != TankId.TankType.PLAYER) {
                if (main.PlayerCamSystem().tanks.Contains(tank))
                {
                    TankController playertank = TankManager.instance.playerTank;
                    main.ShakeTankCamera(playertank, GameManager.Instance.SystemEffects.GetScreenShakeSetting("TankExplosionShake"));
                }
            }

            //Remove tank from camSystems
            foreach (TankCamSystem camSystem in camSystems) if (camSystem.tanks[0] == tank) camSystem.CleanUpLater(); //Clean up main cam system for destroyed tank
        }
        /// <summary>
        /// Finds tank for which this is the primary camera and shakes it according to given settings.
        /// </summary>
        public void ShakeTankCamera(TankController tank, ScreenshakeSettings settings)
        {
            ShakeTankCamera(tank, settings.intensity, settings.duration); //Get data out of settings and send it to other method.
        }
        public void ShakeTankCamera(TankController tank, float intensity, float duration)
        {
            if (tank == null) return;                                                                                  //Do not allow method to run with null camera
            CinemachineVirtualCamera cam = null;                                                                       //Initialize container to store target camera
            foreach (TankCamSystem system in camSystems)
            {
                if (camSystems.Count <= 0) break;
                if (system.tanks[0] == tank)
                {
                    cam = system.vcam; break; //Try to find system for which given tank is the main one
                }
            }
            if (cam == null) { print("Could not find camSystem for given tank when attempting screenshake"); return; } //Indicate if tank cam could not be found
            GameManager.Instance.SystemEffects.ShakeCamera(cam, intensity, duration);                                  //Shake found camera using given settings
        }

        //UTILITY METHODS:
        /// <summary>
        /// The number of currently-enabled cam systems.
        /// </summary>
        public int EnabledCamCount() { return camSystems.Where(c => c.enabled == true).Count(); }
        /// <summary>
        /// The index of given system in camSystems, only considering enabled cameras as part of the list.
        /// </summary>
        public int EnabledCamIndex(TankCamSystem system) { return camSystems.Where(c => c.enabled == true).ToList().IndexOf(system); }
        /// <summary>
        /// The player tank's cam system.
        /// </summary>
        public TankCamSystem PlayerCamSystem() { return camSystems.Where(c => c.tanks.Contains(TankManager.instance.playerTank)).FirstOrDefault(); }
        /// <summary>
        /// Creates a layermask which excludes all camera layers except the given one.
        /// </summary>
        /// <param name="camLayerName"></param>
        /// <returns></returns>
        private LayerMask MakeCamMask(string camLayerName)
        {
            string[] excludedCamLayers = main.camLayers.Where(layer => layer != camLayerName).ToArray();      //Get list of all camera layers excluding the one used by this camera
            LayerMask camLayerMask = ~0;                                                                      //Since the camera needs to be able to see every layer except excluded cameras, start with a layermask which includes every layer
            foreach (string layer in excludedCamLayers) camLayerMask &= ~(1 << LayerMask.NameToLayer(layer)); //Systematically exclude each camera layer which is not designated for this tank cam from the layermask
            return camLayerMask;                                                                              //Set camera culling mask so that camera can only see vcam on designated layer
        }
        /// <summary>
        /// Checks with all cam systems to see if any are still engaged, and returns true if so.
        /// </summary>
        /// <returns></returns>
        public bool CheckIfStillEngaged()
        {
            foreach (TankCamSystem system in camSystems) if (system.engaged) return true; //Return true if a single engaged camera can be found
            return false;                                                                 //Otherwise, return false
        }
    }
}
