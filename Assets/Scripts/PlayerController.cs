using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private Vector2 movement;
    private float speed = 8f;
    private Rigidbody2D rb;
    internal InteractableController currentInteractableItem;
    [SerializeField] private float distance = 3f;
    [SerializeField] private LayerMask ladderMask;
    private bool isClimbing;
    private float defaultGravity;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        defaultGravity = rb.gravityScale;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        //Move the player horizontally
        rb.velocity = new Vector2(movement.x * speed, rb.velocity.y);

        //Check to see if the player is colliding with the ladder
        RaycastHit2D hitInfo = Physics2D.Raycast(transform.position, Vector2.up, distance, ladderMask);

        //If the player is colliding with the ladder
        if(hitInfo.collider != null)
        {
            //If the player is trying to move up, they are climbing
            if(movement.y > 0.25f)
            {
                isClimbing = true;
            }
        }
        else
        {
            isClimbing = false;
        }

        //If the player is climbing, move up and get rid of gravity temporarily
        if (isClimbing)
        {
            rb.velocity = new Vector2(rb.velocity.x, movement.y * speed);
            rb.gravityScale = 0;
        }
        //Once the player stops climbing, bring back gravity
        else
        {
            rb.gravityScale = defaultGravity;
        }
    }

    //Send value from Move callback to the horizontal float
    public void OnMove(InputAction.CallbackContext ctx) => movement = ctx.ReadValue<Vector2>();

    public void OnInteract(InputAction.CallbackContext ctx)
    {
        //If the player presses the interact button and there is something to interact with
        if (ctx.started && currentInteractableItem != null)
        {
            //Call the interaction event
            currentInteractableItem.OnInteraction();
        }
    }

    public void OnPause(InputAction.CallbackContext ctx)
    {
        //If the player presses the pause button
        if (ctx.started)
        {
            //Pause the game
            LevelManager.instance.PauseToggle();
        }
    }

}
