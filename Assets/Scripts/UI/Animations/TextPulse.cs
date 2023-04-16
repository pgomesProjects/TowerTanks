using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(CanvasGroup))]
public class TextPulse : MonoBehaviour
{
    [SerializeField, Range(0, 1), Tooltip("The ending alpha value for the text.")] private float endAlpha;
    [SerializeField, Tooltip("The amount of seconds it takes for the animation to play.")] private float seconds;
    [SerializeField, Tooltip("If true, the animation loops. If false, the animation only plays once.")] private bool loop;
    [SerializeField, Tooltip("If true, the animation will play even if the game is paused.")] private bool ignoreTimeScale;

    // Start is called before the first frame update
    void Start()
    {
        if (loop)
            LeanTween.alphaCanvas(GetComponent<CanvasGroup>(), endAlpha, seconds).setLoopPingPong().setIgnoreTimeScale(ignoreTimeScale);
        else
            LeanTween.alphaCanvas(GetComponent<CanvasGroup>(), endAlpha, seconds).setLoopPingPong(1).setIgnoreTimeScale(ignoreTimeScale);
    }
}
