using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class MoveAnimation : MonoBehaviour
{
    [SerializeField, Tooltip("The starting position for the transform move.")] private Vector2 startingPos;
    [SerializeField, Tooltip("The ending position for the transform move.")] private Vector2 endingPos;
    [SerializeField, Tooltip("The duration for the transform move (in seconds).")] public float duration;
    [SerializeField, Tooltip("The ease type for the transform move.")] private LeanTweenType transformMoveCurve;

    [SerializeField, Tooltip("If true, plays the animation when enabled.")] private bool playOnStart;

    private RectTransform rectTrans;
    private Vector2 originalPosition;

    private void Awake()
    {
        rectTrans = GetComponent<RectTransform>();
        originalPosition = rectTrans.anchoredPosition;
    }

    private void OnEnable()
    {
        if (playOnStart)
            Play();
    }

    private void OnDisable()
    {
        rectTrans.anchoredPosition = originalPosition;
    }

    /// <summary>
    /// Plays the RectTransform moving animation.
    /// </summary>
    public void Play()
    {
        //Resets the screen position when enabled
        rectTrans.anchoredPosition = startingPos;
        LeanTween.move(rectTrans, endingPos, duration).setEase(transformMoveCurve);
    }
}
