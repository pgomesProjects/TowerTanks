using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class Cargo : MonoBehaviour, ISuckable
    {
        public string cargoID = ""; //default identifier for this item
        public enum CargoType { SCRAP, AMMO, EXPLOSIVE, TOOL }
        public CargoType type;

        public GameObject[] contents; //What objects can be inside this?
        public int amount; //how many are in it?

        public InventoryItem inventoryItem;
        internal PlayerMovement currentHolder;
        private Rigidbody2D rb;
        private BoxCollider2D box2D;
        private CircleCollider2D circle2D;
        private CapsuleCollider2D capsule2D;
        public bool ignoreInit; //set to true if we want to just have this object instantiate normally
        private float initCooldown; //time it takes for collider to enable
        internal float cooldown = 0.3f; //time it takes after pickup before this can be used
        public float holdRotationOffset = 0; //rotate this object by Z degrees when held by a character

        private TrailRenderer trail;
        private float trailCooldown; //time it takes for trail to disable

        public bool isOnTank; //whether or not this object is considered "on" the tank
        public LayerMask onTankMask;
        internal Transform tankTransform; //transform of the tank we're currently on

        public float throwForce;

        public bool isContinuous; //Whether or not the Holder can hold down the interact button to use this continuously

        protected virtual void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            switch (type)
            {
                case CargoType.SCRAP:
                    {
                        box2D = GetComponent<BoxCollider2D>();
                        box2D.enabled = false;
                    }
                    break;
                case CargoType.AMMO:
                    {
                        box2D = GetComponent<BoxCollider2D>();
                        box2D.enabled = false;
                    }
                    break;
                case CargoType.EXPLOSIVE:
                    {
                        circle2D = GetComponent<CircleCollider2D>();
                        circle2D.enabled = false;
                    }
                    break;
                case CargoType.TOOL:
                    {
                        capsule2D = GetComponent<CapsuleCollider2D>();
                        capsule2D.enabled = false;
                    }
                    break;
            }

            initCooldown = 0.5f;
            trail = GetComponentInChildren<TrailRenderer>();
            trailCooldown = 4f;
        }

        // Start is called before the first frame update
        protected virtual void Start()
        {
            currentHolder = null;

            if (ignoreInit)
            {
                initCooldown = 0f;
                trailCooldown = 0f;
            }

            StartCoroutine(Initialize());
            StartCoroutine(InitializeTrail());
        }

        // Update is called once per frame
        protected virtual void Update()
        {
            if (currentHolder != null)
            {
                if (rb.isKinematic == false) rb.isKinematic = true;
                if (box2D != null && box2D.enabled == true) box2D.enabled = false;
                if (circle2D != null && circle2D.enabled == true) circle2D.enabled = false;
                if (capsule2D != null && capsule2D.enabled == true) capsule2D.enabled = false;

                float yRot = currentHolder.GetCharacterDirection();
                if (yRot == 1) yRot = 0;
                if (yRot == -1) yRot = -180;

                Vector3 rot = new Vector3(0, yRot, holdRotationOffset);
                Quaternion euler = Quaternion.Euler(rot);
                transform.rotation = euler;
            }

            CheckForOnboard();
        }

        private IEnumerator Initialize()
        {
            yield return new WaitForSeconds(initCooldown);
            if (box2D != null && box2D.enabled == false) box2D.enabled = true;
            if (circle2D != null && circle2D.enabled == false) circle2D.enabled = true;
            if (capsule2D != null && capsule2D.enabled == false) capsule2D.enabled = true;
        }

        private IEnumerator InitializeTrail()
        {
            yield return new WaitForSeconds(trailCooldown);
            if (trail.enabled == true) trail.enabled = false;
        }

        public virtual void Pickup(PlayerMovement player)
        {
            if (currentHolder == null)
            {
                rb.velocity = new Vector2(0, 0);
                rb.angularVelocity = 0;
                currentHolder = player;
                player.isCarryingSomething = true;
                player.currentObject = this;
                cooldown = 0.3f;

                //Add the item to the inventory
                player.GetCharacterHUD()?.InventoryHUD.AddToInventory(inventoryItem);
                //Add the item to the player tracker
                player.GetPlayerData().playerAnalyticsTracker.StartInteraction(inventoryItem.name);

                GameManager.Instance.AudioManager.Play("UseSFX");
            }
        }

        public virtual void Drop(PlayerMovement player, bool throwing, Vector2 direction)
        {
            transform.position = player.transform.position;
            rb.isKinematic = false;
            if (box2D != null) box2D.enabled = true;
            if (circle2D != null) circle2D.enabled = true;
            if (capsule2D != null) capsule2D.enabled = true;

            //Clear the item from the inventory
            player.GetCharacterHUD()?.InventoryHUD.ClearInventory();
            //Remove the interactable from the player tracker
            player.GetPlayerData().playerAnalyticsTracker.EndInteraction(inventoryItem.name);

            if (throwing)
            {
                rb.AddForce(direction * throwForce);
                rb.AddTorque(10f);
            }

            currentHolder = null;
            player.isCarryingSomething = false;
            player.currentObject = null;

            GameManager.Instance.AudioManager.Play("ButtonCancel");
        }

        public virtual void Use(bool held = false) //called from Holder when pressing Interact
        {
            if (type == CargoType.EXPLOSIVE)
            {
                Cargo_Explosive script = GetComponent<Cargo_Explosive>();

                if (!held)
                {
                    if (script.isLit == false) GameManager.Instance.AudioManager.Play("UseSFX", gameObject);
                    script.isLit = true;
                }
            }

            if (type == CargoType.AMMO)
            {
                if (!held)
                {
                    if (currentHolder.currentZone != null)
                    {
                        GunController interactable = currentHolder.currentZone.GetComponentInParent<GunController>();
                        if (interactable?.interactableType == TankInteractable.InteractableType.WEAPONS)
                        {
                            if (interactable.gunType == GunController.GunType.CANNON)
                            {
                                interactable.AddSpecialAmmo(contents[0], amount);
                                Destroy(this.gameObject);

                            }

                            if (interactable.gunType == GunController.GunType.MORTAR)
                            {
                                interactable.AddSpecialAmmo(contents[1], amount);
                                Destroy(this.gameObject);
                            }

                            if (interactable.gunType == GunController.GunType.MACHINEGUN)
                            {
                                amount *= 20;
                                interactable.AddSpecialAmmo(contents[2], amount);
                                Destroy(this.gameObject);
                            }
                        }
                    }
                }
            }

            if (type == CargoType.TOOL)
            {
                if (isContinuous)
                {
                    CargoSprayer sprayer = GetComponent<CargoSprayer>();
                    if (held)
                    {
                        Debug.Log("Using " + this.name + "!");
                        sprayer.isSpraying = true;
                    }
                }
            }
        }

        public void CancelUse()
        {
            if (type == CargoType.TOOL)
            {
                if (isContinuous)
                {
                    CargoSprayer sprayer = GetComponent<CargoSprayer>();
                    
                    if (sprayer.isSpraying)
                    {
                        Debug.Log("Stopped using " + this.name);
                        sprayer.isSpraying = false;
                        sprayer.CancelUse();
                    }
                }
            }
        }

        public void CheckForOnboard()
        {
            var cellCheck = Physics2D.OverlapBox(transform.position, new Vector2(0.5f, 0.5f), 0, onTankMask);
            if (cellCheck != null)
            {
                Transform newParent = cellCheck.transform.parent;
                transform.parent = newParent;
                isOnTank = true;
                TankController tank = newParent.gameObject.GetComponentInParent<TankController>();
                if (tank != null) tankTransform = tank.treadSystem.transform;
            }
            else
            {
                transform.parent = null;
                isOnTank = false;
                tankTransform = null;
            }
        }

        public void Sell(float percentage) //percentage = multiplier 1f = 1x sale, 0.5f = half-price, etc
        {
            if (LevelManager.Instance != null)
            {
                //Apply Percentage Multiplier
                int saleAmount = Mathf.RoundToInt(amount * percentage);

                if (type == CargoType.AMMO) { saleAmount = 25; }

                //Add to Total Resources
                LevelManager.Instance.UpdateResources(saleAmount);
            }

            //Other Effects
            GameManager.Instance.AudioManager.Play("UseWrench", gameObject);
            GameManager.Instance.AudioManager.Play("ItemPickup", gameObject);
            GameManager.Instance.ParticleSpawner.SpawnParticle(6, transform.position, 0.2f, null);
            GameManager.Instance.ParticleSpawner.SpawnParticle(7, transform.position, 0.2f, null);

            Destroy(gameObject);
        }

        public virtual void AssignValue(int value)
        {
            if (value == -1) return;
        }

        public virtual int GetPersistentValue()
        {
            int value = -1;

            return value;
        }

        protected virtual void OnDestroy()
        {
            if (currentHolder != null)
            {
                currentHolder.isCarryingSomething = false;
                currentHolder.currentObject = null;
            }
        }

        //INTERFACE METHODS:
        public void Suck(Vector2 force, Vector2 suctionPoint)
        {

        }

        public Rigidbody2D GetRigidbody2D() => rb;
    }
}
