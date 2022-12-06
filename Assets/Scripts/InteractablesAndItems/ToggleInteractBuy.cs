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
            if (collision.GetComponent<PlayerController>().GetPlayerItem().CompareTag("Hammer"))
            {
                if(LevelManager.instance.levelPhase == GAMESTATE.TUTORIAL)
                {
                    if(TutorialController.main.currentTutorialState != TUTORIALSTATE.READING)
                    {
                        priceText.SetActive(true);
                        playerCanPurchase = true;
                        collision.GetComponent<PlayerController>().currentInteractableToBuy = this;
                    }
                }
                else
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
            if (collision.GetComponent<PlayerController>().GetPlayerItem().CompareTag("Hammer"))
            {
                priceText.SetActive(false);
                playerCanPurchase = false;
                collision.GetComponent<PlayerController>().currentInteractableToBuy = null;
            }
        }
    }

    public void PurchaseInteractable()
    {
        if(LevelManager.instance.levelPhase == GAMESTATE.TUTORIAL)
        {
            if (transform.parent.transform.Find("Indicator").gameObject.activeInHierarchy)
            {
                if (LevelManager.instance.CanPlayerAfford(price))
                {
                    switch (interactableType)
                    {
                        case INTERACTABLETYPE.CANNON:
                            if(TutorialController.main.currentTutorialState == TUTORIALSTATE.BUILDCANNON)
                            {
                                FindObjectOfType<InteractableSpawnerManager>().SpawnCannon(transform.parent.GetComponent<InteractableSpawner>());
                                LevelManager.instance.UpdateResources(-price);
                                //Play sound effect
                                FindObjectOfType<AudioManager>().PlayOneShot("UseSFX", PlayerPrefs.GetFloat("SFXVolume", 0.5f));
                                Destroy(gameObject);
                            }
                            break;
                        case INTERACTABLETYPE.ENGINE:
                            if(TutorialController.main.currentTutorialState == TUTORIALSTATE.BUILDENGINE)
                            {
                                FindObjectOfType<InteractableSpawnerManager>().SpawnEngine(transform.parent.GetComponent<InteractableSpawner>());
                                LevelManager.instance.UpdateResources(-price);
                                //Play sound effect
                                FindObjectOfType<AudioManager>().PlayOneShot("UseSFX", PlayerPrefs.GetFloat("SFXVolume", 0.5f));
                                Destroy(gameObject);
                            }
                            break;
                        case INTERACTABLETYPE.SHELLSTATION:
                            if(TutorialController.main.currentTutorialState == TUTORIALSTATE.BUILDAMMOCRATE)
                            {
                                FindObjectOfType<InteractableSpawnerManager>().SpawnShellStation(transform.parent.GetComponent<InteractableSpawner>());
                                LevelManager.instance.UpdateResources(-price);
                                //Play sound effect
                                FindObjectOfType<AudioManager>().PlayOneShot("UseSFX", PlayerPrefs.GetFloat("SFXVolume", 0.5f));
                                Destroy(gameObject);
                            }
                            break;
                        case INTERACTABLETYPE.THROTTLE:
                            if(TutorialController.main.currentTutorialState == TUTORIALSTATE.BUILDTHROTTLE)
                            {
                                FindObjectOfType<InteractableSpawnerManager>().SpawnThrottle(transform.parent.GetComponent<InteractableSpawner>());
                                LevelManager.instance.UpdateResources(-price);
                                //Play sound effect
                                FindObjectOfType<AudioManager>().PlayOneShot("UseSFX", PlayerPrefs.GetFloat("SFXVolume", 0.5f));
                                Destroy(gameObject);
                            }
                            break;
                    }
                }
            }
        }
        else
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

                //Play sound effect
                FindObjectOfType<AudioManager>().PlayOneShot("UseSFX", PlayerPrefs.GetFloat("SFXVolume", 0.5f));

                Destroy(gameObject);
            }
        }
    }

    public bool PlayerCanPurchase()
    {
        return playerCanPurchase;
    }
}
