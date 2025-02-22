using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ButtonFlash : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    [SerializeField, Tooltip("The speed in which the button flashes (in seconds).")] private float flashSpeed;
    [SerializeField, Tooltip("The color of the text when the button flashes.")] private Color onFlashColor;

    private TextMeshProUGUI buttonText;
    private float currentTimer;
    private Color defaultColor;
    private bool flashButton = false;

    private void Awake()
    {
        buttonText = GetComponentInChildren<TextMeshProUGUI>();
        defaultColor = buttonText.color;
    }

    public void OnSelect(BaseEventData eventData)
    {
        flashButton = true;
    }

    public void OnDeselect(BaseEventData eventData)
    {
        flashButton = false;
        ResetButton();
    }

    // Update is called once per frame
    void Update()
    {
        //If the button is selected, flash the button after a specified amount of seconds
        if (flashButton)
        {
            if (currentTimer > flashSpeed)
            {
                ToggleColor();
                currentTimer = 0f;
            }
            currentTimer += Time.unscaledDeltaTime;
        }
    }

    /// <summary>
    /// Toggles the color of the button text.
    /// </summary>
    private void ToggleColor()
    {
        buttonText.color = buttonText.color == defaultColor? onFlashColor: defaultColor;
    }

    /// <summary>
    /// Resets the button to its default color and resets the timer.
    /// </summary>
    private void ResetButton()
    {
        buttonText.color = defaultColor;
        currentTimer = 0f;
    }
}
