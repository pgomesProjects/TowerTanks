using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseController : MonoBehaviour
{
    public void ReturnToMain()
    {
        SceneManager.LoadScene("Title");
        Time.timeScale = 1.0f;
    }
}
