using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts.Deprecated
{
    public class PlayerCannonDirectionUpdater : MonoBehaviour
    {
        [SerializeField, Tooltip("The transform for the barrel.")] private Transform barrel;
        [SerializeField, Tooltip("The transform for the chair.")] private Transform chair;
        [SerializeField, Tooltip("The transform for the aimer.")] private Transform aimer;
        [SerializeField, Tooltip("The transform for the cannon pivot.")] private Transform cannonPivot;
        [SerializeField, Tooltip("The transform for the spawn point.")] private Transform spawnPoint;

        /// <summary>
        /// Flips the direction of the cannon's X position. Can either be left or right.
        /// </summary>
        public void FlipCannonX()
        {
            //Flip the entire cannon position
            transform.localPosition = FlipPositionX(transform.localPosition);

            //Flip the cannon direction
            if (TryGetComponent<PlayerCannonController>(out PlayerCannonController playerCannonController))
                if (playerCannonController.GetCannonDirection() == CANNONDIRECTION.LEFT)
                    playerCannonController.SetCannonDirection(CANNONDIRECTION.RIGHT);
                else
                    playerCannonController.SetCannonDirection(CANNONDIRECTION.LEFT);

            //Flip the sprites
            foreach (var sprite in GetComponentsInChildren<SpriteRenderer>())
                sprite.flipX = !sprite.flipX;

            //Flip the aimer position
            aimer.transform.localPosition = FlipPositionX(aimer.transform.localPosition);

            //Flip the barrel position
            barrel.transform.localPosition = FlipPositionX(barrel.transform.localPosition);

            //Flip the chair position
            chair.transform.localPosition = FlipPositionX(chair.transform.localPosition);

            //Flip the pivot position
            if (cannonPivot != null)
                cannonPivot.transform.localPosition = FlipPositionX(cannonPivot.transform.localPosition);

            //Flip the spawn point position
            if (spawnPoint != null)
                spawnPoint.transform.localPosition = FlipPositionX(spawnPoint.transform.localPosition);
        }

        /// <summary>
        /// Flips the X position of a Vector3.
        /// </summary>
        /// <param name="pos">The Vector 3 to flip the position of.</param>
        /// <returns></returns>
        private Vector3 FlipPositionX(Vector3 pos) => new Vector3(-pos.x, pos.y, pos.z);
    }
}
