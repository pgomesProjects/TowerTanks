using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace TowerTanks.Scripts
{
    [System.Serializable]
    public class CargoManifest
    {
        [Serializable]
        public class ManifestItem
        {
            [Tooltip("Name of the item")] public string itemID = "";
            [Tooltip("Local spawn vector of the item relative to the parent tank's transform")] public Vector3 localSpawnVector;
            [Tooltip("Value of relevant persistent variable - depends on item. -1 == Use Default Value")] public int persistentValue = -1;
        }

        public List<ManifestItem> items = new List<ManifestItem>();
    }
}
