using System;
using UnityEditor;
using UnityEngine;

public class AddActionPopupWindow : ConfirmationPopupWindow
{
    private string actionName = "";

    public static void OpenActionWindow(string title, string prompt, Action<string> onConfirm)
    {
        AddActionPopupWindow window = CreateInstance<AddActionPopupWindow>();
        window.titleContent = new GUIContent(title);
        window.promptText = prompt;
        window.onConfirm = onConfirm;

        //Center the window in the middle of the screen
        window.position = new Rect(Screen.width / 2f, Screen.height / 2f, 300, 100);
        window.ShowUtility();
    }

    protected override void OnGUI()
    {
        ShowPrompt();
        //Action name input with prompt
        actionName = EditorGUILayout.TextField(actionName);
        EditorGUILayout.Space();
        ShowConfirmationButtons();
    }

    protected override void CheckConfirmation()
    {
        //If there is a name for the action window, invoke the on confirm command
        if (!string.IsNullOrWhiteSpace(actionName))
            onConfirm?.Invoke(actionName);
    }
}