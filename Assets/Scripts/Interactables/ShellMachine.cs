using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ShellMachine
{
    private static bool isShellActive = false;

    public static bool IsShellActive()
    {
        return isShellActive;
    }

    public static void SetShellActive(bool isActive)
    {
        isShellActive = isActive;
    }

}
