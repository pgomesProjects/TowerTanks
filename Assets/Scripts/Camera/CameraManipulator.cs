using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using System.Linq;

public class CameraManipulator : MonoBehaviour
{
    //Objects & Components:
    [Tooltip("Singleton instance of camera manipulator in scene.")] public static CameraManipulator main;
    [Tooltip("Reference to single main camera in scene.")]          private Camera cam;
    [Tooltip("Collider around bounds of main camera")]              private BoxCollider2D playerCamCollider;

    //Settings:
    [Header("Player Camera Settings:")]
    [Tooltip("Virtual camera focused on player tank.")]                                                           public CinemachineVirtualCamera playerTankCamera;
    [SerializeField, Tooltip("Roundness of collider corners around camera plane (smooths out edge UI)."), Min(0)] private float colliderEdgeRadius;
    [SerializeField, Tooltip("Distance between top of tank and top of camera frame."), Min(0)]                    private float upperScreenBuffer;
    [SerializeField, Tooltip("Distance between bottom of tank and bottom of camera frame."), Min(0)]              private float lowerScreenBuffer;
    [Header("Multi-Tank Camera Settings:")]
    [SerializeField, Tooltip("Distance from an enemy tank at which camera will expand to include that tank."), Min(0)] private float enemyEngagementDistance;
    [SerializeField, Tooltip("Radius of target point generated for enemy tank when part of target group."), Min(0)]    private float engagedTankCamRadius;

    //Runtima Variables:
    private CinemachineTargetGroup playerTargetGroup;    //Target group component used to aim player camera
    private Transform[] groupTargets = new Transform[3]; //Markers representing bounds of tank used to apply buffer to camera target zone
    private float lastTallFactor;                        //Last calculated relative height of latest target tank's highest cell
    private Transform camTargetContainer;                //Object in player tank which stores positions for camera target group to keep track of
    private TankController engagedTank;                  //Enemy tank which is currently within engagement distance of player

    //UNITY METHODS:
    private void Awake()
    {
        //Initialization:
        main = this; //Make latest instance of this script main

        //Get objects & components:
        cam = GetComponent<Camera>();                                //Get camera component from this object
        playerCamCollider = GetComponentInChildren<BoxCollider2D>(); //Get box collider for local camera

        //Cleanup:
        if (playerTankCamera != null) //Check within player tank VCam
        {
            //This step needs to be done because the camera wanders in the editor when it has a null target group, but in runtime this system needs to start with an empty targetGroup
            playerTargetGroup = playerTankCamera.GetComponentInChildren<CinemachineTargetGroup>();                                //Get targetGroup component from player camera
            while (playerTargetGroup.m_Targets.Length > 0) playerTargetGroup.RemoveMember(playerTargetGroup.m_Targets[0].target); //Remove all targets
        }
    }
    private void Update()
    {
        //Regular updates:
        UpdateBoundCollider(); //Update camera boundaries (so that systems detecting offscreen stuff work)
        camTargetContainer.eulerAngles = Vector3.zero; //Make sure camera target container's world rotation is always zero

        //Check for engagement:
        if (TankManager.instance.playerTank != null && TankManager.instance.tanks.Count > 1) //The player is not alone
        {
            TankController[] tanks = TankManager.instance.tanks.Where(tank => tank.tankType != TankId.TankType.PLAYER).Select(tank => tank.tankScript).ToArray(); //Get array of all non-player tanks
            foreach (TankController tank in tanks) //Iterate through array of enemy tanks
            {
                float separation = Mathf.Abs(TankManager.instance.playerTank.treadSystem.transform.position.x - tank.treadSystem.transform.position.x); //Get distance between player and enemy tanks
                if (separation <= enemyEngagementDistance) //Enemy tank is within range of player
                {
                    if (engagedTank == null || playerTargetGroup.m_Targets.Length < 3) //Player is not already engaged with an enemy
                    {
                        //Generate new target:
                        engagedTank = tank;                                                    //Indicate that tank is now engaged
                        Transform enemyCamTarget = new GameObject("EnemyCamTarget").transform; //Generate a new target for camera to include in group
                        enemyCamTarget.parent = engagedTank.treadSystem.transform;             //Child target to enemy tread system
                        enemyCamTarget.localPosition = Vector3.zero;                           //Zero out position to enemy
                        playerTargetGroup.AddMember(enemyCamTarget, 1, engagedTankCamRadius);  //Add to target group
                        playerTargetGroup.m_Targets[0].radius = engagedTankCamRadius;          //Update player target radius as well while in engaged mode
                    }
                    else //Player is already engaged with an enemy tank
                    {
                        if (separation < TankManager.instance.playerTank.treadSystem.transform.position.x - engagedTank.treadSystem.transform.position.x) //Newly-engaged tank is closer
                        {
                            //Reposition existing target:
                            engagedTank = tank;                                                        //Indicate that tank is now engaged
                            playerTargetGroup.m_Targets[2].target.parent = tank.treadSystem.transform; //Move camera target to new tank
                            playerTargetGroup.m_Targets[2].target.localPosition = Vector3.zero;        //Zero out target position
                        }
                    }
                }
                else if (tank == engagedTank) //Tank is disengaging
                {
                    //Destroy target:
                    Transform enemyCamTarget = playerTargetGroup.m_Targets[2].target; //Get reference to target object from group
                    playerTargetGroup.RemoveMember(enemyCamTarget);                   //Remove target from group
                    Destroy(enemyCamTarget.gameObject);                               //Destroy unused cam target

                    //Cleanup:
                    engagedTank = null;                        //Clear engaged tank marker
                    playerTargetGroup.m_Targets[0].radius = 0; //Update player target radius when leaving engagement mode
                }
            }
        }

        //Constant updates for debug only:
        if (Application.isEditor) //Only do these updates in the editor (will be updated conditionally otherwise)
        {
            //Buffer updates:
            if (groupTargets[0] != null) groupTargets[0].localPosition = Vector3.down * lowerScreenBuffer;                  //Update height of lower target according to buffer
            if (groupTargets[1] != null) groupTargets[1].localPosition = Vector3.up * (lastTallFactor + upperScreenBuffer); //Update height of upper target according to buffer

            //Radius updates:
            if (playerTargetGroup.m_Targets.Length > 2) playerTargetGroup.m_Targets[2].radius = engagedTankCamRadius; //Keep engaged tank radius updated if applicable
            if (engagedTank != null) playerTargetGroup.m_Targets[0].radius = engagedTankCamRadius;                    //Keep player tank engaged radius updated
            else playerTargetGroup.m_Targets[0].radius = 0;                                                           //Update to disengaged radius on player tank if applicable
        }
    }

    //UTILITY METHODS:
    /// <summary>
    /// Updates the collider which represents camera bounds.
    /// </summary>
    private void UpdateBoundCollider()
    {
        float camHeight = cam.orthographicSize * 2;                                                              //Get real world height of camera frame
        float radiusBuffer = colliderEdgeRadius * 2;                                                             //Get value to account for space taken up by radius of collider
        playerCamCollider.size = new Vector2((cam.aspect * camHeight) - radiusBuffer, camHeight - radiusBuffer); //Set size of collider
        playerCamCollider.edgeRadius = colliderEdgeRadius;                                                       //Set size of edge radius
    }
    public void UpdateTargetGroup(TankController targetTank)
    {
        if (playerTankCamera != null) //PlayerTankCamera component is present (should generally always be)
        {
            //Initialize (once):
            if (playerTargetGroup == null) playerTargetGroup = playerTankCamera.GetComponentInChildren<CinemachineTargetGroup>(); //Get targetGroup component from player camera
            if (playerTargetGroup.m_Targets.Length == 0) //Group target system has not yet been initialized
            {
                camTargetContainer = new GameObject("CamTargetContainer").transform; //Generate a new object to pu camera targets in
                camTargetContainer.parent = targetTank.treadSystem.transform;        //Child target container to player tank's tread transform
                camTargetContainer.localPosition = Vector3.zero;                     //Zero out relative position to tank center

                groupTargets[0] = new GameObject("TreadCamTarget").transform; //Generate first target and name it so we know it's for the bottom bound
                groupTargets[0].parent = camTargetContainer;                  //Child tread target to generated container

                groupTargets[1] = new GameObject("TopCamTarget").transform; //Generate second target and name it so we know it's for the top bound
                groupTargets[1].parent = camTargetContainer;                //Child tread target to generated container

                foreach (Transform target in groupTargets) playerTargetGroup.AddMember(target, 1, 0); //Add targets to tank group
            }

            //Update target settings:
            groupTargets[0].localPosition = Vector3.down * lowerScreenBuffer; //Update height of lower target according to buffer

            //Modify target group:
            lastTallFactor = Vector3.Project((targetTank.highestCell.transform.position - groupTargets[1].parent.transform.position), groupTargets[1].parent.up).magnitude; //Get height of cell in local space of target parent projected onto centered vertical axis
            groupTargets[1].localPosition = Vector3.up * (lastTallFactor + upperScreenBuffer);                                                                              //Apply cam height buffer and set position of target
        }
    }
}
