using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ObjectiveDisplay : MonoBehaviour
{
    [SerializeField, Tooltip("The text to display the type of objective for the mission.")] private TextMeshProUGUI objectiveTitleText;
    [SerializeField, Tooltip("The container for all subobjectives.")] private Transform subobjectiveTransform;

    private void Start()
    {
        ClearSubObjectives();
    }

    public void SetObjectiveName(string objectiveName)
    {
        objectiveTitleText.text = objectiveName;
    }

    private void ClearSubObjectives()
    {
        foreach(Transform trans in subobjectiveTransform)
            Destroy(trans.gameObject);
    }
}
