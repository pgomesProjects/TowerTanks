using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class PlayerController : MonoBehaviour
{
    private int playerIndex;

    private Vector2 movement;
    private float cannonMovement;
    internal float steeringValue;
    private float speed = 8f;
    private Rigidbody2D rb;

    internal int currentLayer = 1;

    [SerializeField] private float distance = 3f;
    [SerializeField] private LayerMask ladderMask;
    private bool canMove;
    private bool isClimbing;
    private float defaultGravity;

    internal InteractableController currentInteractableItem;

    private Item closestItem;
    private Item itemHeld;
    private bool isHoldingItem;
    private bool holdingHammer;

    private GameObject interactableHover;
    private RaycastHit2D ladderRaycast;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        defaultGravity = rb.gravityScale;
        isHoldingItem = false;
        canMove = true;
        interactableHover = transform.Find("HoverPrompt").gameObject;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        //Move the player horizontally
        if (canMove)
        {
            rb.velocity = new Vector2(movement.x * speed, rb.velocity.y);
        }

        //Check to see if the player is colliding with the ladder
        ladderRaycast = Physics2D.Raycast(transform.position, Vector2.up, distance, ladderMask);

        //If the player is colliding with the ladder
        if (ladderRaycast.collider != null)
        {
            DisplayInteractionPrompt("<sprite=30>");
        }
        else
        {
            isClimbing = false;
            HideInteractionPrompt();
        }

        //If the player is climbing, move up and get rid of gravity temporarily
        if (isClimbing)
        {
            rb.velocity = new Vector2(0, movement.y * speed);
            rb.gravityScale = 0;
        }

        //Once the player stops climbing, bring back gravity
        else
        {
            rb.gravityScale = defaultGravity;
        }
    }

    //Send value from Move callback to the horizontal Vector2
    public void OnMove(InputAction.CallbackContext ctx) => movement = ctx.ReadValue<Vector2>();

    //Send value from Angle Cannon callback to the horizontal float
    public void OnAngleCannon(InputAction.CallbackContext ctx) => cannonMovement = ctx.ReadValue<Vector2>().y;

    public void OnControlSteering(InputAction.CallbackContext ctx)
    {
        //If the player presses the steering button
        if (ctx.started)
        {
            if (LevelManager.instance.isSteering)
            {
                steeringValue = ctx.ReadValue<float>();
            }
        }

        if (ctx.canceled)
            steeringValue = 0;
    }

    public float GetCannonMovement()
    {
        return cannonMovement;
    }

    public void OnInteract(InputAction.CallbackContext ctx)
    {
        //If the player presses the interact button
        if (ctx.started)
        {
            //If there is something to interact with
            if (currentInteractableItem != null)
            {
                FindObjectOfType<InteractionsManager>().currentPlayer = this;

                //Call the interaction event
                currentInteractableItem.OnInteraction();
            }

            //If the player is holding the hammer and uses the interact button, attempt to add a new layer
            if (holdingHammer)
            {
                LevelManager.instance.AddLayer(this);
            }


            //If the player is already climbing, move them off of the ladder
            if (isClimbing)
            {
                isClimbing = false;
            }
            //If the player is colliding with the ladder and wants to climb
            else if (ladderRaycast.collider != null)
            {
                isClimbing = true;
                transform.position = new Vector2(-1.5f, transform.position.y);
            }
        }
    }

    public void DisplayInteractionPrompt(string message)
    {
        interactableHover.transform.Find("Prompt").GetComponent<TextMeshProUGUI>().text = message;
        interactableHover.SetActive(true);
    }

    public void HideInteractionPrompt()
    {
        interactableHover.SetActive(false);
    }

    public void OnPause(InputAction.CallbackContext ctx)
    {
        //If the player presses the pause button
        if (ctx.started)
        {
            //Pause the game
            LevelManager.instance.PauseToggle(playerIndex);
        }
    }

    public void SetPlayerMove(bool movePlayer)
    {
        canMove = movePlayer;
    }

    public void MarkClosestItem(Item item)
    {
        closestItem = item;
    }

    public bool IsPlayerHoldingItem()
    {
        return isHoldingItem;
    }

    public Item GetPlayerItem()
    {
        return itemHeld;
    }

    public void DestroyItem()
    {
        Destroy(itemHeld.gameObject);
        itemHeld = null;
        isHoldingItem = false;
    }

    public void OnPickup(InputAction.CallbackContext ctx)
    {
        //If the player presses the pickup button
        if (ctx.started)
        {
            //If the player is not holding an item
            if (!isHoldingItem)
            {
                //If the closest item to the player exists and is not picked up
                if (closestItem != null && !closestItem.IsItemPickedUp())
                {
                    itemHeld = closestItem;

                    //Make the current item the player's child and stick it to the player
                    itemHeld.transform.position = gameObject.transform.position;
                    itemHeld.transform.rotation = Quaternion.identity;
                    itemHeld.GetComponent<Rigidbody2D>().gravityScale = 0;
                    itemHeld.GetComponent<Rigidbody2D>().isKinematic = true;
                    itemHeld.transform.parent = gameObject.transform;

                    Debug.Log("Picked Up Item!");

                    if (itemHeld.CompareTag("Hammer"))
                    {
                        holdingHammer = true;
                    }

                    itemHeld.SetPickUp(true);
                    isHoldingItem = true;
                    closestItem = null;
                }
            }
            else
            {
                //If the player is still holding an item
                if (itemHeld != null)
                {
                    //Remove the item from the player and put it back in the level
                    itemHeld.GetComponent<Rigidbody2D>().gravityScale = itemHeld.GetDefaultGravityScale();
                    itemHeld.GetComponent<Rigidbody2D>().isKinematic = false;
                    itemHeld.transform.parent = null;

                    if (itemHeld.CompareTag("Hammer"))
                    {
                        holdingHammer = false;
                    }

                    itemHeld.SetPickUp(false);
                    isHoldingItem = false;
                    itemHeld = null;
                }
            }
        }
    }

    public int GetPlayerIndex()
    {
        return playerIndex;
    }

    public void SetPlayerIndex(int index)
    {
        playerIndex = index;
    }
}
