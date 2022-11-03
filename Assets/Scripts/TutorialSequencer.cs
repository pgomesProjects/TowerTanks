using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TUTORIALSTATE
{
    BUILDFIRSTLAYER,
    BUILDSECONDLAYER,
    BUILDSHELLSTATION,
    BUILDCANNON,
    GETSHELL,
    FIRECANNON,
    BUILDENGINE,
    GIVEENGINEFUEL,
    BUILDTHROTTLE,
    MOVETHROTTLE
}

public class TutorialSequencer : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void AdvanceTutorial()
    {

    }
}
