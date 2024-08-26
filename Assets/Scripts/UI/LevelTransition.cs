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

    private float startingAlpha, targetAlpha;
    private float timeToReachAlpha;
    private float delta;
    private bool fadeActive = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        startingGatePosLeft = gateLeftTransform.anchoredPosition;
        startingGatePosRight = gateRightTransform.anchoredPosition;
    }

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
                    CloseGate(seconds);
                    transitionActive = true;
                    break;
            }
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
    }

    private void OpenGate(float duration)
    {
        LeanTween.move(gateLeftTransform, new Vector3(startingGatePosLeft.x, 0f, startingGatePosLeft.z), duration).setEase(openGateEaseType).setOnComplete(() => transitionActive = false);
        LeanTween.move(gateRightTransform, new Vector3(startingGatePosRight.x, 0f, startingGatePosRight.z), duration).setEase(openGateEaseType);
    }

    private void CloseGate(float duration)
    {
        LeanTween.move(gateLeftTransform, new Vector3(0f, 0f, startingGatePosLeft.z), duration).setEase(closeGateEaseType).setOnComplete(() => transitionActive = false);
        LeanTween.move(gateRightTransform, new Vector3(0f, 0f, startingGatePosRight.z), duration).setEase(closeGateEaseType);
    }

    private void Update()
    {
        if (fadeActive)
        {
            delta += Time.unscaledDeltaTime / timeToReachAlpha;
            blackFadeCanvas.alpha = Mathf.Lerp(startingAlpha, targetAlpha, delta);

            if(delta >= 1f)
            {
                blackFadeCanvas.alpha = targetAlpha;
                fadeActive = false;
            }
        }
    }
}
