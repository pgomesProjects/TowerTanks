using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class StructureController : MonoBehaviour, IStructure
    {
        public IStructure.StructureType type = IStructure.StructureType.BUILDING;

        //Components
        public Transform towerJoint;
        public List<Room> rooms = new List<Room>();
        public List<Coupler> hatches = new List<Coupler>();
        private Vector2[] cargoNodes;
        private List<InteractableId> interactableList = new List<InteractableId>();
        public RoomAssetKit defaultRoomKit;

        //Settings
        [Header("Structure Settings:")]
        public bool isInvincible;

        void Awake()
        {
            //Get objects & components:
            //_thisTankAI = GetComponent<TankAI>();
            towerJoint = transform.Find("TowerJoint");           //Get tower joint from children

            bool isPrebuilding = true;
            //Room setup:
            rooms = new List<Room>(GetComponentsInChildren<Room>()); //Get list of all rooms which spawn as children of structure (for prefab structures)

            foreach (Room room in rooms) //Scrub through childed room list (should be in order of appearance under towerjoint)
            {
                room.targetStructure = this;
                room.targetTank = null;
                room.Initialize();      //Prepare room for mounting
                if (room.isCore) //Found a core room
                {
                    //TODO: Track Core Rooms if there are multiple
                }
                else //Room has been added to tank for pre-mounting
                {
                    if (room.ignoreMount)
                    {
                        room.targetStructure = null;
                        continue;
                    }

                    //Pre-mount room:
                    room.UpdateRoomType(room.type);              //Apply preset room type
                    room.SnapMove(room.transform.localPosition); //Snap room to position on tank grid
                    room.Mount();                                //Mount room to tank immediately
                }
            }
        }

        private void Start()
        {
            TankInteractable[] interactables = GetComponentsInChildren<TankInteractable>();
            foreach(TankInteractable interactable in interactables)
            {
                if (interactable.installOnStart) interactable.DebugPlace();
            }
        }

        //INTERFACE METHODS:
        #region IStructure
        public IStructure.StructureType GetStructureType() => type; //Get Type -> Tank, Building

        public Transform GetTowerJoint() => towerJoint; //Get Foundation/Core -> object that is going to parent all of the childed rooms & cells

        public Vector2[] GetCargoNodes() => cargoNodes; //Get Cargo Nodes -> Get list of nodes where random items can spawn

        /// <summary>
        /// Gets the current layout of spawned items in the structure.
        /// </summary>
        /// <param name="nodeManifest">Whether this manifest is exclusively looking at existing CargoNodes or not.</param>
        /// <returns>Current manifest of items in the tank.</returns>
        public CargoManifest GetCurrentManifest(bool nodeManifest = false)
        {
            if (LevelManager.Instance == null) return null;
            CargoManifest manifest = new CargoManifest();

            if (!nodeManifest)
            {
                //Find All Cargo Items currently inside the Tank
                Cargo[] cargoItems = GetTowerJoint().GetComponentsInChildren<Cargo>();

                if (cargoItems.Length > 0)
                {
                    foreach (Cargo item in cargoItems) //go through each item in the tank
                    {
                        string itemID = "";
                        foreach (CargoId _cargo in GameManager.Instance.CargoManager.cargoList)
                        {
                            if (_cargo.id == item.cargoID) //if it's on the list of valid cargo items
                            {
                                itemID = item.cargoID;
                            }
                        }

                        if (itemID != "")
                        {
                            CargoManifest.ManifestItem _item = new CargoManifest.ManifestItem();
                            _item.itemID = itemID; //update id

                            Transform temp = item.transform.parent;
                            item.transform.parent = GetTowerJoint().transform; //set new temp parent

                            //Get Item Information
                            _item.localSpawnVector = item.transform.localPosition; //Get it's current localPosition
                            _item.persistentValue = item.GetPersistentValue();

                            item.transform.parent = temp;
                            manifest.items.Add(_item); //add it to the manifest
                        }
                    }
                }
            }
            else
            {
                Vector2[] nodes = GetCargoNodes();
                if (nodes?.Length > 0)
                {
                    foreach (Vector2 node in nodes)
                    {
                        CargoManifest.ManifestItem _item = new CargoManifest.ManifestItem();
                        CargoId random = GameManager.Instance.CargoManager.GetRandomCargo(true);
                        _item.itemID = random.id;
                        _item.localSpawnVector.x = node.x;
                        _item.localSpawnVector.y = node.y;

                        manifest.items.Add(_item);
                    }
                }
            }

            return manifest;
        } //Get Cargo Manifest -> Get Manifest of assigned cargo items & nodes that'll spawn inside the structure

        public void SpawnCargo(CargoManifest manifest) //Called when spawning a tank that contains cargo/items
        {
            if (GameManager.Instance.cargoManifest?.items.Count > 0) //if we have any cargo
            {
                foreach (CargoManifest.ManifestItem item in manifest.items) //go through each item in the manifest
                {
                    GameObject prefab = null;
                    foreach (CargoId cargoItem in GameManager.Instance.CargoManager.cargoList)
                    {
                        if (cargoItem.id == item.itemID) //if the item id matches an object in the cargomanager
                        {
                            prefab = cargoItem.cargoPrefab; //get the object we need to spawn from the list
                        }
                    }

                    GameObject _item = SpawnItem(prefab);
                    Cargo script = _item.GetComponent<Cargo>();
                    script.ignoreInit = true;

                    //Assign Values
                    Vector3 spawnVector = item.localSpawnVector;
                    _item.transform.localPosition = spawnVector;
                    script.AssignValue(item.persistentValue);
                }
            }
        }

        public GameObject SpawnItem(GameObject item)
        {
            //Instantiate(prefab, GetTowerJoint(), false);
            return null;
        } //Spawns an item inside the Structure

        public void UnassignCharactersFromStructure(bool lethal = false) //Unassign Characters -> Unassigns/Kills characters inside this structure when this explodes/despawns
        {

        }

        //TODO: Spawn Interactables in cells inside the structure

        public List<InteractableId> GetInteractableList() => interactableList;

        public void AddInteractableId(GameObject interactable)
        {
            InteractableId newId = new InteractableId();
            newId.interactable = interactable;
            newId.script = interactable.GetComponent<TankInteractable>();
            newId.groupType = newId.script.interactableType;

            newId.stackName = newId.script.stackName;
            interactableList.Add(newId);
        } //Add Interactable to Structure's List of Interactables

        public void LoadRandomWeapons(int weaponCount)
        {
            List<InteractableId> weaponPool = new List<InteractableId>();

            //Get # of Weapons
            foreach (InteractableId id in GetInteractableList())
            {
                if (id.groupType == TankInteractable.InteractableType.WEAPONS) { weaponPool.Add(id); }
            }

            for (int w = 0; w < weaponCount; w++)
            {
                int random = Random.Range(0, weaponPool.Count); //Pick a Random Weapon from the Pool
                GunController gun = weaponPool[random].interactable.GetComponent<GunController>();

                GameObject ammo = null;
                int amount = 3;

                switch (gun.gunType) //Determine Ammo Type & Quantity
                {
                    case GunController.GunType.MACHINEGUN:
                        ammo = GameManager.Instance.CargoManager.projectileList[0].ammoTypes[1];
                        amount *= 20;
                        break;

                    case GunController.GunType.CANNON:
                        ammo = GameManager.Instance.CargoManager.projectileList[1].ammoTypes[1];
                        break;

                    case GunController.GunType.MORTAR:
                        ammo = GameManager.Instance.CargoManager.projectileList[1].ammoTypes[3];
                        break;
                }

                if (ammo != null) gun.AddSpecialAmmo(ammo, amount); //Load the Gun
            }
        } ////Load Random Weapons with S. Ammo

        public void Surrender()
        {
            return;
        } //Surrender -> Throw up White Flag

        //TODO: Set Group Ai for Characters

        public void SetStructureName(string name) //Assign Structure Name
        {
            return;
        }

        public void UpdateSizeValues(bool flagUpdate = false)
        {

        } //UpdateSizeValues -> gets bounds of structure

        public void GenerateCorpse()
        {
            return;
        }

        public RoomAssetKit GetRoomAssetKit() => defaultRoomKit;

        public List<Room> GetRooms() => rooms;

        public List<Coupler> GetHatches() => hatches;

        public bool GetIsInvincible() => isInvincible;
        #endregion
    }
}
