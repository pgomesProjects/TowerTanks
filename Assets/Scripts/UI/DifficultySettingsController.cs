using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DifficultySettingsController : MonoBehaviour
{
    [SerializeField] private GameObject[] flavorTextObjects;

    private void OnDisable()
    {
        foreach (var f in flavorTextObjects)
            f.SetActive(false);
    }
}
