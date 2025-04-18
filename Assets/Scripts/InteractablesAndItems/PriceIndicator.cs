using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace TowerTanks.Scripts.Deprecated
{
    public class PriceIndicator : MonoBehaviour
    {
        [SerializeField, Tooltip("The price of the interactable.")] private int price;
        [SerializeField, Tooltip("The GameObject with the price text.")] private GameObject priceText;
        [SerializeField, Tooltip("The type of interactable.")] private DEPRECATEDINTERACTABLETYPE interactableType;
        [SerializeField, Tooltip("Does the interactable require scrap to be built?")] private bool requiresScrap = true;

        private bool playerCanPurchase; //If true, the player can try to purchase the object. If false, they cannot.
        private PlayerController currentPlayerBuying;   //The current player trying to purchase an interactable

        private void Start()
        {
            playerCanPurchase = false;
            if (price > 0)
                priceText.GetComponentInChildren<TextMeshProUGUI>().text = "Price: " + price;
            else
                priceText.GetComponentInChildren<TextMeshProUGUI>().text = "Free";
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Player"))
            {
                if (currentPlayerBuying == null && GetComponentInParent<GhostInteractables>().CurrentPlayerIsBuilding(collision.GetComponent<PlayerController>()) && (collision.GetComponent<PlayerController>().IsHoldingScrap() || !requiresScrap))
                {
                    currentPlayerBuying = collision.GetComponent<PlayerController>();
                    if (LevelManager.Instance.levelPhase != GAMESTATE.GAMEOVER)
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
                if (currentPlayerBuying == collision.GetComponent<PlayerController>() && (collision.GetComponent<PlayerController>().IsHoldingScrap() || !requiresScrap))
                {
                    ReleasePlayerFromBuying(currentPlayerBuying);
                }
            }
        }

        /// <summary>
        /// Removes the player from being able to buy scrap. 
        /// </summary>
        /// <param name="currentPlayer">The player to remove from the price indicator.</param>
        /// <param name="exitingTrigger">If true, the player is exiting the trigger for the price indicator.</param>
        public void ReleasePlayerFromBuying(PlayerController currentPlayer, bool exitingTrigger = true)
        {
            if (currentPlayer == currentPlayerBuying && (requiresScrap || exitingTrigger))
            {
                currentPlayerBuying.currentInteractableToBuy = null;
                currentPlayerBuying = null;
                priceText.SetActive(false);
                playerCanPurchase = false;
            }
        }

        /// <summary>
        /// Purchase the current interactable if the player can afford it.
        /// </summary>
        public void PurchaseInteractable()
        {
            if (currentPlayerBuying.GetScrapValue() >= price)
            {
                if (LevelManager.Instance.levelPhase != GAMESTATE.GAMEOVER)
                {
                    //Purchase interactable
                    currentPlayerBuying.UseScrap(price);
                    FindObjectOfType<InteractableSpawnerManager>().CreateInteractable(transform.parent.GetComponent<InteractableSpawner>(), interactableType);
                    //Play sound effect
                    GameManager.Instance.AudioManager.Play("TankImpact", gameObject);

                    //Destroy self
                    Destroy(gameObject);
                }
            }
        }

        public bool PlayerCanPurchase(PlayerController currentPlayer)
        {
            if (currentPlayerBuying == null || currentPlayerBuying != currentPlayer)
                return false;

            return playerCanPurchase;
        }
    }
}
