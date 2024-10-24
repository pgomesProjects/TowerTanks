using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class Cargo : MonoBehaviour
    {
        public string cargoID = ""; //default identifier for this item
        public enum CargoType { SCRAP, AMMO, EXPLOSIVE, TOOL }
        public CargoType type;

        public GameObject[] contents; //What objects can be inside this?
        public int amount; //how many are in it?

        internal PlayerMovement currentHolder;
        private Rigidbody2D rb;
        private BoxCollider2D box2D;
        private CircleCollider2D circle2D;
        private CapsuleCollider2D capsule2D;
        public bool ignoreInit; //set to true if we want to just have this object instantiate normally
        private float initCooldown; //time it takes for collider to enable

        private TrailRenderer trail;
        private float trailCooldown; //time it takes for trail to disable

        public bool isOnTank; //whether or not this object is considered "on" the tank
        public LayerMask onTankMask;

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
                transform.rotation = new Quaternion(0, 0, 0, 0);
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

        public void Pickup(PlayerMovement player)
        {
            if (currentHolder == null)
            {
                currentHolder = player;
                player.isCarryingSomething = true;
                player.currentObject = this;

                GameManager.Instance.AudioManager.Play("UseSFX");
            }
        }

        public void Drop(PlayerMovement player, bool throwing, Vector2 direction)
        {
            rb.isKinematic = false;
            if (box2D != null) box2D.enabled = true;
            if (circle2D != null) circle2D.enabled = true;
            if (capsule2D != null) capsule2D.enabled = true;

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

        public void Use(bool held = false) //called from Holder when pressing Alt
        {
            if (type == CargoType.EXPLOSIVE)
            {
                Cargo_Explosive script = GetComponent<Cargo_Explosive>();

                if (script.isLit == false) GameManager.Instance.AudioManager.Play("UseSFX", gameObject);
                script.isLit = true;
            }

            if (type == CargoType.AMMO)
            {
                if (currentHolder.currentZone != null)
                {
                    GunController interactable = currentHolder.currentZone.GetComponentInParent<GunController>();
                    if (interactable.interactableType == TankInteractable.InteractableType.WEAPONS)
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
                    else
                    {
                        if (sprayer.isSpraying)
                        {
                            Debug.Log("Stopped using " + this.name);
                            sprayer.isSpraying = false;
                        }
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
            }
            else
            {
                transform.parent = null;
                isOnTank = false;
            }
        }

        public void Sell(float percentage) //percentage = multiplier 1f = 1x sale, 0.5f = half-price, etc
        {
            //Apply Percentage Multiplier
            int saleAmount = Mathf.RoundToInt(amount * percentage);

            //Add to Total Resources
            LevelManager.Instance.UpdateResources(saleAmount);

            //Other Effects
            GameManager.Instance.AudioManager.Play("UseWrench", gameObject);
            GameManager.Instance.AudioManager.Play("ItemPickup", gameObject);
            GameManager.Instance.ParticleSpawner.SpawnParticle(6, transform.position, 0.2f, null);
            GameManager.Instance.ParticleSpawner.SpawnParticle(7, transform.position, 0.2f, null);

            Destroy(gameObject);
        }

        protected virtual void OnDestroy()
        {
            if (currentHolder != null)
            {
                currentHolder.isCarryingSomething = false;
                currentHolder.currentObject = null;
            }
        }
    }
}
