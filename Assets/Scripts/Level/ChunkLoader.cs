using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkLoader : MonoBehaviour
{
    private const float RENDER_DISTANCE = 300f;

    [SerializeField] private Transform groundParentTransform;
    [SerializeField] private Transform startingGroundPosition;
    [SerializeField] private Transform groundPrefab;
    private PlayerTankController playerTank;

    private Vector3 lastLeftEndPosition, lastRightEndPosition;

    private void Awake()
    {
        playerTank = FindObjectOfType<PlayerTankController>();
        lastLeftEndPosition = startingGroundPosition.Find("LeftEndPosition").position;
        lastRightEndPosition = startingGroundPosition.Find("RightEndPosition").position;
    }

    private void SpawnGroundLeft()
    {
        Transform newGroundTransform = InstantiateGround(lastLeftEndPosition);
        lastLeftEndPosition = newGroundTransform.Find("LeftEndPosition").position;
    }

    private void SpawnGroundRight()
    {
        Transform newGroundTransform = InstantiateGround(lastRightEndPosition);
        lastRightEndPosition = newGroundTransform.Find("RightEndPosition").position;
    }

    private Transform InstantiateGround(Vector3 spawnPosition)
    {
        Transform newGroundTransform = Instantiate(groundPrefab, new Vector3(spawnPosition.x, spawnPosition.y, spawnPosition.z), Quaternion.identity);
        newGroundTransform.SetParent(groundParentTransform);
        return newGroundTransform;
    }

    private void Update()
    {
        //Debug.Log("Left End Position Distance: " + Vector3.Distance(playerTank.transform.position, lastLeftEndPosition));
        //Debug.Log("Right End Position Distance: " + Vector3.Distance(playerTank.transform.position, lastRightEndPosition));

        if(playerTank != null)
        {
            if (Vector3.Distance(playerTank.transform.position, lastLeftEndPosition) < RENDER_DISTANCE)
                SpawnGroundLeft();

            if (Vector3.Distance(playerTank.transform.position, lastRightEndPosition) < RENDER_DISTANCE)
                SpawnGroundRight();
        }
    }
}
