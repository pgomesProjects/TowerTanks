using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    [CreateAssetMenu(fileName = "New Platform Face Button Settings", menuName = "ScriptableObjects/Platform Face Button Settings")]
    public class PlatformFaceButtonsSettings : ScriptableObject
    {
        public PlatformType platformType;
        public PromptInfo northPrompt;
        public PromptInfo westPrompt;
        public PromptInfo eastPrompt;
        public PromptInfo southPrompt;
    }
}
