using TowerTanks.Scripts;
using UnityEditor;
using UnityEngine;

public class ButtonPromptEditor : EditorWindow
{
    private static Vector2Int windowSize = new Vector2Int(600, 800);

    private ButtonPromptSystem buttonPromptSystem;
    private string buttonPromptSystemKey;
    private PlatformType platformType;
    private Vector2 scrollPos;

    private int editingIndex = -1;
    private string editingName = string.Empty;

    [MenuItem("Tools/Button Prompts Editor")]
    private static void OpenWindow()
    {
        ButtonPromptEditor buttonPromptEditor = GetWindow<ButtonPromptEditor>("Button Prompts Editor");
        buttonPromptEditor.minSize = windowSize;
        buttonPromptEditor.maxSize = windowSize;

        //Load the button prompt system
        buttonPromptEditor.LoadButtonPromptSystem();
    }

    private void OnGUI()
    {
        //Have the button prompt system shown
        buttonPromptSystem = (ButtonPromptSystem)EditorGUILayout.ObjectField("Button Prompt System", buttonPromptSystem, typeof(ButtonPromptSystem), false);

        if(buttonPromptSystem != null)
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Platform", EditorStyles.boldLabel);
            platformType = (PlatformType)EditorGUILayout.EnumPopup(platformType, GUILayout.Width(400));
            GUILayout.Space(10);

            EditorGUILayout.LabelField("Game Actions", EditorStyles.boldLabel);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            //Show the actions that exist in the button prompt system
            for (int i = 0; i < buttonPromptSystem.actions.Count; i++)
            {
                EditorGUILayout.BeginVertical();

                Rect labelRect = GUILayoutUtility.GetRect(new GUIContent(buttonPromptSystem.actions[i].name), EditorStyles.label);
                //If the user double clicks the name, let the user rename it
                if (Event.current.type == EventType.MouseDown && labelRect.Contains(Event.current.mousePosition) && Event.current.clickCount == 2)
                {
                    editingIndex = i;
                    editingName = buttonPromptSystem.actions[i].name;
                    Event.current.Use();
                }

                //If this current name is being edited, let the user edit it
                if(editingIndex == i)
                {
                    //Show a text field for renaming
                    EditorGUILayout.BeginHorizontal();
                    editingName = EditorGUILayout.TextField(editingName, GUILayout.Width(150));
                    EditorGUILayout.EndHorizontal();

                    //If the user presses enter or clicks the mouse, rename the action
                    if ((Event.current.isKey && Event.current.keyCode == KeyCode.Return) || Event.current.type == EventType.MouseDown)
                    {
                        buttonPromptSystem.actions[i].name = editingName.Trim();
                        SaveButtonPromptSystem(buttonPromptSystem);

                        //Unfocus from the text field
                        editingIndex = -1;
                    }
                }
                //If it is not being edited, simply show it as a label
                else
                    EditorGUI.LabelField(labelRect, buttonPromptSystem.actions[i].name);

                //Show the action type
                buttonPromptSystem.actions[i].actionType = (ActionType)EditorGUILayout.EnumPopup(buttonPromptSystem.actions[i].actionType, GUILayout.Width(120));
                //Show the button prompt that corresponds to the active platform type
                buttonPromptSystem.actions[i].promptInfo[(int)platformType] = (PromptInfo)EditorGUILayout.ObjectField(buttonPromptSystem.actions[i].promptInfo[(int)platformType], typeof(PromptInfo), false, GUILayout.Width(400));

                //Let the user remove the action
                if (GUILayout.Button("Remove", GUILayout.Width(80)))
                    RemoveAction(buttonPromptSystem.actions[i]);
                GUILayout.Space(10);
                EditorGUILayout.EndVertical();
            }

            //If there are no actions, show a message
            if (buttonPromptSystem.actions.Count == 0)
            {
                EditorGUILayout.LabelField("No Actions To Show.");
                GUILayout.Space(10);
            }
            EditorGUILayout.EndScrollView();

            //Let a user add an action
            if (GUILayout.Button("Add Action", GUILayout.Width(150)))
                AddActionPopupWindow.OpenActionWindow("Add Game Action", "Enter a name for the action:", OnAddActionConfirmed);

            //Save the button prompt system
            SaveButtonPromptSystem(buttonPromptSystem);
        }
    }

    /// <summary>
    /// Add an action on confirmed.
    /// </summary>
    /// <param name="actionName">The name of the action.</param>
    private void OnAddActionConfirmed(string actionName)
    {
        //If the action does not exist in the system, add it
        if(buttonPromptSystem.GetAction(actionName) == null)
        {
            buttonPromptSystem.actions.Add(new ButtonAction(actionName));
            SaveButtonPromptSystem(buttonPromptSystem);
        }
    }

    /// <summary>
    /// Removes the action from the button prompt system.
    /// </summary>
    /// <param name="buttonAction">The button action to remove.</param>
    private void RemoveAction(ButtonAction buttonAction)
    {
        buttonPromptSystem.actions.Remove(buttonAction);
    }

    /// <summary>
    /// Load the button prompt system.
    /// </summary>
    private void LoadButtonPromptSystem()
    {
        //Get the GUID of the saved ButtonPromptSystem
        string guid = EditorPrefs.GetString(buttonPromptSystemKey, string.Empty);
        if (!string.IsNullOrEmpty(guid))
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            buttonPromptSystem = AssetDatabase.LoadAssetAtPath<ButtonPromptSystem>(assetPath);
        }
    }

    private void SaveButtonPromptSystem(ButtonPromptSystem promptSystem)
    {
        //Store the GUID of the selected ButtonPromptSystem asset
        string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(promptSystem));
        EditorPrefs.SetString(buttonPromptSystemKey, guid);
    }
}