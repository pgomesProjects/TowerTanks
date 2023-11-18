using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;

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
    [SerializeField] private PlayerTankController playerTank;
    [SerializeField] private Transform layerParent;
    [SerializeField] private GameObject layerPrefab;
    [SerializeField] private GameObject ghostLayerPrefab;
    [SerializeField] private GameObject tutorialPopup;
    [SerializeField] private TextMeshProUGUI popupText;
    [SerializeField] private TextMeshProUGUI resourcesDisplay;
    [SerializeField] private DialogEvent tutorialEvent;
    [SerializeField] private int scrapValue;
    [SerializeField] private float scrapAnimationSpeed;

    public static LevelManager instance;

    internal bool isPaused;
    internal bool readingTutorial;
    internal GAMESTATE levelPhase = GAMESTATE.TUTORIAL; //Start the game with the tutorial
    internal WeatherConditions currentWeatherConditions;
    private int currentPlayerPaused;
    internal int currentRound;
    internal int totalLayers;
    internal SessionStats currentSessionStats;
    internal bool isSettingUpOnStart;
    private int resourcesNum;
    private GameObject currentGhostLayer;

    private Transform[] spawnPoints;
    private Transform playerParent;

    private IEnumerator resourcesUpdateAnimation;
    private float resourcesAnimationPercent;

    private Dictionary<string, int> itemPrice;

    private void Awake()
    {
        instance = this;
        levelFader.SetActive(true);
        isPaused = false;
        readingTutorial = false;
        currentPlayerPaused = -1;
        totalLayers = 1;
        currentRound = 0;
        resourcesDisplay.text = resourcesNum.ToString();
        itemPrice = new Dictionary<string, int>();
        PopulateItemDictionary();
        TutorialController.main.dialogEvent = tutorialEvent;
        currentSessionStats = ScriptableObject.CreateInstance<SessionStats>();
    }

    private void Start()
    {
        isSettingUpOnStart = true;
        SpawnAllPlayers();
        GameManager.Instance.AudioManager.Play("MainMenuWindAmbience");

        //Starting resources
        switch (GameSettings.difficulty)
        {
            case 0.5f:
                resourcesNum = 1500;
                break;
            case 1.5f:
                resourcesNum = 500;
                break;
            default:
                resourcesNum = 1000;
                break;
        }

        if (GameSettings.skipTutorial)
        {
            resourcesDisplay.text = resourcesNum.ToString("n0");
            TransitionGameState();

            AddLayer(); //Add another layer
        }
        else
        {
            resourcesNum += 200;
            GameObject.FindGameObjectWithTag("Resources").gameObject.SetActive(false);
        }

        if (GameSettings.debugMode)
        {
            resourcesNum = 99999;
            resourcesDisplay.text = "Inf.";
        }
        isSettingUpOnStart = false;
    }

    private void OnEnable()
    {
        GameManager.Instance.MultiplayerManager.OnPlayerConnected += SpawnPlayer;
    }

    private void OnDisable()
    {
        GameManager.Instance.MultiplayerManager.OnPlayerConnected -= SpawnPlayer;
    }

    private void SpawnAllPlayers()
    {
        playerParent = GameObject.FindGameObjectWithTag("PlayerContainer")?.transform;
        spawnPoints = FindObjectOfType<SpawnPoints>()?.spawnPoints;

        foreach(PlayerInput playerInput in GameManager.Instance.MultiplayerManager.GetPlayerInputs())
            SpawnPlayer(playerInput);
    }

    private void SpawnPlayer(PlayerInput playerInput)
    {
        PlayerController character = Instantiate(GameManager.Instance.MultiplayerManager.GetPlayerPrefab());
        character.LinkPlayerInput(playerInput);
        character.GetComponent<Rigidbody2D>().isKinematic = false;
        character.transform.position = spawnPoints[playerInput.playerIndex].position;
        character.transform.SetParent(playerParent);
        character.transform.GetComponent<Renderer>().material.SetColor("_Color", GameManager.Instance.MultiplayerManager.GetPlayerColors()[playerInput.playerIndex]);
        if (levelPhase == GAMESTATE.GAMEACTIVE || GameSettings.skipTutorial)
            character.SetPlayerMove(true);
    }

    /// <summary>
    /// Determines the price of the items the player can buy
    /// </summary>
    private void PopulateItemDictionary()
    {
        //Buy
        itemPrice.Add("NewLayer", 100);
    }

    /// <summary>
    /// Update the global scrap number.
    /// </summary>
    /// <param name="resources">If positive, scrap is gained. If negative, scrap is lost.</param>
    public void UpdateResources(int resources)
    {
        //Display the resources in a fancy way
        if (!GameSettings.debugMode)
        {
            int originalValue = resourcesNum;
            resourcesNum += resources;

            if(resourcesUpdateAnimation == null)
            {
                resourcesUpdateAnimation = ResourcesTextAnimation(originalValue, scrapAnimationSpeed);
                StartCoroutine(resourcesUpdateAnimation);
            }
        }
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

    public int GetItemPrice(string itemName) => itemPrice[itemName];

    private IEnumerator ResourcesTextAnimation(int startingVal, float speed)
    {
        resourcesAnimationPercent = 0;
        while (resourcesAnimationPercent < 1)
        {
            resourcesDisplay.text = Mathf.RoundToInt(Mathf.Lerp(startingVal, resourcesNum, resourcesAnimationPercent)).ToString("n0");
            resourcesAnimationPercent += Time.deltaTime * speed;
            yield return null;
        }

        resourcesDisplay.text = resourcesNum.ToString("n0");
        resourcesUpdateAnimation = null;
    }

    public void AddCoalToTank(CoalController coalController, float amount)
    {
        //Add a percentage of the necessary coal to the furnace
        Debug.Log("Coal Has Been Added To The Furnace!");
        coalController.AddCoal(amount);
    }

    public void PurchaseLayer(PlayerController playerBuilding)
    {
        //Purchase a layer
        AddLayer();
        RemoveGhostLayer();
        GetPlayerTank().GetLayerAt(playerBuilding.currentLayer).GetComponent<GhostInteractables>().CreateGhostInteractables(playerBuilding);
    }

    private void AddLayer()
    {
        //Spawn a new layer and adjust it to go inside of the tank parent object
        GameObject newLayer = Instantiate(layerPrefab);
        newLayer.transform.parent = layerParent;
        newLayer.transform.localPosition = new Vector2(0, totalLayers * 8);

        //Add to the total number of layers and give the new layer an index
        totalLayers++;
        newLayer.name = "TANK LAYER " + totalLayers;
        newLayer.GetComponentInChildren<LayerTransitionManager>().SetLayerIndex(totalLayers);

        //Adjust the top view of the tank
        AdjustCameraPosition();

        //Add layer to the list of layers
        playerTank.GetLayers().Add(newLayer.GetComponent<LayerManager>());

        if (!isSettingUpOnStart)
        {
            //Check interactables on layer
            //CheckInteractablesOnLayer(totalLayers);
            //Play sound effect
            GameManager.Instance.AudioManager.Play("UseSFX");
        }

        //Adjust the weight of the tank
        playerTank.AdjustTankWeight(totalLayers);

        //Adjust the outside of the tank
        playerTank.AdjustOutsideLayerObjects();

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

    /// <summary>
    /// Moves the anchor on top of the tank so that the camera can view the entire tank.
    /// </summary>
    private void AdjustCameraPosition()
    {
        if (totalLayers > 2)
            playerTank.transform.Find("TankFollowTop").transform.localPosition = new Vector2(0, 4 + (totalLayers * 4) + ((totalLayers - 2) * 1.5f));
        else
            playerTank.transform.Find("TankFollowTop").transform.localPosition = new Vector2(0, 4);
    }

    public void AddGhostLayer()
    {
        //Spawn a new layer and adjust it to go inside of the tank parent object
        if(currentGhostLayer == null)
        {
            currentGhostLayer = Instantiate(ghostLayerPrefab);
            currentGhostLayer.transform.parent = playerTank.transform;
            currentGhostLayer.transform.localPosition = new Vector2(0, totalLayers * 8);
        }
        //If there's already a ghost layer but it's inactive, activate it
        else if (!currentGhostLayer.activeInHierarchy)
            currentGhostLayer.SetActive(true);
    }

    public void HideGhostLayer()
    {
        if(currentGhostLayer != null)
            currentGhostLayer.SetActive(false);
    }

    public void RemoveGhostLayer() => Destroy(currentGhostLayer);

    public void PauseToggle(int playerIndex)
    {
        //Debug.Log("Pausing: " + isPaused);

        //If the game is not paused, pause the game
        if (isPaused == false)
        {
            Time.timeScale = 0;

            GameManager.Instance.AudioManager.PauseAllSounds();
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
                GameManager.Instance.AudioManager.ResumeAllSounds();
                isPaused = false;
                pauseGameCanvas.SetActive(false);
                InputForOtherPlayers(currentPlayerPaused, false);
                currentPlayerPaused = -1;
            }
        }
    }

    /// <summary>
    /// Cancels repairs for all players.
    /// </summary>
    public void CancelAllLayerRepairs()
    {
        foreach (var player in FindObjectsOfType<PlayerController>())
            player.CancelLayerRepair();
    }

    public void ReactivateAllInput()
    {
        //InputForOtherPlayers(currentPlayerPaused, false);
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
                    player.GetPlayerInput().actions.Disable();
                }
                //Enable other player input
                else
                {
                    player.GetPlayerInput().actions.Enable();
                }
            }
            else
            {
                //Make sure the current player's action asset is tied to the EventSystem so they can use the menus
                EventSystem.current.GetComponent<InputSystemUIInputModule>().actionsAsset = player.GetPlayerInput().actions;
            }
        }
    }

    public void AdjustLayerSystem(int destroyedLayer)
    {
        //If there are no more layers, the game is over
        if (totalLayers == 0)
        {
            Debug.Log("Tank Is Destroyed!");
            playerTank.DestroyTank();
            //Switch from gameplay to game over
            TransitionGameState();
            return;
        }

        //Adjust the layer numbers for the layers above the one that got destroyed
        foreach (var i in playerTank.GetComponentsInChildren<LayerManager>())
        {
            int nextLayerIndex = i.GetComponentInChildren<LayerTransitionManager>().GetNextLayerIndex();

            if (nextLayerIndex > destroyedLayer + 1)
            {
                i.GetComponentInChildren<LayerTransitionManager>().SetLayerIndex(nextLayerIndex - 1);
                i.UnlockAllInteractables();
            }
        }

        //Adjust the top view of the tank
        AdjustCameraPosition();

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

            //If the player is trying to repair on this layer, cancel the action
            player.GetComponent<PlayerController>().CancelLayerRepair();
        }

        //Adjust the weight of the tank
        playerTank.AdjustTankWeight(totalLayers);
    }

    public void ResetPlayerCamera()
    {
        Debug.Log("Resetting Camera...");
        StartCoroutine(CameraEventController.instance.BringCameraToPlayer(2));
    }

    public void ShowGoPrompt()
    {
        if(goPrompt != null)
            goPrompt.SetActive(true);
    }

    public void HideGoPrompt()
    {
        if (goPrompt != null)
            goPrompt.GetComponent<GoArrowAnimation>().EndAnimation();
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
                playerTank.ResetTankDistance();
                break;
            //Gameplay to Game Over
            case GAMESTATE.GAMEACTIVE:
                levelPhase = GAMESTATE.GAMEOVER;
                CameraEventController.instance.FreezeCamera();
                GameOver();
                break;
        }
    }

    public void StartCombatMusic(int layers)
    {
        if(!GameManager.Instance.AudioManager.IsPlaying("CombatMusic"))
            GameManager.Instance.AudioManager.Play("CombatMusic");

        //Decides how many layers of music should play depending on the amount of enemy layers
        int musicLayers;
        if (layers >= 7)
            musicLayers = 4;

        else if(layers >= 5)
            musicLayers = 3;

        else if (layers >= 3)
            musicLayers = 2;

        else
            musicLayers = 1;

        //Set layers that are playing to full volume while muting layers that are not playing
        for(int i = 0; i < 4; i++)
        {
            if(i + 1 <= musicLayers)
                AkSoundEngine.SetRTPCValue("CombatLayer" + i + "Volume", 100f);
            else
                AkSoundEngine.SetRTPCValue("CombatLayer" + i + "Volume", 0f);
        }

        AkSoundEngine.SetRTPCValue("GlobalCombatVolume", 100, GameManager.Instance.AudioManager.GlobalGameObject);
    }

    /// <summary>
    /// Fades out and stops the combat music.
    /// </summary>
    /// <param name="fadeDuration">The duration of the fade (in seconds).</param>
    /// <returns></returns>
    public IEnumerator StopCombatMusic(float fadeDuration)
    {
        AkSoundEngine.SetRTPCValue("GlobalCombatVolume", 0, GameManager.Instance.AudioManager.GlobalGameObject, (int)(fadeDuration * 1000f));
        yield return new WaitForSeconds(fadeDuration);

        if (GameManager.Instance.AudioManager != null)
        {
            if (GameManager.Instance.AudioManager.IsPlaying("CombatMusic"))
                GameManager.Instance.AudioManager.Stop("CombatMusic");
        }
    }

    private void GameOver()
    {
        //Stop all of the in-game sounds
        GameManager.Instance.AudioManager.StopAllSounds();

        //Destroy all particles
        foreach (var particle in FindObjectsOfType<ParticleSystem>())
            Destroy(particle.gameObject);

        //Stop all coroutines
        StopAllCoroutines();

        Time.timeScale = 0.0f;
        gameOverCanvas.SetActive(true);
        GameManager.Instance.AudioManager.Play("DeathStinger");
        StartCoroutine(ReturnToMain());
    }

    IEnumerator ReturnToMain()
    {
        yield return new WaitForSecondsRealtime(10);

        gameOverCanvas.SetActive(false);
        sessionStatsCanvas.SetActive(true);
    }

    public void ShowPopup(bool showPopup) => tutorialPopup.GetComponent<CanvasGroup>().alpha = showPopup ? 1 : 0;
    public void SetPopupText(string newText) => popupText.text = newText;

    public int GetScrapValue() => scrapValue;
    public PlayerTankController GetPlayerTank() => playerTank;

    private void OnDestroy()
    {
        //Scene cleanup
        foreach (var particle in FindObjectsOfType<ParticleSystem>())
            Destroy(particle);
    }
}
