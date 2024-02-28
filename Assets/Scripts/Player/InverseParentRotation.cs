using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InverseParentRotation : MonoBehaviour
{
    private void FixedUpdate()
    {
        transform.rotation = Quaternion.identity;
    }
}
