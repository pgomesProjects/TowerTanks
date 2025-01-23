using System;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class ReadyUpManager : MonoBehaviour
    {
        [SerializeField] private ReadyComponent readyComponentPrefab;
        [SerializeField] private RectTransform readyComponentParent;

        private CanvasGroup canvasGroup;
        private List<ReadyComponent> playerReadyComponents;
        private bool canReady;
        private bool allReady;

        public static Action OnAllReady;

        // Start is called before the first frame update
        void Start()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0;
            allReady = false;
            playerReadyComponents = new List<ReadyComponent>();
        }

        public void Init()
        {
            //Clear the parent if it has anything in it
            ClearReadyComponents();

            PlayerData[] playerData = GameManager.Instance.MultiplayerManager.GetAllPlayers();

            for (int i = 0; i < playerData.Length; i++)
            {
                ReadyComponent newComponent = Instantiate(readyComponentPrefab, readyComponentParent);
                newComponent.UpdatePlayerNumber(playerData[i].playerInput.playerIndex);
                newComponent.UpdateReadyStatus(false);
                playerReadyComponents.Add(newComponent);
            }

            canvasGroup.alpha = 1;
            canReady = true;
        }

        private void ClearReadyComponents()
        {
            foreach (ReadyComponent readyComponent in playerReadyComponents)
                Destroy(readyComponent.gameObject);

            playerReadyComponents.Clear();
        }

        public void HideReadyUpManager()
        {
            ClearReadyComponents();
            canvasGroup.alpha = 0;
            canReady = false;
        }

        public void ReadyPlayer(int playerIndex, bool isReady = true)
        {
            if (GameManager.Instance.InGameMenu)
                return;

            if (allReady || !canReady)
                return;

            playerReadyComponents[playerIndex].UpdateReadyStatus(isReady);

            if (IsAllReady())
                StartReadySequence();
        }

        public void StartReadySequence()
        {
            allReady = true;
            OnAllReady?.Invoke();
        }

        public bool IsAllReady()
        {
            foreach (ReadyComponent playerComponent in playerReadyComponents)
            {
                if (!playerComponent.IsReady())
                    return false;
            }
            return true;
        }

        public bool IsPlayerReady(int playerIndex)
        {
            return playerReadyComponents[playerIndex].IsReady();
        }
    }
}
