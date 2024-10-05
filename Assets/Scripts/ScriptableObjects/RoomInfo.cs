using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    [CreateAssetMenu(fileName = "NewRoom", menuName = "ScriptableObjects/Room Information")]
    public class RoomInfo : ScriptableObject
    {
        public new string name;
        public Room roomObject;
        public Sprite sprite;
    }
}
