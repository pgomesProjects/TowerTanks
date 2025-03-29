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

        protected virtual void Awake()
        {
            interactableController = GetComponent<TankInteractable>();
        }
        
        public virtual void Init()
        {
            //this empty method just needs to be here for now for the override call in DistributeToken() to work
        }

        public virtual void Update()
        {
            if (myTankAI?.tank != null)
            {
                if (interactableController.isBroken || interactableController.operatorID != null)
                {
                    myTankAI.pausedInteractables.Add(myInteractableID);
                    myTankAI.RetrieveToken(myInteractableID);
                }
            }
        }

        public void ReceiveToken() 
        {
            tokenActivated = true;
        }
        
        public void ReturnToken()
        {
            tokenActivated = false;
            enabled = false;
        }

        private void OnDestroy()
        {
            if (myTankAI == null) return;
            if (!myTankAI.tokenActivatedInteractables.Contains(myInteractableID)) return;
            myTankAI.RetrieveToken(myInteractableID);
            myTankAI.DistributeToken(mySpecificType);
        }
    }
}
