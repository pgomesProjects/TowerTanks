using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Character : SerializedMonoBehaviour
{
    #region Fields and Properties

    public enum CharacterState { CLIMBING, NONCLIMBING, OPERATING, REPAIRING }; //Simple state system, in the future this will probably be refactored

    public CharacterState currentState;                  //to an FSM.

    //Components
    protected Rigidbody2D rb;
    protected CapsuleCollider2D characterHitbox;
    protected GameObject currentLadder;
    protected Bounds ladderBounds;
    protected PlayerHUD characterHUD;
    protected int characterIndex;
    protected Transform hands;
    protected bool isAlive;
    protected bool isDead;
    protected Transform characterVisualParent;
    protected Color characterColor = new Color(1, 1, 1, 1);

    [Header("Character Information")]
    [SerializeField] protected CharacterSettings characterSettings;
    protected float taskProgress;
    protected float currentHealth;

    [Header("Movement")]
    [SerializeField] protected float moveSpeed;
    [SerializeField] protected float jumpForce;
    [SerializeField] protected float extraGravityForce;
    [SerializeField] protected float climbSpeed;
    [Range(0, 2)]
    [SerializeField] protected float groundedBoxX, groundedBoxY;

    [SerializeField] protected float groundedBoxOffset;
    
    [SerializeField] protected float maxYVelocity, minYVelocity;

    [Header("Jetpack values")]
    [SerializeField] protected float fuelDepletionRate;
    [SerializeField] protected float fuelRegenerationRate;
    protected float currentFuel;

    [SerializeField] private float characterDeathParticleSize = 0.03f;
    [SerializeField] protected float respawnTime = 3f;
    private float currentRespawnTime;
    private bool isRespawning;
    
    //internal movement
    private Transform currentCellJoint;
    private int cellLayerIndex = 15;
    
    protected LayerMask ladderLayer;

    private TankController assignedTank;

    //temp
    protected float moveSpeedHalved; // once we have a state machine for the player, we wont need these silly fields.
    protected float currentMoveSpeed; // this is fine for the sake of prototyping though.

    [Button(ButtonSizes.Medium)]
    private void DebugModifyPlayerHealth()
    {
        ModifyHealth(healthToModify);
    }

    [Button(ButtonSizes.Medium)]
    private void DebugKillPlayer()
    {
        KillCharacterImmediate();
    }
    public int healthToModify = -10;

    #endregion

    #region Unity Methods
    protected virtual void Awake()
    {
        ladderLayer = 1 << LayerMask.NameToLayer("Ladder");
        rb = GetComponent<Rigidbody2D>();
        characterHitbox = GetComponent<CapsuleCollider2D>();
        currentHealth = characterSettings.maxHealth;
        characterVisualParent = transform.GetChild(0);
        hands = characterVisualParent.Find("Hands");
    }

    protected virtual void Start()
    {
        ResetPlayer();
        isAlive = true;
        isDead = false;
    }

    protected virtual void Update()
    {

        if (!isAlive)
        {
            if (isRespawning)
                RespawnTimer();

            return;
        }

        currentFuel = Mathf.Clamp(currentFuel, 0, characterSettings.fuelAmount);
        
        var cellJoint = Physics2D.OverlapBox(
            transform.position,
            transform.localScale * 1.5f,
            0f, 
            1 << cellLayerIndex)?.gameObject.transform;
        
        if (currentCellJoint != cellJoint) // will only run once every time a new cell is entered
        {
            currentCellJoint = cellJoint;
            transform.SetParent(currentCellJoint);
            if (currentCellJoint == null)
            {
                transform.rotation = Quaternion.identity; //player is always internally rotated with the tank,
                //so we need to reset the rotation when they leave a tank to avoid weirdness.
                //it's just the player's visual sprite which is always at 0 rotation
            }
            else
            {
                if (!Mathf.Approximately(transform.eulerAngles.z, currentCellJoint.eulerAngles.z))
                {
                    transform.rotation = Quaternion.Euler(new Vector3(0, 0, currentCellJoint.eulerAngles.z));
                }
            }
        }
    }

    protected virtual void FixedUpdate()
    {
        if (currentState == CharacterState.NONCLIMBING) MoveCharacter();

        else if (currentState == CharacterState.CLIMBING) ClimbLadder();

        else if (currentState == CharacterState.OPERATING) OperateInteractable();

        else if (currentState == CharacterState.REPAIRING) RepairCell();
    }

    protected virtual void OnDrawGizmos()
    {
        //visualizes the grounded box for debugging
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(new Vector2(transform.position.x, transform.position.y - groundedBoxOffset), new Vector3(groundedBoxX, groundedBoxY, 0));
    }

    /* TODO: Ladders are not triggers. Change this to use checksurfacecollider with the ladder layerindex
    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Climbable"))
        {
            Debug.Log("ladder found");
            currentLadder = other.gameObject;
        }
    }

    protected virtual void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Climbable"))
        {
            currentLadder = null;
        }
    }*/
    #endregion

    #region Movement

    protected bool CheckGround()
    {
        LayerMask groundLayer = (1 << LayerMask.NameToLayer("Ground"));
        return Physics2D.OverlapBox(new Vector2(transform.position.x,
                                                     transform.position.y - groundedBoxOffset),
                                                   new Vector2(groundedBoxX, groundedBoxY),
                                                   0f,
                                                   groundLayer);
        
    }
    
    protected Collider2D CheckSurfaceCollider(int layer)
    {
        return Physics2D.OverlapBox(new Vector2(transform.position.x,
                transform.position.y - transform.localScale.y),
            new Vector2(groundedBoxX, groundedBoxY),
            0f,
            1 << layer);
    }

    protected abstract void MoveCharacter();

    protected void PropelJetpack()
    {
        rb.AddForce(Vector2.up * jumpForce);
    }

    protected void SetLadder()
    {
        if (currentLadder == null) return;
        currentState = CharacterState.CLIMBING;
        rb.bodyType = RigidbodyType2D.Kinematic;
        transform.eulerAngles = currentLadder.transform.eulerAngles;
        // converts the player's position from world space to local space relative to the ladder
        // have to do this cause ladders are rotated sometimes
        Vector3 localPosition = currentLadder.transform.InverseTransformPoint(transform.position);

        // set the local x position to 0 bc ladder transform is in the the center of the ladder
        localPosition.x = 0; 

        // Convert the updated local position back to world space
        Vector3 worldPosition = currentLadder.transform.TransformPoint(localPosition);

        transform.position = worldPosition;
    }

    protected virtual void ClimbLadder()
    {

    }

    protected virtual void SwitchOffLadder()
    {
        currentState = CharacterState.NONCLIMBING;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.velocity = Vector2.zero;
        CancelInteraction();
    }

    protected virtual void OperateInteractable()
    {
        rb.velocity = Vector2.zero;
    }

    protected virtual void RepairCell()
    {
        rb.velocity = Vector2.zero;
    }

    public void CancelInteraction()
    {
        currentState = CharacterState.NONCLIMBING;
    }

    public virtual void LinkPlayerHUD(PlayerHUD newHUD)
    {
        characterHUD = newHUD;
        characterHUD.InitializeHUD(characterIndex);
    }
    #endregion

    #region Character Functions

    public float ModifyHealth(float newHealth)
    {
        return SetCharacterHealth(currentHealth + newHealth);
    }

    private float SetCharacterHealth(float characterHealth)
    {
        float tempHealth = currentHealth;

        currentHealth = Mathf.Clamp(characterHealth, 0f, characterSettings.maxHealth);

        float healthDif = tempHealth - currentHealth;

        //Shake the character HUD if they are taking damage
        if (healthDif > 0)
            characterHUD?.ShakePlayerHUD(0.25f, 7f);

        characterHUD?.DamageAvatar(1f - (currentHealth / characterSettings.maxHealth), 0.25f);

        if (currentHealth <= 0)
            OnCharacterDeath();

        return healthDif;
    }

    public void KillCharacterImmediate()
    {
        currentHealth = 0;
        characterHUD?.DamageAvatar(1f - (currentHealth / characterSettings.maxHealth), 0.01f);
        OnCharacterDeath();
    }

    protected virtual void OnCharacterDeath()
    {
        GameManager.Instance.AudioManager.Play("ExplosionSFX", gameObject);
        GameManager.Instance.ParticleSpawner.SpawnParticle(Random.Range(0, 2), transform.position, characterDeathParticleSize, null);

        isRespawning = assignedTank != null;

        if (isRespawning)
        {
            characterHUD?.ShowRespawnTimer(true);
            currentRespawnTime = respawnTime;
        }
        else
        {
            characterHUD?.KillPlayerHUD();
            isDead = true;
        }

        //TODO: (Ryan)
        //Needs to drop any objects/tools currently holding/equipped
        //Needs to be kicked out of any interactable they're operating
        //Needs to be unparented from anything they're parented to

        rb.isKinematic = true;
        characterHitbox.enabled = false;
        characterVisualParent?.gameObject.SetActive(false);
        isAlive = false;
        ResetPlayer();
    }

    protected virtual void ResetPlayer()
    {
        currentHealth = characterSettings.maxHealth;
        currentFuel = characterSettings.fuelAmount;
        currentState = CharacterState.NONCLIMBING;
        moveSpeedHalved = moveSpeed / 2;
    }

    private void RespawnTimer()
    {
        currentRespawnTime -= Time.deltaTime;

        characterHUD?.UpdateRespawnBar(1 - (currentRespawnTime / respawnTime), currentRespawnTime);

        if (currentRespawnTime <= 0f)
        {
            RespawnPlayer();
            isRespawning = false;
            isAlive = true;
        }
    }

    private void RespawnPlayer()
    {
        characterHUD?.DamageAvatar(1f - (currentHealth / characterSettings.maxHealth), 0.01f);
        characterHUD?.ShowRespawnTimer(false);
        LevelManager.Instance?.MoveCharacterToSpawn(this);
        rb.isKinematic = false;
        characterHitbox.enabled = true;
        characterVisualParent?.gameObject.SetActive(true);
    }

    public Color GetCharacterColor() => characterColor;
    public TankController GetAssignedTank() => assignedTank;
    public void SetAssignedTank(TankController assignedTank) => this.assignedTank = assignedTank;
    public bool IsDead() => isDead;

    #endregion
}
