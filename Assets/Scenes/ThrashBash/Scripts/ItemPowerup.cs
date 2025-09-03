
using System;
using System.Diagnostics;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

public enum item_powerup_name
{
    SizeUp, SizeDown, SpeedUp, AtkUp, DefUp, AtkDown, DefDown, LowGrav, ENUM_LENGTH
}

public enum item_powerup_stat_name
{
   Scale, Speed, Atk, Def, Grav, ENUM_LENGTH
}

public enum item_powerup_stat_behavior
{
    Null, Set, Add, Multiply, ENUM_LENGTH
}

public enum item_powerup_destroy_reason_code
{
    OtherPickup, ItemExpire, PowerupFade, ENUM_LENGTH
}

public class ItemPowerup : ItemGeneric
{
    [NonSerialized] public int powerup_stored_global_index;
    [NonSerialized] public int powerup_type;
    [NonSerialized] public float powerup_duration;
    [NonSerialized] public double powerup_start_ms;
    [NonSerialized] public double powerup_timer_network = 0.0f;
    [NonSerialized] public int powerup_owner_id = -1;
    [NonSerialized] public bool powerup_active = false;
    [NonSerialized] public bool powerup_ignore = false;
    [NonSerialized] public float[] powerup_stat_value;
    [NonSerialized] public int[] powerup_stat_behavior;

    [SerializeField] public AudioClip[] powerup_snd_clips;
    [SerializeField] public Sprite[] powerup_sprites;

    public void SetPowerupStats(int pType)
    {
        powerup_stat_value = new float[(int)item_powerup_stat_name.ENUM_LENGTH];
        powerup_stat_behavior = new int[(int)item_powerup_stat_name.ENUM_LENGTH];

        // Default values
        powerup_stat_value[(int)item_powerup_stat_name.Scale] = 1.0f;
        powerup_stat_behavior[(int)item_powerup_stat_name.Scale] = (int)item_powerup_stat_behavior.Null;
        powerup_stat_value[(int)item_powerup_stat_name.Speed] = 1.0f;
        powerup_stat_behavior[(int)item_powerup_stat_name.Speed] = (int)item_powerup_stat_behavior.Null;
        powerup_stat_value[(int)item_powerup_stat_name.Atk] = 1.0f;
        powerup_stat_behavior[(int)item_powerup_stat_name.Atk] = (int)item_powerup_stat_behavior.Null;
        powerup_stat_value[(int)item_powerup_stat_name.Def] = 1.0f;
        powerup_stat_behavior[(int)item_powerup_stat_name.Def] = (int)item_powerup_stat_behavior.Null;
        powerup_stat_value[(int)item_powerup_stat_name.Grav] = 1.0f;
        powerup_stat_behavior[(int)item_powerup_stat_name.Grav] = (int)item_powerup_stat_behavior.Null;

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
            case (int)item_powerup_name.SizeUp:
                powerup_stat_value[(int)item_powerup_stat_name.Scale] = 1.5f;
                powerup_stat_behavior[(int)item_powerup_stat_name.Scale] = (int)item_powerup_stat_behavior.Multiply;
                break;
            case (int)item_powerup_name.SizeDown:
                powerup_stat_value[(int)item_powerup_stat_name.Scale] = (1.0f/1.5f);
                powerup_stat_behavior[(int)item_powerup_stat_name.Scale] = (int)item_powerup_stat_behavior.Multiply;
                break;
            case (int)item_powerup_name.SpeedUp:
                powerup_stat_value[(int)item_powerup_stat_name.Speed] = 1.0f;
                powerup_stat_behavior[(int)item_powerup_stat_name.Speed] = (int)item_powerup_stat_behavior.Add;
                break;
            case (int)item_powerup_name.AtkUp:
                powerup_stat_value[(int)item_powerup_stat_name.Atk] = 2.0f;
                powerup_stat_behavior[(int)item_powerup_stat_name.Atk] = (int)item_powerup_stat_behavior.Multiply;
                break;
            case (int)item_powerup_name.DefUp:
                powerup_stat_value[(int)item_powerup_stat_name.Def] = 2.0f;
                powerup_stat_behavior[(int)item_powerup_stat_name.Def] = (int)item_powerup_stat_behavior.Multiply;
                break;
            case (int)item_powerup_name.AtkDown:
                powerup_stat_value[(int)item_powerup_stat_name.Atk] = 0.5f;
                powerup_stat_behavior[(int)item_powerup_stat_name.Atk] = (int)item_powerup_stat_behavior.Multiply;
                break;
            case (int)item_powerup_name.DefDown:
                powerup_stat_value[(int)item_powerup_stat_name.Def] = 0.5f;
                powerup_stat_behavior[(int)item_powerup_stat_name.Def] = (int)item_powerup_stat_behavior.Multiply;
                break;
            case (int)item_powerup_name.LowGrav:
                powerup_stat_value[(int)item_powerup_stat_name.Grav] = 0.5f;
                powerup_stat_behavior[(int)item_powerup_stat_name.Grav] = (int)item_powerup_stat_behavior.Multiply;
                break;
            default:
                break;
        }
        return;
    }

    private void Start()
    {
        item_snd_source.transform.parent = null; // Unparent the sound object so it is not destroyed alongside this one
    }

    private void Update()
    {
        // Item on field expired
        if (ProcessSpawnTimer() && !powerup_active && !powerup_ignore)
        {
            gameController.SendDestroyPowerup(powerup_stored_global_index, (int)item_powerup_destroy_reason_code.ItemExpire, true);
        }

        if (powerup_owner_id != Networking.LocalPlayer.playerId) { return; }

        if (powerup_active && !powerup_ignore)
        {
            powerup_timer_network = Networking.CalculateServerDeltaTime(Networking.GetServerTimeInSeconds(), powerup_start_ms);
            if (powerup_duration > 0 && powerup_timer_network >= powerup_duration)
            {
                // Run event on playerAttributes to remove effects
                var plyAttr = gameController.FindPlayerAttributes(Networking.LocalPlayer);
                if (plyAttr != null) { plyAttr.ProcessPowerUp(this, false); }
                gameController.SendDestroyPowerup(powerup_stored_global_index, (int)item_powerup_destroy_reason_code.PowerupFade, true);
            }
        }

    }

    private void FixedUpdate()
    {
        transform.rotation = Networking.LocalPlayer.GetRotation();
    }

    private void OnTriggerEnter(Collider other)
    {
        // We don't care if someone else hits it because that will be handled via network event.
        // This does mean that it's possible that two people can get the same powerup due to lag, but that's a fun bonus!
        if (Networking.GetOwner(other.gameObject) != Networking.LocalPlayer || powerup_active || powerup_ignore) { return; }
        if (other.GetComponent<PlayerHitbox>() == null) { return; }

        ApplyPowerup();

        gameController.SendDestroyPowerup(powerup_stored_global_index, (int)item_powerup_destroy_reason_code.OtherPickup, true);

    }

    public void ApplyPowerup()
    {
        // Run event on playerAttributes to add effects
        if (!powerup_ignore) { powerup_owner_id = Networking.LocalPlayer.playerId; }
        var plyAttr = gameController.FindPlayerAttributes(Networking.LocalPlayer);
        if (plyAttr != null && !powerup_ignore) { plyAttr.ProcessPowerUp(this, true); }
        powerup_active = true;

        var m_Renderer = GetComponentInChildren<Renderer>();
        m_Renderer.enabled = false;
        foreach (Transform child in transform)
        {
            if (child.name.Contains("ItemSprite"))
            {
                var m_Renderer_child = child.GetComponent<Renderer>();
                m_Renderer_child.enabled = false;
            }
        }
        powerup_start_ms = Networking.GetServerTimeInSeconds();
        if (!powerup_ignore) { gameController.PlaySFXFromArray(item_snd_source, powerup_snd_clips, powerup_type); }
    }
}
