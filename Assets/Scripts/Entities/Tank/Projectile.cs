using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    //Objects & Components:
    public LayerMask layerMask;
    private Transform smokeTrail;

    //Settings:
    public float damage;  //Damage projectile will deal upon hitting a valid target
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
        if (target != null && target.GetComponentInParent<Cell>() != null)
        {
            target.GetComponentInParent<Cell>().Damage(75);
            GameManager.Instance.AudioManager.Play("ShellImpact", gameObject);
        }

        if (target != null && target.CompareTag("Destructible"))
        {
            target.GetComponent<DestructibleObject>().Damage(75);
            GameManager.Instance.AudioManager.Play("ShellImpact", gameObject);
        }

        //Effects
        GameManager.Instance.AudioManager.Play("ExplosionSFX", gameObject);
        GameManager.Instance.ParticleSpawner.SpawnParticle(Random.Range(0, 2), transform.position, 0.1f, null);

        //Seperate smoketrail
        smokeTrail.parent = null;
        Lifetime lt = smokeTrail.gameObject.AddComponent<Lifetime>();
        ParticleSystem ps = smokeTrail.gameObject.GetComponent<ParticleSystem>();
        ps.Stop();
        lt.lifeTime = 0.5f;
        Destroy(gameObject);
    }
}