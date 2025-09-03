
using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UIElements;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon;

public class UIPlyToSelf : UdonSharpBehaviour
{

    [NonSerialized] public VRCPlayerApi owner;
    [SerializeField] public GameController gameController;
    [SerializeField] TMP_Text PTSPrimaryInfo;
    [SerializeField] public TMP_Text PTSSecondaryInfo;
    [SerializeField] public RectTransform PTSPowerupSprite_parent;
    [NonSerialized] public UnityEngine.UI.Image[] PTSPowerupSprites;


    [NonSerialized] public PlayerAttributes playerAttributes;

    void Start()
    {
        var item_index = 0;
        var item_size = 0;
        for (var i = 0; i < transform.childCount; i++)
        {
            var child = (RectTransform)transform.GetChild(i);
            if (child.name.Contains("PTSPowerupPanel")) { PTSPowerupSprite_parent = child; break;  }
        }

        foreach (GameObject child in (Transform)PTSPowerupSprite_parent) 
        {
            if (child.name.Contains("PTSPowerupSprite")) { item_size++; }
        }

        PTSPowerupSprites = new UnityEngine.UI.Image[item_size];
        foreach (Transform child in (Transform)PTSPowerupSprite_parent)
        {
            if (child.name.Contains("PTSPowerupSprite")) 
            {
                PTSPowerupSprites[item_index] = child.GetComponent<UnityEngine.UI.Image>();
                item_index++;
            }
        }

    }
    public override void OnOwnershipTransferred(VRCPlayerApi newOwner)
    {
        owner = newOwner;
        playerAttributes = gameController.FindPlayerAttributes(newOwner);
    }

    private void Update()
    {
        if (owner != Networking.LocalPlayer || owner == null) { return; }

        var showTextPrimary = "Damage: " + Mathf.RoundToInt(playerAttributes.ply_dp)
            + "%\nLives: " + Mathf.RoundToInt(playerAttributes.ply_lives)
            + "\n ATK: " + Mathf.RoundToInt(playerAttributes.ply_atk * (playerAttributes.ply_scale * gameController.scale_damage_factor) * 100.0f) / 100.0f + "x"
            + " | DEF: " + Mathf.RoundToInt(playerAttributes.ply_def * (playerAttributes.ply_scale * gameController.scale_damage_factor) * 100.0f) / 100.0f + "x";
        var showTextSecondary = "";

        if (gameController.option_teamplay) { showTextPrimary += "\nTeam: " + playerAttributes.ply_team; } //To-Do: have an array of team colors, and change the person's text color accordingly
        if (playerAttributes.last_kill_ply > -1 && VRCPlayerApi.GetPlayerById(playerAttributes.last_kill_ply) != null) { showTextSecondary += "You knocked out " + VRCPlayerApi.GetPlayerById(playerAttributes.last_kill_ply).displayName + "!"; }
    
        switch (playerAttributes.ply_state)
        {
            case (int)player_state_name.Inactive:
                showTextPrimary = "Move in the zone to join the game!";
                break;
            case (int)player_state_name.Joined:
                showTextPrimary = "Waiting for game to start";
                break;
            case (int)player_state_name.Respawning:
                if (playerAttributes.last_hit_by_ply == null) { showTextSecondary = "You fell off the map!"; }
                else { showTextSecondary = "You were knocked out by " + playerAttributes.last_hit_by_ply.displayName + "!"; }
                showTextPrimary += "\n(Invulnerability: " +  
                    Mathf.Floor(playerAttributes.ply_respawn_duration - playerAttributes.ply_respawn_timer + 1.0f).ToString() + ")";
                break;
            case (int)player_state_name.Dead:
                showTextPrimary = "You were defeated!";
                if (playerAttributes.last_hit_by_ply != null) { showTextSecondary = "Your last KO was from " + playerAttributes.last_hit_by_ply.displayName + "!"; }
                break;
            default:
                break;
        }
        switch (gameController.round_state)
        {
            case (int)round_state_name.Ready:
                showTextPrimary = Mathf.Floor(gameController.ready_length - gameController.round_timer + 1.0f).ToString();
                break;
            case (int)round_state_name.Ongoing:
                showTextPrimary = Mathf.Floor(gameController.round_length - gameController.round_timer).ToString() + "\n" + showTextPrimary;
                break;
            case (int)round_state_name.Over:
                showTextPrimary = "Game Over!" + "\n" + showTextPrimary;
                break;
            default:
                break;
        }
        PTSPrimaryInfo.text = showTextPrimary;
        PTSSecondaryInfo.text = showTextSecondary;

        // Handle powerup sprites
        var powerup_len = (int)Mathf.Min(PTSPowerupSprites.Length, playerAttributes.powerups_active.Length);
        for (int i = 0; i < PTSPowerupSprites.Length; i++)
        {
            PTSPowerupSprites[i].sprite = gameController.Sprite_None;
            PTSPowerupSprites[i].GetComponentInChildren<TMP_Text>().text = "";

            if (i < powerup_len)
            {
                if (playerAttributes.powerups_active[i] == null) { continue; }
                var powerup = playerAttributes.powerups_active[i].GetComponent<ItemPowerup>();
                if (powerup == null) { continue; }
                PTSPowerupSprites[i].sprite = powerup.powerup_sprites[powerup.powerup_type];
                PTSPowerupSprites[i].GetComponentInChildren<TMP_Text>().text = (Mathf.Floor((float)(powerup.powerup_duration - powerup.powerup_timer_network)*10.0f)/10.0f).ToString();
            }

        }
    }

    private void FixedUpdate()
    {
        if (owner != Networking.LocalPlayer || owner == null) { return; }
        var scaleUI = (Networking.LocalPlayer.GetAvatarEyeHeightAsMeters() / 1.6f);
        transform.localScale = new Vector3(0.003f, 0.003f, 0.003f) * scaleUI;
        transform.position = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position + (Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation * Vector3.forward * scaleUI);
        transform.rotation = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation;
    }

}
