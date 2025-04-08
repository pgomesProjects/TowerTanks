using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.U2D;

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
        [Header("Back Wall:")]
        [Tooltip("Tiled sprite used for the back wall of the entire cell.")] public Sprite backWallSprite;
        [Tooltip("Scale of back wall sprite."), Min(0)]                      public float backWallScale = 1;
        [Header("Side Walls:")]
        [Tooltip("Prefab containing sprite shape asset to be used for external wall.")] public GameObject spriteShapeProfile;
        [Header("Materials:")]
        [Tooltip("Material used for GPU instancing of all room assets. IMPORTANT for performance.")] public Material defaultMaterial;

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
            if (room.ignoreRoomKit) return; //Don't kit the room if we want to ignore it

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
                room.backWallSprite.sharedMaterial = defaultMaterial;                                //Apply universal renderer material

                //Setup Room Animator
                GameObject animator = room.roomData.roomAnimator;
                GameObject _animator = Instantiate(animator, room.backWallSprite.transform, false);
                room.roomAnimator = _animator.GetComponent<Animator>();

                //Modify sorting group:
                room.backWallSprite.GetComponent<SortingGroup>().sortingOrder = 1; //Adjust sorting order of back wall so it can appear in front of some elements

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

            //Setup outline wall object:
            if (room.outerWallController == null && room.wallVerts.Length > 0) //Outer wall object needs to be spawned (and room has vertices assigned)
            {
                //Generate object:
                room.outerWallController = Instantiate(spriteShapeProfile).GetComponent<SpriteShapeController>(); //Instantiate sprite shape profile object and get controller out of it
                room.outerWallController.transform.parent = room.transform;                                       //Child wall object to room
                room.outerWallController.transform.localPosition = Vector3.zero;                                  //Move object on top of room (just for neatness)

                //Move vertices:
                room.outerWallController.spline.Clear(); //Start out by clearing spline
                for (int x = 0; x < room.wallVerts.Length + 2; x++) //Iterate through room wall vertices, wrapping around the last two so that final wall and corner are included
                {
                    room.outerWallController.spline.InsertPointAt(x, room.wallVerts[x < room.wallVerts.Length ? x : x - room.wallVerts.Length] * (1 / spriteShapeProfile.transform.localScale.x)); //Insert spline point at world position of each room vertex point (adjust for scale of profile) (detect wrap so that wallvert index does not go out of bounds and instead overflows)
                }

                //Hide demo walls:
                foreach (Cell cell in room.cells) //Iterate through cells in room
                {
                    foreach (GameObject wall in cell.walls) //Iterate through each wall in cell
                    {
                        wall.GetComponent<SpriteRenderer>().enabled = false; //Disable spriterenderer for demo wall
                    }
                }
            }
            else //Room already has a wall object but is being re-kit
            {
                //Replace profile here
            }

        }
    }
}
