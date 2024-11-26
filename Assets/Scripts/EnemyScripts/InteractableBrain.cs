using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class InteractableBrain : MonoBehaviour //should be disabled in scene view on scene load, or won't work
    {
        [HideInInspector] public TankAI myTankAI;

        [HideInInspector] public InteractableId myInteractableID;

        [HideInInspector] public INTERACTABLE myInteractableType;
        //will define common functionality for all interactable AI Brains. just here to use for polymorphism for now
        
        public bool tokenActivated;
        
        private void OnDestroy()
        {
            if (!myTankAI.tokenActivatedInteractables.Contains(myInteractableID)) return;
            myTankAI.RetrieveToken(myInteractableID);
            myTankAI.DistributeToken(myInteractableType);
        }
    }
}
