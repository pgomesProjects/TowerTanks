using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ObjectiveDisplay : MonoBehaviour
{
    [SerializeField, Tooltip("The text to display the type of objective for the mission.")] private TextMeshProUGUI objectiveTitleText;
    [SerializeField, Tooltip("The container for all subobjectives.")] private Transform subObjectiveTransform;
    [SerializeField, Tooltip("The sub objective prefab.")] private GameObject subObjectivePrefab;

    private List<SubObjective> subObjectives = new List<SubObjective>();

    private struct SubObjective
    {
        public GameObject subObjectiveObject;
        public int id;
    }

    private void Awake()
    {
        ClearSubObjectives();
    }

    public void SetObjectiveName(string objectiveName)
    {
        objectiveTitleText.text = objectiveName;
    }

    public void AddSubObjective(int id, string objectiveProgress)
    {
        if(!(FindSubObjectiveById(id).Value.id == id))
        {
            GameObject subObjectiveObject = Instantiate(subObjectivePrefab, subObjectiveTransform);
            subObjectiveObject.GetComponentInChildren<TextMeshProUGUI>().text = objectiveProgress;

            SubObjective newSubObjective = new SubObjective();
            newSubObjective.subObjectiveObject = subObjectiveObject;
            newSubObjective.id = id;
            subObjectives.Add(newSubObjective);
        }
    }

    public void UpdateSubObjective(int id, string objectiveProgress)
    {
        SubObjective? currentSubObjective = FindSubObjectiveById(id);

        if (currentSubObjective.Value.id == id)
            currentSubObjective.Value.subObjectiveObject.GetComponentInChildren<TextMeshProUGUI>().text = objectiveProgress;
    }

    private SubObjective? FindSubObjectiveById(int id) => subObjectives.Find(obj => obj.id == id);

    private void ClearSubObjectives()
    {
        foreach(Transform trans in subObjectiveTransform)
            Destroy(trans.gameObject);
    }
}
