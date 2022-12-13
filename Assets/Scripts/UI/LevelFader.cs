using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelFader : MonoBehaviour
{

    public static LevelFader instance;
    public Animator animator;
    public float fadeInSeconds = 1.0f;
    public float fadeOutSeconds = 1.0f;
    private string levelToLoad;

    void Awake()
    {
        instance = this;

        //If the fade in time is set to 0, just skip to the end of the animation
        if (fadeInSeconds == 0)
        {
            animator.CrossFade("fade_in", 0f, 0, 1f);
        }
        //If not, set the speed of the animation based on the number of seconds desired to be played at
        else
            animator.speed = 1 / fadeInSeconds;
    }

    public void FadeToLevel(string levelName)
    {
        //Name of scene to be loaded
        levelToLoad = levelName;

        //If the fade out time is set to 0, just skip to the end of the animation
        if (fadeOutSeconds == 0)
        {
            animator.CrossFade("fade_out", 0f, 0, 1f);
        }
        //If not, set the speed of the animation based on the number of seconds desired to be played at
        else
            animator.speed = 1 / fadeOutSeconds;

        //Tell the animator to fade out the level
        animator.SetTrigger("FadeOut");
    }

    public void OnFadeComplete()
    {
        //Load scene
        SceneManager.LoadScene(levelToLoad);
    }
}
