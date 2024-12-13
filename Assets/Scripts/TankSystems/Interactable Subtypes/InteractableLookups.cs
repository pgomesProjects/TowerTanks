using System;
using System.Collections.Generic;

namespace TowerTanks.Scripts
{
    public static class InteractableLookups
    {
        /// <summary>
        /// Returns the type of brain script that associates with the interactable
        /// (should match up with the script on the prefab for that interactable)
        /// </summary>
        public static Dictionary<INTERACTABLE, Type> enumToBrainMap = new()
        {
            {INTERACTABLE.Cannon, typeof(SimpleCannonBrain)},
            {INTERACTABLE.Mortar, typeof(SimpleMortarBrain)},
            {INTERACTABLE.MachineGun, typeof(SimpleMachineGunBrain)},
            {INTERACTABLE.Throttle, typeof(InteractableBrain)},
            {INTERACTABLE.Boiler, typeof(BoilerBrain)}
        };

        /// <summary>
        /// Returns a list of interactables that belong to the group parameter
        /// </summary>
        public static Dictionary<TankInteractable.InteractableType, List<INTERACTABLE>> typesInGroup =
            new()
            {
                {
                    TankInteractable.InteractableType.WEAPONS, new List<INTERACTABLE>
                    {
                        INTERACTABLE.Cannon,
                        INTERACTABLE.Mortar,
                        INTERACTABLE.MachineGun
                    }
                },
                {
                    TankInteractable.InteractableType.ENGINEERING, new List<INTERACTABLE>
                    {
                        INTERACTABLE.Throttle,
                        INTERACTABLE.Boiler
                    }
                },
                {
                    TankInteractable.InteractableType.DEFENSE, new List<INTERACTABLE>
                    {
                        INTERACTABLE.EnergyShield
                    }
                }
            };
        /// <summary>
        /// Will return a specific interactable's classification (ex: Cannon returns as type WEAPONS)
        /// </summary>
        public static Dictionary<INTERACTABLE, TankInteractable.InteractableType> typeToGroupMap = new()
        {
            {INTERACTABLE.Cannon, TankInteractable.InteractableType.WEAPONS},
            {INTERACTABLE.Mortar, TankInteractable.InteractableType.WEAPONS},
            {INTERACTABLE.MachineGun, TankInteractable.InteractableType.WEAPONS},
            {INTERACTABLE.Throttle, TankInteractable.InteractableType.ENGINEERING},
            {INTERACTABLE.Boiler, TankInteractable.InteractableType.ENGINEERING},
            {INTERACTABLE.EnergyShield, TankInteractable.InteractableType.DEFENSE}
        };
    }
}
