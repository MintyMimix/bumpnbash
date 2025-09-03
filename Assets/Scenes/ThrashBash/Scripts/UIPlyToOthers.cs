
using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class UIPlyToOthers : UdonSharpBehaviour
{
    [NonSerialized] public VRCPlayerApi owner;
    [SerializeField] public GameController gameController;
    [SerializeField] public TMP_Text PTOInfo;

    [NonSerialized] public PlayerAttributes playerAttributes;

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
        if (owner == Networking.LocalPlayer || owner == null) { return; }
        var showText = "Damage: " + playerAttributes.ply_dp
            + "%\nLives: " + playerAttributes.ply_lives
            + "%\n ATK: " + Mathf.RoundToInt(playerAttributes.ply_atk * (playerAttributes.ply_scale * gameController.scale_damage_factor) * 100.0f) / 100.0f + "x"
            + " | DEF: " + Mathf.RoundToInt(playerAttributes.ply_def * (playerAttributes.ply_scale * gameController.scale_damage_factor) * 100.0f) / 100.0f + "x";
        if (gameController.option_teamplay) { showText += "\nTeam: " + playerAttributes.ply_team; } //To-Do: have an array of team colors, and change the person's text color accordingly
        switch (playerAttributes.ply_state)
        {
            case (int)player_state_name.Inactive:
                showText = "(Inactive)";
                break;
            case (int)player_state_name.Respawning:
                showText = "-- Respawning --\n" + showText;
                break;
            case (int)player_state_name.Dead:
                showText = "Defeated!\n" + showText;
                break;
            default:
                break;
        }
        PTOInfo.text = showText;
    }

    private void FixedUpdate()
    {
        if (owner == Networking.LocalPlayer || owner == null) { return; }
        var scaleUI = (Networking.LocalPlayer.GetAvatarEyeHeightAsMeters() / 1.6f);
        transform.SetPositionAndRotation(owner.GetPosition() + new Vector3(0.0f, 2.2f * scaleUI, 0.0f), Networking.LocalPlayer.GetRotation());
        transform.localScale = new Vector3(0.003f, 0.003f, 0.003f) * scaleUI;
    }

    /*
    public GameController gameController;
    public TMP_Text PTODebugInfo;
    public VRCPlayerApi owner;
    void Start()
    {
        
    }

    private void Update()
    {
        var debugText = "";
        owner = Networking.GetOwner(gameObject);
        var ownerAttr = gameController.FindPlayerAttributes(owner);
        debugText += owner.displayName + " {" + owner.playerId + "}";
        debugText += "\n" + ownerAttr.ply_dp + " % [" + ownerAttr.ply_lives + "]";
        debugText += "\n" + "PlayerAttributes: " + Networking.GetOwner(gameController.FindOwnedObject(owner, "PlayerAttributes")).playerId.ToString();
        debugText += "\n" + "PlayerWeapon: " + Networking.GetOwner(gameController.FindOwnedObject(owner, "PlayerWeapon")).playerId.ToString();
        //debugText += "\n" + "PlayerHitbox: " + .playerId.ToString();
        PTODebugInfo.text = debugText;

        transform.SetPositionAndRotation(owner.GetPosition() + new Vector3 (0.0f, 1.5f, 0.0f), owner.GetRotation());
    }*/
}
