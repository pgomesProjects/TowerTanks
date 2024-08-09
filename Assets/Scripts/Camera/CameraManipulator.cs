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

    //Runtima Variables:


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
        //Update collider:
        float camHeight = cam.orthographicSize * 2;                                                              //Get real world height of camera frame
        float radiusBuffer = colliderEdgeRadius * 2;                                                             //Get value to account for space taken up by radius of collider
        playerCamCollider.size = new Vector2((cam.aspect * camHeight) - radiusBuffer, camHeight - radiusBuffer); //Set size of collider
        playerCamCollider.edgeRadius = colliderEdgeRadius;                                                       //Set size of edge radius
    }
}
