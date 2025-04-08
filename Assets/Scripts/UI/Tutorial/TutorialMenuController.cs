using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TowerTanks.Scripts
{
    public class TutorialMenuController : MonoBehaviour
    {
        [SerializeField, Tooltip("The container for the tutorial items.")] private RectTransform tutorialContainer;
        [SerializeField, Tooltip("The prefab for the tutorial menu item.")] private TutorialMenuItem tutorialMenuItemPrefab;

        [SerializeField] private float menuMovementCooldown;

        private PlayerControlSystem playerControls;
        private int currentIndexSelected;
        private float currentMenuCooldown;
        private bool canMove;

        private void Awake()
        {
            playerControls = new PlayerControlSystem();
            CreateTutorialMenuItems();
        }

        private void OnEnable()
        {
            currentIndexSelected = 0;
            canMove = true;
            tutorialContainer.GetChild(currentIndexSelected).GetComponent<Selectable>()?.Select();
            playerControls?.Enable();
            TutorialPopupController.OnTutorialStarted += DeselectMenu;
            TutorialPopupController.OnTutorialEnded += ReselectMenu;
        }

        private void OnDisable()
        {
            playerControls?.Disable();
            TutorialPopupController.OnTutorialStarted -= DeselectMenu;
            TutorialPopupController.OnTutorialEnded -= ReselectMenu;
        }

        private void DeselectMenu()
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        private void ReselectMenu()
        {
            tutorialContainer.GetChild(currentIndexSelected).GetComponent<Selectable>()?.Select();
        }

        private void Update()
        {
            if (GameManager.Instance.tutorialWindowActive)
                return;

            Vector2 movement = playerControls.UI.Navigate.ReadValue<Vector2>();

            if (!canMove)
            {
                currentMenuCooldown += Time.unscaledDeltaTime;
                if (currentMenuCooldown >= menuMovementCooldown)
                    canMove = true;
            }

            if (movement.y > 0.1f && canMove)
            {
                Selectable nextSelectable = tutorialContainer.GetChild(Mathf.Clamp(currentIndexSelected - 1, 0, tutorialContainer.childCount - 1)).GetComponent<Selectable>();

                if (nextSelectable != null)
                {
                    SelectMenuIndex(currentIndexSelected - 1, nextSelectable);
                }
            }

            if (movement.y < -0.1f && canMove)
            {
                Selectable nextSelectable = tutorialContainer.GetChild(Mathf.Clamp(currentIndexSelected + 1, 0, tutorialContainer.childCount - 1)).GetComponent<Selectable>();

                if(nextSelectable != null)
                {
                    SelectMenuIndex(currentIndexSelected + 1, nextSelectable);
                }
            }
        }

        private void SelectMenuIndex(int menuIndex, Selectable nextSelectable)
        {
            canMove = false;
            currentMenuCooldown = 0f;
            GameManager.Instance.AudioManager.Play("ButtonHover");

            currentIndexSelected = Mathf.Clamp(menuIndex, 0, tutorialContainer.childCount - 1);
            EventSystem.current.SetSelectedGameObject(null);
            nextSelectable.Select();
            RefreshContainerPosition();
        }

        private void RefreshContainerPosition()
        {
            tutorialContainer.anchoredPosition = new Vector2(tutorialContainer.anchoredPosition.x, Mathf.Max(0, 122.5f * (currentIndexSelected - 8)));
        }

        /// <summary>
        /// Creates the tutorial menu items.
        /// </summary>
        private void CreateTutorialMenuItems()
        {
            for(int i = 0; i < GameManager.Instance.tutorialsList.Length; i++)
            {
                TutorialMenuItem menuItem = Instantiate(tutorialMenuItemPrefab, tutorialContainer);
                menuItem.InitializeButton(i, GameManager.Instance.tutorialsList[i].tutorialPopup.header);
            }
        }
    }
}
