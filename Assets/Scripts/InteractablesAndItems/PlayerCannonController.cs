using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCannonController : CannonController
{
    [SerializeField, Tooltip("The number of seconds that the player must wait in between firing shots.")] private float cannonCooldown;

    [SerializeField, Tooltip("The chair GameObject that the player sits in.")] private GameObject chair;
    [SerializeField, Tooltip("The sprite for the cannon when it is unloaded.")] private Sprite unloadedCannonSprite;
    [SerializeField, Tooltip("The sprite for the cannon when it is loaded.")] private Sprite loadedCannonSprite;

    private InteractableController cannonInteractable;  //The interactable component of the cannon

    private float playerCannonMovement; //The movement of the player's joystick to measure the cannon rotation
    private Vector3 playerPosition; //The position of the player interacting with the cannon

    private bool cannonReady;   //If true, the player cannon is ready to fire. If false, it cannot be fired.
    private float currentCooldown;  //The current cooldown for the player cannon

    private LineRenderer lineRenderer;  //The line renderer of the cannon to show its trajectory
    private const int N_TRAJECTORY_POINTS = 10; //The number of points to show on the line renderer

    // Start is called before the first frame update
    void Start()
    {
        cannonInteractable = GetComponentInParent<InteractableController>();

        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = N_TRAJECTORY_POINTS;

        cannonReady = true;
    }

    /// <summary>
    /// Tries to fire the cannon when used.
    /// </summary>
    public override void OnUseInteractable()
    {
        if (IsInteractionActive() && firstInteractionComplete)
            CheckForCannonFire();

        base.OnUseInteractable();
    }

    /// <summary>
    /// Checks the cannon to see if it can be fired.
    /// </summary>
    public void CheckForCannonFire()
    {
        //If there is ammo
        if (cannonReady)
        {
            //Fire the cannon
            Fire();
            cannonReady = false;
            UpdateCannonBody();

            CameraEventController.instance.ShakeCamera(5f, 0.2f);   //Shake the camera

            if (LevelManager.instance.levelPhase == GAMESTATE.TUTORIAL)
            {
                if (TutorialController.main.currentTutorialState == TUTORIALSTATE.FIRECANNON)
                    TutorialController.main.OnTutorialTaskCompletion();
            }
            else
                currentCooldown = cannonCooldown;   //Reset cooldown
        }
    }

    /// <summary>
    /// Updates the cannon body sprite depending on the state it is in.
    /// </summary>
    private void UpdateCannonBody()
    {
        if (cannonReady)
        {
            if (loadedCannonSprite != null)
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

    // Update is called once per frame
    void Update()
    {
        //If the cannon is linked to an interactable
        if (cannonInteractable != null)
        {
            //If there is a player locked into the cannon, use it
            if (cannonInteractable.GetLockedInPlayer() != null)
            {
                //Debug.Log("Cannon Movement: " + cannonInteractable.GetCurrentPlayer().GetCannonMovement());

                playerCannonMovement = cannonInteractable.GetLockedInPlayer().GetCannonMovement();  //Check for cannon movement

                //If the player is spinning the cannon, play the aim sound effect
                if (cannonInteractable.GetLockedInPlayer().IsPlayerSpinningCannon())
                {
                    if (!FindObjectOfType<AudioManager>().IsPlaying("CannonAimSFX", gameObject))
                    {
                        Debug.Log("Aiming...");
                        FindObjectOfType<AudioManager>().Play("CannonAimSFX", gameObject);
                    }
                }
                //If the player is not spinning the cannon and the aim sound is playing, play the locked in sound effect
                else if (FindObjectOfType<AudioManager>().IsPlaying("CannonAimSFX", gameObject))
                {
                    Debug.Log("Locked In!");
                    FindObjectOfType<AudioManager>().Stop("CannonAimSFX", gameObject);
                    FindObjectOfType<AudioManager>().Play("CannonLockSFX", gameObject);
                }

                cannonRotation += new Vector3(0, 0, (playerCannonMovement / 100) * cannonSpeed);    //Rotate the cannon

                //Make sure the cannon stays within the lower and upper bound
                if(lowerAngleBound > upperAngleBound)
                    cannonRotation.z = Mathf.Clamp(cannonRotation.z, upperAngleBound, lowerAngleBound);
                else
                    cannonRotation.z = Mathf.Clamp(cannonRotation.z, lowerAngleBound, upperAngleBound);

                //Rotate cannon
                cannonPivot.eulerAngles = cannonRotation;

                //Show the line renderer if the difficulty is easy
                if (GameSettings.difficulty == 0.5f)
                {
                    lineRenderer.enabled = true;
                    UpdateLineRenderer();
                }

                CheckForTargetInTrajectory("EnemyLayer");   //Check to see if the player can hit the enemy
            }
            //If there is no player locked in, do not show the line renderer
            else if (GameSettings.difficulty == 0.5f)
                lineRenderer.enabled = false;
        }

        CheckForCooldown(); //Always check the cannon's cooldown
    }

    /// <summary>
    /// Increments the player cannon's cooldown.
    /// </summary>
    private void CheckForCooldown()
    {
        if (LevelManager.instance.levelPhase != GAMESTATE.TUTORIAL)
        {
            //If there is a cooldown, reduce the cooldown gradually
            if (currentCooldown > 0)
            {
                currentCooldown -= Time.deltaTime;
            }
            //If there is no ammo, add ammo automatically and update the sprite
            else if (!cannonReady)
            {
                cannonReady = true;
                FindObjectOfType<AudioManager>().Play("CannonReload", gameObject);
                UpdateCannonBody();
            }
        }
    }

    /// <summary>
    /// Overrides the LockPlayer logic of the interactable.
    /// </summary>
    /// <param name="currentPlayer">The current player locked in.</param>
    /// <param name="lockPlayer">If true, the player should be locked in. If false, the player should be unlocked.</param>
    public override void LockPlayer(PlayerController currentPlayer, bool lockPlayer)
    {
        base.LockPlayer(currentPlayer, lockPlayer);

        if (lockPlayer)
            MovePlayerToChair(currentPlayer, true);
        else
            MovePlayerToChair(currentPlayer, false);
    }

    /// <summary>
    /// Moves the player into the cannon chair.
    /// </summary>
    /// <param name="playerController">The player to move into the cannon chair.</param>
    /// <param name="moveIntoChair">If true, move the player into the chair. If false, get the player out of the chair.</param>
    private void MovePlayerToChair(PlayerController playerController, bool moveIntoChair)
    {
        //If the player is supposed to be in the chair, move them into the chair
        if (moveIntoChair)
        {
            //Forcefully flip the player depending on the direction of the cannon
            bool flipPlayer = currentCannonDirection == CANNONDIRECTION.LEFT ? true : false;
            currentPlayer.GetComponent<SpriteRenderer>().flipX = flipPlayer;

            Vector3 chairPos = chair.transform.position;
            //Offset to make the player match the chair
            if(flipPlayer)
                chairPos.x = chair.transform.position.x - 0.34f;
            else
                chairPos.x = chair.transform.position.x + 0.34f;
            chairPos.y = chair.transform.position.y + 0.6f;
            playerPosition = chairPos;
            currentPlayer.transform.position = playerPosition;

            //Remove gravity so that the player stays still
            currentPlayer.GetComponent<Rigidbody2D>().gravityScale = 0f;

            chair.GetComponent<SpriteRenderer>().sortingOrder = 15; //Move the chair in front of the player

            playerController.GetComponent<Animator>().SetBool("isManningCannon", true); //Adjust animation state
        }
        //If not, move them out of the chair
        else
        {
            currentPlayer.GetComponent<Animator>().SetBool("isManningCannon", false);   //Adjust animation state

            currentPlayer.GetComponent<Rigidbody2D>().gravityScale = currentPlayer.GetDefaultGravity();

            chair.GetComponent<SpriteRenderer>().sortingOrder = 4;  //Move the chair behind the player
        }
    }

    /// <summary>
    /// Updates the line renderer component on the cannon to show the trajectory of the projectile.
    /// </summary>
    private void UpdateLineRenderer()
    {
        UpdateInitialVelocity();

        float g = Physics2D.gravity.magnitude * projectile.GetComponent<Rigidbody2D>().gravityScale;

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
            lineRenderer.SetPosition(i, pos);
            fTime += timeStep;
        }
    }
}
