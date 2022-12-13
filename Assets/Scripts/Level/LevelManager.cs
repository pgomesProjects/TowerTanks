using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;

public enum TANKSPEED
{
    REVERSEFAST, REVERSE, STATIONARY, FORWARD, FORWARDFAST
}

public enum GAMESTATE
{
    TUTORIAL, GAMEACTIVE, GAMEOVER
}

public class LevelManager : MonoBehaviour
{
    [SerializeField] private GameObject levelFader;
    [SerializeField] private GameObject gameOverCanvas;
    [SerializeField] private GameObject pauseGameCanvas;
    [SerializeField] private GameObject sessionStatsCanvas;
    [SerializeField] private GameObject goPrompt;
    [SerializeField] private GameObject playerTank;
    [SerializeField] private Transform layerParent;
    [SerializeField] private GameObject layerPrefab;
    [SerializeField] private GameObject ghostLayerPrefab;
    [SerializeField] private GameObject tutorialPopup;
    [SerializeField] private TextMeshProUGUI resourcesDisplay;
    [SerializeField] private DialogEvent tutorialEvent;

    public static LevelManager instance;

    internal bool isPaused;
    internal bool readingTutorial;
    internal GAMESTATE levelPhase = GAMESTATE.TUTORIAL; //Start the game with the tutorial
    private int currentPlayerPaused;
    internal int currentRound;
    internal int totalLayers;
    internal SessionStats currentSessionStats;
    internal bool isSteering;
    internal float gameSpeed;
    internal int speedIndex;
    internal float[] currentSpeed = { -1.5f, -1f, 0f, 1, 1.5f };
    internal bool isSettingUpOnStart;
    private int resourcesNum = 2000;
    private GameObject currentGhostLayer;

    private Dictionary<string, int> itemPrice;

    private void Awake()
    {
        instance = this;
        levelFader.SetActive(true);
        isPaused = false;
        readingTutorial = false;
        currentPlayerPaused = -1;
        totalLayers = 0;
        currentRound = 0;
        isSteering = false;
        speedIndex = (int)TANKSPEED.FORWARD;
        gameSpeed = currentSpeed[speedIndex];
        resourcesDisplay.text = resourcesNum.ToString();
        itemPrice = new Dictionary<string, int>();
        PopulateItemDictionary();
        TutorialController.main.dialogEvent = tutorialEvent;
        currentSessionStats = ScriptableObject.CreateInstance<SessionStats>();
    }

    private void Start()
    {
        isSettingUpOnStart = true;
        if (GameSettings.skipTutorial)
        {
            resourcesNum = 1000;
            resourcesDisplay.text = resourcesNum.ToString();
            UpdateSpeed((int)TANKSPEED.STATIONARY);
            TransitionGameState();

            AddLayer();
            AddLayer();
        }
        else
        {
            GameObject.FindGameObjectWithTag("Resources").gameObject.SetActive(false);
            UpdateSpeed((int)TANKSPEED.STATIONARY);
        }

        gameSpeed = currentSpeed[speedIndex];
        isSettingUpOnStart = false;
    }

    /// <summary>
    /// Determines the price of the items the player can buy
    /// </summary>
    private void PopulateItemDictionary()
    {
        //Buy
        itemPrice.Add("NewLayer", 250);
    }

    public void UpdateSpeed(int speedUpdate)
    {
        speedIndex = speedUpdate;
        gameSpeed = currentSpeed[speedIndex];

        Debug.Log("Tank Speed: " + gameSpeed);

        playerTank.GetComponent<PlayerTankController>().UpdateTreadsSFX();

        //Update the enemy speed comparative to the player
        foreach (var i in FindObjectsOfType<EnemyController>())
        {
            i.UpdateEnemySpeed();
        }
    }

    public void UpdateSpeed(float speed)
    {
        gameSpeed = speed;

        //Update the enemy speed comparative to the player
        foreach (var i in FindObjectsOfType<EnemyController>())
        {
            i.UpdateEnemySpeed();
        }
    }

    public void UpdateResources(int resources)
    {
        int originalValue = resourcesNum;
        resourcesNum += resources;

        //Display the resources in a fancy way
        StartCoroutine(ResourcesTextAnimation(originalValue, resourcesNum, 2));
    }

    public bool CanPlayerAfford(int price)
    {
        if (resourcesNum >= price)
            return true;
        return false;
    }

    public bool CanPlayerAfford(string itemName)
    {
        if (resourcesNum >= itemPrice[itemName])
            return true;
        return false;
    }

    private IEnumerator ResourcesTextAnimation(int startingVal, int endingVal, float seconds)
    {
        float elapsedTime = 0;
        while (elapsedTime < seconds)
        {
            resourcesDisplay.text = Mathf.RoundToInt(Mathf.Lerp(startingVal, endingVal, elapsedTime / seconds)).ToString();
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        resourcesDisplay.text = endingVal.ToString();
    }

    public void AddCoalToTank(CoalController coalController, float amount)
    {
        //Add a percentage of the necessary coal to the furnace
        Debug.Log("Coal Has Been Added To The Furnace!");
        coalController.AddCoal(amount);
    }

    public void PurchaseLayer()
    {
        //Purchase a layer
        UpdateResources(-itemPrice["NewLayer"]);
        AddLayer();
        RemoveGhostLayer();
    }

    private void AddLayer()
    {
        //Spawn a new layer and adjust it to go inside of the tank parent object
        GameObject newLayer = Instantiate(layerPrefab);
        newLayer.transform.parent = layerParent;
        newLayer.transform.localPosition = new Vector2(0, totalLayers * 8);

        if(totalLayers > 2)
        {
            playerTank.transform.Find("TankFollowTop").transform.localPosition = new Vector2(-13, (totalLayers * 8) + 4);
        }
        else
        {
            playerTank.transform.Find("TankFollowTop").transform.localPosition = new Vector2(-13, 12);
        }

        //Add to the total number of layers and give the new layer an index
        totalLayers++;
        newLayer.name = "TANK LAYER " + totalLayers;
        newLayer.GetComponentInChildren<LayerManager>().SetLayerIndex(totalLayers + 1);

        //Add layer to the list of layers
        playerTank.GetComponent<PlayerTankController>().GetLayers().Insert(totalLayers - 1, newLayer.GetComponent<LayerHealthManager>());

        if (!isSettingUpOnStart)
        {
            //Check interactables on layer
            CheckInteractablesOnLayer(totalLayers);
            //Play sound effect
            FindObjectOfType<AudioManager>().PlayOneShot("UseSFX", PlayerPrefs.GetFloat("SFXVolume", 0.5f));
        }

        //Adjust the weight of the tank
        playerTank.GetComponent<PlayerTankController>().AdjustTankWeight(totalLayers);

        if (levelPhase == GAMESTATE.TUTORIAL)
        {
            if (TutorialController.main.currentTutorialState == TUTORIALSTATE.BUILDLAYERS && totalLayers >= 2)
            {
                //Tell tutorial that task is complete
                TutorialController.main.OnTutorialTaskCompletion();
            }
        }

        if (totalLayers > currentSessionStats.maxHeight)
            currentSessionStats.maxHeight = totalLayers;
    }

    public void AddGhostLayer()
    {
        //Spawn a new layer and adjust it to go inside of the tank parent object
        currentGhostLayer = Instantiate(ghostLayerPrefab);
        currentGhostLayer.transform.parent = playerTank.transform;
        currentGhostLayer.transform.localPosition = new Vector2(0, totalLayers * 8);
    }

    public void HideGhostLayer()
    {
        if(currentGhostLayer != null)
        {
            currentGhostLayer.SetActive(false);
        }
    }

    public void RemoveGhostLayer()
    {
        Destroy(currentGhostLayer);
    }

    public void CheckInteractablesOnLayer(int index)
    {
        Debug.Log("Checking Layer " + (index - 1) + " For Interactables");

        if(index - 1 >= 0)
        {
            //Check the interactable spawners
            foreach (var i in playerTank.GetComponent<PlayerTankController>().GetLayerAt(index - 1).GetComponentsInChildren<InteractableSpawner>())
            {
                //If there is not an interactable spawned, show the ghost interactables
                if (!i.IsInteractableSpawned())
                {
                    if (i.transform.position.x < 0)
                        i.SetCurrentGhostIndex(1);

                    FindObjectOfType<InteractableSpawnerManager>().ShowNewGhostInteractable(i);
                }
            }
        }
    }

    public void DestroyGhostInteractables(int index)
    {
        Debug.Log("Destroy Ghosts On Index " + (index));

        if(index >= 0)
        {
            //Check the interactable spawners
            foreach (var i in playerTank.GetComponent<PlayerTankController>().GetLayerAt(index).GetComponentsInChildren<InteractableSpawner>())
            {
                //If there is an interactable spawned
                if (i.IsInteractableSpawned())
                {
                    //If there is a ghost interactable, destroy it
                    if (i.transform.GetChild(1).CompareTag("GhostObject"))
                    {
                        Destroy(i.transform.GetChild(1).gameObject);
                    }
                }
            }
        }
    }

    public void PauseToggle(int playerIndex)
    {
        //Debug.Log("Pausing: " + isPaused);

        //If the game is not paused, pause the game
        if (isPaused == false)
        {
            Time.timeScale = 0;
            FindObjectOfType<AudioManager>().PauseAllSounds();
            currentPlayerPaused = playerIndex;
            isPaused = true;
            pauseGameCanvas.SetActive(true);
            pauseGameCanvas.GetComponent<PauseController>().UpdatePauseText(playerIndex);
            InputForOtherPlayers(currentPlayerPaused, true);
        }
        //If the game is paused, resume the game if the person that paused the game unpauses
        else if (isPaused == true)
        {
            if (playerIndex == currentPlayerPaused)
            {
                EventSystem.current.SetSelectedGameObject(null);
                Time.timeScale = 1;
                FindObjectOfType<AudioManager>().ResumeAllSounds();
                isPaused = false;
                pauseGameCanvas.SetActive(false);
                InputForOtherPlayers(currentPlayerPaused, false);
                currentPlayerPaused = -1;
            }
        }
    }

    private void InputForOtherPlayers(int currentActivePlayer, bool disableInputForOthers)
    {
        Debug.Log("Current Active Player: " + (currentActivePlayer + 1));

        foreach (var player in FindObjectsOfType<PlayerController>())
        {
            if (player.GetPlayerIndex() != currentActivePlayer)
            {
                Debug.Log(player.GetPlayerIndex() + 1);

                //Disable other player input
                if (disableInputForOthers)
                {
                    Debug.Log("Deactivating Player " + (player.GetPlayerIndex() + 1) + " Controller Input...");
                    player.GetComponent<PlayerInput>().actions.Disable();
                }
                //Enable other player input
                else
                {
                    Debug.Log("Activating Player " + (player.GetPlayerIndex() + 1) + " Controller Input...");
                    player.GetComponent<PlayerInput>().actions.Enable();
                }
            }
            else
            {
                //Make sure the current player's action asset is tied to the EventSystem so they can use the menus
                EventSystem.current.GetComponent<InputSystemUIInputModule>().actionsAsset = player.GetComponent<PlayerInput>().actions;
            }
        }
    }

    private void Update()
    {
        /*        foreach (var player in FindObjectsOfType<PlayerController>())
                {
                    Debug.Log("Player " + (player.GetPlayerIndex() + 1) + " Action Map: " + player.GetComponent<PlayerInput>().currentActionMap.name);
                }*/
    }

    public void AdjustLayerSystem(int destroyedLayer)
    {
        //If there are no more layers, the game is over
        if (totalLayers == 0)
        {
            Debug.Log("Tank Is Destroyed!");
            //Destroy the tank
            Destroy(GameObject.FindGameObjectWithTag("PlayerTank"));
            //Switch from gameplay to game over
            TransitionGameState();
            return;
        }

        //Adjust the layer numbers for the layers above the one that got destroyed
        foreach (var i in playerTank.GetComponentsInChildren<LayerHealthManager>())
        {
            int nextLayerIndex = i.GetComponentInChildren<LayerManager>().GetNextLayerIndex();

            if (nextLayerIndex > destroyedLayer + 1)
            {
                i.GetComponentInChildren<LayerManager>().SetLayerIndex(nextLayerIndex - 1);
                i.UnlockAllInteractables();
            }
        }

        //Adjust the top view of the tank
        playerTank.transform.Find("TankFollowTop").transform.localPosition = new Vector2(-13, (totalLayers * 8) + 4);

        //Adjust the ghost layer if active
        if (currentGhostLayer != null)
        {
            currentGhostLayer.transform.localPosition = new Vector2(0, totalLayers * 8);
        }

        //Adjust the players's layer numbers
        foreach (var player in GameObject.FindGameObjectsWithTag("Player"))
        {
            //If the player is on or above the destroyed layer
            if(player.GetComponent<PlayerController>().currentLayer >= destroyedLayer)
            {
                //If the player is not on the outside of the tank
                if(player.GetComponent<PlayerController>().currentLayer != totalLayers)
                {
                    //Decrement the layer number
                    player.GetComponent<PlayerController>().currentLayer--;
                }
            }
        }

        //Adjust the weight of the tank
        playerTank.GetComponent<PlayerTankController>().AdjustTankWeight(totalLayers);
    }

    public void ShowGoPrompt()
    {
        if(goPrompt != null)
            goPrompt.SetActive(true);
    }

    public void HideGoPrompt()
    {
        if (goPrompt != null)
            goPrompt.SetActive(false);
    }

    public void TransitionGameState()
    {
        switch (levelPhase)
        {
            //Tutorial to Gameplay
            case GAMESTATE.TUTORIAL:
                levelPhase = GAMESTATE.GAMEACTIVE;
                tutorialPopup.SetActive(false);
                readingTutorial = false;
                GameObject.FindGameObjectWithTag("PlayerTank").GetComponent<PlayerTankController>().ResetTankDistance();
                break;
            //Gameplay to Game Over
            case GAMESTATE.GAMEACTIVE:
                levelPhase = GAMESTATE.GAMEOVER;
                GameOver();
                break;
        }
    }

    public void StartCombatMusic(int layers)
    {

        if (layers >= 7)
        {
            if (!FindObjectOfType<AudioManager>().IsPlaying("CombatLayer3"))
                FindObjectOfType<AudioManager>().Play("CombatLayer3", PlayerPrefs.GetFloat("BGMVolume", 0.5f));
        }

        else if(layers >= 5)
        {
            if (!FindObjectOfType<AudioManager>().IsPlaying("CombatLayer2"))
                FindObjectOfType<AudioManager>().Play("CombatLayer2", PlayerPrefs.GetFloat("BGMVolume", 0.5f));
        }

        else if (layers >= 3)
        {
            if (!FindObjectOfType<AudioManager>().IsPlaying("CombatLayer1"))
                FindObjectOfType<AudioManager>().Play("CombatLayer1", PlayerPrefs.GetFloat("BGMVolume", 0.5f));
        }

        else
        {
            if (!FindObjectOfType<AudioManager>().IsPlaying("CombatLayer0"))
                FindObjectOfType<AudioManager>().Play("CombatLayer0", PlayerPrefs.GetFloat("BGMVolume", 0.5f));
        }
    }

    public void StopCombatMusic()
    {
        if (FindObjectOfType<AudioManager>() != null)
        {
            if (FindObjectOfType<AudioManager>().IsPlaying("CombatLayer0"))
                FindObjectOfType<AudioManager>().Stop("CombatLayer0");

            if (FindObjectOfType<AudioManager>().IsPlaying("CombatLayer1"))
                FindObjectOfType<AudioManager>().Stop("CombatLayer1");

            if (FindObjectOfType<AudioManager>().IsPlaying("CombatLayer2"))
                FindObjectOfType<AudioManager>().Stop("CombatLayer2");

            if (FindObjectOfType<AudioManager>().IsPlaying("CombatLayer3"))
                FindObjectOfType<AudioManager>().Stop("CombatLayer3");
        }
    }

    private void GameOver()
    {
        //Stop all of the in-game sounds
        FindObjectOfType<AudioManager>().StopAllSounds();

        //Destroy all particles
        foreach (var particle in FindObjectsOfType<ParticleSystem>())
            Destroy(particle.gameObject);

        //Stop all coroutines
        StopAllCoroutines();

        Time.timeScale = 0.0f;
        gameOverCanvas.SetActive(true);
        FindObjectOfType<AudioManager>().Play("DeathStinger", PlayerPrefs.GetFloat("BGMVolume", 0.5f));
        StartCoroutine(ReturnToMain());
    }

    IEnumerator ReturnToMain()
    {
        yield return new WaitForSecondsRealtime(9);

        gameOverCanvas.SetActive(false);
        sessionStatsCanvas.SetActive(true);
    }
}
