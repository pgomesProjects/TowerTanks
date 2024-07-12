using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cargo : MonoBehaviour
{
    public enum CargoType { SCRAP, AMMO, EXPLOSIVE }
    public CargoType type;

    public float amount;

    internal PlayerMovement currentHolder;
    private Rigidbody2D rb;
    private BoxCollider2D box2D;
    private CircleCollider2D circle2D;
    private float initCooldown; //time it takes for collider to enable
    
    private TrailRenderer trail;
    private float trailCooldown; //time it takes for trail to disable

    public bool isOnTank; //whether or not this object is considered "on" the tank
    public LayerMask onTankMask; 

    public float throwForce;

    public void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        switch (type) 
        {
            case CargoType.SCRAP: 
                {
                    box2D = GetComponent<BoxCollider2D>();
                    box2D.enabled = false;
                } 
                break;
            case CargoType.AMMO:
                {
                    box2D = GetComponent<BoxCollider2D>();
                    box2D.enabled = false;
                }
                break;
            case CargoType.EXPLOSIVE: 
                {
                    circle2D = GetComponent<CircleCollider2D>();
                    circle2D.enabled = false;
                }
                break;
        }
        
        initCooldown = 0.5f;
        trail = GetComponentInChildren<TrailRenderer>();
        trailCooldown = 4f;
    }

    // Start is called before the first frame update
    public void Start()
    {
        currentHolder = null;
        StartCoroutine(Initialize());
        StartCoroutine(InitializeTrail());
    }

    // Update is called once per frame
    public void Update()
    {
        if (currentHolder != null)
        {
            if (rb.isKinematic == false) rb.isKinematic = true;
            if (box2D != null && box2D.enabled == true) box2D.enabled = false;
            if (circle2D != null && circle2D.enabled == true) circle2D.enabled = false;
            transform.rotation = new Quaternion(0, 0, 0, 0);
        }

        CheckForOnboard();
    }

    private IEnumerator Initialize()
    {
        yield return new WaitForSeconds(initCooldown);
        if (box2D != null && box2D.enabled == false) box2D.enabled = true;
        if (circle2D != null && circle2D.enabled == false) circle2D.enabled = true;
    }

    private IEnumerator InitializeTrail()
    {
        yield return new WaitForSeconds(trailCooldown);
        if (trail.enabled == true) trail.enabled = false;
    }

    public void Pickup(PlayerMovement player)
    {
        if (currentHolder == null)
        {
            currentHolder = player;
            player.isCarryingSomething = true;
            player.currentObject = this;

            GameManager.Instance.AudioManager.Play("UseSFX");
        }
    }

    public void Drop(PlayerMovement player, bool throwing, Vector2 direction)
    {
        rb.isKinematic = false;
        if (box2D != null) box2D.enabled = true;
        if (circle2D != null) circle2D.enabled = true;

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

    public void Use() //called from Holder when pressing Alt
    {
        if (type == CargoType.EXPLOSIVE)
        {
            Cargo_Explosive script = GetComponent<Cargo_Explosive>();
            script.isLit = true;

            GameManager.Instance.AudioManager.Play("UseSFX", gameObject);
        }
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

    public void OnDestroy()
    {
        if (currentHolder != null)
        {
            currentHolder.isCarryingSomething = false;
            currentHolder.currentObject = null;
        }
    }
}
