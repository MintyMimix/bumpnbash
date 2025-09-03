
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;


public enum item_type_name
{
    Powerup, Weapon, ENUM_LENGTH
}

public enum item_snd_clips_name
{
    Spawn, PickupOther, ItemExpire, PowerupFade, ENUM_LENGTH
}

public class ItemGeneric : UdonSharpBehaviour
{
    [SerializeField] public GameController gameController; // Assign this in inspector

    [NonSerialized] public int item_type;
    [NonSerialized] public float item_spawn_duration;
    [NonSerialized] public double item_spawn_ms;
    [NonSerialized] public double item_spawn_timer_local = 0.0f;
    [NonSerialized] public double item_spawn_timer_network = 0.0f;

    [SerializeField] public AudioSource item_snd_source;
    [SerializeField] public AudioClip[] item_snd_clips;
    
    // Process the spawn timer. Return true if an event should fire.
    internal bool ProcessSpawnTimer()
    {
        item_spawn_timer_local += Time.deltaTime;
        item_spawn_timer_network = Networking.CalculateServerDeltaTime(Networking.GetServerTimeInSeconds(), item_spawn_ms);
        if (item_spawn_duration > 0 && (item_spawn_timer_local >= item_spawn_duration || item_spawn_timer_network >= item_spawn_duration))
        {
            return true;
        }
        return false;
    }

}
