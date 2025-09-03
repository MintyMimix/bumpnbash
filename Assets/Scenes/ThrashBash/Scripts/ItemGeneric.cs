
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
    [SerializeField] public Transform spriteItem; // Assign this in inspector
    [SerializeField] public Transform spriteFlag; // Assign this in inspector
    [SerializeField] public Transform spritePole; // Assign this in inspector

    [NonSerialized] [UdonSynced] public int item_state = (int)item_state_name.Spawning;
    [NonSerialized] public int item_owner_id = -1;
    [NonSerialized] public sbyte item_team_id = 0; // -1: all, -2: FFA only
    [NonSerialized] public int item_stored_global_index = -1;
    [NonSerialized] public int item_type;
    [NonSerialized] public bool item_is_template = false;
    [SerializeField] public ItemSpawner spawner_parent;
    [NonSerialized] public bool trigger_destroy = false;
    [NonSerialized] public bool apply_after_spawn = false; // used by item spawner template for item bomb only
    [NonSerialized] public bool allow_effects_to_apply = false; // used as an intermediary state to prevent a powerup from being grabbed while it's being set or reset to prevent replicates
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

        //if (transform.parent == null) { return null; }
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
        if (item_state != (int)item_state_name.InWorld || item_is_template || !allow_effects_to_apply) { return false; }

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

    internal void SetTeamColor(int team_id)
    {
        CheckForSpawnerParent();
        Renderer main_Renderer = GetComponent<MeshRenderer>();
        Renderer[] child_Renderers = GetComponentsInChildren<MeshRenderer>();
        Renderer m_Renderer = main_Renderer;

        bool recolor_children = false;
        if (spawner_parent != null) { recolor_children = spawner_parent.gameController.flag_for_mobile_vr; }

        int team_to_render = 0;
        bool render_for_team = false;
        if (spawner_parent != null) { render_for_team = spawner_parent.gameController.option_teamplay; }

        for (int i = -1; i < Mathf.Max(1, child_Renderers.Length); i++)
        {
            if (i > -1 && recolor_children && child_Renderers.Length > 0) { m_Renderer = child_Renderers[i]; }
            else if (i > -1 && recolor_children && child_Renderers.Length == 0) { break; }
            else if (i > -1 && !recolor_children) { break; }

            if (spawner_parent != null && m_Renderer != null)
            {

                if (render_for_team && team_id >= 0 && team_id < spawner_parent.gameController.team_count) { team_to_render = team_id; }
                //else if (render_for_team && spawner_parent.gameController.local_plyAttr != null && gameController.local_plyAttr.ply_team >= 0) { team_to_render = gameController.local_plyAttr.ply_team; }
                else { render_for_team = false; }

                if (render_for_team)
                {
                    byte alpha = 25; // previously 92
                    if (i > 0) { alpha = 255; }
                    m_Renderer.material.SetColor("_Color",
                            new Color32(
                            (byte)Mathf.Max(0, Mathf.Min(255, spawner_parent.gameController.team_colors_bright[team_to_render].r)),
                            (byte)Mathf.Max(0, Mathf.Min(255, spawner_parent.gameController.team_colors_bright[team_to_render].g)),
                            (byte)Mathf.Max(0, Mathf.Min(255, spawner_parent.gameController.team_colors_bright[team_to_render].b)),
                            (byte)alpha));
                    if (i > 0) { alpha = 0; }
                    m_Renderer.material.EnableKeyword("_EMISSION");
                    m_Renderer.material.SetColor("_EmissionColor",
                        new Color32(
                        (byte)Mathf.Max(0, Mathf.Min(255, -80 + spawner_parent.gameController.team_colors[team_to_render].r)),
                        (byte)Mathf.Max(0, Mathf.Min(255, -80 + spawner_parent.gameController.team_colors[team_to_render].g)),
                        (byte)Mathf.Max(0, Mathf.Min(255, -80 + spawner_parent.gameController.team_colors[team_to_render].b)),
                        (byte)alpha));
                }
                else
                {
                    if (i > 0)
                    {
                        m_Renderer.material.SetColor("_Color", new Color32(255, 255, 255, 255));
                        m_Renderer.material.EnableKeyword("_EMISSION");
                        m_Renderer.material.SetColor("_EmissionColor", new Color32(255, 255, 255, 0)); 
                    }
                    else
                    {
                        m_Renderer.material.SetColor("_Color", new Color32(167, 167, 167, 0));
                        m_Renderer.material.EnableKeyword("_EMISSION");
                        m_Renderer.material.SetColor("_EmissionColor", new Color32(145, 145, 145, 255));
                    }
                }
            }
        }

        if (spriteFlag != null && spawner_parent != null)
        {
            if (render_for_team && team_id >= 0 && team_id < spawner_parent.gameController.team_count) { team_to_render = team_id; }
            //else if (render_for_team && spawner_parent.gameController.local_plyAttr != null && gameController.local_plyAttr.ply_team >= 0) { team_to_render = gameController.local_plyAttr.ply_team; }
            else { render_for_team = false; }

            spriteFlag.gameObject.SetActive(spawner_parent.gameController.option_teamplay && render_for_team);

            if (spriteFlag.gameObject.activeInHierarchy && gameController.local_uiplytoself != null)
            {
                Renderer s_Renderer = spriteFlag.GetComponent<Renderer>();

                bool use_cb_sprite = false;
                if (spawner_parent.gameController.local_ppp_options != null && spawner_parent.gameController.local_ppp_options.colorblind) { use_cb_sprite = true; }
                else { use_cb_sprite = false; }

                s_Renderer.material.SetTexture("_MainTex", spawner_parent.gameController.local_uiplytoself.PTSFlagSprite.texture);
                if (spritePole != null) 
                { 
                    spritePole.gameObject.SetActive(!use_cb_sprite);
                    spritePole.GetComponent<Renderer>().material.SetTexture("_MainTex", spawner_parent.gameController.local_uiplytoself.PTSTeamPoleImage.sprite.texture);
                }

                if (team_id >= 0 && team_id < gameController.team_colors.Length)
                {
                    s_Renderer.material.SetColor("_Color", spawner_parent.gameController.team_colors[team_to_render]);
                    if (use_cb_sprite) { s_Renderer.material.SetTexture("_MainTex", spawner_parent.gameController.team_sprites[team_to_render].texture); }
                }
                else
                {
                    s_Renderer.material.SetColor("_Color", new Color32(255, 255, 255, 255));
                    if (use_cb_sprite) { s_Renderer.material.SetTexture("_MainTex", spawner_parent.gameController.team_sprites[0].texture); }
                }
            }
            else if (spritePole != null)
            {
                spritePole.gameObject.SetActive(spawner_parent.gameController.option_teamplay && render_for_team);
            }
        }
    }

    private void OnEnable()
    {
        SetTeamColor(item_team_id);
    }

    private void OnDisable()
    {
        allow_effects_to_apply = false;
    }

}
