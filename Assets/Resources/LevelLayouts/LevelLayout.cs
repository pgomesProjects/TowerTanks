using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelLayout", menuName = "ScriptableObjects/LevelLayout", order = 1)]
public class LevelLayout : ScriptableObject
{
    public GameObject[] chunks;
}
