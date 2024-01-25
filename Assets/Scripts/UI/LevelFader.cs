using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelFader : MonoBehaviour
{
    [SerializeField, Tooltip("The Image used to fade between scenes.")] private CanvasGroup blackFadeCanvas;

    public static LevelFader Instance;

    private float startingAlpha, targetAlpha;
    private float timeToReachAlpha;
    private float delta;
    private bool transitionActive = false;

    private void Awake()
    {
        Instance = this;
    }

    public void FadeIn(float seconds)
    {
        Debug.Log("Fading In For " + seconds.ToString() + " Seconds");
        startingAlpha = 1f;
        targetAlpha = 0f;
        timeToReachAlpha = seconds;
        delta = 0f;
        blackFadeCanvas.alpha = startingAlpha;
        transitionActive = true;
    }

    public void FadeOut(float seconds)
    {
        Debug.Log("Fading Out For " + seconds.ToString() + " Seconds");
        startingAlpha = 0f;
        targetAlpha = 1f;
        timeToReachAlpha = seconds;
        delta = 0f;
        blackFadeCanvas.alpha = startingAlpha;
        transitionActive = true;
    }

    private void Update()
    {
        if (transitionActive)
        {
            delta += Time.unscaledDeltaTime / timeToReachAlpha;
            blackFadeCanvas.alpha = Mathf.Lerp(startingAlpha, targetAlpha, delta);

            if(delta >= 1f)
            {
                blackFadeCanvas.alpha = targetAlpha;
                transitionActive = false;
            }
        }
    }
}
