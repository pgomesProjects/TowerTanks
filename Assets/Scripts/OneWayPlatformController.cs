using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OneWayPlatformController : MonoBehaviour
{
    private GameObject currentOneWayPlatform;

    [SerializeField] private CapsuleCollider2D playerCollider;

    // Update is called once per frame
    void Update()
    {
        if (GetComponent<PlayerController>().IsPlayerClimbing())
        {
            //If the player is climbing and they are collidiing with a one way platform, disable platform collision
            if(currentOneWayPlatform != null)
            {
                StartCoroutine(DisableCollision());
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("OneWayPlatform"))
        {
            currentOneWayPlatform = collision.gameObject;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("OneWayPlatform"))
        {
            currentOneWayPlatform = null;
        }
    }

    private IEnumerator DisableCollision()
    {
        BoxCollider2D platformCollider = currentOneWayPlatform.GetComponent<BoxCollider2D>();

        Physics2D.IgnoreCollision(playerCollider, platformCollider);
        yield return new WaitForSeconds(0.5f);
        Physics2D.IgnoreCollision(playerCollider, platformCollider, false);
    }
}
