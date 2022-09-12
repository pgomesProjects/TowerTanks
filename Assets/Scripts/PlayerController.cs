using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private float horizontal;
    private float speed = 8f;
    private Rigidbody2D rb;
    internal InteractableController currentInteractableItem;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        //Move the player horizontally
        rb.velocity = new Vector2(horizontal * speed, rb.velocity.y);
    }

    //Send value from Move callback to the horizontal float
    public void OnMove(InputAction.CallbackContext ctx) => horizontal = ctx.ReadValue<Vector2>().x;

    public void OnInteract(InputAction.CallbackContext ctx)
    {
        //If the player presses the interact button and there is something to interact with
        if (ctx.started && currentInteractableItem != null)
        {
            //Call the interaction event
            currentInteractableItem.OnInteraction();
        }
    }

}
