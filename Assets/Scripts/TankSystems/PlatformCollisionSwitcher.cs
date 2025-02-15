using System;
using System.Collections;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class PlatformCollisionSwitcher : MonoBehaviour
    {
        //todo: have platform detect player & turn back on collider once player has exited the collider
        private Collider2D platformCollider;
        private PlatformEffector2D platformEffector;

        private void Awake()
        {
            platformCollider = GetComponent<Collider2D>();
            platformEffector = GetComponent<PlatformEffector2D>();
        }

        private void Start()
        {
            if (transform.localEulerAngles.z != 0)
            {
                Destroy(platformEffector);
                Destroy(platformCollider);
            }
        }

        public IEnumerator DisableCollision(Collider2D playerCollider)
        {
            //Debug.Log("Disabling collision");
            CharacterLegFloater legFloater = playerCollider.GetComponent<CharacterLegFloater>();
            if (legFloater != null) legFloater.DisableFloater(true);
            Physics2D.IgnoreCollision(playerCollider, platformCollider);
            yield return new WaitForSeconds(0.5f);
            if (legFloater != null) legFloater.DisableFloater(false);
            Physics2D.IgnoreCollision(playerCollider, platformCollider, false);
        }
    }
}
