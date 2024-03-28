using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[System.Serializable]
public class BuildStep
{
    [SerializeField, Tooltip("Name of the room to build during this step")] public string roomID = "";
    [SerializeField, Tooltip("What type of room to build")] public Room.RoomType roomType;
    [SerializeField, Tooltip("Where to spawn this room inside the tank's parent transform")] public Vector3 localSpawnVector;
    [SerializeField, Tooltip("How many times to rotate the room clockwise before placing")] public int rotate = 0;
}
