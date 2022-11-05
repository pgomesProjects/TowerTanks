using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
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
    [SerializeField] private GameObject gameOverCanvas;
    [SerializeField] private GameObject pauseGameCanvas;
    [SerializeField] private GameObject playerTank;
    [SerializeField] private GameObject layerPrefab;
    [SerializeField] private GameObject ghostLayerPrefab;
    [SerializeField] private GameObject tutorialPopup;
    [SerializeField] private ParticleSystem explosionParticles;
    [SerializeField] private TextMeshProUGUI resourcesDisplay;

    public static LevelManager instance;

    internal bool isPaused;
    internal GAMESTATE levelPhase = GAMESTATE.TUTORIAL; //Start the game with the tutorial
    private int currentPlayerPaused;
    internal int totalLayers;
    internal bool hasFuel;
    internal bool isSteering;
    internal float gameSpeed;
    internal int speedIndex;
    internal float[] currentSpeed = { -1.5f, -1f, 0f, 1, 1.5f };
    private int resourcesNum = 1000;
    private List<LayerHealthManager> layers;
    private GameObject currentGhostLayer;

    private Dictionary<string, int> itemPrice;

    private void Awake()
    {
        instance = this;
        isPaused = false;
        currentPlayerPaused = -1;
        totalLayers = 2;
        hasFuel = true;
        isSteering = false;
        speedIndex = (int)TANKSPEED.FORWARD;
        gameSpeed = currentSpeed[speedIndex];
        resourcesDisplay.text = "Resources: " + resourcesNum;
        itemPrice = new Dictionary<string, int>();
        layers = new List<LayerHealthManager>(2);
        PopulateItemDictionary();
        AdjustLayersInList();
    }

    private void Start()
    {
        if (GameSettings.skipTutorial)
        {
            speedIndex = (int)TANKSPEED.FORWARD;
            TransitionGameState();
        }
        else
        {
            speedIndex = (int)TANKSPEED.STATIONARY;
        }

        gameSpeed = currentSpeed[speedIndex];
    }

    /// <summary>
    /// Determines the price of the items the player can buy
    /// </summary>
    private void PopulateItemDictionary()
    {
        //Buy
        itemPrice.Add("NewLayer", 250);
    }

    private void AdjustLayersInList()
    {
        //Clear the list
        layers.Clear();

        //Insert each layer at the appropriate index
        foreach(var i in FindObjectsOfType<LayerHealthManager>())
        {
            layers.Add(i);
        }

        PrintLayerList();
    }

    private void PrintLayerList()
    {

        for (int i = 0; i < layers.Count; i++)
        {
            Debug.Log("Index " + i + ": " + layers[i].name);
        }
    }

    public void UpdateSpeed(int speedUpdate)
    {
        speedIndex = speedUpdate;
        gameSpeed = currentSpeed[speedIndex];

        Debug.Log("Tank Speed: " + gameSpeed);

        //If the current speed is stationary
        if (gameSpeed == currentSpeed[(int)TANKSPEED.STATIONARY])
        {
            FindObjectOfType<AudioManager>().Stop("TankIdle");
        }
        //If the tank idle isn't already playing, play it
        else if (!FindObjectOfType<AudioManager>().IsPlaying("TankIdle"))
        {
            FindObjectOfType<AudioManager>().Play("TankIdle", PlayerPrefs.GetFloat("SFXVolume", 0.5f));
        }

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

    private IEnumerator ResourcesTextAnimation(int startingVal, int endingVal, float seconds)
    {
        float elapsedTime = 0;
        while (elapsedTime < seconds)
        {
            resourcesDisplay.text = "Resources: " + Mathf.RoundToInt(Mathf.Lerp(startingVal, endingVal, elapsedTime / seconds)).ToString();
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        resourcesDisplay.text = "Resources: " + endingVal.ToString();
    }

    public void AddCoalToTank(CoalController coalController, float amount)
    {
        //Add a percentage of the necessary coal to the furnace
        Debug.Log("Coal Has Been Added To The Furnace!");
        coalController.AddCoal(amount);
    }

    public void PurchaseLayer(PlayerController player)
    {
        //If the player is outside of the tank (in a layer that does not exist inside the tank)
        if (player.currentLayer > totalLayers)
        {
            //If the players have enough money to purchase a layer
            if (CanPlayerAfford(itemPrice["NewLayer"]))
            {
                UpdateResources(-itemPrice["NewLayer"]);
                AddLayer();
                RemoveGhostLayer();
            }
        }
    }

    private void AddLayer()
    {
        //Spawn a new layer and adjust it to go inside of the tank parent object
        GameObject newLayer = Instantiate(layerPrefab);
        newLayer.transform.parent = playerTank.transform;
        newLayer.transform.localPosition = new Vector2(0, totalLayers * 8);

        playerTank.transform.Find("TankFollowTop").transform.localPosition = new Vector2(-13, (totalLayers * 8) + 4);

        //Add to the total number of layers and give the new layer an index
        totalLayers++;
        newLayer.GetComponentInChildren<LayerManager>().SetLayerIndex(totalLayers + 1);

        //Add layer to the list of layers
        layers.Insert(newLayer.GetComponentInChildren<LayerManager>().GetNextLayerIndex() - 2, newLayer.GetComponent<LayerHealthManager>());
        PrintLayerList();

        //Check interactables on layer
        CheckInteractablesOnLayer(newLayer.GetComponentInChildren<LayerManager>().GetNextLayerIndex() - 1);

        //Play sound effect
        FindObjectOfType<AudioManager>().PlayOneShot("UseSFX", PlayerPrefs.GetFloat("SFXVolume", 0.5f));

        //Adjust the weight of the tank
        playerTank.GetComponent<PlayerTankController>().AdjustTankWeight(totalLayers);
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

    private void RemoveGhostLayer()
    {
        Destroy(currentGhostLayer);
    }

    public void CheckInteractablesOnLayer(int index)
    {
        Debug.Log("Checking Layer " + layers[index - 1].name);

        if(index - 1 > 0)
        {
            //Check the interactable spawners
            foreach (var i in layers[index - 1].GetComponentsInChildren<InteractableSpawner>())
            {
                //If there is not an interactable spawned, show the ghost interactables
                if (!i.IsInteractableSpawned())
                {
                    FindObjectOfType<InteractableSpawnerManager>().ShowNewGhostInteractable(i);
                }
            }
        }
    }

    public void DestroyGhostInteractables(int index)
    {
        Debug.Log("Destroy Ghosts On Index " + (index - 2));

        if(index - 2 > 0)
        {
            //Check the interactable spawners
            foreach (var i in layers[index - 2].GetComponentsInChildren<InteractableSpawner>())
            {
                //If there is an interactable spawned
                if (i.IsInteractableSpawned())
                {
                    //If there is a ghost interactable, destroy it
                    if (i.transform.GetChild(0).CompareTag("GhostObject"))
                    {
                        Destroy(i.transform.GetChild(0).gameObject);
                    }
                }
            }
        }
    }

    public void PauseToggle(int playerIndex)
    {
        Debug.Log("Pausing: " + isPaused);

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
            }
        }

        playerTank.transform.Find("TankFollowTop").transform.localPosition = new Vector2(-13, (totalLayers * 8) + 4);

        //Update the list of layers accordingly
        AdjustLayersInList();

        //Adjust the weight of the tank
        playerTank.GetComponent<PlayerTankController>().AdjustTankWeight(totalLayers);
    }

    public void SpawnExplosion(Vector3 pos)
    {
        //Spawn explosion particles and add sound effect
        Instantiate(explosionParticles, pos, Quaternion.identity);
        FindObjectOfType<AudioManager>().PlayOneShot("ExplosionSFX", PlayerPrefs.GetFloat("SFXVolume", 0.5f));
    }

    public void TransitionGameState()
    {
        switch (levelPhase)
        {
            //Tutorial to Gameplay
            case GAMESTATE.TUTORIAL:
                levelPhase = GAMESTATE.GAMEACTIVE;
                if(FindObjectOfType<EnemySpawnManager>() != null)
                    FindObjectOfType<EnemySpawnManager>().GetReadyForEnemySpawn();
                tutorialPopup.SetActive(false);
                break;
            //Gameplay to Game Over
            case GAMESTATE.GAMEACTIVE:
                levelPhase = GAMESTATE.GAMEOVER;
                GameOver();
                break;
        }
    }

    private void GameOver()
    {
        //Stop all of the in-game sounds
        FindObjectOfType<AudioManager>().StopAllSounds();

        //Stop all coroutines
        StopAllCoroutines();

        Time.timeScale = 0.0f;
        gameOverCanvas.SetActive(true);
        StartCoroutine(ReturnToMain());
    }

    IEnumerator ReturnToMain()
    {
        yield return new WaitForSecondsRealtime(3);

        SceneManager.LoadScene("Title");
        Time.timeScale = 1.0f;
    }
}
