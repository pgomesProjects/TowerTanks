using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GoArrowAnimation : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField, Range(0, 1), Tooltip("The ending alpha for the arrow animation.")] private float endAlpha;
    [SerializeField, Tooltip("The amount of units to move the arrow on the X axis during the animation.")] private float animationMovementUnits;
    [SerializeField, Tooltip("The amount of seconds it takes for the animation to play.")] private float animationDuration;
    [SerializeField, Tooltip("The ease type for the arrow animation.")] private LeanTweenType easeType;
    [SerializeField, Tooltip("If true, the animation loops. If false, the animation only plays once.")] private bool loop;
    [Space(10)]
    [Header("End Animation Settings")]
    [SerializeField, Tooltip("The amount of seconds it takes for the end animation to play.")] private float endAnimationDuration;
    [SerializeField, Tooltip("The ease type for the end arrow animation.")] private LeanTweenType endAnimationEaseType;

    private Vector3 startingPosition;       //The starting position for the go arrow
    private Color startingColor;            //The starting color for the go arrow
    private RectTransform rectTransform;    //The go arrow RectTransform component
    private LTDescr arrowAlphaAnimation;    //The current arrow alpha animation

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        startingPosition = rectTransform.anchoredPosition;
        startingColor = rectTransform.GetComponent<Image>().color;
    }

    private void OnEnable()
    {
        //Reset values
        rectTransform.anchoredPosition = startingPosition;
        rectTransform.GetComponent<Image>().color = startingColor;

        PlayAnimation();
    }

    private void OnDisable()
    {
        LeanTween.cancel(gameObject);
    }

    /// <summary>
    /// Plays the Go Arrow animation.
    /// </summary>
    private void PlayAnimation()
    {
        if (loop)
        {
            LeanTween.moveX(rectTransform, startingPosition.x + animationMovementUnits, animationDuration).setEase(easeType).setLoopPingPong();
            arrowAlphaAnimation = LeanTween.alpha(rectTransform, endAlpha, animationDuration).setEase(easeType).setLoopPingPong();
        }
        else
        {
            LeanTween.moveX(rectTransform, startingPosition.x + animationMovementUnits, animationDuration).setEase(easeType).setLoopPingPong(1);
            arrowAlphaAnimation = LeanTween.alpha(rectTransform, endAlpha, animationDuration).setEase(easeType).setLoopPingPong(1);
        }
    }

    /// <summary>
    /// Ends the go arrow animation and then disables itself.
    /// </summary>
    public void EndAnimation()
    {
        LeanTween.pause(arrowAlphaAnimation.id);
        LeanTween.alpha(rectTransform, 0f, animationDuration).setEase(endAnimationEaseType).setOnComplete(() => gameObject.SetActive(false));
    }
}
