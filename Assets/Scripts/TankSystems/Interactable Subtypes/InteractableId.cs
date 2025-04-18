using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.Serialization;

namespace TowerTanks.Scripts
{
    [Tooltip("Tank Interactable identifier used during Runtime")]
    [System.Serializable]
    public class InteractableId
    {
        public string stackName;
        public GameObject interactable;
        public TankInteractable script;
        public InteractableBrain brain;
        public TankInteractable.InteractableType groupType;

        //RUNTIME METHODS:
        [HorizontalGroup("Horizontal Buttons")]
        [VerticalGroup("Horizontal Buttons/Column 1")]
        [Button("  Use", ButtonSizes.Small, Icon = SdfIconType.Square), Tooltip("Use Interactable's primary function")]
        public void Use()
        {
            script.Use(true);
        }
        [VerticalGroup("Horizontal Buttons/Column 1")]
        [Button("  Break", ButtonSizes.Small, Icon = SdfIconType.Hammer), Tooltip("Break this interactable.")]
        public void Break()
        {
            script.Break();
        }
        [VerticalGroup("Horizontal Buttons/Column 2")]
        [Button("  Secondary", ButtonSizes.Small, Icon = SdfIconType.Triangle), Tooltip("Uses Interactable's secondary function")]
        public void Secondary()
        {
            script.SecondaryUse(false);
        }
        [VerticalGroup("Horizontal Buttons/Column 2")]
        [Button("  Destroy", ButtonSizes.Small, Icon = SdfIconType.EmojiDizzy), Tooltip("Destroy the Interactable")]
        public void Destroy()
        {
            script.DebugDestroy();
        }
        
    }
}
