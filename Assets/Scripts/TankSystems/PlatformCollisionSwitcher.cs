using System;
using System.Collections;
using UnityEngine;

public class PlatformCollisionSwitcher : MonoBehaviour
{
    //todo: have platform detect player & turn back on collider once player has exited the collider
    private Collider2D platformCollider;
    private PlatformEffector2D platformEffector;
    private CharacterLegFloater characterLegFloater; // to disable leg floater on platform dropdown
    
    private void Awake()
    {
        //if (Mathf.Approximately(transform.localRotation))
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
        var currLegFloater = playerCollider.GetComponent<CharacterLegFloater>();
        if (characterLegFloater == null || characterLegFloater != currLegFloater)
            characterLegFloater = playerCollider.GetComponent<CharacterLegFloater>();
        Physics2D.IgnoreCollision(playerCollider, platformCollider);
        characterLegFloater.SetStandOnPlatform(false); // disables leg systems as to fall through platform
        yield return new WaitForSeconds(0.5f);
        Physics2D.IgnoreCollision(playerCollider, platformCollider, false);
        characterLegFloater.SetStandOnPlatform(true);
    }
}
