using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class TitlescreenAnimationEvents : MonoBehaviour
    {
        [SerializeField, Tooltip("The titlescreen controller")] private TitlescreenController titlescreenController;

        public void LoadLevel() => titlescreenController?.LoadNextScene();
    }
}
