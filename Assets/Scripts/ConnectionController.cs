using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ConnectionController
{
    internal static bool[] connectedControllers = {false, false, false, false};

    public static int CheckForIndex()
    {
        //Check to see which index to give the newest player. Newest player gets the smallest index with no player connected
        for (int i = 0; i < connectedControllers.Length; i++) {
            if(connectedControllers[i] == false)
                return i;
        }

        //Return -1 if players are full
        return -1;
    }
}
