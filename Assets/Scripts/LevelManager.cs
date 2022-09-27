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

    public static LevelManager instance;

    internal bool isPaused;
    internal bool hasFuel;
    internal bool isSteering;
    internal float gameSpeed;
    internal int speedIndex;
    internal float[] currentSpeed = {-1.5f, -1f, 0f, 1, 1.5f};

    private void Awake()
    {
        instance = this;
        isPaused = false;
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
