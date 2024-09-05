using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InverseParentRotation : MonoBehaviour
{
    private Character thisCharacter;

    private void Start()
    {
        thisCharacter = transform.parent.GetComponentInParent<Character>();
    }

    private void LateUpdate()
    {
        if (thisCharacter.currentState == Character.CharacterState.CLIMBING)
        {
            transform.rotation = transform.parent.rotation;
        }
        else
        {
            transform.rotation = Quaternion.identity;
        }
    }
}
