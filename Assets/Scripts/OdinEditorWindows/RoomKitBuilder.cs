using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;

public class RoomKitBuilder : OdinEditorWindow
{
    //Classes, Enums & Structs:
    private enum KitVisType
    {
        /// <summary> Visualize kit as a single cell, without optional components. </summary>
        Cell,
        /// <summary> Visualize kit as a full room, with random optional components. </summary>
        Room
    }

    //Objects & Components:
    [SerializeField, Tooltip("The kit you are modifying.")] private RoomAssetKit targetKit;


    //Settings:
    [SerializeField, Tooltip("This is how the kit will be displayed in the demo window.")] private KitVisType visualizationMethod = 0;
    [Button("Visualize", buttonSize: ButtonSizes.Small), Tooltip("Re-generates visualization with random assets from set.")] 
    private void GenerateVisualization()
    {

    }

    //Runtime variables:
    private int activeVisType = -1;

    //FUNCTIONALITY METHODS:
    private void OnEnable()
    {
        base.OnEnable(); //Call base enablement method
    }
    private void OnValidate()
    {
        //Update vis method:
        if ((int)visualizationMethod != activeVisType) //Active visualization method is out of date
        {
            switch (visualizationMethod)
            {
                case KitVisType.Cell:
                    break;
                case KitVisType.Room:
                    break;
            }
            activeVisType = (int)visualizationMethod; //Indicate that active vis method has changed
        }
    }
    private void OnDestroy()
    {
        base.OnDestroy(); //Call base destruction method
    }

    //UTLILTY METHODS:
    [MenuItem("Tools/RoomKitBuilder")] //Allows menu to be opened from the Tools dropdown menu
    private static void OpenWindow()
    {
        //NOTE: Copied from Sirenix tutorial (https://www.youtube.com/watch?v=O6JbflBK4Fo)
        GetWindow<RoomKitBuilder>().Show(); //Open this window in the Unity editor
    }
}
