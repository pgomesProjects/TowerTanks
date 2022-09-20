using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CannonController : MonoBehaviour
{

    [SerializeField] private GameObject projectile;
    [SerializeField] private GameObject spawnPoint;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void Fire(){
        GameObject currentProjectile = Instantiate(projectile, new Vector3(spawnPoint.transform.position.x, spawnPoint.transform.position.y, 0), Quaternion.identity);
        currentProjectile.GetComponent<Rigidbody2D>().AddForce(Vector2.right * 2000);
    }

}
