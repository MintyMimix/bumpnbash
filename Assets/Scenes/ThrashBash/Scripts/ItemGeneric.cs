
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
    //[NonSerialized] public bool allow_multiple_owners = false;

    [SerializeField] public AudioSource item_snd_source;
    [SerializeField] public AudioClip[] item_snd_clips;

    internal ItemSpawner GetSpawnerParent()
    {
        if (spawner_parent != null) { return spawner_parent; }
        if (gameController == null)
        {
            GameObject gcObj = GameObject.Find("GameController");
            if (gcObj != null) { gameController = gcObj.GetComponent<GameController>(); }
        }

        if (transform.parent == null) { return null; }
        spawner_parent = transform.GetComponentInParent<ItemSpawner>();
        return spawner_parent;
    }

    internal bool CheckForSpawnerParent()
    {
        if (spawner_parent != null) { return true; }
        spawner_parent = GetSpawnerParent();
        return spawner_parent != null;
    }

    internal bool CheckValidCollisionEvent(Collider other)
    {
        // If this item is not in the world, don't bother checking collisions
        if (item_state != (int)item_state_name.InWorld || item_is_template) { return false; }

        // We also only care if a playerHitbox is colliding with this (layers should make this impossible, but just in case)
        // 2025-07-03 Update: We can also consider WeaponHurtbox, but if only if it belongs to a punching glove
        bool isPlayer = other.gameObject.layer == LayerMask.GetMask("Player") || other.gameObject.layer == LayerMask.GetMask("PlayerLocal");
        if (other.GetComponent<PlayerHitbox>() == null && other.GetComponent<WeaponHurtbox>() == null && !isPlayer) { return false; }
        else if (other.GetComponent<WeaponHurtbox>() != null && other.GetComponent<WeaponHurtbox>().damage_type != (int)damage_type_name.Strike && other.GetComponent<WeaponHurtbox>().damage_type != (int)damage_type_name.Kapow) { return false; }

        // We only care if someone else got this if this is a free-floating non-template item (i.e. neither handled by a spawner nor created by a player)
        if (Networking.GetOwner(other.gameObject) != Networking.LocalPlayer)
        {
            if (!CheckForSpawnerParent()) { item_state = (int)item_state_name.Destroyed; return false; } //Destroy(gameObject); }
            else { return false; }
        }

        // We are good on checks if this is FFA, but teams need more processing
        if (!gameController.option_teamplay || item_team_id < 0) { return true; }

        PlayerAttributes plyAttr = gameController.local_plyAttr;
        if (plyAttr == null) { return false; } // To-do: should this be true?

        return item_team_id == plyAttr.ply_team;

    }

    internal void SetTeamColor(int team_id, bool recolor_children = false)
    {
        Renderer main_Renderer = GetComponent<MeshRenderer>();
        Renderer[] child_Renderers = GetComponentsInChildren<MeshRenderer>();
        Renderer m_Renderer = main_Renderer;
        for (int i = -1; i < child_Renderers.Length; i++)
        {
            if (i > -1) { m_Renderer = child_Renderers[i]; }
            else if (!recolor_children) { break; }

            if (spawner_parent != null && m_Renderer != null)
            {
                int team_to_render = 0;
                bool render_for_team = spawner_parent.gameController.option_teamplay;
                if (render_for_team && team_id >= 0 && team_id < spawner_parent.gameController.team_count) { team_to_render = team_id; }
                else if (render_for_team && spawner_parent.gameController.local_plyAttr != null && gameController.local_plyAttr.ply_team >= 0) { team_to_render = gameController.local_plyAttr.ply_team; }
                else { render_for_team = false; }

                if (render_for_team)
                {
                    m_Renderer.material.SetColor("_Color",
                            new Color32(
                            (byte)Mathf.Max(0, Mathf.Min(255, spawner_parent.gameController.team_colors_bright[team_to_render].r)),
                            (byte)Mathf.Max(0, Mathf.Min(255, spawner_parent.gameController.team_colors_bright[team_to_render].g)),
                            (byte)Mathf.Max(0, Mathf.Min(255, spawner_parent.gameController.team_colors_bright[team_to_render].b)),
                            (byte)92));
                    m_Renderer.material.EnableKeyword("_EMISSION");
                    m_Renderer.material.SetColor("_EmissionColor",
                        new Color32(
                        (byte)Mathf.Max(0, Mathf.Min(255, -80 + spawner_parent.gameController.team_colors[team_to_render].r)),
                        (byte)Mathf.Max(0, Mathf.Min(255, -80 + spawner_parent.gameController.team_colors[team_to_render].g)),
                        (byte)Mathf.Max(0, Mathf.Min(255, -80 + spawner_parent.gameController.team_colors[team_to_render].b)),
                        (byte)92));
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

}
