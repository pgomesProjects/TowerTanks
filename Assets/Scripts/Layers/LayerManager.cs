using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LayerManager : MonoBehaviour
{
    [Header("Layer Settings")]
    [SerializeField, Tooltip("The health of the layer.")] private int health = 100;
    [SerializeField, Tooltip("The amount of resources returned when destroyed.")] private int destroyResourcesValue = 80;
    [SerializeField, Tooltip("A particle to show when the layer is damaged.")] private GameObject scrapDamage;
    [Space(10)]

    [Header("Debug Options")]
    [SerializeField, Tooltip("Deals 10 damage to the layer.")] private bool debugLayerDamage;
    [SerializeField, Tooltip("Sets the layer on fire.")] private bool debugFire;

    private float gravity;
    private int maxHealth;

    private Rigidbody2D rb;

    private bool layerInPlace = false;
    private bool layerFalling = false;

    private Transform outsideObjects;

    private void Awake()
    {
        outsideObjects = transform.Find("OutsideObjects");
        gravity = -Physics2D.gravity.y;
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        maxHealth = health;

        switch (GameSettings.difficulty)
        {
            case 0.5f:
                destroyResourcesValue = (int)(destroyResourcesValue * 1.5f);
                break;
            case 1.5f:
                destroyResourcesValue = (int)(destroyResourcesValue * 0.5f);
                break;
        }
    }

    private void Update()
    {
        if (Application.isEditor)
        {
            if (debugLayerDamage)
            {
                debugLayerDamage = false;
                DealDamage(10, false);
            }

            if (debugFire)
            {
                debugFire = false;
                CheckForFireSpawn(100);
            }
        }
    }

    private void FixedUpdate()
    {
        List<ContactPoint2D> contactPoints = new List<ContactPoint2D>();
        rb.GetContacts(contactPoints);
        string contactNames = "";
        foreach(var i in contactPoints)
        {
            contactNames += i.collider.name + " | ";
        }

        /*        if(transform.GetComponentInParent<EnemyController>() != null)
                    Debug.Log(contactNames);*/

        if (!CollisionOnBottom(contactPoints))
        {
            transform.Translate(Vector2.down * gravity * Time.deltaTime);
            if (layerInPlace)
                layerFalling = true;
        }
        else
        {
            if (layerFalling)
            {
                OnLayerCollision();
            }
            else
            {
                layerInPlace = true;
            }

            if (transform.tag == "Layer")
                KeepYAtMultiple(8);
        }
    }

    private void OnLayerCollision()
    {
        //Player tank layer collision logic
        if(transform.tag == "Layer")
        {
            Debug.Log("Player Tank Layer Collided!");
        }
        //Enemy tank layer collision logic
        else
        {
            Debug.Log("Enemy Tank Layer Collided!");
        }

        layerFalling = false;
    }

    private void KeepYAtMultiple(int multiple)
    {
        float yPos = transform.position.y / multiple;
        int closestInt = (int)Mathf.Round(yPos);
        yPos = closestInt * multiple;
        transform.position = new Vector3(transform.position.x, yPos, transform.position.z);
    }

    private bool CollisionOnBottom(List<ContactPoint2D> contactPoints)
    {
        foreach(var i in contactPoints)
        {
            if (transform.tag != "EnemyLayer" && i.collider.CompareTag("Layer") && i.collider.gameObject.transform.position.y < transform.position.y)
                return true;

            if (i.collider.CompareTag("TankBottom"))
                return true;

            if (i.collider.CompareTag("EnemyLayer") && i.collider.gameObject.transform.position.y < transform.position.y)
                return true;
        }

        return false;
    }

    public void DealDamage(int dmg, bool shakeCam)
    {
        health -= dmg;
        health = Mathf.Clamp(health, 0, maxHealth);
        //Instantiate(scrapDamage, transform.position, Quaternion.identity);

        if (transform.CompareTag("Layer"))
            Debug.Log("Layer " + (GetComponentInChildren<LayerTransitionManager>().GetNextLayerIndex() - 1) + " Health: " + health);

        if (shakeCam)
            CameraEventController.instance.ShakeCamera(5f, 0.1f);

        FindObjectOfType<AudioManager>().Play("TankImpact", gameObject);

        //Check to see if the layer's diegetics need to be updated
        if (GetComponentInChildren<DamageDiegeticController>() != null)
        {
            float damagePercent = (float)health / maxHealth;
            GetComponentInChildren<DamageDiegeticController>().UpdateDiegetic(damagePercent);
        }

        //Check to see if the layer will be destryoed
        CheckForDestroy();
    }

    public void DealDamage(int dmg, bool shakeCam, float shakeIntensity)
    {
        health -= dmg;

        if (transform.CompareTag("Layer"))
        {
            Debug.Log("Layer " + (GetComponentInChildren<LayerTransitionManager>().GetNextLayerIndex() - 1) + " Health: " + health); ;
        }

        if (shakeCam)
            CameraEventController.instance.ShakeCamera(shakeIntensity, 0.1f);

        FindObjectOfType<AudioManager>().Play("TankImpact");

        UpdateLayerDamageDiegetic();

        //Check to see if the layer will be destryoed
        CheckForDestroy();
    }

    private void UpdateLayerDamageDiegetic()
    {
        //Check to see if the layer's diegetics need to be updated
        if (GetComponentInChildren<DamageDiegeticController>() != null)
        {
            float damagePercent = (float)health / maxHealth;
            GetComponentInChildren<DamageDiegeticController>().UpdateDiegetic(damagePercent);
        }
    }

    public int GetLayerHealth()
    {
        return health;
    }

    public int GetMaxHealth()
    {
        return maxHealth;
    }

    public void RepairLayer(int repair)
    {
        health += repair;
        health = Mathf.Clamp(health, 0, maxHealth);
        UpdateLayerDamageDiegetic();
    }

    public void ShowOutsideObjects(bool showObjects)
    {
        foreach (Transform obj in outsideObjects.transform)
            obj.gameObject.SetActive(showObjects);
    }

    public void CheckForFireSpawn(float chanceToCatchFire)
    {
        //If the layer can catch fire
        if(GetComponentInChildren<FireBehavior>(true) != null)
        {
            Debug.Log("Checking For Fire...");

            //If the layer is already on fire, ignore everything else
            if (GetComponentInChildren<FireBehavior>() != null && GetComponentInChildren<FireBehavior>().IsLayerOnFire())
                return;

            float percent = Random.Range(0, 100);

            chanceToCatchFire *= GameSettings.difficulty;

            //If the chance to catch fire has been met, turn on the fire diegetics
            if (percent < chanceToCatchFire)
            {
                transform.Find("FireDiegetics").gameObject.SetActive(true);
            }
        }
    }

    private void CheckForDestroy()
    {
        //If the health of the layer is less than or equal to 0, destroy self
        if (health <= 0)
        {
            if (transform.CompareTag("Layer"))
            {
                //Remove the layer from the total number of layers
                LevelManager.instance.totalLayers--;

                //Adjust the tank accordingly
                LevelManager.instance.AdjustLayerSystem((GetComponentInChildren<LayerTransitionManager>().GetNextLayerIndex() - 1));

                //Add to player resources
                LevelManager.instance.UpdateResources(destroyResourcesValue);

                //Shake the camera
                CameraEventController.instance.ShakeCamera(10f, 1f);
            }

            //If an enemy layer is destroyed, tell the tank that the layer was destroyed
            else if (transform.CompareTag("EnemyLayer"))
            {
                GetComponentInParent<EnemyController>().EnemyLayerDestroyed();
            }

            FindObjectOfType<AudioManager>().Play("LargeExplosionSFX", gameObject);

            //Destroy the layer
            Destroy(gameObject);
        }
    }

    public void UnlockAllInteractables()
    {
        foreach(var i in GetComponentsInChildren<InteractableController>())
        {
            if (i.IsInteractionActive())
            {
                i.UnlockAllPlayers();
            }
        }
    }

    public Transform GetCannons() => transform.Find("Cannons");
    public Transform GetDrills() => transform.Find("Drills");

    public void HideCannons()
    {
        foreach (var cannon in GetCannons().GetComponentsInChildren<Transform>())
            cannon.gameObject.SetActive(false);
    }

    public void HideDrills()
    {
        foreach (var drill in GetDrills().GetComponentsInChildren<Transform>())
            drill.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if(transform.GetComponentInParent<PlayerTankController>() != null)
        {
            //Update the list of layers accordingly
            transform.GetComponentInParent<PlayerTankController>().AdjustLayersInList();
            FindObjectOfType<AudioManager>().Play("MedExplosionSFX", gameObject);
        }
    }
}
