using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PriceIndicator : MonoBehaviour
{
    [SerializeField, Tooltip("The price of the interactable.")] private int price;
    [SerializeField, Tooltip("The GameObject with the price text.")] private GameObject priceText;
    [SerializeField, Tooltip("The type of interactable.")] private INTERACTABLETYPE interactableType;
    [SerializeField, Tooltip("Does the interactable require scrap to be built?")] private bool requiresScrap = true;

    private bool playerCanPurchase; //If true, the player can try to purchase the object. If false, they cannot.

    private void Start()
    {
        playerCanPurchase = false;
        if(price > 0)
            priceText.GetComponentInChildren<TextMeshProUGUI>().text = "Price: " + price;
        else
            priceText.GetComponentInChildren<TextMeshProUGUI>().text = "Free";
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if(collision.GetComponent<PlayerController>().IsHoldingScrap() || !requiresScrap)
            {
                if (LevelManager.instance.levelPhase != GAMESTATE.TUTORIAL || TutorialController.main.currentTutorialState != TUTORIALSTATE.READING)
                {
                    priceText.SetActive(true);
                    playerCanPurchase = true;
                    collision.GetComponent<PlayerController>().currentInteractableToBuy = this;
                }
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if(collision.GetComponent<PlayerController>().IsHoldingScrap() || !requiresScrap)
            {
                priceText.SetActive(false);
                playerCanPurchase = false;
                collision.GetComponent<PlayerController>().currentInteractableToBuy = null;
            }
        }
    }

    /// <summary>
    /// Purchase the current interactable if the player can afford it.
    /// </summary>
    public void PurchaseInteractable()
    {
        if (LevelManager.instance.CanPlayerAfford(price))
        {
            if(LevelManager.instance.levelPhase != GAMESTATE.TUTORIAL || transform.parent.transform.Find("Indicator").gameObject.activeInHierarchy)
            {
                //Purchase interactable
                LevelManager.instance.UpdateResources(-price);
                FindObjectOfType<InteractableSpawnerManager>().CreateInteractable(transform.parent.GetComponent<InteractableSpawner>(), interactableType);
                //Play sound effect
                FindObjectOfType<AudioManager>().Play("UseSFX", PlayerPrefs.GetFloat("SFXVolume", GameSettings.defaultSFXVolume), gameObject);

                //Destroy self
                Destroy(gameObject);
            }
        }
    }

    public bool PlayerCanPurchase() => playerCanPurchase;
}
