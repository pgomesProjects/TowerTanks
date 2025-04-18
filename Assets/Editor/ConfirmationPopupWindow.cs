using System;
using UnityEditor;
using UnityEngine;

public class ConfirmationPopupWindow : EditorWindow
{
    protected string promptText;
    protected Action<string> onConfirm;

    public static void OpenConfirmationWindow(string title, string prompt, Action<string> onConfirm)
    {
        ConfirmationPopupWindow window = CreateInstance<ConfirmationPopupWindow>();
        window.titleContent = new GUIContent(title);
        window.promptText = prompt;
        window.onConfirm = onConfirm;

        //Center the window in the middle of the screen
        window.position = new Rect(Screen.width / 2f, Screen.height / 2f, 300, 100);
        window.ShowUtility();
    }

    protected virtual void OnGUI()
    {
        ShowPrompt();
        ShowConfirmationButtons();
    }

    protected void ShowPrompt()
    {
        //Prompt
        EditorGUILayout.LabelField(promptText);
    }

    protected virtual void CheckConfirmation()
    {
        //Add a condition for the confirmation to invoke
    }

    protected void ShowConfirmationButtons()
    {
        GUILayout.BeginHorizontal();
        //If there is a name, invoke the action before closing
        if (GUILayout.Button("Confirm"))
        {
            CheckConfirmation();
            Close();
        }

        //Close the window
        if (GUILayout.Button("Cancel"))
            Close();

        GUILayout.EndHorizontal();
    }
}