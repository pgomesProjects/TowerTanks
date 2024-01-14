using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Level Event", menuName = "Level Data / Level Event")]
public class LevelEvents : ScriptableObject
{
    public string levelName;
    public string levelDescription;
}
