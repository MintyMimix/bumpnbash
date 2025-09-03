
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ItemWeapon : ItemGeneric
{
    [NonSerialized] public int iweapon_type;
    [NonSerialized] public int iweapon_ammo = -1;
    [NonSerialized] public float iweapon_duration = -1.0f;
    [NonSerialized] public byte iweapon_extra_data = 0;

    [NonSerialized] public bool render_iweapon = true;

    [SerializeField] public AudioClip[] iweapon_snd_clips;
    [SerializeField] public Sprite[] iweapon_sprites;
    [SerializeField] public int[] iweapon_ammo_list;
    [SerializeField] public float[] iweapon_duration_list;


    private void Start()
    {
        if (gameController == null)
        {
            GameObject gcObj = GameObject.Find("GameController");
            if (gcObj != null) { gameController = gcObj.GetComponent<GameController>(); }
        }
        CheckForSpawnerParent();
    }
   
    public void SetiWeaponStats()
    {
        if (iweapon_type >= 0 & iweapon_type < iweapon_sprites.Length)
        {
            foreach (Transform child in transform)
            {
                if (child.name.Contains("ItemSprite"))
                {
                    var m_Renderer = child.GetComponent<Renderer>();
                    m_Renderer.material.SetTexture("_MainTex", iweapon_sprites[iweapon_type].texture);
                }
            }
        }

        if (iweapon_type >= 0 && iweapon_type < iweapon_ammo_list.Length && iweapon_ammo_list[iweapon_type] != 2) { iweapon_ammo = iweapon_ammo_list[iweapon_type]; }
        if (iweapon_type >= 0 && iweapon_type < iweapon_duration_list.Length && iweapon_duration_list[iweapon_type] != 2) { iweapon_duration = iweapon_duration_list[iweapon_type]; }
    }

    private void Update()
    {
        var m_Renderer = GetComponentInChildren<Renderer>();

        // If powerup is a template, make sure it doesn't render in the world
        if (m_Renderer.enabled && (item_is_template || !render_iweapon))
        {
            m_Renderer.enabled = false;
            foreach (Transform child in transform)
            {
                if (child.name.Contains("ItemSprite"))
                {
                    var m_Renderer_child = child.GetComponent<Renderer>();
                    m_Renderer_child.enabled = false;
                }
            }
        }

        // Events which only run when the timer ticks to zero below
        if (item_state == (int)item_state_name.Disabled) { return; }
        if (item_state == (int)item_state_name.Destroyed)
        {
            Destroy(gameObject);
        }

        if (gameController == null)
        {
            GameObject gcObj = GameObject.Find("GameController");
            if (gcObj != null) { gameController = gcObj.GetComponent<GameController>(); }
        }

    }
    private void LateUpdate()
    {
        if (item_state == (int)item_state_name.InWorld && apply_after_spawn && spawner_parent != null)
        {
            if (gameController != null & gameController.local_plyhitbox != null) { OnTriggerEnter(gameController.local_plyhitbox.GetComponent<Collider>()); }
        }
        else if (item_state == (int)item_state_name.InWorld && !apply_after_spawn)
        {
            allow_effects_to_apply = true;
        }
    }

    private void FixedUpdate()
    {
        transform.rotation = Networking.LocalPlayer.GetRotation();
        if (item_owner_id > -1)
        {
            transform.position = VRCPlayerApi.GetPlayerById(item_owner_id).GetPosition();
            item_snd_source.transform.position = VRCPlayerApi.GetPlayerById(item_owner_id).GetPosition();
        }
    }

    public void OnTriggerEnter(Collider other)
    {
        // Check if the player colliding with this is valid
        if (!CheckValidCollisionEvent(other)) { return; }
        allow_effects_to_apply = false;
        // Apply powerups to self. Player gets a local copy that can't be touched but acts as a template to be read off of for plyAttr, which will store of a list of these objects and destroy as needed
        PlayerWeapon plyWeapon = gameController.local_plyweapon;
        bool player_is_boss = plyWeapon.weapon_type == (int)weapon_type_name.BossGlove && gameController.option_gamemode == (int)gamemode_name.BossBash && gameController.local_plyAttr.ply_team == 1;
        if (plyWeapon != null && !player_is_boss)
        {
            item_is_template = true; // Temporarily set template status of self to true, then reset at end of instantiate
            plyWeapon.weapon_temp_ammo = iweapon_ammo;
            plyWeapon.weapon_temp_duration = iweapon_duration;
            plyWeapon.weapon_temp_timer = 0.0f;
            plyWeapon.weapon_type = iweapon_type;
            plyWeapon.weapon_extra_data = iweapon_extra_data;
            plyWeapon.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "UpdateStatsFromWeaponType");
            if (iweapon_snd_clips != null && iweapon_type >= 0 && iweapon_type < iweapon_snd_clips.Length)
            {
                if (gameController.local_plyAttr != null)
                {
                    gameController.local_plyAttr.SendTutorialMessage((int)powerup_type_name.ENUM_LENGTH + iweapon_type);
                    if (gameController.local_plyAttr.ply_training) { gameController.local_plyAttr.ResetTutorialMessage((int)powerup_type_name.ENUM_LENGTH + iweapon_type); }
                }
                gameController.PlaySFXFromArray(plyWeapon.snd_source_weaponcharge, iweapon_snd_clips, iweapon_type);
                //Debug.Log(gameObject.name + ": Attempting to play sound " + iweapon_snd_clips[iweapon_type].name + " for type " + iweapon_type);
            }
            if (gameController.local_uiplytoself != null && iweapon_type >= 0 && iweapon_sprites != null && iweapon_type < iweapon_sprites.Length) { gameController.local_uiplytoself.PTSWeaponSprite.sprite = iweapon_sprites[iweapon_type]; }
            item_is_template = false;
        }
        else if (plyWeapon != null && player_is_boss)
        {
            gameController.PlaySFXFromArray(plyWeapon.snd_source_weaponcharge, item_snd_clips, (int)item_snd_clips_name.Spawn);
        }

        // Despawn powerup for everyone else, with reason code of "someone else got it"
        // This does mean that it's possible that two people can get the same powerup due to lag, but that's a fun bonus!
        //item_snd_source.transform.position = spawner_parent.transform.position;
        bool has_spawner_parent = CheckForSpawnerParent();
        if (has_spawner_parent && !spawner_parent.is_template)
        {
            spawner_parent.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "DespawnItem", (int)item_snd_clips_name.PickupOther, Networking.LocalPlayer.playerId, true);
        }
        else if (has_spawner_parent && spawner_parent.is_template)
        {
            spawner_parent.DespawnItem((int)item_snd_clips_name.PickupOther, Networking.LocalPlayer.playerId, false);
        }
        else if (!has_spawner_parent && item_state != (int)item_state_name.Spawning)
        {
            item_state = (int)item_state_name.Destroyed;
        }

    }

    public override void OnPlayerTriggerEnter(VRCPlayerApi player)
    {
        OnTriggerEnter(gameController.FindPlayerOwnedObject(player, "PlayerHitbox").GetComponent<Collider>());
    }

}
