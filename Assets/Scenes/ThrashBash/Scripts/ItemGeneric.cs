
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

public enum item_state_name
{
    Disabled, Spawnable, InWorld, ActiveOnOwner, FadingFromOwner, ENUM_LENGTH
}

public enum item_sfx_index
{
    OtherPickup, ItemExpire, PowerupFade, ENUM_LENGTH
}

public class ItemGeneric : UdonSharpBehaviour
{
    [SerializeField] public GameController gameController; // Assign this in inspector

    [NonSerialized] [UdonSynced] public int item_state = 0;
    [NonSerialized] public int item_owner_id = -1;
    [NonSerialized] public int item_stored_global_index = -1;
    [NonSerialized] public int item_type;
    [NonSerialized] public bool item_is_template = false;
    [NonSerialized] public ItemSpawner spawner_parent;

    [SerializeField] public AudioSource item_snd_source;
    [SerializeField] public AudioClip[] item_snd_clips;

    internal bool CheckForSpawnerParent()
    {
        if (transform.parent == null) { return false; }
        if (transform.GetComponentInParent<ItemSpawner>() == null) { return false; }
        spawner_parent = transform.GetComponentInParent<ItemSpawner>();
        return true;
    }

}
