using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
            newLayer.transform.localPosition = new Vector2(0, totalLayers);

            //Add to the total number of layers and give the new layer an index
            totalLayers++;
            newLayer.GetComponentInChildren<LayerManager>().SetLayerIndex(totalLayers + 1);
        }
    }

    public void PauseToggle()
    {
        //If the game is not paused, pause the game
        if (!isPaused)
            Time.timeScale = 0;
        //If the game is paused, resume the game
        else
            Time.timeScale = 1;

        isPaused = !isPaused;
        pauseGameCanvas.gameObject.SetActive(isPaused);
    }

    public void GameOver()
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
