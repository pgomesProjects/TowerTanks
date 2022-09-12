using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GoalProgressManager : MonoBehaviour
{
    private Slider progressGoalSlider;

    [SerializeField] private bool isTankMoving;

    [SerializeField][Tooltip("How many seconds it takes to reach the goal.")]
        internal float secondsToGoal = 100f;

    //Min = 0, Max = 100
    private float percent = 0;

    // Start is called before the first frame update
    void Start()
    {
        isTankMoving = true;
        progressGoalSlider = GetComponent<Slider>();
    }

    // Update is called once per frame
    void Update()
    {
        //If the tank is moving, update the goal percentage
        if (isTankMoving)
        {
            //Update the slider while the goal percentage is under 100%
            if (percent >= 100)
            {
                //Win Condition
                //Debug.Log("Goal Reached!");
            }
            else
            {
                UpdateGoalSlider();
            }
        }
    }

    private void UpdateGoalSlider()
    {
        //Add to percent completion and update the slider accordingly
        percent += (1 / (secondsToGoal / 100)) * Time.deltaTime;
        progressGoalSlider.value = percent;
    }
}
