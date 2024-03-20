using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[System.Serializable]
public class TankId
{
    [InlineButton("GenerateName", SdfIconType.Dice6Fill, "")]
    public string TankName;
    
    public void GenerateName()
    {
        var generator = new TankNameGenerator();
        TankName = generator.GenerateRandomName();

        if (gameObject != null) gameObject.GetComponent<TankController>().TankName = TankName;
    }

    public enum TankType { PLAYER, ENEMY };
    public TankType tankType;

    //Components
    public GameObject gameObject;

    [HorizontalGroup("Horizontal Buttons")]
    [VerticalGroup("Horizontal Buttons/Column 1")]
    [Button("Destroy")] public void Destroy()
    {

    }
    [VerticalGroup("Horizontal Buttons/Column 2")]
    [Button("Repair")]
    public void Repair()
    {

    }

}
