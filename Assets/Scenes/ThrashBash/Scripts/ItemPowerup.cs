
using System;
using System.Diagnostics;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

public enum powerup_type_name // NOTE: NEEDS TO ALSO BE CHANGED IN GAMECONTROLLER IF ANY ARE ADDED/REMOVED FOR KeyToPowerupType()
{
    SizeUp, SizeDown, SpeedUp, AtkUp, DefUp, AtkDown, DefDown, LowGrav, PartialHeal, FullHeal, Fallback, ENUM_LENGTH
}

public enum powerup_stat_name
{
   Scale, Speed, Atk, Def, Grav, Damage, ENUM_LENGTH
}

public enum powerup_stat_behavior_name
{
    Null, Set, Add, Multiply, ENUM_LENGTH
}

public class ItemPowerup : ItemGeneric
{
    [NonSerialized] public int powerup_type;
    [NonSerialized] public float powerup_duration;
    [NonSerialized] public double powerup_start_ms;
    [NonSerialized] public double powerup_timer_local = 0.0f;
    [NonSerialized] public double powerup_timer_network = 0.0f;

    [SerializeField] public AudioClip[] powerup_snd_clips;
    [SerializeField] public Sprite[] powerup_sprites;

    [NonSerialized] public float[] powerup_stat_value;
    [NonSerialized] public int[] powerup_stat_behavior;

    public void SetPowerupStats(int pType)
    {
        powerup_stat_value = new float[(int)powerup_stat_name.ENUM_LENGTH];
        powerup_stat_behavior = new int[(int)powerup_stat_name.ENUM_LENGTH];

        // Default values
        powerup_stat_value[(int)powerup_stat_name.Scale] = 1.0f;
        powerup_stat_behavior[(int)powerup_stat_name.Scale] = (int)powerup_stat_behavior_name.Null;
        powerup_stat_value[(int)powerup_stat_name.Speed] = 1.0f;
        powerup_stat_behavior[(int)powerup_stat_name.Speed] = (int)powerup_stat_behavior_name.Null;
        powerup_stat_value[(int)powerup_stat_name.Atk] = 1.0f;
        powerup_stat_behavior[(int)powerup_stat_name.Atk] = (int)powerup_stat_behavior_name.Null;
        powerup_stat_value[(int)powerup_stat_name.Def] = 1.0f;
        powerup_stat_behavior[(int)powerup_stat_name.Def] = (int)powerup_stat_behavior_name.Null;
        powerup_stat_value[(int)powerup_stat_name.Grav] = 1.0f;
        powerup_stat_behavior[(int)powerup_stat_name.Grav] = (int)powerup_stat_behavior_name.Null;
        powerup_stat_value[(int)powerup_stat_name.Damage] = 0.0f;
        powerup_stat_behavior[(int)powerup_stat_name.Damage] = (int)powerup_stat_behavior_name.Null;

        foreach (Transform child in transform)
        {
            if (child.name.Contains("ItemSprite"))
            {
                var m_Renderer = child.GetComponent<Renderer>();
                m_Renderer.material.SetTexture("_MainTex", powerup_sprites[pType].texture);
            }
        }
        

        switch (pType)
        {
            case (int)powerup_type_name.SizeUp:
                powerup_stat_value[(int)powerup_stat_name.Scale] = 2.0f;
                powerup_stat_behavior[(int)powerup_stat_name.Scale] = (int)powerup_stat_behavior_name.Multiply;
                break;
            case (int)powerup_type_name.SizeDown:
                powerup_stat_value[(int)powerup_stat_name.Scale] = (1.0f/2.0f);
                powerup_stat_behavior[(int)powerup_stat_name.Scale] = (int)powerup_stat_behavior_name.Multiply;
                break;
            case (int)powerup_type_name.SpeedUp:
                powerup_stat_value[(int)powerup_stat_name.Speed] = 1.5f;
                powerup_stat_behavior[(int)powerup_stat_name.Speed] = (int)powerup_stat_behavior_name.Add;
                break;
            case (int)powerup_type_name.AtkUp:
                powerup_stat_value[(int)powerup_stat_name.Atk] = 1.5f;
                powerup_stat_behavior[(int)powerup_stat_name.Atk] = (int)powerup_stat_behavior_name.Multiply;
                break;
            case (int)powerup_type_name.DefUp:
                powerup_stat_value[(int)powerup_stat_name.Def] = 1.5f;
                powerup_stat_behavior[(int)powerup_stat_name.Def] = (int)powerup_stat_behavior_name.Multiply;
                break;
            case (int)powerup_type_name.AtkDown:
                powerup_stat_value[(int)powerup_stat_name.Atk] = 0.5f;
                powerup_stat_behavior[(int)powerup_stat_name.Atk] = (int)powerup_stat_behavior_name.Multiply;
                break;
            case (int)powerup_type_name.DefDown:
                powerup_stat_value[(int)powerup_stat_name.Def] = 0.5f;
                powerup_stat_behavior[(int)powerup_stat_name.Def] = (int)powerup_stat_behavior_name.Multiply;
                break;
            case (int)powerup_type_name.LowGrav:
                powerup_stat_value[(int)powerup_stat_name.Grav] = 0.25f;
                powerup_stat_behavior[(int)powerup_stat_name.Grav] = (int)powerup_stat_behavior_name.Multiply;
                break;
            case (int)powerup_type_name.PartialHeal:
                powerup_stat_value[(int)powerup_stat_name.Damage] = 0.5f;
                powerup_stat_behavior[(int)powerup_stat_name.Damage] = (int)powerup_stat_behavior_name.Multiply;
                break;
            case (int)powerup_type_name.FullHeal:
                powerup_stat_value[(int)powerup_stat_name.Damage] = 0.0f;
                powerup_stat_behavior[(int)powerup_stat_name.Damage] = (int)powerup_stat_behavior_name.Multiply;
                break;
            default:
                break;
        }
        return;
    }

    private void Start()
    {
        CheckForSpawnerParent();
    }

    private void Update()
    {
        var m_Renderer = GetComponentInChildren<Renderer>();
        
        // If powerup is a template, make sure it doesn't render in the world
        if (item_is_template && m_Renderer.enabled)
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
        if (!ProcessTimer()) { return; }
        if (item_state == (int)item_state_name.ActiveOnOwner && item_is_template) {
            FadeOutAndDestroy();
        }
        else if (item_state == (int)item_state_name.FadingFromOwner && item_is_template)
        {
            Destroy(gameObject);
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

    internal bool ProcessTimer()
    {
        powerup_timer_local += Time.deltaTime;
        powerup_timer_network = Networking.CalculateServerDeltaTime(Networking.GetServerTimeInSeconds(), powerup_start_ms);
        if (powerup_duration > 0 && (powerup_timer_local >= powerup_duration || powerup_timer_network >= powerup_duration))
        {
            return true;
        }
        return false;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the player colliding with this is valid
        if (!CheckValidCollisionEvent(other)) { return; }

        // Apply powerups to self. Player gets a local copy that can't be touched but acts as a template to be read off of for plyAttr, which will store of a list of these objects and destroy as needed
        var plyAttr = gameController.FindPlayerAttributes(Networking.LocalPlayer);
        if (plyAttr != null) 
        { 
            
            item_is_template = true; // Temporarily set template status of self to true, then reset at end of instantiate
            var powerup_obj = Instantiate(this.gameObject);
            ItemPowerup powerup = powerup_obj.GetComponent<ItemPowerup>();
            powerup.item_state = (int)item_state_name.ActiveOnOwner;
            powerup.item_is_template = true;
            powerup.item_owner_id = Networking.LocalPlayer.playerId;
            powerup.powerup_start_ms = Networking.GetServerTimeInSeconds();
            powerup.powerup_type = powerup_type;
            powerup.powerup_duration = powerup_duration;
            powerup.spawner_parent = null;
            powerup.SetPowerupStats(powerup_type);
            plyAttr.ProcessPowerUp(powerup_obj, true);
            item_is_template = false;
        }

        // Despawn powerup for everyone else, with reason code of "someone else got it"
        // This does mean that it's possible that two people can get the same powerup due to lag, but that's a fun bonus!
        //item_snd_source.transform.position = spawner_parent.transform.position;
        if (CheckForSpawnerParent())
        {
            spawner_parent.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "DespawnItem", (int)item_snd_clips_name.PickupOther, Networking.LocalPlayer.playerId, true);
        }
    }

    public void FadeOutAndDestroy()
    {
        var plyAttr = gameController.FindPlayerAttributes(VRCPlayerApi.GetPlayerById(item_owner_id));
        if (plyAttr != null) { plyAttr.ProcessPowerUp(gameObject, false); }

        item_state = (int)item_state_name.FadingFromOwner;
        powerup_start_ms = Networking.GetServerTimeInSeconds();
        powerup_timer_local = 0.0f;
        powerup_timer_network = 0.0f;

        if (powerup_snd_clips.Length > (int)item_sfx_index.PowerupFade) { powerup_duration = powerup_snd_clips[(int)item_sfx_index.PowerupFade].length; }
        else { powerup_duration = 0.0f; }
        //gameController.PlaySFXFromArray(item_snd_source, item_snd_clips, (int)item_sfx_index.PowerupFade);
    }

}
