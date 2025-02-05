using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    [System.Serializable]
    public class CargoManager : MonoBehaviour
    {
        public List<CargoId> cargoList = new List<CargoId>();

        private string[] cargoWeights;

        private void Awake()
        {
            cargoWeights = SetupWeights();
        }

        public string[] SetupWeights(bool scarcityBalancing = false)
        { 
            int count = 0;
            int length = 0;
            foreach (CargoId cargo in cargoList) //Get weights from every available chunk in the spawner
            {
                int finalWeight = cargo.weight; //get default weight
                if (scarcityBalancing && cargo.scarcityBalanced) //if we're balancing for scarcity,
                {
                    CargoManifest tempManifest = LevelManager.Instance.playerTank.GetCurrentManifest(); //get temp current tank manifest
                    int itemCount = 0; 
                    foreach(CargoManifest.ManifestItem item in tempManifest.items) //iterate through each item in the temp manifest
                    {
                        if (item.itemID == cargo.id) //if it matches the id of the weight we're trying to get
                        {
                            itemCount++; //add it to the count
                        }
                    }

                    float multiplier = Mathf.Lerp(1f, 0, (itemCount / 4f)); //determine negative multiplier on item weight based on count. Lower count = higher multiplier

                    float calculatedWeight = finalWeight * multiplier; 
                    calculatedWeight = Mathf.Clamp(calculatedWeight, 1, finalWeight); //calculate new weight based on multiplier applied to the previous weight, Min 1, Max Default Weight

                    finalWeight = Mathf.RoundToInt(calculatedWeight);
                }

                length += finalWeight;
            }

            string[] weights = new string[length]; //sets up total weight values

            foreach (CargoId cargo in cargoList) //assigns weights to spawner array
            {
                if (cargo.weight > 0)
                {
                    for (int i = 0; i < cargo.weight; i++)
                    {
                        weights[count] = cargo.cargoPrefab.name;
                        count++;
                    }
                }
            }

            return weights;
        }

        /// <summary>
        /// Retrieves a random cargo item from the list of potential drops.
        /// </summary>
        /// <param name="scarcitybalancing">If true, cargo weights will be reduced by player scarcity values.</param>
        /// <returns>A randomized CargoId & it's information about a cargo item.</returns>
        public CargoId GetRandomCargo(bool scarcitybalancing = false)
        {
            CargoId cargo = null;
            string[] tempWeights = cargoWeights;

            //Determine final weights based on Scarcity
            if (scarcitybalancing == true)
            {
                tempWeights = SetupWeights(true);
            }

            //Roll for a random Item
            int random = Random.Range(0, tempWeights.Length);

            foreach (CargoId weight in cargoList)
            {
                if (weight.cargoPrefab.name == tempWeights[random])
                {
                    cargo = weight;
                }
            }

            return cargo;
        }

        [System.Serializable]
        public class ProjectileId
        {
            public Projectile.ProjectileType type;
            public GameObject[] ammoTypes;
        }

        [SerializeField] public List<ProjectileId> projectileList = new List<ProjectileId>();

        [SerializeField] public Sprite[] ammoSymbols;

        public GameObject GetProjectileByNameHash(string name)
        {
            GameObject projectile = null;

            foreach(ProjectileId id in projectileList)
            {
                foreach(GameObject ammo in id.ammoTypes)
                {
                    if (ammo.name == name) projectile = ammo;
                }
            }

            return projectile;
        }
    }
}
