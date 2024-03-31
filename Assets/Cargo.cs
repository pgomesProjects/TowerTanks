using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cargo : MonoBehaviour
{
    public enum CargoType { SCRAP, AMMO }
    public CargoType type;

    public float amount;

    private PlayerMovement currentHolder;
    private Rigidbody2D rb;
    private BoxCollider2D box2D;
    private float initCooldown; //time it takes for collider to enable
    
    private TrailRenderer trail;
    private float trailCooldown; //time it takes for trail to disable

    public bool isOnTank; //whether or not this object is considered "on" the tank
    public LayerMask onTankMask; 

    public float throwForce;

    public void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        box2D = GetComponent<BoxCollider2D>();
        box2D.enabled = false;
        initCooldown = 0.5f;
        trail = GetComponentInChildren<TrailRenderer>();
        trailCooldown = 4f;
    }

    // Start is called before the first frame update
    void Start()
    {
        currentHolder = null;
    }

    // Update is called once per frame
    void Update()
    {
        if (currentHolder != null)
        {
            if (rb.isKinematic == false) rb.isKinematic = true;
            if (box2D.enabled == true) box2D.enabled = false;
            transform.rotation = new Quaternion(0, 0, 0, 0);
        }

        if (initCooldown > 0) initCooldown -= Time.deltaTime;
        else
        {
            if (box2D.enabled == false) box2D.enabled = true;
        }

        if (trailCooldown > 0) trailCooldown -= Time.deltaTime;
        else
        {
            if (trail.enabled == true) trail.enabled = false;
        }

        CheckForOnboard();
    }

    public void Pickup(PlayerMovement player)
    {
        currentHolder = player;
        player.isCarryingSomething = true;
        player.currentObject = this;

        GameManager.Instance.AudioManager.Play("UseSFX");
    }

    public void Drop(PlayerMovement player, bool throwing, Vector2 direction)
    {
        rb.isKinematic = false;
        box2D.enabled = true;

        if (throwing)
        {
            rb.AddForce(direction * throwForce);
            rb.AddTorque(10f);
        }

        currentHolder = null;
        player.isCarryingSomething = false;
        player.currentObject = null;

        GameManager.Instance.AudioManager.Play("ButtonCancel");
    }

    public void CheckForOnboard()
    {
        var cellCheck = Physics2D.OverlapBox(transform.position, new Vector2(0.5f, 0.5f), 0, onTankMask);
        if (cellCheck != null)
        {
            Transform newParent = cellCheck.transform.parent;
            transform.parent = newParent;
            isOnTank = true;
        }
        else
        {
            transform.parent = null;
            isOnTank = false;
        }
    }
}
