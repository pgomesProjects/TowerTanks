using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Diagnostics;

public class GitInfo : MonoBehaviour
{
    public static string branchName;

    void Awake()
    {
        branchName = GetCurrentGitBranch();
        UnityEngine.Debug.Log("Current Git Branch: " + branchName);

        UnityEditor.PlayerSettings.bundleVersion = branchName;
        UnityEditor.AssetDatabase.SaveAssets();
    }

    /// <summary>
    /// Returns the name of the current active git branch.
    /// </summary>
    /// <returns>The name of the Git branch.</returns>
    private string GetCurrentGitBranch()
    {
        string gitDir = Path.Combine(Application.dataPath, "../.git");

        //If the git folder cannot be found, return the application version instead
        if (!Directory.Exists(gitDir))
        {
            UnityEngine.Debug.LogError("Git directory not found. Make sure your project is under Git version control. (Getting version info from build settings instead.)");
            return Application.version;
        };

        try
        {
            ProcessStartInfo startInfo = new ProcessStartInfo("git")
            {
                WorkingDirectory = Application.dataPath,
                Arguments = "rev-parse --abbrev-ref HEAD",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = Process.Start(startInfo))
            {
                using (StreamReader reader = process.StandardOutput)
                {
                    string result = reader.ReadToEnd().Trim();
                    return result;
                }
            }

        }

        //If the git information cannot be found, return the application version instead
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogError("Error getting Git branch: " + ex.Message + "\nGetting version info from build settings instead.");
            return Application.version;
        }
    }


}
