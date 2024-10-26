using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TowerTanks.Scripts
{
    [CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/RoomAssetKit", order = 1)]
    public class RoomAssetKit : ScriptableObject
    {
        //Classes, Enums & Structs:
        [System.Serializable]
        [Tooltip("Defines a sprite which may be selected for the room kit based on a weighted system, and placed according to designated offset and scale.")]
        public class RoomAsset
        {
            //Values:
            [Tooltip("The image this asset represents.")]                                           public Sprite sprite;
            [Tooltip("How likely this asset is to be chosen if alternates are available."), Min(0)] public float weight = 1;
            [Tooltip("Modifier used to precisely place asset in correct position.")]                public Vector2 offset;
            [Tooltip("Modifier used to make sure asset is the right size.")]                        public Vector2 scale = Vector2.one;

            //FUNCTIONALITY METHODS:
            /// <summary>
            /// Applies and positions asset in given spriterenderer/transform, according to asset settings.
            /// </summary>
            public void Apply(SpriteRenderer socket)
            {
                socket.sprite = sprite;                                                                                                                                   //Set sprite
                socket.transform.localScale = new Vector3(scale.x * (1 / socket.transform.parent.localScale.x), scale.y * (1 / socket.transform.parent.localScale.y), 1); //Set scale of sprite object (compensating for scale of parent container, necessary for making walls work)
                socket.transform.localPosition = new Vector3(offset.x, offset.y, 0);                                                                                      //Set position of sprite object
            }
        }

        //Assets:
        [Header("Essential Assets:")]
        [Tooltip("Tiled sprite used for the back wall of the entire cell.")] public Sprite backWallSprite;
        [Tooltip("Scale of back wall sprite."), Min(0)]                      public float backWallScale = 1;
        [Space()]
        [Tooltip("Assets for upper wall of the cell.")] public RoomAsset[] cellTopWalls;
        [Tooltip("Assets for right wall of the cell.")] public RoomAsset[] cellRightWalls;
        [Tooltip("Assets for lower wall of the cell.")] public RoomAsset[] cellBottomWalls;
        [Tooltip("Assets for left wall of the cell.")]  public RoomAsset[] cellLeftWalls;
        [Space()]
        [Tooltip("Corner piece for outside corners of walls with no neighbors.")]                  public RoomAsset[] wallCorner90 = new RoomAsset[4];
        [Tooltip("Corner piece for straightaway walls that connect directly to neighbors.")]       public RoomAsset[] wallCorner180 = new RoomAsset[4];
        [Tooltip("Corner piece for inside corner walls that connect to connectors or neighbors.")] public RoomAsset[] wallCorner270 = new RoomAsset[4];
        [Tooltip("Corner piece for straightaway walls that connect to connectors.")]               public RoomAsset[] wallCornerConnector = new RoomAsset[4];
        [Space()]
        [Tooltip("Assets for upper wall of a connector.")] public RoomAsset[] connectorTopWalls;
        [Tooltip("Assets for right wall of a connector.")] public RoomAsset[] connectorRightWalls;
        [Tooltip("Assets for lower wall of a connector.")] public RoomAsset[] connectorBottomWalls;
        [Tooltip("Assets for left wall of a connector.")]  public RoomAsset[] connectorLeftWalls;

        //FUNCTIONALITY METHODS:
        /// <summary>
        /// Returns a random asset from the given list, accounting for weights of each item.
        /// </summary>
        /// <param name="source">The asset list to pick from.</param>
        public RoomAsset GetAsset(RoomAsset[] source)
        {
            //Validity checks:
            if (source.Length == 0) { Debug.LogError("Tried to get asset from an empty list!"); return null; } //Indicate error if list is empty

            //Get asset:
            if (source.Length == 1) return source[0]; //If there is a single asset in list, simply return that
            float totalWeight = 0;                                           //Initialize value to store total weight of all assets in list
            foreach (RoomAsset asset in source) totalWeight += asset.weight; //Tally up weights of all assets in list
            float selector = Random.Range(0, totalWeight);                   //Get a value between zero and total weight (both inclusive), which can be used to select an element and will prefer randomly picking elements with larger weights
            foreach (RoomAsset asset in source) //Iterate through asset list again
            {
                selector -= asset.weight;           //Subtract each asset weight from selector
                if (selector < 0) { return asset; } //Return asset once selector value has been reached
            }
            return source[^1]; //If nothing has been selected by the selector subtraction method, pick the last asset (random value must be equal to total weight)
        }
        /// <summary>
        /// Applies this kit to given room.
        /// </summary>
        public void KitRoom(Room room)
        {
            //First-time back wall setup:
            if (backWallSprite != null && room.backWallSprite == null) //Room does not already have a back wall sprite
            {
                //Get full bounds of room:
                Bounds bounds = room.GetRoomBounds(); //Use room bounds method to get bounds

                //Generate back wall sprite:
                room.backWallSprite = new GameObject(room.name + "_backWall", typeof(SpriteRenderer), typeof(SortingGroup)).GetComponent<SpriteRenderer>(); //Generate an object with a spriterenderer and a sorting group and name it after this room
                room.backWallSprite.transform.parent = room.transform;                         //Child sprite object to room transform
                room.backWallSprite.sprite = backWallSprite;                                   //Apply back wall sprite to renderer
                room.backWallSprite.drawMode = SpriteDrawMode.Tiled;                           //Set sprite to tile mode so that it repeats if necessary
                room.backWallSprite.tileMode = SpriteTileMode.Continuous;                      //Use continuous mode for tiling (adaptive messes with scaling calculation
                room.backWallSprite.maskInteraction = SpriteMaskInteraction.VisibleInsideMask; //Make wall sprite only visible inside masks (will be back walls of cells and connectors)

                //Set wall dimensions:
                room.backWallSprite.transform.position = bounds.center;                                  //Center wall sprite to bounds of room
                room.backWallSprite.transform.localEulerAngles = Vector3.zero;                           //Make sure wall rotation matches room rotation
                room.backWallSprite.transform.localScale = new Vector3(backWallScale, backWallScale, 1); //Adjust scale of sprite according to setting
                room.backWallSprite.size = bounds.size * (1 / backWallScale);                            //Adjust size of sprite to precise size of full room, accounting for scale of object

                //Create mask quilt:
                List<GameObject> roomBackWallObjects = new List<GameObject>();                                //Create a list to store all back wall objects in rooms
                foreach (Cell cell in room.cells) roomBackWallObjects.Add(cell.backWall);                     //Add back walls from cells to list
                foreach (Connector connector in room.connectors) roomBackWallObjects.Add(connector.backWall); //Add back walls from connectors to list
                foreach (GameObject roomBackWall in roomBackWallObjects) //Iterate through back walls in room
                {
                    SpriteMask mask = roomBackWall.AddComponent<SpriteMask>();        //Add a sprite mask to back wall object
                    mask.sprite = roomBackWall.GetComponent<SpriteRenderer>().sprite; //Get square sprite from existing back wall renderer
                    Destroy(roomBackWall.GetComponent<SpriteRenderer>());             //Destroy sprite renderer now that it isn't needed
                    roomBackWall.transform.parent = room.backWallSprite.transform;    //Child mask to actual back wall sprite so it properly masks out footprint of room
                }
            }

            //Set up granular cell components:
            RoomAsset[][] neswWallAssets = { cellTopWalls, cellRightWalls, cellBottomWalls, cellLeftWalls }; //Get array of all side wall asset sets
            foreach (Cell cell in room.cells) //Iterate through cells in room
            {
                //CELL WALLS:
                for (int x = 0; x < 4; x++) //Iterate through each wall in cell
                {
                    //Validity checks:
                    if (!cell.walls[x].activeSelf) continue;     //Skip walls which are disabled
                    if (neswWallAssets[x].Length == 0) continue; //Skip walls which do not have a corresponding asset yet

                    //Apply middle section of wall:
                    GetAsset(neswWallAssets[x]).Apply(cell.walls[x].transform.Find("Middle").GetComponent<SpriteRenderer>()); //Assign a random wall from the appropriate wall asset list (this will be the flat section of the wall which does not need to adapt depending on adjacent cells).
                    cell.walls[x].GetComponent<SpriteRenderer>().enabled = false;                                             //Disable placeholder sprite asset
                }

                //CELL WALL CORNERS:
                for (int x = 0; x < 4; x++) //Iterate through each of the four corners in the cell
                {
                    //Determine corner type:
                    if (cell.connectors[x] != null) //Corner is on the counterclockwise (left) side of a connector
                    {
                        //USE CONNECTOR CORNER
                    }
                    else if (cell.connectors[x - 1 >= 0 ? x - 1 : 3] != null) //Corner is on the clockwise (right) side of a connector
                    {
                        //USE CONNECTOR CORNER
                    }
                    else if (cell.neighbors[x] != null || cell.neighbors[x - 1 >= 0 ? x - 1 : 3] != null) //Corner is being modified by at least one neighboring cell
                    {
                        if (cell.neighbors[x] != null && cell.neighbors[x - 1 >= 0 ? x - 1 : 3] != null) //Corner has a neighboring cell to its left and right
                        {
                            if (cell.neighbors[x].neighbors[x - 1 >= 0 ? x - 1 : 3] != null) //Corner is in the middle of a 2x2 arrangement of 4 cells
                            {
                                //CORNER IS INVISIBLE
                            }
                            else //Corner is in the middle of the inside elbow of an L-shaped arrangement of 3 cells
                            {
                                //USE 270 CORNER
                            }
                        }
                        else //Corner is only being modified by one neighboring cell
                        {
                            if (cell.neighbors[x] != null) //Corner is part of a straightaway with its clockwise neighbor
                            {
                                //USE 180 CORNER
                            }
                            else //Corner is part of a straightaway with its counterclockwise neighbor
                            {
                                //USE 180 CORNER
                            }
                        }
                    }
                    else //Corner is not affected by any neighbors or connectors
                    {
                        wallCorner90[x].Apply(cell.corners[x]); //Use 90 degree corner, select corresponding one from corner array
                    }
                }

                //CELL FLOORS:
                if (cell.neighbors[2] == null) //Cell has no lower neighbors
                {
                    //PLACE FLOOR
                }
            }

            //Set up connectors:
            foreach (Connector connector in room.connectors) //Iterate through connectors in room
            {
                //NOTE: Add stuff to set up connector back wall mask with rest of the cells

                //Set up walls:
                if (connector.vertical) //Only left and right walls need to be set up
                {
                    //Get walls to modify:
                    GameObject leftWall = connector.walls[0].transform.position.x < connector.walls[1].transform.position.x ? connector.walls[0] : connector.walls[1]; //Get leftmost wall in connector based on relative position
                    GameObject rightWall = leftWall == connector.walls[0] ? connector.walls[1] : connector.walls[0];                                                   //Right wall is whichever wall left wall is not

                    //Apply wall asset:
                    if (connectorLeftWalls.Length > 0) //Only apply wall asset if one is present in kit
                    {
                        GetAsset(connectorLeftWalls).Apply(leftWall.transform.Find("Sprite").GetComponent<SpriteRenderer>()); //Assign a random wall from the appropriate wall asset list
                        leftWall.GetComponent<SpriteRenderer>().enabled = false;                                              //Disable placeholder sprite asset
                    }
                    if (connectorRightWalls.Length > 0) //Only apply wall asset if one is present in kit
                    {
                        GetAsset(connectorRightWalls).Apply(rightWall.transform.Find("Sprite").GetComponent<SpriteRenderer>()); //Assign a random wall from the appropriate wall asset list
                        rightWall.GetComponent<SpriteRenderer>().enabled = false;                                               //Disable placeholder sprite asset
                    }
                }
                else //Only top and bottom walls need to be set up
                {
                    //Get walls to modify:
                    GameObject topWall = connector.walls[0].transform.position.y > connector.walls[1].transform.position.y ? connector.walls[0] : connector.walls[1]; //Get uppermost wall in connector based on relative position
                    GameObject bottomWall = topWall == connector.walls[0] ? connector.walls[1] : connector.walls[0];                                                  //Bottom wall is whichever wall top wall is not

                    //Apply wall asset:
                    if (connectorTopWalls.Length > 0) //Only apply wall asset if one is present in kit
                    {
                        GetAsset(connectorTopWalls).Apply(topWall.transform.Find("Sprite").GetComponent<SpriteRenderer>()); //Assign a random wall from the appropriate wall asset list
                        topWall.GetComponent<SpriteRenderer>().enabled = false;                                             //Disable placeholder sprite asset
                    }
                    if (connectorBottomWalls.Length > 0) //Only apply wall asset if one is present in kit
                    {
                        GetAsset(connectorBottomWalls).Apply(bottomWall.transform.Find("Sprite").GetComponent<SpriteRenderer>()); //Assign a random wall from the appropriate wall asset list
                        bottomWall.GetComponent<SpriteRenderer>().enabled = false;                                                //Disable placeholder sprite asset
                    }
                }
            }
        }
    }
}
