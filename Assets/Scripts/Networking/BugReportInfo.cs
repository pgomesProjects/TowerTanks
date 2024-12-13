using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BugSeverity { SEVERE = 1, MODERATE = 2, MILD = 3 }

public class BugReportInfo
{
    public BugSeverity bugSeverity { get; private set; }
    public string title { get; private set; }
    public string description { get; private set; }
    public string gameVersion { get; private set; }

    public BugReportInfo()
    {
        this.title = "Found a test bug.";
        this.gameVersion = "Version: " + Application.version;
        this.description = "This is a test description.";
        this.bugSeverity = BugSeverity.MILD;
    }

    /// <summary>
    /// Prints a description based on the type of severity level provided.
    /// </summary>
    /// <param name="severity">The severity level of the bug.</param>
    /// <returns>Returns a description of the bug severity.</returns>
    public static string GetSeverityDescription(BugSeverity severity)
    {
        switch (severity)
        {
            case BugSeverity.MILD:
                return "Mild: Small bugs with no real gameplay impact. These issues are more cosmetic or non-intrusive.";
            case BugSeverity.MODERATE:
                return "Moderate: Bugs that impact gameplay significantly but do not stop progress. These bugs might cause frustration.";
            case BugSeverity.SEVERE:
                return "Severe: Game-breaking bugs, crashes, or major issues that heavily affect gameplay experience.";
            default:
                return "Unknown severity type.";
        }
    }
}

public class PlayerSystemSpecs
{
    public string operatingSystem { get; private set; }
    public string deviceModel { get; private set; }
    public string processorType { get; private set; }
    public int processorCount { get; private set; }
    public string graphicsDeviceName { get; private set; }
    public string graphicsAPI { get; private set; }
    public int graphicsMemorySize { get; private set; }
    public int systemMemorySize { get; private set; }

    public PlayerSystemSpecs()
    {
        operatingSystem = SystemInfo.operatingSystem;
        deviceModel = SystemInfo.deviceModel;
        processorType = SystemInfo.processorType;
        processorCount = SystemInfo.processorCount;
        graphicsDeviceName = SystemInfo.graphicsDeviceName;
        graphicsAPI = SystemInfo.graphicsDeviceVersion;
        graphicsMemorySize = SystemInfo.graphicsMemorySize;
        systemMemorySize = SystemInfo.systemMemorySize;
    }

    /// <summary>
    /// Prints all of the user's system information.
    /// </summary>
    /// <returns>Returns the system information.</returns>
    public override string ToString()
    {
        string log = "===System Information===\n";

        log += "Operating System: " + operatingSystem + "\n";
        log += "Device Model: " + deviceModel + "\n";
        log += "Processor Type: " + processorType + "\n";
        log += "Processor Count: " + processorCount + "\n";
        log += "Graphics Device: " + graphicsDeviceName + "\n";
        log += "Graphics API: " + graphicsAPI + "\n";
        log += "Graphics Memory Size: " + graphicsMemorySize + " MB (" + (graphicsMemorySize / 1024f).ToString("F2") + " GB)\n";
        log += "System Memory Size: " + systemMemorySize + " MB (" + (systemMemorySize / 1024f).ToString("F2") + " GB)";

        return log;
    }
}
