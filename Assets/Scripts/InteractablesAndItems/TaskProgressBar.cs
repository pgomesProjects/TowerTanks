using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TaskProgressBar : MonoBehaviour
{
    [SerializeField, Tooltip("The progress bar for the task.")] private ProgressBar taskProgressBar;

    private bool isTaskActive;
    private float assignedTaskTime;
    private float elapsedTaskTime;

    private void Awake()
    {
        if(taskProgressBar == null)
        {
            Debug.LogWarning("No progress bar found!");
            Destroy(gameObject);
        }
        else
            taskProgressBar.gameObject.SetActive(false);
    }

    /// <summary>
    /// Starts the task timer.
    /// </summary>
    /// <param name="assignedCooldownTime">The cooldown time for the current task.</param>
    public void StartTask(float assignedCooldownTime)
    {
        this.assignedTaskTime = assignedCooldownTime;
        elapsedTaskTime = 0f;
        isTaskActive = true;
        taskProgressBar.UpdateProgressValue(0);
        taskProgressBar.gameObject.SetActive(true);
    }

    private void Update()
    {
        if (isTaskActive)
        {
            elapsedTaskTime += Time.deltaTime;
            taskProgressBar.UpdateProgressValue(elapsedTaskTime / assignedTaskTime);

            //If the cooldown has been reached, end the task
            if (elapsedTaskTime >= assignedTaskTime)
                EndTask();
        }
    }

    /// <summary>
    /// Ends the task timer.
    /// </summary>
    public void EndTask()
    {
        isTaskActive = false;
        taskProgressBar.gameObject.SetActive(false);
    }
}
