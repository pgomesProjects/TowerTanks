using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    [System.Serializable]
    public struct TutorialItem
    {
        internal bool hasBeenViewedInGame;
        public TutorialPopupSettings tutorialPopup;
    }

    [System.Serializable]
    public class PopupSettings
    {
        public Sprite tutorialImage;
        public string tutorialText;
    }

    [CreateAssetMenu(fileName = "New Tutorial Popup", menuName = "ScriptableObjects/Tutorial Popup")]
    public class TutorialPopupSettings : ScriptableObject
    {
        public PopupSettings[] tutorialPages;
    }
}
