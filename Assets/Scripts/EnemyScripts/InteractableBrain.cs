using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class InteractableBrain : MonoBehaviour //inheritors should be disabled in scene view on scene load, or won't work
    {
        [HideInInspector] public TankAI myTankAI;

        [HideInInspector] public InteractableId myInteractableID;

        [HideInInspector] public INTERACTABLE myInteractableType;

        [HideInInspector] public TankInteractable interactableController;

        
        public bool tokenActivated;

        private void Awake()
        {
            interactableController = GetComponent<TankInteractable>();
        }
        
        public void ReceiveToken() 
        {
            tokenActivated = true;
        }
        
        public void ReturnToken()
        {
            myTankAI.RetrieveToken(myInteractableID);
            enabled = false;
        }

        private void OnDestroy()
        {
            if (!myTankAI.tokenActivatedInteractables.Contains(myInteractableID)) return;
            myTankAI.RetrieveToken(myInteractableID);
            myTankAI.DistributeToken(myInteractableType);
        }
    }
}
