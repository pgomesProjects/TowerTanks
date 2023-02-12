using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public static class ConnectionController
{
    private static List<InputDevice> connectedDevices = new List<InputDevice>();

    public static int CheckForIndex()
    {
        //Check to see which index to give the newest player. Newest player gets the smallest index with no player connected
        for (int i = 0; i < MultiplayerManager.connectedControllers.Length; i++) {
            if(MultiplayerManager.connectedControllers[i] == false)
                return i;
        }

        //Return -1 if players are full
        return -1;
    }

    public static int NumberOfActivePlayers()
    {
        int activePlayers = 0;
        foreach (var i in MultiplayerManager.connectedControllers)
        {
            if (i)
            {
                activePlayers++;
            }
        }

        return activePlayers;
    }

    public static bool PlayersFull()
    {
        foreach (var i in MultiplayerManager.connectedControllers)
        {
            if (i == false)
                return false;
        }

        return true;
    }
}
