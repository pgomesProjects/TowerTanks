using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkLoader : MonoBehaviour
{
    private const float RENDER_DISTANCE = 300f; //The maximum distance that the player tank can see from

    [SerializeField, Tooltip("The parent to keep all ground pieces in when spawned.")] private Transform groundParentTransform;
    [SerializeField, Tooltip("The ground object that the level starts with.")] private Transform startingGroundPosition;
    [SerializeField, Tooltip("The ground prefab to use for spawning new ground objects.")] private Transform groundPrefab;

    private PlayerTankController playerTank;    //The player tank object

    private Vector3 lastLeftEndPosition, lastRightEndPosition;  //The farthest left and right positions of the spawned ground
    private float groundWidth;

    private void Awake()
    {
        playerTank = FindObjectOfType<PlayerTankController>();

        //Get the starting ground piece's left and right end positions
        lastLeftEndPosition = startingGroundPosition.Find("LeftEndPosition").position;
        lastRightEndPosition = startingGroundPosition.Find("RightEndPosition").position;

        groundWidth = Mathf.Abs(lastLeftEndPosition.x) + Mathf.Abs(lastRightEndPosition.x); 
    }

    /// <summary>
    /// Spawns ground to the right of the last left end position and sets a new left end position.
    /// </summary>
    private void SpawnGroundLeft()
    {
        Transform newGroundTransform = InstantiateGround(new Vector3(lastLeftEndPosition.x - (groundWidth / 2), lastLeftEndPosition.y, lastLeftEndPosition.z));
        lastLeftEndPosition = newGroundTransform.Find("LeftEndPosition").position;
    }

    /// <summary>
    /// Spawns ground to the right of the last right end position and sets a new right end position.
    /// </summary>
    private void SpawnGroundRight()
    {
        Transform newGroundTransform = InstantiateGround(new Vector3(lastRightEndPosition.x + (groundWidth / 2), lastRightEndPosition.y, lastRightEndPosition.z));
        lastRightEndPosition = newGroundTransform.Find("RightEndPosition").position;
    }

    /// <summary>
    /// Creates a new ground object based on the spawn position given.
    /// </summary>
    /// <param name="spawnPosition">The position for the ground to spawn at.</param>
    /// <returns>The transform of the newly spawned ground object.</returns>
    private Transform InstantiateGround(Vector3 spawnPosition)
    {
        Transform newGroundTransform = Instantiate(groundPrefab, spawnPosition, Quaternion.identity);
        newGroundTransform.SetParent(groundParentTransform);    //Gets put into a parent for organization
        return newGroundTransform;
    }

    private void Update()
    {
        //Debug.Log("Left End Position Distance: " + Vector3.Distance(playerTank.transform.position, lastLeftEndPosition));
        //Debug.Log("Right End Position Distance: " + Vector3.Distance(playerTank.transform.position, lastRightEndPosition));

        if(playerTank != null)
        {
            //If the distance between the player tank and the last left end position is less than the render distance, create more ground to the left
            if (Vector3.Distance(playerTank.transform.position, lastLeftEndPosition) < RENDER_DISTANCE)
                SpawnGroundLeft();

            //If the distance between the player tank and the last right end position is less than the render distance, create more ground to the right
            if (Vector3.Distance(playerTank.transform.position, lastRightEndPosition) < RENDER_DISTANCE)
                SpawnGroundRight();
        }
    }
}
