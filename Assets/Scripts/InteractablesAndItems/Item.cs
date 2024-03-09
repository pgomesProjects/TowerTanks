using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour
{
    [SerializeField] protected float timeToUse = 0;
    [SerializeField] protected bool keepInsideTank = true;
    protected bool canPickUp;
    protected bool isPickedUp;
    private float defaultGravity;
    private PlayerTankController playerTank;

    // Start is called before the first frame update
    void Start()
    {
        canPickUp = false;
        isPickedUp = false;
        defaultGravity = 2;
        playerTank = LevelManager.Instance.GetPlayerTank();
    }

    private void Update()
    {
        if (keepInsideTank)
        {
            if(playerTank != null)
            {
                Vector3 itemPos = transform.localPosition;
                itemPos.x = Mathf.Clamp(itemPos.x, -playerTank.tankBarrierRange, playerTank.tankBarrierRange);
                transform.localPosition = itemPos;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            //If the player is not holding an item
            if (!collision.GetComponent<PlayerController>().IsPlayerHoldingItem() && !isPickedUp)
            {
                canPickUp = true;
                //Debug.Log("Can Pick Up Item!");
                collision.GetComponent<PlayerController>().MarkClosestItem(this);
                //collision.GetComponent<PlayerController>().DisplayInteractionPrompt("<sprite=27>");
            }
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            //If the player is not holding an item
            if (!collision.GetComponent<PlayerController>().IsPlayerHoldingItem() && !isPickedUp)
            {
                canPickUp = true;
                //Debug.Log("Can Pick Up Item!");
                collision.GetComponent<PlayerController>().MarkClosestItem(this);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            //If the player is not holding an item
            if (!collision.GetComponent<PlayerController>().IsPlayerHoldingItem() && !isPickedUp)
            {
                canPickUp = false;
                //Debug.Log("Can No Longer Pick Up Item!");
                collision.GetComponent<PlayerController>().MarkClosestItem(null);
                //collision.GetComponent<PlayerController>().HideInteractionPrompt();
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Player" || isPickedUp)
        {
            Physics2D.IgnoreCollision(collision.collider, GetComponent<Collider2D>());
        }
    }

    public float GetTimeToUse()
    {
        return timeToUse;
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

    public void SetRotateConstraint(bool canRotate)
    {
        GetComponent<Rigidbody2D>().freezeRotation = canRotate;
    }
}
