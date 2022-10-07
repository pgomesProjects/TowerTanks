using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public enum TANKSPEED
{
    REVERSEFAST, REVERSE, STATIONARY, FORWARD, FORWARDFAST
}

public class LevelManager : MonoBehaviour
{
    [SerializeField] private Canvas gameOverCanvas;
    [SerializeField] private Canvas pauseGameCanvas;
    [SerializeField] private GameObject playerTank;
    [SerializeField] private GameObject layerPrefab;

    public static LevelManager instance;

    internal bool isPaused;
    private int currentPlayerPaused;
    internal int totalLayers;
    internal bool hasFuel;
    internal bool isSteering;
    internal float gameSpeed;
    internal int speedIndex;
    internal float[] currentSpeed = {-1.5f, -1f, 0f, 1, 1.5f};

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
    }

    public void UpdateSpeed(int speedUpdate)
    {
        speedIndex += speedUpdate;
        gameSpeed = currentSpeed[speedIndex];

        Debug.Log("Tank Speed: " + gameSpeed);

        //Update the enemy speed comparative to the player
        foreach(var i in FindObjectsOfType<EnemyController>())
        {
            i.UpdateEnemySpeed();
        }
    }

    public void AddLayer(PlayerController player)
    {
        //If the player is outside of the tank (in a layer that does not exist inside the tank)
        if(player.currentLayer > totalLayers)
        {
            //Spawn a new layer and adjust it to go inside of the tank parent object
            GameObject newLayer = Instantiate(layerPrefab);
            newLayer.transform.parent = playerTank.transform;
            newLayer.transform.localPosition = new Vector2(0, totalLayers * 8);

            //Add to the total number of layers and give the new layer an index
            totalLayers++;
            newLayer.GetComponentInChildren<LayerManager>().SetLayerIndex(totalLayers + 1);
        }
    }

    public void PauseToggle(int playerIndex)
    {
        Debug.Log("Pausing: " + isPaused);

        //If the game is not paused, pause the game
        if (isPaused == false)
        {
            Time.timeScale = 0;
            currentPlayerPaused = playerIndex;
            isPaused = true;
            pauseGameCanvas.GetComponent<PauseController>().UpdatePauseText(playerIndex);
            pauseGameCanvas.gameObject.SetActive(true);
            InputForOtherPlayers(currentPlayerPaused, true);
        }
        //If the game is paused, resume the game if the person that paused the game unpauses
        else if(isPaused == true)
        {
            if (playerIndex == currentPlayerPaused)
            {
                Time.timeScale = 1;
                isPaused = false;
                pauseGameCanvas.gameObject.SetActive(false);
                InputForOtherPlayers(currentPlayerPaused, false);
                currentPlayerPaused = -1;
            }
        }
    }

    private void InputForOtherPlayers(int currentActivePlayer, bool disableInputForOthers)
    {
        Debug.Log("Current Active Player: " + (currentActivePlayer + 1));

        foreach(var player in FindObjectsOfType<PlayerController>())
        {
            if(player.GetPlayerIndex() != currentActivePlayer)
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
        foreach (var player in FindObjectsOfType<PlayerController>())
        {
            Debug.Log("Player " + (player.GetPlayerIndex() + 1) + " Action Map: " + player.GetComponent<PlayerInput>().currentActionMap.name);
        }
    }

    public void AdjustLayerSystem(int destroyedLayer)
    {
        //If there are no more layers, the game is over
        if(totalLayers == 0)
        {
            Debug.Log("Tank Is Destroyed!");
            GameOver();
            return;
        }

        //Adjust the layer numbers for the layers above the one that got destroyed
        foreach(var i in FindObjectsOfType<LayerHealthManager>())
        {
            int nextLayerIndex = i.GetComponentInChildren<LayerManager>().GetNextLayerIndex();

            if (nextLayerIndex > destroyedLayer + 1)
            {
                i.GetComponentInChildren<LayerManager>().SetLayerIndex(nextLayerIndex - 1);
            }
        }
    }
    private void GameOver()
    {
        Time.timeScale = 0.0f;
        gameOverCanvas.gameObject.SetActive(true);
        StartCoroutine(ReturnToMain());
    }

    IEnumerator ReturnToMain()
    {
        yield return new WaitForSecondsRealtime(3);

        SceneManager.LoadScene("Title");
        Time.timeScale = 1.0f;
    }
}
