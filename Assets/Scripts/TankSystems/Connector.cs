using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Connector : MonoBehaviour
{
    //Objects & Components:
    internal Room room;               //The room this connector is a part of
    internal SpriteRenderer backWall; //Renderer for back wall of connector

    //RUNTIME METHODS:
    private void Awake()
    {
        //Get objects & components:
        room = GetComponentInParent<Room>();                             //Get parent room
        backWall = transform.GetChild(0).GetComponent<SpriteRenderer>(); //Get back wall sprite renderer
    }
}
