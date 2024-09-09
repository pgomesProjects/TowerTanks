using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using Sirenix.OdinInspector;

public class ProgressBar : MonoBehaviour
{
    private enum TextDisplay { Fraction, Percentage }
    private enum AnimationStatus { Inactive, Delay, Animating }
    private enum AnimationDirection { Positive, Negative }

    [SerializeField, Tooltip("The fill for the progress bar.")] private Image progressBarFill;
    [SerializeField, Tooltip("The fill for the progress bar for when the value is updated.")] private Image progressUpdateBarFill;
    [SerializeField, Tooltip("The text component for the progress bar.")] private TextMeshProUGUI progressBarText;
    [SerializeField, Tooltip("The type of text to display on the progress bar.")] private TextDisplay textDisplay;
    [Space]
    [SerializeField, Tooltip("The current value of the progress bar.")] private float currentValue = 50;
    [SerializeField, Tooltip("The current value of the update progress bar.")] private float updateBarValue = 60;
    [SerializeField, Tooltip("The maximum value of the progress bar.")] private float maxValue = 100;
    [InlineButton("DebugRemoveFromProgressBar", SdfIconType.Dash, "Remove 10")]
    [InlineButton("DebugAddToProgressBar", SdfIconType.Plus, "Add 10")]
    [Space]
    [SerializeField, Tooltip("If true, the progress bar is animated whenever the value is updated.")] private bool isAnimated;
    [SerializeField, Tooltip("The delay before the progress bar animates to the updated value.")] private float delayBeforeAnimation;
    [SerializeField, Tooltip("The amount of time it takes for the progress bar to reach the updated value.")] private float progressBarSpeed;

    public UnityEvent OnProgressComplete;

    private bool isAnimating;
    private float displayValue;

    private AnimationStatus animationStatus;
    private AnimationDirection currentAnimationDirection;

    private float currentDelay;
    private float animationElapsedTime;
    private float startingValue;

    private void DebugAddToProgressBar()
    {
        UpdateProgressValue(currentValue + 10);
    }

    private void DebugRemoveFromProgressBar()
    {
        UpdateProgressValue(currentValue - 10);
    }

    private void Start()
    {
        Init(currentValue, maxValue);
    }

    /// <summary>
    /// Initializes the values of the progress bar.
    /// </summary>
    /// <param name="currentValue">The starting value of the progress bar.</param>
    /// <param name="maxValue">The maximum value of the progress bar.</param>
    public void Init(float currentValue, float maxValue)
    {
        this.currentValue = currentValue;
        this.maxValue = maxValue;

        //Update the displays of the progress bars
        displayValue = currentValue;
        updateBarValue = currentValue;

        UpdateProgressBar(displayValue);
        UpdateProgressUpdateBar(updateBarValue);
    }

    /// <summary>
    /// Updates the progress bar's overall value.
    /// </summary>
    /// <param name="newValue">The new value of the progress bar.</param>
    public void UpdateProgressValue(float newValue)
    {
        //Update the actual value of the progress bar
        currentValue = Mathf.Clamp(newValue, 0, maxValue);

        if (isAnimated)
        {
            isAnimating = true;
            animationStatus = AnimationStatus.Delay;
            currentDelay = 0f;

            //If the new value is larger, make the update bar bigger before updating the main progress bar
            if(currentValue > displayValue)
            {
                updateBarValue = currentValue;
                startingValue = displayValue;
                UpdateProgressUpdateBar(updateBarValue);
                currentAnimationDirection = AnimationDirection.Positive;
            }

            //If the new value is smaller, update the main progress bar before updating the update bar
            else if(currentValue < displayValue)
            {
                displayValue = currentValue;
                startingValue = updateBarValue;
                UpdateProgressBar(displayValue);
                currentAnimationDirection = AnimationDirection.Negative;
            }

            //If there is no change to the value, there is nothing to animate
            else
            {
                displayValue = currentValue;
                updateBarValue = displayValue;
                UpdateProgressBar(displayValue);
                UpdateProgressUpdateBar(updateBarValue);

                animationStatus = AnimationStatus.Inactive;
            }
        }

        //If there is no animations, update the values immediately
        else
        {
            displayValue = currentValue;
            updateBarValue = displayValue;
            UpdateProgressBar(displayValue);
            UpdateProgressUpdateBar(updateBarValue);
        }

        //Check to see if the progress bar is full
        CheckForCompletion();
    }

    private void Update()
    {
        if (isAnimating)
        {
            switch (animationStatus)
            {
                //If the animation is delayed, run the delayed timer and switch to the animating state afterwards
                case AnimationStatus.Delay:
                    currentDelay += Time.deltaTime;

                    if (currentDelay >= delayBeforeAnimation)
                    {
                        animationStatus = AnimationStatus.Animating;
                        animationElapsedTime = 0f;
                    }
                    break;

                //Animate the progress bar
                case AnimationStatus.Animating:

                    animationElapsedTime += Time.deltaTime;

                    switch (currentAnimationDirection)
                    {
                        //If the new value is larger, update the main progress bar display
                        case AnimationDirection.Positive:
                            displayValue = Mathf.Lerp(startingValue, updateBarValue, Mathf.Clamp01(animationElapsedTime / progressBarSpeed));
                            UpdateProgressBar(displayValue);
                            break;

                        //If the new value is smaller, update the update progress bar display
                        case AnimationDirection.Negative:
                            updateBarValue = Mathf.Lerp(startingValue, currentValue, Mathf.Clamp01(animationElapsedTime / progressBarSpeed));
                            UpdateProgressUpdateBar(updateBarValue);
                            break;
                    }

                    if (animationElapsedTime / progressBarSpeed >= 1.0f)
                    {
                        isAnimating = false;
                        animationStatus = AnimationStatus.Inactive;
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// Checks to see if the progress bar has reached its maximum value.
    /// </summary>
    private void CheckForCompletion()
    {
        //If the value has reached its maximum, call any functions that are subscribed to the OnProgressComplete event
        if (currentValue >= maxValue)
            OnProgressComplete?.Invoke();
    }

    /// <summary>
    /// Updates the main progress bar.
    /// </summary>
    /// <param name="value">The value to update the progress bar with.</param>
    private void UpdateProgressBar(float value)
    {
        //Gets the fill amount (a value between 0 and 1)
        float fillAmount = Mathf.Clamp01(value / maxValue);

        progressBarFill.fillAmount = fillAmount;

        //If there is any progress bar text available, show the fill amount as either a fraction or a percentage
        if (progressBarText != null)
        {
            switch (textDisplay)
            {
                case TextDisplay.Fraction:
                    progressBarText.text = value.ToString("F0") + " / " + maxValue.ToString("F0");
                    break;
                case TextDisplay.Percentage:
                    progressBarText.text = (fillAmount * 100f).ToString("0") + "%";
                    break;
            }
        }
    }

    /// <summary>
    /// Updates the update progress bar (the progress bar behind the main one).
    /// </summary>
    /// <param name="value">The value to update the progress bar with.</param>
    private void UpdateProgressUpdateBar(float value) => progressUpdateBarFill.fillAmount = Mathf.Clamp01(value / maxValue);
}
