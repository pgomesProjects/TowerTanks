using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Character : SerializedMonoBehaviour
{
    #region Fields and Properties

    public enum CharacterState { CLIMBING, NONCLIMBING, OPERATING }; //Simple state system, in the future this will probably be refactored

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
        SelfDestruct();
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
        hands = transform.transform.Find("Hands");
        characterVisualParent = transform.GetChild(0);
    }

    protected virtual void Start()
    {
        ResetPlayer();
        isAlive = true;
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
        if (currentCellJoint != cellJoint)
        {
            currentCellJoint = cellJoint;
            transform.SetParent(currentCellJoint);
        }
    }

    protected virtual void FixedUpdate()
    {
        if (currentState == CharacterState.NONCLIMBING) MoveCharacter();

        else if (currentState == CharacterState.CLIMBING) ClimbLadder();

        else if (currentState == CharacterState.OPERATING) OperateInteractable();
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
        if (currentLadder != null)
        {
            currentState = CharacterState.CLIMBING;
            rb.bodyType = RigidbodyType2D.Kinematic;
            transform.position = new Vector2(currentLadder.transform.position.x, transform.position.y);
            ladderBounds = currentLadder.GetComponent<Collider2D>().bounds;
        }

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

    public void CancelInteraction()
    {
        currentState = CharacterState.NONCLIMBING;
    }

    public void LinkPlayerHUD(PlayerHUD newHUD)
    {
        characterHUD = newHUD;
        characterHUD.InitializeHUD(characterIndex);
    }
    #endregion

    #region Character Functions

    public void ModifyHealth(float newHealth)
    {
        currentHealth += newHealth;
        currentHealth = Mathf.Clamp(currentHealth, 0f, characterSettings.maxHealth);

        //Shake the character HUD if they are taking damage
        if (newHealth < 0)
            characterHUD?.ShakePlayerHUD(0.25f, 7f);

        characterHUD?.DamageAvatar(1f - (currentHealth / characterSettings.maxHealth), 0.25f);

        if (currentHealth <= 0)
            OnCharacterDeath();
    }

    protected void SelfDestruct()
    {
        currentHealth = 0;
        characterHUD?.DamageAvatar(1f - (currentHealth / characterSettings.maxHealth), 0.01f);
        OnCharacterDeath();
    }

    protected virtual void OnCharacterDeath(bool isRespawnable = true)
    {
        GameManager.Instance.AudioManager.Play("ExplosionSFX", gameObject);
        GameManager.Instance.ParticleSpawner.SpawnParticle(Random.Range(0, 2), transform.position, characterDeathParticleSize, null);

        isRespawning = isRespawnable;

        if (isRespawning)
        {
            characterHUD?.ShowRespawnTimer(true);
            currentRespawnTime = respawnTime;
        }

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
    #endregion
}
