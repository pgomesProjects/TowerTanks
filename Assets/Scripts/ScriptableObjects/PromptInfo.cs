using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    [CreateAssetMenu(fileName = "New Prompt", menuName = "ScriptableObjects/Prompt Info")]
    public class PromptInfo : ScriptableObject
    {
        public new string name;
        public int spriteID;
        public Sprite promptSprite;
    }
}
