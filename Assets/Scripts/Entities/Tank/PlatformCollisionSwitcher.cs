using System.Collections;
using UnityEngine;

public class PlatformCollisionSwitcher : MonoBehaviour
{
    //todo: have platform detect player & turn back on collider once player has exited the collider
    private Collider2D platformCollider;
    
    private void Awake()
    {
        //if (Mathf.Approximately(transform.localRotation))
        platformCollider = GetComponent<Collider2D>();
    }

    public IEnumerator DisableCollision(Collider2D playerCollider)
    {
        Physics2D.IgnoreCollision(playerCollider, platformCollider);
        yield return new WaitForSeconds(0.5f);
        Physics2D.IgnoreCollision(playerCollider, platformCollider, false);
    }
}
