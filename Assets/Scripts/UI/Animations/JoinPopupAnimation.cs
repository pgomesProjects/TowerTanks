using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class JoinPopupAnimation : MonoBehaviour
{
    [Header("Start Animation Settings")]
    [SerializeField, Tooltip("The number of seconds for the pop up animation to take.")] private float animationDuration;
    [SerializeField, Tooltip("The y position of the animation.")] private float animationYPosition;
    [SerializeField, Tooltip("The ease type for the starting pop up animation.")] private AnimationCurve startingAnimationCurve;
    [Space(10)]

    [Header("End Animation Settings")]
    [SerializeField, Tooltip("The ease type for the ending pop up animation.")] private LeanTweenType endAnimationEaseType;
    [SerializeField, Tooltip("The number of seconds for the end animation to take.")] private float endAnimationDuration;

    private void OnEnable()
    {
        GetComponent<RectTransform>().anchoredPosition = Vector3.zero;
        GetComponent<CanvasGroup>().alpha = 1f;
        PlayAnimation();
    }

    private void OnDisable()
    {
        LeanTween.cancel(gameObject);
    }

    /// <summary>
    /// Plays the pop up join animation.
    /// </summary>
    private void PlayAnimation()
    {
        LeanTween.moveY(GetComponent<RectTransform>(), animationYPosition, animationDuration).setEase(startingAnimationCurve).setOnComplete(EndAnimation);
    }

    /// <summary>
    /// Plays the pop up ending animation.
    /// </summary>
    private void EndAnimation()
    {
        LeanTween.moveY(GetComponent<RectTransform>(), 0f, endAnimationDuration).setEase(endAnimationEaseType).setOnComplete(() => gameObject.SetActive(false));
        LeanTween.alphaCanvas(GetComponent<CanvasGroup>(), 0f, endAnimationDuration).setEase(endAnimationEaseType);
    }
}
