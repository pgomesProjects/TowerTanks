using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace TowerTanks.Scripts
{
    [System.Serializable]
    public class ShopManager : MonoBehaviour
    {
        public enum ShopItemType 
        { 
            Stack_Random, 
            Stack_RandomWeapon, 
            Stack_RandomNonWeapon, 
            Stack_RandomDefense,
            Stack_RandomEngineering,
            Stack_RandomLogistics,
            Stack_RandomConsumable,
            Cargo_RandomSetOfThree
        }

        [System.Serializable]
        public class ShopItem
        {
            public ShopItemType type;
            public float cost;
        }

        public ShopItem[] defaultStock; //default shop lineup
        public ShopTerminal[] terminals; //list of active shop terminals on the Shop Tank

        // Start is called before the first frame update
        void Start()
        {
            //InitializeShop();
        }

        // Update is called once per frame
        void Update()
        {
        
        }

        public void InitializeShop()
        {
            terminals = GetComponentsInChildren<ShopTerminal>();

            for(int t = 0; t < defaultStock.Length; t++)
            {
                terminals[t].item = defaultStock[t];
            }
        }
    }
}
