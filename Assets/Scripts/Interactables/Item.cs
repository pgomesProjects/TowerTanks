using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour
{
    protected bool canPickUp;
    protected bool isPickedUp;
    private float defaultGravity;

    // Start is called before the first frame update
    void Start()
    {
        canPickUp = false;
        isPickedUp = false;
        defaultGravity = GetComponent<Rigidbody2D>().gravityScale;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            //If the player is not holding an item
            if (!collision.GetComponent<PlayerController>().IsPlayerHoldingItem())
            {
                canPickUp = true;
                Debug.Log("Can Pick Up Item!");
                collision.GetComponent<PlayerController>().MarkClosestItem(this);
            }
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            //If the player is not holding an item
            if (!collision.GetComponent<PlayerController>().IsPlayerHoldingItem())
            {
                canPickUp = true;
                Debug.Log("Can Pick Up Item!");
                collision.GetComponent<PlayerController>().MarkClosestItem(this);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            //If the player is not holding an item
            if (!collision.GetComponent<PlayerController>().IsPlayerHoldingItem())
            {
                canPickUp = false;
                Debug.Log("Can No Longer Pick Up Item!");
                collision.GetComponent<PlayerController>().MarkClosestItem(null);
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            Physics2D.IgnoreCollision(collision.collider, GetComponent<Collider2D>());
        }
    }

    public bool IsItemPickedUp()
    {
        return isPickedUp;
    }

    public void SetPickUp(bool pickedUp)
    {
        isPickedUp = pickedUp;
    }

    public float GetDefaultGravityScale()
    {
        return defaultGravity;
    }
}
