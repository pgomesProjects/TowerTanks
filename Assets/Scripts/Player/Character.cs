using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Character : SerializedMonoBehaviour
{
    #region Fields and Properties

    protected enum CharacterState { CLIMBING, NONCLIMBING }; //Simple state system, in the future this will probably be refactored
    protected CharacterState currentState;                   //to an FSM.

    //Components
    protected Rigidbody2D rb;
    protected GameObject currentLadder;
    protected Bounds ladderBounds;
    protected PlayerHUD characterHUD;
    protected int characterIndex;

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

    [SerializeField] protected float maxYVelocity, minYVelocity;

    [Header("Jetpack values")]
    [SerializeField] protected float fuelDepletionRate;
    [SerializeField] protected float fuelRegenerationRate;
    protected float currentFuel;

    //temp
    protected float moveSpeedHalved; // once we have a state machine for the player, we wont need these silly fields.
    protected float currentMoveSpeed; // this is fine for the sake of prototyping though.

    [Button(ButtonSizes.Medium)]
    private void DebugModifyPlayerHealth()
    {
        ModifyHealth(healthToModify);
    }
    public int healthToModify = -10;

    #endregion

    #region Unity Methods
    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        currentHealth = characterSettings.maxHealth;
    }

    protected virtual void Start()
    {
        currentFuel = characterSettings.fuelAmount;
        currentState = CharacterState.NONCLIMBING;
        moveSpeedHalved = moveSpeed / 2;
    }

    protected virtual void Update()
    {
        currentFuel = Mathf.Clamp(currentFuel, 0, characterSettings.fuelAmount);
        //Debug.Log($"Current Fuel: {currentFuel}");
    }

    protected virtual void FixedUpdate()
    {
        if (currentState == CharacterState.NONCLIMBING) MoveCharacter();

        else if (currentState == CharacterState.CLIMBING)
        {
            DetectLadderBounds();
            ClimbLadder();
        }
    }

    protected virtual void OnDrawGizmos()
    {
        //visualizes the grounded box for debugging
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(new Vector2(transform.position.x, transform.position.y - transform.localScale.y / 2), new Vector3(groundedBoxX, groundedBoxY, 0));
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Ladder"))
        {
            Debug.Log("ladder found");
            currentLadder = other.gameObject;
        }
    }

    protected virtual void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Ladder"))
        {
            currentLadder = null;
        }
    }
    #endregion

    #region Movement

    protected bool CheckGround()
    {
        LayerMask groundLayer = (1 << LayerMask.NameToLayer("Ground"));
        return Physics2D.OverlapBox(new Vector2(transform.position.x,
                                                     transform.position.y - transform.localScale.y / 2),
                                                   new Vector2(groundedBoxX, groundedBoxY),
                                                   0f,
                                                   groundLayer);
        
    }

    protected abstract void MoveCharacter();
    
    protected abstract void ClimbLadder();

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


    protected virtual void DetectLadderBounds()
    {
        // Create a LayerMask for the ladder layer.
        int ladderLayerIndex = LayerMask.NameToLayer("Ladder");
        LayerMask ladderLayer = 1 << ladderLayerIndex;


        // Get all the ladders within a certain radius of the player.
        Collider2D[] nearbyLadders = Physics2D.OverlapCircleAll(transform.position, .5f, ladderLayer);

        foreach (Collider2D ladder in nearbyLadders)
        {
            // For each ladder, add its bounds to ladderBounds.
            ladderBounds.Encapsulate(ladder.bounds);
            
        }
    }

    protected virtual void SwitchOffLadder()
    {
        currentState = CharacterState.NONCLIMBING;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.velocity = Vector2.zero;
    }

    public void LinkPlayerHUD(PlayerHUD newHUD)
    {
        characterHUD = newHUD;
        characterHUD.InitializeHUD(characterIndex);
    }
    #endregion

    #region Character Functions

    protected void ModifyHealth(float newHealth)
    {
        currentHealth += newHealth;
        currentHealth = Mathf.Clamp(currentHealth, 0f, characterSettings.maxHealth);

        //Shake the character HUD if they are taking damage
        if (newHealth < 0)
            characterHUD.ShakePlayerHUD(0.25f, 7f);

        characterHUD.DamageAvatar(1f - (currentHealth / characterSettings.maxHealth), 0.25f);

        if (currentHealth <= 0)
            OnCharacterDeath();
    }

    protected abstract void OnCharacterDeath();

    #endregion
}
