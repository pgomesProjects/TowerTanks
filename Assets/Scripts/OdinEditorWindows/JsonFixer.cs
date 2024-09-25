using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System.IO;

[ExecuteInEditMode]
public class JsonFixer : OdinEditorWindow
{
    [SerializeField, Tooltip("Directory of file folder who's contents will be added to json array.")] private string filePath;
    [Button("Add Directory")]
    private void Add()
    {
        if (Directory.Exists(Application.dataPath + filePath))
        {
            DirectoryInfo info = new DirectoryInfo(Application.dataPath + filePath); //Get info about designated folder
            FileInfo[] files = info.GetFiles("*.*");                                 //Get info on every file in directory
            List<TextAsset> newJsonList = new List<TextAsset>();                     //Create list to store acquired files
            foreach (FileInfo file in files) //Iterate through list of files in given directory
            {
                
            }
        } else Debug.LogError("Filepath is invalid.");
    }
    [SerializeField, Tooltip("Array of json files to apply the fix to")] private TextAsset[] jsons;
    [Button("Fix JSONs")]
    private void Fix()
    {

    }

    //UTLILTY METHODS:
    [MenuItem("Tools/JsonFixer")] //Allows menu to be opened from the Tools dropdown menu
    private static void OpenWindow()
    {
        //NOTE: Copied from Sirenix tutorial (https://www.youtube.com/watch?v=O6JbflBK4Fo)
        GetWindow<JsonFixer>().Show(); //Open this window in the Unity editor
    }
}
