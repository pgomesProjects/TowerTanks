using UnityEngine.Audio;
using System;
using UnityEngine;

public class AudioManager : MonoBehaviour
{

    public AudioMixerGroup bgmMixer, sfxMixer;
    public Sound[] sounds;

    void Awake()
    {
        foreach (Sound s in sounds)
        {
            s.source = gameObject.AddComponent<AudioSource>();
            s.source.clip = s.clip;

            s.source.pitch = s.pitch;
            s.source.loop = s.loop;

            //If the sound does not loop, treat as a sound effect and give it a SFX audio mixer
            if (!s.loop)
            {
                if(sfxMixer != null)
                    s.source.outputAudioMixerGroup = sfxMixer;
            }
            //If the sound loops, treat as background music and give it a BGM audio mixer
            else
            {
                if(bgmMixer != null)
                    s.source.outputAudioMixerGroup = bgmMixer;
            }
        }
    }

    public void Play(string name, float audioVol)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        s.source.Play();
        s.source.volume = audioVol;
    }

    public void PlayAtRandomPitch(string name, float audioVol)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        s.source.PlayOneShot(s.clip, audioVol);

        float pitchRandom = UnityEngine.Random.Range(0.9f, 1.1f);

        s.source.pitch = pitchRandom;
    }

    public void PlayOneShot(string name, float audioVol)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        s.source.PlayOneShot(s.clip, audioVol);
    }

    public bool IsPlaying(string name) //Checks to see if the audio track is still playing
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        return s.source.isPlaying;
    }//end of isPlaying

    public void ChangeVolume(string name, float audioVol)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        s.source.volume = audioVol;
    }

    public void ChangePitch(string name, float audioPitch)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        s.source.pitch = audioPitch;
    }

    public void Pause(string name) //Pauses audio
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        s.source.Pause();
    }

    public void PauseAllSounds()
    {
        foreach (Sound s in sounds)
        {
            s.source.Pause();
        }
    }

    public void Resume(string name) //Resumes audio that was paused
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        s.source.UnPause();
    }

    public void ResumeAllSounds()
    {
        foreach (Sound s in sounds)
        {
            s.source.UnPause();
        }
    }

    public void Stop(string name) //Stops a sound
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        s.source.Stop();
    }

    public void StopAllSounds()
    {
        foreach (Sound s in sounds)
        {
            s.source.Stop();
        }
    }
}
