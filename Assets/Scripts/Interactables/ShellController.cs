using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ShellController : MonoBehaviour
{
    private bool canPickUp;
    private bool isPickedUp;

    // Start is called before the first frame update
    void Start()
    {
        canPickUp = false;
        isPickedUp = false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            canPickUp = true;
            Debug.Log("Can Pick Up Shell!");
            collision.GetComponent<PlayerController>().GivePlayerItem(gameObject);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            canPickUp = false;
            Debug.Log("Can No Longer Pick Up Shell!");
            collision.GetComponent<PlayerController>().GivePlayerItem(null);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.tag == "Player")
        {
            Physics2D.IgnoreCollision(collision.collider, GetComponent<Collider2D>());
        }
    }

    public void SetPickUp(bool pickedUp)
    {
        isPickedUp = pickedUp;
    }

}
