using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TaskProgressBar : ProgressBar
{
    private bool isTaskActive;
    private float assignedTaskTime;
    private float elapsedTaskTime;

    protected override void Start()
    {
        base.Start();
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
        UpdateProgressValue(0);
    }

    protected override void Update()
    {
        base.Update();

        if (isTaskActive)
        {
            elapsedTaskTime += Time.deltaTime;
            UpdateProgressValue(elapsedTaskTime / assignedTaskTime);

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
        Destroy(gameObject);
    }
}
