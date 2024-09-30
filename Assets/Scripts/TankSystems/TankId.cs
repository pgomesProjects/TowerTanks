using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;

[System.Serializable]
public class TankId
{
    [InlineButton("MatchName", SdfIconType.ArrowDown, "")]
    [InlineButton("GenerateName", SdfIconType.Dice6Fill, "")]
    public string TankName = "New Tank";
    [SerializeField] internal TankController tankScript;

    public enum TankType { PLAYER, ENEMY };
    public TankType tankType;

    //Components
    public GameObject gameObject;
    [Tooltip("Level layout to load")]
    public TextAsset design;
    [SerializeField, Tooltip("If true, builds the current design on this tank when the game starts")] public bool buildOnStart;

    public void MatchName() //Matches the Tank's name with the current design - good for overwriting designs
    {
        TankName = design.name;

        if (gameObject != null)
        {
            tankScript = gameObject.GetComponent<TankController>();
            tankScript.TankName = TankName;
            gameObject.name = TankName;
        }
    }

    public void GenerateName()
    {
        var generator = new TankNameGenerator();
        TankNames nameType = null;
        if (tankType == TankType.ENEMY) nameType = Resources.Load<TankNames>("TankNames/PirateNames");
        TankName = generator.GenerateRandomName(nameType);

        if (gameObject != null)
        {
            tankScript = gameObject.GetComponent<TankController>();
            tankScript.TankName = TankName;
            gameObject.name = TankName;
        }
    }

    [HorizontalGroup("Horizontal Buttons")]
    [VerticalGroup("Horizontal Buttons/Column 1")]
    [Button(" Destroy", Icon = SdfIconType.EmojiDizzy)] public void Destroy()
    {
        if (tankType != TankType.PLAYER)
        {
            tankScript = gameObject.GetComponent<TankController>();
            tankScript.BlowUp(true);
            TankManager tankMan = GameObject.Find("TankManager").GetComponent<TankManager>();
            if (tankMan != null) tankMan.tanks.Remove(this);
        }
    }
    [VerticalGroup("Horizontal Buttons/Column 2")]
    [Button(" Build", Icon = SdfIconType.Hammer), Tooltip("Press during runtime to construct the tank based on the current design")]
    public void Build()
    {
        if (gameObject != null)
        {
            string json = design.text;
            if (json != null)
            {
                TankDesign _design = JsonUtility.FromJson<TankDesign>(json);
                //Debug.Log("" + layout.chunks[0] + ", " + layout.chunks[1] + "...");
                tankScript = gameObject.GetComponent<TankController>();
                tankScript.Build(_design);
            }
        }
    }
    [VerticalGroup("Horizontal Buttons/Column 2")]
    [Button(" Save", Icon = SdfIconType.Save), Tooltip("Saves the current tank as a new tank design")]
    public void SaveDesign()
    {
        if (gameObject != null)
        {
            //Get the current design
            tankScript = gameObject.GetComponent<TankController>();
            TankDesign design = tankScript.GetCurrentDesign();
            if (design != null) //Debug.Log("I got a design called " + design.TankName + "." + " It's first room is " + design.buildingSteps[0].roomID);
            {
                //Convert it into a json
                string json = JsonUtility.ToJson(design, true);
                string path = "Assets/Resources/TankDesigns/" + design.TankName + ".json";

                if (File.Exists(path)) { Debug.LogWarning("File exists. Overwriting Existing File."); }
                File.WriteAllText(path, json);
                AssetDatabase.Refresh();
            }
        }
    }

}
