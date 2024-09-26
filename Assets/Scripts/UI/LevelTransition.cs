using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelTransition : MonoBehaviour
{
    public enum LevelTransitionType { FADE, GATE };

    [Header("Fade Settings")]
    [SerializeField, Tooltip("The Image used to fade between scenes.")] private CanvasGroup blackFadeCanvas;

    [Header("Gate Settings")]
    [SerializeField, Tooltip("The RectTransform for the left gate.")] private RectTransform gateLeftTransform;
    [SerializeField, Tooltip("The RectTransform for the right gate.")] private RectTransform gateRightTransform;
    [SerializeField, Tooltip("The ease type for opening the gate.")] private LeanTweenType openGateEaseType;
    [SerializeField, Tooltip("The ease type for closing the gate.")] private LeanTweenType closeGateEaseType;

    public static LevelTransition Instance;

    private bool transitionActive = false;

    private Vector3 startingGatePosLeft, startingGatePosRight;
    private CanvasGroup gateCanvasGroup;

    private float startingAlpha, targetAlpha;
    private float timeToReachAlpha;
    private float delta;
    private bool fadeActive = false;

    public static Action OnTransitionStarted;
    public static Action OnTransitionCompleted;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        gateCanvasGroup = gateLeftTransform.GetComponentInParent<CanvasGroup>();
        startingGatePosLeft = gateLeftTransform.anchoredPosition;
        startingGatePosRight = gateRightTransform.anchoredPosition;

        ResetLevelTransition();
    }

    /// <summary>
    /// Resets the level transition canvas groups so that they are not visible.
    /// </summary>
    private void ResetLevelTransition()
    {
        blackFadeCanvas.alpha = 0f;
        gateCanvasGroup.alpha = 0f;
    }

    /// <summary>
    /// Starts the level transition animation.
    /// </summary>
    /// <param name="seconds">The duration of the start transition animation.</param>
    /// <param name="currentTransition">The type of level transition to play.</param>
    public void StartTransition(float seconds, LevelTransitionType currentTransition)
    {
        if (!transitionActive)
        {
            switch (currentTransition)
            {
                case LevelTransitionType.FADE:
                    startingAlpha = 0f;
                    targetAlpha = 1f;
                    timeToReachAlpha = seconds;
                    delta = 0f;
                    blackFadeCanvas.alpha = startingAlpha;
                    fadeActive = true;
                    transitionActive = true;
                    break;

                case LevelTransitionType.GATE:
                    gateCanvasGroup.alpha = 1f;
                    CloseGate(seconds);
                    transitionActive = true;
                    break;
            }

            OnTransitionStarted?.Invoke();
        }
    }

    public void EndTransition(float seconds, LevelTransitionType currentTransition)
    {
        switch(currentTransition)
        {
            case LevelTransitionType.FADE:
                startingAlpha = 1f;
                targetAlpha = 0f;
                timeToReachAlpha = seconds;
                delta = 0f;
                blackFadeCanvas.alpha = startingAlpha;
                fadeActive = true;
                transitionActive = true;
                break;

            case LevelTransitionType.GATE:
                OpenGate(seconds);
                transitionActive = true;
                break;
        }

        Invoke("CompleteLevelTransition", seconds);
    }

    /// <summary>
    /// Opens the gate in the gate level transition.
    /// </summary>
    /// <param name="duration">The duration it takes to open the gate.</param>
    private void OpenGate(float duration)
    {
        LeanTween.move(gateLeftTransform, new Vector3(startingGatePosLeft.x, 0f, startingGatePosLeft.z), duration).setEase(openGateEaseType).setOnComplete(() => transitionActive = false);
        LeanTween.move(gateRightTransform, new Vector3(startingGatePosRight.x, 0f, startingGatePosRight.z), duration).setEase(openGateEaseType);
    }

    /// <summary>
    /// Closes the gate in the gate level transition.
    /// </summary>
    /// <param name="duration">The duration it takes to close the gate.</param>
    private void CloseGate(float duration)
    {
        LeanTween.move(gateLeftTransform, new Vector3(0f, 0f, startingGatePosLeft.z), duration).setEase(closeGateEaseType).setOnComplete(() => transitionActive = false);
        LeanTween.move(gateRightTransform, new Vector3(0f, 0f, startingGatePosRight.z), duration).setEase(closeGateEaseType);
    }

    private void Update()
    {
        UpdateLevelFade();
    }

    /// <summary>
    /// Updates the level fader if active.
    /// </summary>
    private void UpdateLevelFade()
    {
        if (fadeActive)
        {
            delta += Time.unscaledDeltaTime / timeToReachAlpha;
            blackFadeCanvas.alpha = Mathf.Lerp(startingAlpha, targetAlpha, delta);

            if (delta >= 1f)
            {
                blackFadeCanvas.alpha = targetAlpha;
                fadeActive = false;
            }
        }
    }

    /// <summary>
    /// Resets the canvas and invokes the transition completed action.
    /// </summary>
    private void CompleteLevelTransition()
    {
        ResetLevelTransition();
        OnTransitionCompleted?.Invoke();
    }
}
