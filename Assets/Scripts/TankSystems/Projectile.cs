using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    //Objects & Components:
    public LayerMask layerMask;
    private Transform smokeTrail;
    public float particleScale;

    //Settings:
    public float damage;  //Damage projectile will deal upon hitting a valid target
    public bool hasSplashDamage; //Whether or not this projectile deals splash damage
    public SplashData[] splashData; //Contains all values related to different splash damage zones
    public float maxLife; //Maximum amount of time projectile can spend before it auto-destructs
    public float radius;  //Radius around projectile which is used to check for hits
    public float gravity;

    //Runtime Variables:
    private Vector2 velocity; //Speed and trajectory of projectile
    private float timeAlive;

    //RUNTIME METHODS:
    private void Awake()
    {
        smokeTrail = transform.Find("smokeTrail");
    }

    private void Update()
    {
        velocity += gravity * Time.deltaTime * Vector2.down;
        //transform.rotation = Quaternion.AngleAxis(Vector3.Angle(Vector2.right, velocity), Vector3.back);

        CheckforCollision();

        timeAlive += Time.deltaTime;
        if (timeAlive >= maxLife) Hit(null);
    }

    public void CheckforCollision()
    {
        Collider2D hit = Physics2D.OverlapCircle(transform.position, radius, layerMask);
        if (hit == null)
        {
            hit = Physics2D.CircleCast(transform.position, radius, velocity, (velocity.magnitude * Time.deltaTime), layerMask).collider;
        }
        if (hit != null)
        {
            Hit(hit);
            return;
        }
        else
        {
            Vector2 newPos = (Vector2)transform.position + (velocity * Time.deltaTime);
            transform.position = newPos;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);

        if (hasSplashDamage)
        {
            foreach (SplashData splash in splashData)
            {
                Color tempColor = Color.yellow;
                Gizmos.color = tempColor;
                Gizmos.DrawWireSphere(transform.position, splash.splashRadius);
            }
        }
    }

    private void LateUpdate()
    {
        transform.right = velocity.normalized;   //Rotate the transform in the direction of the velocity that it has
    }

    //FUNCTIONALITY METHODS:
    /// <summary>
    /// Fires projectile at given velocity from given position.
    /// </summary>
    /// <param name="velocity"></param>
    public void Fire(Vector2 position, Vector2 startVelocity)
    {
        velocity = startVelocity;
        transform.position = position;
        //transform.rotation = Quaternion.AngleAxis(Vector3.Angle(Vector2.right, velocity), Vector3.back);

        Collider2D hit = Physics2D.OverlapCircle(position, radius, layerMask);
        if (hit != null) Hit(hit);
    }

    private void Hit(Collider2D target)
    {
        List<Collider2D> hitThisFrame = new List<Collider2D>(); //Create Temp List for Colliders Hit
        hitThisFrame.Add(target); //Add Direct Hit to Collider
        bool damagedCoreThisFrame = false;

        //Handle Projectile Direct Damage
        if (target != null && target.GetComponentInParent<Cell>() != null) //Hit Cell
        {
            Cell cellHit = target.GetComponentInParent<Cell>();
            cellHit.Damage(damage);
            if (cellHit.room.isCore) damagedCoreThisFrame = true;
            GameManager.Instance.AudioManager.Play("ShellImpact", gameObject);
        }

        if (target != null && target.CompareTag("Destructible")) //Hit Destructible Object
        {
            target.GetComponent<DestructibleObject>().Damage(damage);
            GameManager.Instance.AudioManager.Play("ShellImpact", gameObject);
        }

        if (target != null && target.GetComponentInParent<Character>() != null) //Hit Character
        {
            Character character = target.GetComponentInParent<Character>();
            character.ModifyHealth(-damage);
        }

        //Handle Projectile Splash Damage
        if (hasSplashDamage) 
        {
            foreach (SplashData splash in splashData) //Handle for each individual splash zone
            {
                Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, splash.splashRadius, layerMask);
                foreach (Collider2D collider in colliders)
                {
                    if (!hitThisFrame.Contains(collider)) //If the Collider has not been damaged by any other damage sources in this event this frame
                    {
                        hitThisFrame.Add(collider); 

                        Cell cellScript = collider.gameObject.GetComponent<Cell>();
                        if (cellScript != null)
                        {
                            if (!damagedCoreThisFrame)
                            {
                                cellScript.Damage(splash.splashDamage);
                                if (cellScript.room.isCore) damagedCoreThisFrame = true;
                            }
                        }

                        if (collider.CompareTag("Destructible"))
                        {
                            collider.gameObject.GetComponent<DestructibleObject>().Damage(splash.splashDamage);
                        }

                        Character character = collider.gameObject.GetComponent<Character>();
                        if (character != null)
                        {
                            character.ModifyHealth(-splash.splashDamage);
                        }
                    }
                }
            }
        }

        hitThisFrame.Clear();

        //Effects
        GameManager.Instance.AudioManager.Play("ExplosionSFX", gameObject);
        GameManager.Instance.ParticleSpawner.SpawnParticle(Random.Range(0, 2), transform.position, particleScale, null);

        //Seperate smoketrail
        if (smokeTrail != null)
        {
            smokeTrail.parent = null;
            Lifetime lt = smokeTrail.gameObject.AddComponent<Lifetime>();
            ParticleSystem ps = smokeTrail.gameObject.GetComponent<ParticleSystem>();
            ps.Stop();
            lt.lifeTime = 0.5f;
        }

        Destroy(gameObject);
    }
}
