using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ToggleInteractBuy : MonoBehaviour
{
    public enum INTERACTABLETYPE { NONE, CANNON, ENGINE, SHELLSTATION, THROTTLE  };

    [SerializeField] private int price;
    [SerializeField] private GameObject priceText;
    [SerializeField] private INTERACTABLETYPE interactableType;

    private bool playerCanPurchase;

    private void Start()
    {
        playerCanPurchase = false;
        priceText.GetComponentInChildren<TextMeshProUGUI>().text = "Price: " + price;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (collision.GetComponent<PlayerController>().IsPlayerHoldingHammer())
            {
                priceText.SetActive(true);
                playerCanPurchase = true;
                collision.GetComponent<PlayerController>().currentInteractableToBuy = this;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (collision.GetComponent<PlayerController>().IsPlayerHoldingHammer())
            {
                priceText.SetActive(false);
                playerCanPurchase = false;
                collision.GetComponent<PlayerController>().currentInteractableToBuy = null;
            }
        }
    }

    public void PurchaseInteractable()
    {
        if (LevelManager.instance.CanPlayerAfford(price))
        {
            LevelManager.instance.UpdateResources(-price);

            switch (interactableType)
            {
                case INTERACTABLETYPE.CANNON:
                    FindObjectOfType<InteractableSpawnerManager>().SpawnCannon(transform.parent.GetComponent<InteractableSpawner>());
                    break;
                case INTERACTABLETYPE.ENGINE:
                    FindObjectOfType<InteractableSpawnerManager>().SpawnEngine(transform.parent.GetComponent<InteractableSpawner>());
                    break;
                case INTERACTABLETYPE.SHELLSTATION:
                    FindObjectOfType<InteractableSpawnerManager>().SpawnShellStation(transform.parent.GetComponent<InteractableSpawner>());
                    break;
                case INTERACTABLETYPE.THROTTLE:
                    FindObjectOfType<InteractableSpawnerManager>().SpawnThrottle(transform.parent.GetComponent<InteractableSpawner>());
                    break;
            }

            Destroy(gameObject);
        }
    }

    public bool PlayerCanPurchase()
    {
        return playerCanPurchase;
    }
}
