using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public enum ActionType
    {
        Press,
        Hold,
        Rotate
    }

    [CreateAssetMenu(fileName = "New Prompt", menuName = "ScriptableObjects/Prompt Info")]
    public class PromptInfo : ScriptableObject
    {
        public new string name;
        public ActionType actionType;
        public int spriteID;
        public Sprite promptSprite;
    }
}
