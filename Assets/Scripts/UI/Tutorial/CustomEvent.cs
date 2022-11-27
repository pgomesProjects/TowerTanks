using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class CustomEvent : MonoBehaviour
{
    //Template functions that all custom events must have
    public abstract void CheckForCustomEvent(int indexNumber);
    public abstract void CustomOnEventComplete();
}
