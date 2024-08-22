using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LadderCollGenerator : MonoBehaviour
{
    //Generates colliders for the top and bottom of a ladder if there is no ladder above or below it
    private LayerMask ladderLayer;
    private BoxCollider2D myBoxCollider2D;
    private Bounds boxBounds;
    Vector2 topRightPoint, topLeftPoint, bottomRightPoint, bottomLeftPoint;

    private bool setTop, setBottom;
    
    IEnumerator Start()
    {
        myBoxCollider2D = GetComponent<BoxCollider2D>();
        boxBounds = myBoxCollider2D.bounds;
        
        topRightPoint = new Vector2(boxBounds.center.x + boxBounds.extents.x, boxBounds.center.y + boxBounds.extents.y);
        topLeftPoint = new Vector2(boxBounds.center.x - boxBounds.extents.x, boxBounds.center.y + boxBounds.extents.y);
        
        bottomRightPoint = new Vector2(boxBounds.center.x + boxBounds.extents.x, boxBounds.center.y - boxBounds.extents.y);
        bottomLeftPoint = new Vector2(boxBounds.center.x - boxBounds.extents.x, boxBounds.center.y - boxBounds.extents.y);
        
        ladderLayer = 1 << LayerMask.NameToLayer("Ladder");
        yield return new WaitForSeconds(0.1f); // We buffer the generation to make sure all ladders are in place

        var topHit = Physics2D.Raycast(transform.position, Vector2.up, .8f, ladderLayer);
        var bottomHit = Physics2D.Raycast(transform.position, Vector2.down, .8f, ladderLayer);

        if (!topHit) //if no ladder is over this one, create an edge collider for the player to stop on
        {
            GenerateCollider(topLeftPoint, topRightPoint);
            setTop = true;
        }
        if (!bottomHit)
        {
            GenerateCollider(bottomLeftPoint, bottomRightPoint);
            setBottom = true;
        }
    }

    private void Update()
    {
        boxBounds = myBoxCollider2D.bounds;
        
        topRightPoint = new Vector2(boxBounds.center.x + boxBounds.extents.x, boxBounds.center.y + boxBounds.extents.y);
        topLeftPoint = new Vector2(boxBounds.center.x - boxBounds.extents.x, boxBounds.center.y + boxBounds.extents.y);
        
        bottomRightPoint = new Vector2(boxBounds.center.x + boxBounds.extents.x, boxBounds.center.y - boxBounds.extents.y);
        bottomLeftPoint = new Vector2(boxBounds.center.x - boxBounds.extents.x, boxBounds.center.y - boxBounds.extents.y);
        //this is only in update so i have gizmos as a visualizer for the edge collider. will be deleted later
    }

    private void GenerateCollider(Vector2 leftPoint, Vector2 rightPoint)
    {
        GameObject edgeCollGo = new GameObject("LadderEnd");
        edgeCollGo.layer = LayerMask.NameToLayer("LadderEnd");
        edgeCollGo.transform.SetParent(transform);
        edgeCollGo.transform.position = (leftPoint + rightPoint) * .5f; // Centers the colliders transform
        EdgeCollider2D collider = edgeCollGo.AddComponent<EdgeCollider2D>();

        // Convert the points from world space to local space
        Vector2 localLeftPoint = edgeCollGo.transform.InverseTransformPoint(leftPoint);
        Vector2 localRightPoint = edgeCollGo.transform.InverseTransformPoint(rightPoint);

        collider.SetPoints(new List<Vector2>() { localLeftPoint, localRightPoint });
    }
    
    private void OnDrawGizmos()
    {
        float lineThickness = 0.1f; // Set the thickness of your line here

        if (setTop)
        {
            Gizmos.color = Color.red;
            Vector3 midPoint = (topLeftPoint + topRightPoint) / 2;
            float lineLength = Vector2.Distance(topLeftPoint, topRightPoint);
            Gizmos.DrawCube(midPoint, new Vector3(lineLength, lineThickness, lineThickness));
        }

        if (setBottom)
        {
            Gizmos.color = Color.blue;
            Vector3 midPoint = (bottomLeftPoint + bottomRightPoint) / 2;
            float lineLength = Vector2.Distance(bottomLeftPoint, bottomRightPoint);
            Gizmos.DrawCube(midPoint, new Vector3(lineLength, lineThickness, lineThickness));
        }
        
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + (Vector3.up * .8f));
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + (Vector3.down * .8f));
    }
}
