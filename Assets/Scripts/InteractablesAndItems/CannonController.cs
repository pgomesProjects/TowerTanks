using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CANNONDIRECTION { LEFT, RIGHT, UP, DOWN };

public class CannonController : InteractableController
{

    [SerializeField] private int maxShells;
    [SerializeField] private float cannonSpeed;
    [SerializeField] private float lowerAngleBound, upperAngleBound;
    [SerializeField] private GameObject projectile;
    [SerializeField] private GameObject spawnPoint;
    [SerializeField] private float cannonForce = 2000;
    [SerializeField] private CANNONDIRECTION currentCannonDirection;
    [SerializeField] private Sprite unloadedCannonSprite;
    [SerializeField] private Sprite loadedCannonSprite;
    private Vector3 initialVelocity;
    private InteractableController cannonInteractable;
    [SerializeField] private GameObject chair;
    [SerializeField] private Transform cannonPivot;
    private Vector3 cannonRotation;
    private float playerCannonMovement;
    private Vector3 playerPosition;

    //private LineRenderer lineRenderer;
    private const int N_TRAJECTORY_POINTS = 10;

    private int currentAmmo;

    [SerializeField] private GameObject cSmoke;
    // Start is called before the first frame update
    void Start()
    {
        cannonInteractable = GetComponentInParent<InteractableController>();
/*        lineRenderer = GetComponentInChildren<LineRenderer>();
        lineRenderer.positionCount = N_TRAJECTORY_POINTS;*/
        currentAmmo = 0;
    }

    /// <summary>
    /// Checks to see if the player can load ammo into the cannon
    /// </summary>
    public void CheckForReload(PlayerController player)
    {
        //If the cannon can fit ammo
        if (currentAmmo < maxShells)
        {
            if(LevelManager.instance.levelPhase == GAMESTATE.TUTORIAL)
            {
                if(TutorialController.main.currentTutorialState == TUTORIALSTATE.GRABSHELL)
                {
                    LoadShell(player);
                    TutorialController.main.OnTutorialTaskCompletion();
                }
            }
            else
                LoadShell(player);
        }
    }

    private void LoadShell(PlayerController player)
    {
        //Get rid of the player's item and load
        player.DestroyItem();

        //Play sound effect
        FindObjectOfType<AudioManager>().PlayOneShot("CannonReload", PlayerPrefs.GetFloat("SFXVolume", 0.5f));
        currentAmmo++;
        UpdateCannonBody();
    }

    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (currentPlayerLockedIn == null)
            {
                canInteract = true;
                currentPlayerColliding = collision.GetComponent<PlayerController>();
                currentPlayerColliding.DisplayInteractionPrompt("<sprite=30>");
            }

            //Tell the player that this is the item that they can interact with
            collision.GetComponent<PlayerController>().currentInteractableItem = this;
        }
    }

    protected override void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (currentPlayerLockedIn == null)
            {
                if (currentPlayerColliding != null)
                    currentPlayerColliding.HideInteractionPrompt();
                canInteract = false;
                currentPlayerColliding = null;
            }

            //Player can no longer interact with this item
            collision.GetComponent<PlayerController>().currentInteractableItem = null;
        }
    }

    public void CheckForCannonFire()
    {
        //If there is ammo
        if(currentAmmo > 0 || maxShells == 0)
        {
            if(LevelManager.instance.levelPhase == GAMESTATE.TUTORIAL)
            {
                if(TutorialController.main.currentTutorialState == TUTORIALSTATE.FIRECANNON)
                {
                    //Fire the cannon
                    Fire();
                    UpdateCannonBody();

                    //Shake the camera
                    CameraEventController.instance.ShakeCamera(5f, 0.2f);

                    TutorialController.main.OnTutorialTaskCompletion();
                }
            }
            else
            {
                //Fire the cannon
                Fire();
                UpdateCannonBody();
                //Shake the camera
                CameraEventController.instance.ShakeCamera(5f, 0.2f);
            }
        }
    }

    public void Fire()
    {
        //Spawn projectile at cannon spawn point
        if (spawnPoint != null)
        {
            GameObject currentProjectile = Instantiate(projectile, new Vector3(spawnPoint.transform.position.x, spawnPoint.transform.position.y, 0), Quaternion.identity);
            currentAmmo--;

            //Play sound effect
            FindObjectOfType<AudioManager>().PlayOneShot("CannonFire", PlayerPrefs.GetFloat("SFXVolume", 0.5f));

            Instantiate(cSmoke, spawnPoint.transform.position, Quaternion.identity);

            //Determine the direction of the cannon
            Vector2 direction = Vector2.zero;
            float startingShellRot = 0;

            switch (currentCannonDirection)
            {
                case CANNONDIRECTION.LEFT:
                    direction = -GetCannonVectorHorizontal(cannonRotation.z * Mathf.Deg2Rad);
                    startingShellRot = -cannonPivot.eulerAngles.z + 90;
                    break;
                case CANNONDIRECTION.RIGHT:
                    direction = GetCannonVectorHorizontal(cannonRotation.z * Mathf.Deg2Rad);
                    startingShellRot = -cannonPivot.eulerAngles.z - 90;
                    break;
                case CANNONDIRECTION.UP:
                    direction = GetCannonVectorVertical(cannonRotation.z * Mathf.Deg2Rad);
                    break;
                case CANNONDIRECTION.DOWN:
                    direction = -GetCannonVectorVertical(cannonRotation.z * Mathf.Deg2Rad);
                    break;
            }

            //Add a damage component to the projectile
            currentProjectile.AddComponent<DamageObject>().damage = currentProjectile.GetComponent<ShellItemBehavior>().GetDamage();
            DamageObject currentDamager = currentProjectile.GetComponent<DamageObject>();
            currentDamager.StartRotation(startingShellRot);

            Debug.Log("Direction: " + direction);

            //Shoot the project at the current angle and direction
            currentProjectile.GetComponent<Rigidbody2D>().AddForce(direction * cannonForce);
        }
    }

    public IEnumerator FireAtDelay()
    {
        while (true)
        {
            //Debug.Log("Starting Fire Wait...");
            int timeWait = Random.Range(7, 12);
            yield return new WaitForSeconds(timeWait);

            Debug.Log("Enemy Fire!");
            Fire();
        }
    }

    private void UpdateCannonBody()
    {
        if (currentAmmo > 0)
        {
            if(loadedCannonSprite != null)
            {
                transform.Find("Body").GetComponent<SpriteRenderer>().sprite = loadedCannonSprite;
            }
        }
        else
        {
            if (unloadedCannonSprite != null)
            {
                transform.Find("Body").GetComponent<SpriteRenderer>().sprite = unloadedCannonSprite;
            }
        }
    }

    private Vector2 GetCannonVectorHorizontal(float radians)
    {
        //Trigonometric function to get the vector (hypotenuse) of the current cannon angle
        return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
    }

    private Vector2 GetCannonVectorVertical(float radians)
    {
        //Trigonometric function to get the vector (hypotenuse) of the current cannon angle
        return new Vector2(Mathf.Sin(radians), Mathf.Cos(radians));
    }

    private void Update()
    {
        //If the cannon is linked to an interactable
        if (cannonInteractable != null)
        {
            if (cannonInteractable.CanInteract())
            {
                //Debug.Log("Cannon Movement: " + cannonInteractable.GetCurrentPlayer().GetCannonMovement());

                playerCannonMovement = cannonInteractable.GetCurrentPlayer().GetCannonMovement();

                if(cannonInteractable.GetCurrentPlayer().IsPlayerSpinningCannon())
                {
                    if (!FindObjectOfType<AudioManager>().IsPlaying("CannonAimSFX"))
                    {
                        Debug.Log("Aiming...");
                        FindObjectOfType<AudioManager>().Play("CannonAimSFX", PlayerPrefs.GetFloat("SFXVolume", 0.5f));
                    }
                }
                else if (FindObjectOfType<AudioManager>().IsPlaying("CannonAimSFX"))
                {
                    Debug.Log("Locked In!");
                    FindObjectOfType<AudioManager>().Stop("CannonAimSFX");
                    FindObjectOfType<AudioManager>().Play("CannonLockSFX", PlayerPrefs.GetFloat("SFXVolume", 0.5f));
                }


                cannonRotation += new Vector3(0, 0, (playerCannonMovement / 100) * cannonSpeed);

                //Make sure the cannon stays within the lower and upper bound
                cannonRotation.z = Mathf.Clamp(cannonRotation.z, lowerAngleBound, upperAngleBound);

                //Rotate cannon
                cannonPivot.eulerAngles = cannonRotation;

                //lineRenderer.enabled = true;
                UpdateLineRenderer();
            }
            else
            {
                //lineRenderer.enabled = false;
            }
        }

        if(currentPlayerLockedIn != null)
        {
            Vector3 chairPos = chair.transform.position;
            //Offset to make the player match the chair
            chairPos.x += 0.34f;
            chairPos.y += 0.6f;
            playerPosition = chairPos;
            currentPlayerLockedIn.transform.position = playerPosition;
        }

        initialVelocity = new Vector3(0, 0, cannonForce) - spawnPoint.transform.position;
    }

    private void UpdateLineRenderer()
    {
        float g = Physics2D.gravity.magnitude;
        float velocity = initialVelocity.magnitude;
        float angle = Mathf.Atan2(initialVelocity.y, initialVelocity.x);

        Vector3 start = spawnPoint.transform.position;

        float timeStep = 0.1f;
        float fTime = 0f;
        for (int i = 0; i < N_TRAJECTORY_POINTS; i++)
        {
            float dx = velocity * fTime * Mathf.Cos(angle);
            float dy = velocity * fTime * Mathf.Sin(angle) - (g * fTime * fTime / 2f);
            Vector3 pos = new Vector3(start.x + dx, start.y + dy, 0);
            //lineRenderer.SetPosition(i, pos);
            fTime += timeStep;
        }
    }

    public override void LockPlayer(bool lockPlayer)
    {
        base.LockPlayer(lockPlayer);

        if (lockPlayer)
        {
            //Move the player to the chair
            Vector3 chairPos = chair.transform.position;
            //Offset to make the player match the chair
            chairPos.x += 0.34f;
            chairPos.y += 0.6f;
            playerPosition = chairPos;
            currentPlayer.GetComponent<Rigidbody2D>().gravityScale = 0f;
            currentPlayer.transform.position = playerPosition;
            chair.GetComponent<SpriteRenderer>().sortingOrder = 15;
            currentPlayer.GetComponent<Animator>().SetBool("isManningCannon", true);
        }
        else
        {
            currentPlayer.GetComponent<Animator>().SetBool("isManningCannon", false);
            currentPlayer.GetComponent<Rigidbody2D>().gravityScale = currentPlayer.GetDefaultGravity();
            chair.GetComponent<SpriteRenderer>().sortingOrder = 4;
        }
    }

    public void SetCannonDirection(CANNONDIRECTION direction)
    {
        currentCannonDirection = direction;
    }

    public void CannonLookAt(Vector3 target)
    {
        // Get angle in Radians
        float cannonAngleRad = Mathf.Atan2(target.y - cannonPivot.transform.position.y, target.x - cannonPivot.transform.position.x);
        // Get angle in Degrees
        float cannonAngleDeg = (180 / Mathf.PI) * cannonAngleRad;

        switch (currentCannonDirection)
        {
            //Flip the cannon if facing left
            case CANNONDIRECTION.LEFT:
                cannonAngleDeg += 180;
                break;
        }

        //Debug.Log("Cannon Degree: " + cannonAngleDeg);

        Quaternion lookAngle = Quaternion.Euler(0, 0, cannonAngleDeg);
        Quaternion currentAngle = Quaternion.Slerp(cannonPivot.transform.rotation, lookAngle, Time.deltaTime);

        //Clamp the angle of the cannon
        currentAngle.z = Mathf.Clamp(currentAngle.z, lowerAngleBound, upperAngleBound);

        // Rotate Object
        cannonPivot.transform.rotation = currentAngle;
        cannonRotation = cannonPivot.eulerAngles;

        //Debug.Log("Current Angle: " + cannonRotation);
    }
}
