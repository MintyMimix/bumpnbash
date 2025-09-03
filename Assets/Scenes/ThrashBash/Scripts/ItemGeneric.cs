
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
    [NonSerialized] public sbyte item_team_id = 0; // -1: all, -2: FFA only
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

    internal bool CheckValidCollisionEvent(Collider other)
    {
        // If this item is not in the world, don't bother checking collisions
        if (item_state != (int)item_state_name.InWorld || item_is_template) { return false; }

        // We also only care if a playerHitbox is colliding with this (layers should make this impossible, but just in case)
        if (other.GetComponent<PlayerHitbox>() == null) { return false; }

        // We only care if someone else got this if this is a free-floating non-template item (i.e. neither handled by a spawner nor created by a player)
        if (Networking.GetOwner(other.gameObject) != Networking.LocalPlayer)
        {
            if (spawner_parent == null) { Destroy(gameObject); }
            else { return false; }
        }

        // We are good on checks if this is FFA, but teams need more processing
        if (!gameController.option_teamplay || item_team_id < 0) { return true; }

        var plyAttr = gameController.FindPlayerAttributes(Networking.LocalPlayer);
        if (plyAttr == null) { return false; } // To-do: should this be true?

        return item_team_id == plyAttr.ply_team;

    }

}
