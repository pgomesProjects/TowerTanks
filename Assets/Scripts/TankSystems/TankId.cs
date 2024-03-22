using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[System.Serializable]
public class TankId
{
    [InlineButton("GenerateName", SdfIconType.Dice6Fill, "")]
    public string TankName;
    private TankController tankScript;

    public enum TankType { PLAYER, ENEMY };
    public TankType tankType;

    //Components
    public GameObject gameObject;
    public TankDesign design;

    public void GenerateName()
    {
        var generator = new TankNameGenerator();
        TankName = generator.GenerateRandomName();

        if (gameObject != null)
        {
            tankScript = gameObject.GetComponent<TankController>();
            tankScript.TankName = TankName;
        }
    }

    [HorizontalGroup("Horizontal Buttons")]
    [VerticalGroup("Horizontal Buttons/Column 1")]
    [Button("Destroy")] public void Destroy()
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
    [Button("Build"), Tooltip("Press during runtime to construct the tank based on the current design")]
    public void Build()
    {
        if (gameObject != null)
        {
            tankScript = gameObject.GetComponent<TankController>();
            tankScript.Build(design);
        }
    }

}
