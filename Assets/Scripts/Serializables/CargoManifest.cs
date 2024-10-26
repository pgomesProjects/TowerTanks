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
        }

        public List<ManifestItem> items = new List<ManifestItem>();
    }
}
