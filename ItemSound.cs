
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using System;

public class ItemSound : UdonSharpBehaviour
{
    [NonSerialized] public double snd_final_start_ms;
    [NonSerialized] public double snd_final_timer;
    [NonSerialized] public bool snd_final_play = false;

    private void Update()
    {
        var duration = 0.0f;

        if (GetComponent<AudioSource>().clip != null) 
        { 
            duration = GetComponent<AudioSource>().clip.length; 
        }
        if (snd_final_play && snd_final_timer < duration)
        {
            snd_final_timer = Networking.CalculateServerDeltaTime(Networking.GetServerTimeInSeconds(), snd_final_start_ms);
        }
        else if (snd_final_play)
        {
            Destroy(gameObject);
        }
    }

    public void FinalPlay()
    {
        snd_final_start_ms = Networking.GetServerTimeInSeconds();
        snd_final_play = true;
    }
}
