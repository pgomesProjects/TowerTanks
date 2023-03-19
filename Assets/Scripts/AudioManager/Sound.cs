using UnityEngine.Audio;
using UnityEngine;

public enum SOUNDTYPE { BGM, SFX }

[System.Serializable]
public class Sound
{
    public string name;

    public AK.Wwise.Event audioEvent;

    [Range(.1f, 3f)]
    public float pitch = 1;

    public bool loop;

    public SOUNDTYPE soundType;

    //internal AudioSource source;

}