using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectiveTracker : MonoBehaviour
{
    [SerializeField, Tooltip("The UI used to display objective information.")] private ObjectiveDisplay objectiveDisplay;
    [SerializeField, Tooltip("The tank to keep track of.")] private TankController playerTank;

    private ObjectiveType currentObjective;
    private float targetDistance;

    private bool missionActive;
    public static Action OnMissionComplete;

    private void OnEnable()
    {
        LevelManager.OnMissionStart += InitializeObjective;
    }

    private void OnDisable()
    {
        LevelManager.OnMissionStart -= InitializeObjective;
    }

    public void InitializeObjective(LevelEvents currentLevel)
    {
        missionActive = true;
        objectiveDisplay.SetObjectiveName(currentLevel.GetObjectiveName());
        targetDistance = playerTank.transform.position.x + currentLevel.metersToTravel;
        currentObjective = currentLevel.objectiveType;
    }

    private void Update()
    {
        if(missionActive)
            CheckObjective();
    }

    private void CheckObjective()
    {
        switch (currentObjective)
        {
            case ObjectiveType.TravelDistance:

                if (playerTank != null)
                {
                    //Calculate the distance between the tank and the target
                    float distance = targetDistance - playerTank.transform.position.x;

                    //If the target has been reached, complete the objective
                    if (distance <= 0)
                    {
                        Debug.Log("Tank has passed the target.");
                        OnMissionComplete?.Invoke();
                        missionActive = false;
                    }
                }
                break;
        }
    }
}
