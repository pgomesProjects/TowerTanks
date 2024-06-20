using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadingZoneController : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerController currentPlayer = collision.GetComponent<PlayerController>();
            currentPlayer.SetPlayerClimb(true);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerController currentPlayer = collision.GetComponent<PlayerController>();
            currentPlayer.SetPlayerClimb(false);
        }
    }
}
