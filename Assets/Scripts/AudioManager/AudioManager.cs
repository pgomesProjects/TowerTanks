using UnityEngine.Audio;
using System;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Tooltip("The list of sounds that will be played in the game.")] public Sound[] sounds;

    List<GameObject> registeredGameObjects = new List<GameObject>();    //A list of all registered game objects in Wwise

    private void Awake()
    {
        AkSoundEngine.RegisterGameObj(gameObject);  //Registers the AudioManager as a GameObject for sound to play it
        registeredGameObjects.Add(gameObject);
    }

    /// <summary>
    /// Plays a sound using the Wwise Post event system.
    /// </summary>
    /// <param name="name">The name of the sound.</param>
    /// <param name="audioVol">The volume of the sound.</param>
    /// <param name="audioLocation">The game object that the event will be played on.</param>
    public void Play(string name, GameObject audioLocation = null)
    {
        Sound s = GetSound(name);
        if(audioLocation != null)
        {
            //If this GameObject is not registered to play sounds, make sure its registered
            if (!AkSoundEngine.IsGameObjectRegistered(audioLocation))
            {
                AkSoundEngine.RegisterGameObj(audioLocation);
                registeredGameObjects.Add(audioLocation);
            }

            s.audioEvent.Post(audioLocation);
        }
        else
            s.audioEvent.Post(gameObject);
    }

    /// <summary>
    /// Plays the section of a sound.
    /// </summary>
    /// <param name="name">The name of the sound.</param>
    /// <param name="audioVol">The volume of the sound.</param>
    /// <param name="start">The start position for the sound to be played at (in seconds).</param>
    /// <param name="stop">The end position for the sound to be played at (in seconds).</param>
    public void PlayAtSection(string name, float start, float stop)
    {
        Sound s = GetSound(name);

/*        AudioClip subClip = MakeSubclip(s.clip, start, stop);

        s.source.PlayOneShot(subClip, audioVol);

        s.source.pitch = 1;*/
    }

/*    private AudioClip MakeSubclip(AudioClip clip, float start, float stop)
    {
        *//*        int frequency = clip.frequency;
                int channels = clip.channels;

                float timeLength = stop - start;
                int samplesLength = (int)(frequency * timeLength);

                AudioClip newClip = AudioClip.Create(clip.name + "-sub", samplesLength, channels, frequency, false);

                float[] data = new float[samplesLength];
                clip.GetData(data, (int)(frequency * start));
                newClip.SetData(data, 0);

                return newClip;*//*
        return null;
    }*/

    /// <summary>
    /// Checks to see if an audio track is actively playing.
    /// </summary>
    /// <param name="name">The name of the sound.</param>
    /// <param name="playLocation">The game object that the Wwise event is attached to.</param>
    /// <returns>If true, the sound searched for is playing. If false, the sound is not playing.</returns>
    public bool IsPlaying(string name, GameObject playLocation = null)
    {
        Sound s = GetSound(name);
        uint[] playingIDs = new uint[sounds.Length];

        //Get the ID of the event from the sound engine
        uint playingEventID = AkSoundEngine.GetIDFromString(s.audioEvent.Name);
        uint count = (uint)playingIDs.Length;

        //Populate the playingIDs array with all events that are playing from the GameObject
        if(playLocation != null)
            AkSoundEngine.GetPlayingIDsFromGameObject(playLocation, ref count, playingIDs);
        else
            AkSoundEngine.GetPlayingIDsFromGameObject(gameObject, ref count, playingIDs);

        //Check to see if the event ID being searched for is in the list of event IDs being played
        for (int i = 0; i < count; i++)
        {
            uint playingID = playingIDs[i];
            uint eventID = AkSoundEngine.GetEventIDFromPlayingID(playingID);

            if (eventID == playingEventID)
                return true;
        }

        return false;

    }

    /// <summary>
    /// Changes the volume of a sound.
    /// </summary>
    /// <param name="name">The name of the sound.</param>
    /// <param name="audioVol">The new volume for the sound.</param>
    public void ChangeVolume(string name, float audioVol)
    {
        Sound s = GetSound(name);
    }

    /// <summary>
    /// Changes the pitch of a sound.
    /// </summary>
    /// <param name="name">The name of the sound.</param>
    /// <param name="audioPitch">The new pitch for the sound.</param>
    public void ChangePitch(string name, float audioPitch)
    {
        Sound s = GetSound(name);
    }

    /// <summary>
    /// Pauses a sound event.
    /// </summary>
    /// <param name="name">The name of the sound.</param>
    public void Pause(string name)
    {
        Sound s = GetSound(name);
    }

    /// <summary>
    /// Suspends all active sound events.
    /// </summary>
    public void PauseAllSounds()
    {
        //AkSoundEngine.Suspend();
        AkSoundEngine.PostEvent("Global_Pause", gameObject);
    }

    /// <summary>
    /// Resumes the sound event, if paused.
    /// </summary>
    /// <param name="name">The name of the sound.</param>
    public void Resume(string name)
    {
        Sound s = GetSound(name);
    }

    /// <summary>
    /// Resumes all sounds events that are suspended.
    /// </summary>
    public void ResumeAllSounds()
    {
        AkSoundEngine.PostEvent("Global_Unpause", gameObject);
    }

    /// <summary>
    /// Stops a sound event.
    /// </summary>
    /// <param name="name">The name of the sound.</param>
    /// <param name="audioLocation">The game object that the Wwise event is attached to.</param>
    public void Stop(string name, GameObject audioLocation = null)
    {
        Sound s = GetSound(name);
        if(audioLocation != null)
            s.audioEvent.Stop(audioLocation);
        else
            s.audioEvent.Stop(gameObject);
    }

    /// <summary>
    /// Stops all active sound events.
    /// </summary>
    public void StopAllSounds()
    {
        AkSoundEngine.StopAll();
    }

    public void UpdateMusicVolume()
    {
        AkSoundEngine.SetRTPCValue("MusicVolume", PlayerPrefs.GetFloat("BGMVolume", GameSettings.defaultBGMVolume) * 100f);
    }

    public void UpdateSFXVolume()
    {
        AkSoundEngine.SetRTPCValue("SFXVolume", PlayerPrefs.GetFloat("SFXVolume", GameSettings.defaultSFXVolume) * 100f);
    }

    private Sound GetSound(string name) => Array.Find(sounds, sound => sound.name == name);
    public string GetEventName(string name) => GetSound(name).audioEvent.Name;
    public float GetSoundLength(string name) => 0f;

    private void OnDestroy()
    {
        AkSoundEngine.UnregisterAllGameObj();
        registeredGameObjects.Clear();
    }
}
