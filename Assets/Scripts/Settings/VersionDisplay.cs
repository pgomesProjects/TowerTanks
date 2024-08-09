using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(GitInfo))]
public class VersionDisplay : MonoBehaviour
{
    [SerializeField, Tooltip("The text to display the version info for.")] private TextMeshProUGUI versionText;

    // Start is called before the first frame update
    void Start()
    {
        string versionName = GitInfo.branchName;
        versionText.text = versionName;
    }
}