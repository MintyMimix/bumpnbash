
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
    Disabled, Spawnable, Spawning, InWorld, ActiveOnOwner, FadingFromOwner, Destroyed, ENUM_LENGTH
}

public enum item_sfx_index
{
    OtherPickup, ItemExpire, PowerupFade, ENUM_LENGTH
}

[UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]

public class ItemGeneric : UdonSharpBehaviour
{
    [SerializeField] public GameController gameController; // Assign this in inspector

    [NonSerialized] [UdonSynced] public int item_state = (int)item_state_name.Spawning;
    [NonSerialized] public int item_owner_id = -1;
    [NonSerialized] public sbyte item_team_id = 0; // -1: all, -2: FFA only
    [NonSerialized] public int item_stored_global_index = -1;
    [NonSerialized] public int item_type;
    [NonSerialized] public bool item_is_template = false;
    [NonSerialized] public ItemSpawner spawner_parent;
    [NonSerialized] public bool trigger_destroy = false;
    [NonSerialized] public bool allow_multiple_owners = false;

    [SerializeField] public AudioSource item_snd_source;
    [SerializeField] public AudioClip[] item_snd_clips;

    internal bool CheckForSpawnerParent()
    {
        if (spawner_parent != null) { return true; }
        if (gameController == null)
        {
            GameObject gcObj = GameObject.Find("GameController");
            if (gcObj != null) { gameController = gcObj.GetComponent<GameController>(); }
        }

        if (transform.parent == null) { return false; }
        spawner_parent = transform.GetComponentInParent<ItemSpawner>();
        return spawner_parent != null;
    }

    internal bool CheckValidCollisionEvent(Collider other)
    {
        // If this item is not in the world, don't bother checking collisions
        if (item_state != (int)item_state_name.InWorld || item_is_template) { return false; }

        // We also only care if a playerHitbox is colliding with this (layers should make this impossible, but just in case)
        // 2025-07-03 Update: We can also consider WeaponHurtbox, but if only if it belongs to a punching glove
        if (other.GetComponent<PlayerHitbox>() == null && other.GetComponent<WeaponHurtbox>() == null) { return false; }
        else if (other.GetComponent<WeaponHurtbox>() != null && other.GetComponent<WeaponHurtbox>().damage_type != (int)damage_type_name.Strike && other.GetComponent<WeaponHurtbox>().damage_type != (int)damage_type_name.Kapow) { return false; }

        // We only care if someone else got this if this is a free-floating non-template item (i.e. neither handled by a spawner nor created by a player)
        if (Networking.GetOwner(other.gameObject) != Networking.LocalPlayer)
        {
            if (!CheckForSpawnerParent()) { item_state = (int)item_state_name.Destroyed; return false; } //Destroy(gameObject); }
            else { return false; }
        }

        // We are good on checks if this is FFA, but teams need more processing
        if (!gameController.option_teamplay || item_team_id < 0) { return true; }

        var plyAttr = gameController.local_plyAttr;
        if (plyAttr == null) { return false; } // To-do: should this be true?

        return item_team_id == plyAttr.ply_team;

    }

    internal void SetTeamColor(int team_id)
    {
        var m_Renderer = GetComponent<MeshRenderer>();
        if (spawner_parent != null)
        {
            if (spawner_parent.gameController.option_teamplay && team_id >= 0)
            {
                m_Renderer.material.SetColor("_Color",
                    new Color32(
                    (byte)Mathf.Max(0, Mathf.Min(255, 80 + spawner_parent.gameController.team_colors[team_id].r)),
                    (byte)Mathf.Max(0, Mathf.Min(255, 80 + spawner_parent.gameController.team_colors[team_id].g)),
                    (byte)Mathf.Max(0, Mathf.Min(255, 80 + spawner_parent.gameController.team_colors[team_id].b)),
                    (byte)92));
                m_Renderer.material.EnableKeyword("_EMISSION");
                m_Renderer.material.SetColor("_EmissionColor",
                    new Color32(
                    (byte)Mathf.Max(0, Mathf.Min(255, -80 + spawner_parent.gameController.team_colors[team_id].r)),
                    (byte)Mathf.Max(0, Mathf.Min(255, -80 + spawner_parent.gameController.team_colors[team_id].g)),
                    (byte)Mathf.Max(0, Mathf.Min(255, -80 + spawner_parent.gameController.team_colors[team_id].b)),
                    92));
            }
            else
            {
                m_Renderer.material.SetColor("_Color", new Color32(255, 255, 255, 92));
                m_Renderer.material.EnableKeyword("_EMISSION");
                m_Renderer.material.SetColor("_EmissionColor", new Color32(83, 83, 83, 92));
            }
        }
    }

}
