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
            CheckRotation();
        }

        public void CheckRotation()
        {
            if (!Mathf.Approximately(transform.eulerAngles.z, 0) && !Mathf.Approximately(transform.eulerAngles.z, 180))
            {
                Destroy(platformEffector);
                Destroy(platformCollider);
            }

            if (Mathf.Approximately(transform.eulerAngles.z, 180) && platformEffector)
            {
                platformEffector.rotationalOffset = 180;
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
