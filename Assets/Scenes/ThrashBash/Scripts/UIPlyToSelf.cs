
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon;
using static VRC.SDKBase.VRCPlayerApi;

public class UIPlyToSelf : UdonSharpBehaviour
{
    public VRCPlayerApi owner;
    public GameController gameController;
    public TMP_Text PTSPrimaryInfo;
    public TMP_Text PTSSecondaryInfo;

    public PlayerAttributes playerAttributes;

    void Start()
    {
        
    }
    public override void OnOwnershipTransferred(VRCPlayerApi newOwner)
    {
        owner = newOwner;
        playerAttributes = gameController.FindPlayerAttributes(newOwner);
    }

    private void Update()
    {
        if (owner != Networking.LocalPlayer || owner == null) { return; }
        var showTextPrimary = "Damage: " + playerAttributes.ply_dp + "%\nLives: " + playerAttributes.ply_lives;
        var showTextSecondary = "";
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
        //if (playerAttributes.last_kill_ply != null) { showTextSecondary = "You knocked out " + playerAttributes.last_kill_ply.displayName + "!" + showTextSecondary; }
        if (playerAttributes.last_kill_ply > -1) { showTextPrimary = "REPORT TO DEV IF THIS DISPLAYS";  }
        PTSPrimaryInfo.text = showTextPrimary;
        PTSSecondaryInfo.text = showTextSecondary;
    }

    private void FixedUpdate()
    {
        if (owner != Networking.LocalPlayer || owner == null) { return; }
        var scaleUI = (Networking.LocalPlayer.GetAvatarEyeHeightAsMeters() / 1.6f);
        transform.localScale = new Vector3(0.003f, 0.003f, 0.003f) * scaleUI;
        transform.position = Networking.LocalPlayer.GetTrackingData(TrackingDataType.Head).position + (Networking.LocalPlayer.GetTrackingData(TrackingDataType.Head).rotation * Vector3.forward * scaleUI);
        transform.rotation = Networking.LocalPlayer.GetTrackingData(TrackingDataType.Head).rotation;
    }
}
