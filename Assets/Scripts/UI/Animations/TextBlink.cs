using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class TextBlink : MonoBehaviour
{
    [SerializeField, Range(0, 1), Tooltip("The ending alpha value for the text.")] private float endAlpha;
    [SerializeField, Tooltip("The amount of time in between blinks.")] private float blinkSpeed;

    private float startingAlpha;    //The starting alpha for the text
    private void Awake()
    {
        startingAlpha = GetComponent<CanvasGroup>().alpha;
    }

    private void OnEnable()
    {
        GetComponent<CanvasGroup>().alpha = startingAlpha;
        InvokeRepeating("ToggleText", blinkSpeed, blinkSpeed);
    }

    private void ToggleText() => GetComponent<CanvasGroup>().alpha = GetComponent<CanvasGroup>().alpha == startingAlpha ? endAlpha : startingAlpha;
}
