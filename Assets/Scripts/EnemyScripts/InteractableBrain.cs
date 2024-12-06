using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace TowerTanks.Scripts
{
    public class InteractableBrain : MonoBehaviour //inheritors should be disabled in scene view on scene load, or won't work
    {
        [HideInInspector] public TankAI myTankAI;

        [HideInInspector] public InteractableId myInteractableID;

        public INTERACTABLE mySpecificType;

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
            myTankAI.DistributeToken(mySpecificType);
        }
    }
}
