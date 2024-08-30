using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CameraManipulator : MonoBehaviour
{
    //Objects & Components:
    [Tooltip("Singleton instance of camera manipulator in scene.")] public static CameraManipulator main;
    [Tooltip("Reference to single main camera in scene.")]          private Camera cam;
    [Tooltip("Collider around bounds of main camera")]              private BoxCollider2D playerCamCollider;

    //Settings:
    [Header("Player Camera Settings:")]
    [Tooltip("Virtual camera focused on player tank.")] public CinemachineVirtualCamera playerTankCamera;
    [SerializeField, Tooltip("Roundness of collider corners around camera plane (smooths out edge UI)."), Min(0)] private float colliderEdgeRadius;
    [SerializeField, Tooltip("Buffer distance above and below tank where camera will zoom out to.")]              private float cameraHeightBuffer;

    //Runtima Variables:
    private Transform[] groupTargets = new Transform[2]; //Markers representing bounds of tank used to apply buffer to camera target zone

    //UNITY METHODS:
    private void Awake()
    {
        //Initialization:
        main = this; //Make latest instance of this script main

        //Get objects & components:
        cam = GetComponent<Camera>();                                //Get camera component from this object
        playerCamCollider = GetComponentInChildren<BoxCollider2D>(); //Get box collider for local camera
    }
    private void Update()
    {
        UpdateBoundCollider(); //Update camera boundaries (so that systems detecting offscreen stuff work)
    }
    private void OnDrawGizmos()
    {
        if (Application.isEditor && Application.isPlaying)
        {
            Gizmos.color = Color.cyan;
            if (groupTargets[1] != null) Gizmos.DrawSphere(groupTargets[1].position, 0.2f);
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
            CinemachineTargetGroup targetGroup = playerTankCamera.GetComponentInChildren<CinemachineTargetGroup>(); //Get targetGroup component from player camera
            if (targetGroup.m_Targets.Length == 0) //Group target system has not yet been initialized
            {
                groupTargets[0] = new GameObject("TreadCamTarget").transform; //Generate first target and name it so we know it's for the bottom bound
                groupTargets[0].parent = targetTank.treadSystem.transform;    //Child tread target to tank's treadSystem

                groupTargets[1] = new GameObject("TopCamTarget").transform; //Generate second target and name it so we know it's for the top bound
                groupTargets[1].parent = targetTank.treadSystem.transform;  //Child upper target to tank's treadsystem (NOTE: maybe change to tower joint if necessary)

                foreach (Transform target in groupTargets) targetGroup.AddMember(target, 1, 0); //Add targets to tank group
            }

            //Update target settings:
            groupTargets[0].localPosition = Vector3.down * cameraHeightBuffer; //Update height of lower target according to buffer

            //Modify target group:
            Vector3 tallFactor = Vector3.Project((targetTank.highestCell.transform.position - groupTargets[1].parent.transform.position), groupTargets[1].parent.up); //Get height of cell in local space of target parent projected onto centered vertical axis
            groupTargets[1].localPosition = tallFactor + (tallFactor.normalized * cameraHeightBuffer);                                                                //Apply cam height buffer and set position of target
            
            //CinemachineTargetGroup targetGroup = playerTankCamera.GetComponentInChildren<CinemachineTargetGroup>(); //Get target group component from tank virtual camera
            //if (targetGroup.m_Targets.Length > 1) targetGroup.RemoveMember(targetGroup.m_Targets[1].target);        //Remove previous highest cell (kind of janky)
            //if (targetTank.highestCell != null) targetGroup.AddMember(targetTank.highestCell.transform, 1, 0);      //Have camera follower bounds include tallest cell on tank
        }
    }
}
