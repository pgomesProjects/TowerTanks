using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum KEYTYPE { MOVEMENT, SPACE, LEFT_CLICK, RIGHT_CLICK, LEFT_SQUARE_BRACKET, RIGHT_SQUARE_BRACKET, E, SCROLL_WHEEL }
public enum GAMEPADTYPE { MOVEMENT, BUTTON_SOUTH, BUTTON_EAST, BUTTON_WEST, BUTTON_NORTH, LEFT_TRIGGER, RIGHT_TRIGGER, SPIN_LEFT_STICK }

public class ControlSchemeUIUpdater : MonoBehaviour
{
    private static string[] KEYBOARD_TYPES =
    {
        "[WASD or Arrow Keys]",
        "[Space]",
        "[Left Click]",
        "[Right Click]",
        "[[]",
        "[]]",
        "[E]",
        "[Scroll Wheel]",
    };

    private static string[] GAMEPAD_TYPES =
    {
        "<sprite=37 tint=1>",
        "<sprite=27 tint=1>",
        "<sprite=28 tint=1>",
        "<sprite=29 tint=1>",
        "<sprite=30 tint=1>",
        "<sprite=33 tint=1>",
        "<sprite=34 tint=1>",
        "<sprite=37 tint=1>",
    };

    private UIPrompt[] CONTROLS =
    {
        new UIPrompt("Movement", KEYBOARD_TYPES[(int)KEYTYPE.MOVEMENT], GAMEPAD_TYPES[(int)GAMEPADTYPE.MOVEMENT]),
        new UIPrompt("Pick Up", KEYBOARD_TYPES[(int)KEYTYPE.SPACE], GAMEPAD_TYPES[(int)GAMEPADTYPE.BUTTON_SOUTH]),
        new UIPrompt("Continue", KEYBOARD_TYPES[(int)KEYTYPE.SPACE], GAMEPAD_TYPES[(int)GAMEPADTYPE.BUTTON_SOUTH]),
        new UIPrompt("Throw", KEYBOARD_TYPES[(int)KEYTYPE.LEFT_CLICK], GAMEPAD_TYPES[(int)GAMEPADTYPE.BUTTON_EAST]),
        new UIPrompt("Use", KEYBOARD_TYPES[(int)KEYTYPE.RIGHT_CLICK], GAMEPAD_TYPES[(int)GAMEPADTYPE.BUTTON_WEST]),
        new UIPrompt("Cycle Left", KEYBOARD_TYPES[(int)KEYTYPE.LEFT_SQUARE_BRACKET], GAMEPAD_TYPES[(int)GAMEPADTYPE.LEFT_TRIGGER]),
        new UIPrompt("Cycle Right", KEYBOARD_TYPES[(int)KEYTYPE.RIGHT_SQUARE_BRACKET], GAMEPAD_TYPES[(int)GAMEPADTYPE.RIGHT_TRIGGER]),
        new UIPrompt("Interact", KEYBOARD_TYPES[(int)KEYTYPE.E], GAMEPAD_TYPES[(int)GAMEPADTYPE.BUTTON_NORTH]),
        new UIPrompt("Rotate Cannon", KEYBOARD_TYPES[(int)KEYTYPE.SCROLL_WHEEL], GAMEPAD_TYPES[(int)GAMEPADTYPE.SPIN_LEFT_STICK]),
    };

    public string UpdatePrompt(string controlsName)
    {
        UIPrompt currentUIPrompt = GetControls(controlsName);
        if(currentUIPrompt != null)
        {
            if(GameSettings.controlSchemeUI == "Gamepad")
            {
                return currentUIPrompt.gamepadPrompt;
            }
            else if(GameSettings.controlSchemeUI == "Keyboard and Mouse")
            {
                return currentUIPrompt.keyboardPrompt;
            }
        }

        return string.Empty;
    }

    private UIPrompt GetControls(string name)
    {
        foreach(var control in CONTROLS)
        {
            if(control.name == name)
                return control;
        }
        return null;
    }
}

[System.Serializable]
public class UIPrompt
{
    public string name { get; set; }
    public string keyboardPrompt { get; set; }
    public string gamepadPrompt { get; set; }

    public UIPrompt(string n, string keyboard, string gamepad)
    {
        name = n;
        keyboardPrompt = keyboard;
        gamepadPrompt = gamepad;
    }
}