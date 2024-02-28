using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveWithTank : MonoBehaviour
{
    public Transform target; // The target to follow

    private void FixedUpdate()
    {
        if (target == null)
        {
            Debug.LogWarning("MoveWithTank: No target set for " + gameObject.name);
            return;
        }

        // Calculate the difference between the target's position and this object's position
        Vector3 difference =  target.position - transform.position;

        // Add the difference to this object's position
        transform.position += difference;
    }
}