using UnityEngine.Audio;
using System;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{

    public AudioMixerGroup bgmMixer, sfxMixer;
    public Sound[] sounds;

    void Awake()
    {
        foreach (Sound s in sounds)
        {
/*            s.source = gameObject.AddComponent<AudioSource>();
            s.source.clip = s.clip;

            s.source.pitch = s.pitch;
            s.source.loop = s.loop;

            switch (s.soundType)
            {
                case SOUNDTYPE.BGM:
                    if (sfxMixer != null)
                        s.source.outputAudioMixerGroup = sfxMixer;
                    break;
                case SOUNDTYPE.SFX:
                    if (bgmMixer != null)
                        s.source.outputAudioMixerGroup = bgmMixer;
                    break;
            }*/
        }
    }

    public void Play(string name, float audioVol, GameObject audioLocation = null)
    {
        Sound s = GetSound(name);
        if(audioLocation != null)
            s.audioEvent.Post(audioLocation);
        else
            s.audioEvent.Post(gameObject);
        /*        s.source.Play();
                s.source.volume = audioVol;*/
    }

    public void PlayAtRandomPitch(string name, float audioVol)
    {
        Sound s = GetSound(name);
/*        s.source.PlayOneShot(s.clip, audioVol);

        float pitchRandom = UnityEngine.Random.Range(0.9f, 1.1f);

        s.source.pitch = pitchRandom;*/
    }

    public void PlayAtSection(string name, float audioVol, float start, float stop)
    {
        Sound s = GetSound(name);

/*        AudioClip subClip = MakeSubclip(s.clip, start, stop);

        s.source.PlayOneShot(subClip, audioVol);

        s.source.pitch = 1;*/
    }

    private AudioClip MakeSubclip(AudioClip clip, float start, float stop)
    {
        /*        int frequency = clip.frequency;
                int channels = clip.channels;

                float timeLength = stop - start;
                int samplesLength = (int)(frequency * timeLength);

                AudioClip newClip = AudioClip.Create(clip.name + "-sub", samplesLength, channels, frequency, false);

                float[] data = new float[samplesLength];
                clip.GetData(data, (int)(frequency * start));
                newClip.SetData(data, 0);

                return newClip;*/
        return null;
    }

/*    public void PlayOneShot(string name, float audioVol)
    {
        Sound s = GetSound(name);
        s.audioEvent.Post(gameObject);
        //s.source.PlayOneShot(s.clip, audioVol);
    }*/

    public bool IsPlaying(string name, GameObject playLocation = null) //Checks to see if the audio track is still playing
    {
        Sound s = GetSound(name);

        uint[] playingIDs = new uint[sounds.Length];

        uint playingEventID = AkSoundEngine.GetIDFromString(s.audioEvent.Name);
        uint count = (uint)playingIDs.Length;

        if(playLocation != null)
            AkSoundEngine.GetPlayingIDsFromGameObject(playLocation, ref count, playingIDs);
        else
            AkSoundEngine.GetPlayingIDsFromGameObject(gameObject, ref count, playingIDs);

        for (int i = 0; i < count; i++)
        {
            uint playingID = playingIDs[i];
            uint eventID = AkSoundEngine.GetEventIDFromPlayingID(playingID);

            if (eventID == playingEventID)
                return true;
        }

        return false;

    }//end of isPlaying

    public void ChangeVolume(string name, float audioVol)
    {
        Sound s = GetSound(name);
        //s.source.volume = audioVol;
    }

    public void ChangePitch(string name, float audioPitch)
    {
        Sound s = GetSound(name);
        //s.source.pitch = audioPitch;
    }

    public void Pause(string name) //Pauses audio
    {
        Sound s = GetSound(name);
        //s.source.Pause();
    }

    public void PauseAllSounds()
    {
        AkSoundEngine.Suspend();
/*        foreach (Sound s in sounds)
        {
            s.source.Pause();
        }*/
    }

    public void Resume(string name) //Resumes audio that was paused
    {
        Sound s = GetSound(name);
        //s.source.UnPause();
    }

    public void ResumeAllSounds()
    {
        AkSoundEngine.WakeupFromSuspend();
/*        foreach (Sound s in sounds)
        {
            s.source.UnPause();
        }*/
    }

    public void Stop(string name, GameObject audioLocation = null) //Stops a sound
    {
        Sound s = GetSound(name);
        if(audioLocation != null)
            s.audioEvent.Stop(audioLocation);
        else
            s.audioEvent.Stop(gameObject);
        //s.source.Stop();
    }

    public void StopAllSounds()
    {
        AkSoundEngine.StopAll();
/*        foreach (Sound s in sounds)
        {
            s.source.Stop();
        }*/
    }

    public void UpdateAllVolumes()
    {
/*        foreach (Sound s in sounds)
        {
            switch (s.soundType)
            {
                case SOUNDTYPE.BGM:
                    s.source.volume = PlayerPrefs.GetFloat("BGMVolume", GameSettings.defaultBGMVolume);
                    break;
                case SOUNDTYPE.SFX:
                    s.source.volume = PlayerPrefs.GetFloat("SFXVolume", GameSettings.defaultSFXVolume);
                    break;
            }
        }*/
    }

    private Sound GetSound(string name) => Array.Find(sounds, sound => sound.name == name);
    public string GetEventName(string name) => GetSound(name).audioEvent.Name;
    public float GetSoundLength(string name) => 0f;//GetSound(name).clip.length;
}
