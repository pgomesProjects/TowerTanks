using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    [System.Serializable]
    public class TankConsumable : TankInteractable
    {
        [Header("Consumable Properties:")]
        public Room.RoomType conversionType;

        public void Use()
        {
            //Effects
            GameManager.Instance.AudioManager.Play("TankImpact", gameObject);
            GameManager.Instance.ParticleSpawner.SpawnParticle(6, transform.position, 0.25f, null);
        }

        public void ConvertRoom(Cell target)
        {
            target.room.UpdateRoomType(conversionType);

            /*
            //Check for Interactables
            if (target.room.type == Room.RoomType.Armor || target.room.type == Room.RoomType.Cargo)
            {
                foreach (Cell cell in target.room.cells)
                {
                    if (cell.interactable != null)
                    {
                        if (cell.interactable.gameObject.GetComponent<TankConsumable>() == null)
                        {
                            StackManager.AddToStack(GameManager.Instance.TankInteractableToEnum(cell.interactable));
                            cell.interactable.DebugDestroy();
                        }
                    }
                }
            }*/

            /*
            //Cleanup
            Destroy(gameObject);*/
        }

        public virtual void OnDestroy()
        {
            base.OnDestroy();
        }
    }
}
