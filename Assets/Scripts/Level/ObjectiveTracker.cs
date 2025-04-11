using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class ObjectiveTracker : MonoBehaviour
    {
        [SerializeField, Tooltip("The UI used to display objective information.")] private ObjectiveDisplay objectiveDisplay;

        private float targetDistance;

        private bool missionActive;
        public static Action OnMissionComplete;

        private float distanceTraveled;

        private void OnEnable()
        {
            LevelManager.OnMissionStart += InitializeObjective;
            LevelManager.OnEnemyDefeated += OnSubObjectiveUpdate;
        }

        private void OnDisable()
        {
            LevelManager.OnMissionStart -= InitializeObjective;
            LevelManager.OnEnemyDefeated -= OnSubObjectiveUpdate;
        }

        public void InitializeObjective(LevelEvents currentLevel)
        {
            missionActive = true;
            objectiveDisplay.SetObjectiveName(currentLevel.objectiveName);
            targetDistance = TankManager.Instance.playerTank.GetTowerJoint().transform.position.x + currentLevel.metersToTravel;

            int subObjectiveID = 1;
            foreach(SubObjectiveEvent subObjective in currentLevel.subObjectives)
            {
                switch (subObjective.objectiveType)
                {
                    case ObjectiveType.DefeatEnemies:
                        objectiveDisplay.AddSubObjective(subObjectiveID++, "Enemies Defeated: 0 / " + subObjective.enemiesToDefeat);
                        break;
                }
            }
        }

        private void Update()
        {
            if (missionActive)
                CheckObjective();
        }

        private void OnSubObjectiveUpdate(LevelEvents currentLevel)
        {
            int subObjectiveID = 1;
            foreach(SubObjectiveEvent subObjective in currentLevel.subObjectives)
            {
                switch (subObjective.objectiveType)
                {
                    case ObjectiveType.DefeatEnemies:
                        string objectiveMessage;
                        if (LevelManager.Instance.enemiesDestroyed >= subObjective.enemiesToDefeat)
                            objectiveMessage = "Objective Complete!";
                        else
                            objectiveMessage = "Enemies Defeated: " + LevelManager.Instance.enemiesDestroyed + " / " + subObjective.enemiesToDefeat;

                        objectiveDisplay.UpdateSubObjective(subObjectiveID, objectiveMessage);
                        break;
                }
            }
        }

        private void CheckObjective()
        {
            if (TankManager.Instance.playerTank != null)
            {
                //Calculate the distance between the tank and the target
                distanceTraveled = targetDistance - TankManager.Instance.playerTank.GetTowerJoint().transform.position.x;

                //If the target has been reached, complete the objective
                if (distanceTraveled == 0)
                {
                    Debug.Log("Tank has passed the target.");
                    OnMissionComplete?.Invoke();
                    missionActive = false;
                }
            }
        }

        public float GetDistanceTraveled() => targetDistance - distanceTraveled;
    }
}
