using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace TowerTanks.Scripts
{
    [Tooltip("Controller for elements which players (and enemies) interact with to control and use the tank.")]
    public class TankInteractable : MonoBehaviour
    {
        //Objects & Components:
        private SpriteRenderer[] renderers;    //Array of all renderers in interactable
        public TankController tank; //Controller script for tank this interactable is attached to
        private InteractableZone interactZone; //Hitbox for player detection
        public Transform seat; //Transform operator snaps to while using this interactable
        public enum InteractableType { WEAPONS, ENGINEERING, DEFENSE, LOGISTICS, CONSUMABLE, SHOP };
        public InteractableType interactableType;

        //Interactable Scripts
        private GunController gunScript;
        private EngineController engineScript;
        private ThrottleController throttleScript;
        private TankConsumable consumableScript;
        [HideInInspector]
        public Collider2D thisCollider;

        //Settings:
        [Header("Stack Properties:")]
        [Tooltip("Display name for interactable while in stack.")]      public string stackName;
        [Tooltip("Reference to this interactable's prefab.")]           public GameObject prefabRef;
        [Tooltip("Image used to represent this interactable in UI.")]   public Sprite uiImage;
        [Tooltip("Ghost object used when building this interactable.")] public GameObject ghostPrefab;

        //ADD SPATIAL CONSTRAINT SYSTEM
        [Button("Debug Place")] public void DebugPlace()
        {
            Collider2D targetColl = Physics2D.OverlapArea(transform.position + new Vector3(-0.1f, 0.1f), transform.position + new Vector3(0.1f, -0.1f), LayerMask.GetMask("Cell")); //Try to get cell collider interactable is on top of
            if (targetColl == null || !targetColl.TryGetComponent(out Cell cell)) { Debug.LogWarning("Could not find cell."); return; }                                             //Cancel if interactable is not on a cell
            InstallInCell(targetColl.GetComponent<Cell>());                                                                                                                         //Install interactable in target cell
        }
        [Button("Debug Destroy")] public void DebugDestroy()
        {
            Destroy(gameObject); //Destroy interactable
        }

        //Runtime Variables:
        [Tooltip("The cell this interactable is currently installed within.")]                                      internal Cell parentCell;
        [Tooltip("True if interactable is a ghost and is currently unuseable.")]                                    internal bool ghosted;
        [Tooltip("True if a user is currently operating this system")]                                              internal bool hasOperator;
        [Tooltip("User currently interacting with this system.")]                                                   internal PlayerMovement operatorID;
        [Header("General Settings:")]
        [Tooltip("Whether or not interact can be held down to use this interactable continuously"), SerializeField] public bool isContinuous;
        [Tooltip("Whether or not this interactable can be aimed in some way"), SerializeField]                      public bool canAim;
        [Tooltip("Direction this interactable is facing. (1 = right; -1 = left)")]                                  public float direction = 1;
        [Tooltip("Unique identifier associating this interactable with a stack item")]                              internal int stackId = 0;
        [Tooltip("Whether this Interactable is currently broken or not.")]                                          public bool isBroken = false;
        [Tooltip("Installs this interactable in current structure on Start().")]                                    public bool installOnStart = false;

        [Header("Visual Settings:")]
        public Animator overlayAnimator;
        private SpriteOverlay overlay;
        public GameObject particleOverlay;

        //Debug
        internal bool debugMoveUp;
        internal bool debugMoveDown;
        internal bool debugMoveLeft;
        internal bool debugMoveRight;
        public bool debugFlip = false;
        private float introBuffer = 0.2f; //small window when a new operator enters the interactable where they can't use it
        protected float cooldown;

        //RUNTIME METHODS:
        protected virtual void Awake()
        {
            //Get objects & components:
            tank = GetComponentInParent<TankController>();
            thisCollider = GetComponentInChildren<Collider2D>();
            renderers = GetComponentsInChildren<SpriteRenderer>(); //Get all spriterenderers for interactable visual
            interactZone = GetComponentInChildren<InteractableZone>();
            seat = transform.Find("Seat");
            gunScript = GetComponent<GunController>();
            engineScript = GetComponent<EngineController>();
            throttleScript = GetComponent<ThrottleController>();
            consumableScript = GetComponent<TankConsumable>();
            
            overlay = transform.Find("Visuals")?.GetComponent<SpriteOverlay>();
            
            if (overlay != null) overlay.enabled = false;
            if (overlayAnimator != null) overlayAnimator.enabled = false;
        }

        public void Start()
        {
            if (installOnStart) DebugPlace();
        }

        public virtual void OnDestroy()
        {
            if (hasOperator)
            {
                Exit(false);
            }

            //Destruction Cleanup:
            if (parentCell != null) //Interactable is mounted in a cell
            {
                parentCell.interactable = null; //Clear cell reference to this interactable
            }

            //Id Update
            if (tank != null)
            {
                foreach (InteractableId id in tank.interactableList)
                {
                    if (id.interactable == this.gameObject)
                    {
                        tank.interactableList.Remove(id);
                        break;
                    }
                }
            }
        }

        private void OnDisable()
        {
            if (hasOperator)
            {
                Exit(false);
            }
        }

        protected virtual void FixedUpdate()
        {
            if (operatorID != null)
            {
                operatorID.gameObject.transform.position = seat.position;
                //operatorID.gameObject.transform.rotation = seat.rotation;
            }

            if (cooldown > 0)
            {
                cooldown -= Time.fixedDeltaTime;
            }

            if (debugFlip) { debugFlip = false; Flip(); }
            if (debugMoveUp) { debugMoveUp = false; SnapMoveTick(Vector2.up); }
            if (debugMoveDown) { debugMoveDown = false; SnapMoveTick(Vector2.down); }
            if (debugMoveLeft) { debugMoveLeft = false; SnapMoveTick(Vector2.left); }
            if (debugMoveRight) { debugMoveRight = false; SnapMoveTick(Vector2.right); }
        }

        public virtual void LockIn(GameObject playerID) //Called from InteractableZone.cs when a user locks in to the interactable
        {
            hasOperator = true;
            operatorID = playerID.GetComponent<PlayerMovement>();

            if (operatorID != null)
            {
                operatorID.currentInteractable = this;
                operatorID.isOperator = true;
                operatorID.gameObject.GetComponent<Rigidbody2D>().isKinematic = true;
                if (operatorID.currentObject != null) operatorID.currentObject.Drop(operatorID, false, Vector2.zero);

                Debug.Log(operatorID + " is in!");
                GameManager.Instance.AudioManager.Play("UseSFX");

                if (cooldown <= 0) cooldown = introBuffer;
            }

            //Show that the player can cancel to leave
            operatorID.GetCharacterHUD()?.SetButtonPrompt(GameAction.Cancel, true);
        }

        public virtual void Exit(bool sameZone) //Called from operator (PlayerMovement.cs) when they press Cancel
        {
            if (operatorID != null)
            {
                operatorID.currentInteractable = null;
                operatorID.isOperator = false;
                operatorID.gameObject.GetComponent<Rigidbody2D>().isKinematic = false;
                operatorID.CancelInteraction(startJump:true);
                
                if(GameManager.Instance.currentSceneState == SCENESTATE.BuildScene)
                    operatorID.GetPlayerData().undoActionAvailable = false;

                if (!interactZone.players.Contains(operatorID.gameObject) && sameZone)
                {
                    interactZone.players.Add(operatorID.gameObject); //reassign operator to possible interactable players
                    operatorID.currentZone = interactZone;
                }

                hasOperator = false;
                Debug.Log(operatorID + " is out!");

                //Remove the cancel option
                operatorID.GetCharacterHUD()?.SetButtonPrompt(GameAction.Cancel, false);

                operatorID = null;

                GameManager.Instance.AudioManager.Play("ButtonCancel");
            }
        }

        public virtual void Use(bool overrideConditions = false) //Called from operator when they press Interact
        {
            if (isBroken) return;
            //Debug.Log("Interact Started");
        }

        public virtual void CancelUse() //Called from operator when they release Interact
        {
            if (isBroken) return;
            if (gunScript != null && gunScript.gunType == GunController.GunType.MORTAR && cooldown <= 0)
            {
                gunScript.Fire(false, tank.tankType);
            }
        }

        public void Shift(int direction) //Called from operator when they flick L-Stick L/R
        {
            if (isBroken) return;
            if (throttleScript != null && cooldown <= 0)
            {
                throttleScript.UseThrottle(direction);
                cooldown = 0.1f;
            }
        }

        public virtual void Rotate(float force) //Called from operator when they rotate the joystick
        {
            if (isBroken) return;
            if (gunScript != null && cooldown <= 0) gunScript.RotateBarrel(force, true);
        }

        public void SecondaryUse(bool held)
        {
            if (engineScript != null) engineScript.repairInputHeld = held;
        }

        //UTILITY METHODS:
        /// <summary>
        /// Installs interactable into target cell.
        /// </summary>
        /// <param name="target">The cell interactable will be installed in.</param>
        public bool InstallInCell(Cell target)
        {
            //Universal installation:
            parentCell = target;                                                                                     //Get reference to target cell
            transform.parent = parentCell.transform;                                                                 //Child to target cell
            transform.localPosition = Vector3.zero;                                                                  //Match position with target cell
            transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, transform.localEulerAngles.y, 0); //Match rotation with target cell

            //Cell installation:
            target.interactable = this; //Give cell reference to the interactable installed in it
            if (consumableScript != null) consumableScript.ConvertRoom(target);

            //Cleanup:
            tank = GetComponentInParent<TankController>(); //Get tank controller interactable is being attached to
            if (tank != null) { tank.AddInteractableId(this.gameObject); }
            return true;                                   //Indicate that interactable was successfully installed in target cell
        }

        public void SnapMoveTick(Vector2 direction)
        {
            //Get target position:
            direction = direction.normalized;                                           //Make sure direction is normalized
            Vector2 targetPos = (Vector2)transform.localPosition + (direction * 0.25f); //Get target position based off of current position
            SnapMove(targetPos);                                                        //Use normal snapMove method to place room
        }

        public void SnapMove(Vector2 targetPoint)
        {
            //Validity checks:
            if (tank != null) //Interactable is already mounted
            {
                Debug.LogError("Tried to move interactable while it is mounted!"); //Log error
                return;                                                    //Cancel move
            }

            //Constrain to grid:
            Vector2 newPoint = targetPoint * 4;                                       //Multiply position by four so that it can be rounded to nearest quarter unit
            newPoint = new Vector2(Mathf.Round(newPoint.x), Mathf.Round(newPoint.y)); //Round position to nearest unit
            newPoint /= 4;                                                            //Divide result after rounding to get actual value
            transform.localPosition = newPoint;                                       //Apply new position
                                                                                      //transform.localEulerAngles = Vector3.zero;                                //Zero out rotation relative to parent tank
        }

        public void Flip()
        {
            if (direction == 1)
            {
                transform.Rotate(new Vector3(0, 180, 0));
                direction = -1;
            }
            else
            {
                transform.Rotate(new Vector3(0, -180, 0));
                direction = 1;
            }
        }

        public string[] GetSpecialAmmoRef()
        {
            string[] specialAmmoRef = null;

            if (gunScript != null)
            {
                if (gunScript.specialAmmo.Count > 0) 
                {
                    specialAmmoRef = new string[gunScript.specialAmmo.Count];
                    for (int i = 0; i < gunScript.specialAmmo.Count; i++)
                    {
                        specialAmmoRef[i] = gunScript.specialAmmo[i].name;
                    }
                }
            }

            return specialAmmoRef;
        }

        [Button("Break")]
        public virtual void Break()
        {
            if (!isBroken)
            {
                isBroken = true;
                if (overlay == null) return;
                overlay.enabled = true;
                overlayAnimator.enabled = true;

                GameManager.Instance.ParticleSpawner.SpawnParticle(28, transform.position, 0.4f, this.transform);
                particleOverlay?.SetActive(true);
            }
        }

        [Button("Fix")]
        public virtual void Fix()
        {
            if (isBroken)
            {
                isBroken = false;
                if (overlay == null) return;
                overlay.ResetOverlay();
                overlay.enabled = false;
                overlayAnimator.enabled = false;

                particleOverlay?.SetActive(false);
            }
        }
    }
}
