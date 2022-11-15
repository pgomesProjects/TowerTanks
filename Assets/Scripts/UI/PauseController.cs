using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class PauseController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI pauseText;

    public void ReturnToMain()
    {
        SceneManager.LoadScene("Title");
        Time.timeScale = 1.0f;
    }

    public void UpdatePauseText(int playerIndex)
    {
        pauseText.text = "Player " + (playerIndex + 1) + " Paused";
    }

}
