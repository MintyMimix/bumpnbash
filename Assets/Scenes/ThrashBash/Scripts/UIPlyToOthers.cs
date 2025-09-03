
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
    [SerializeField] public GameObject PTSTopPanel;
    [SerializeField] public TMP_Text PTOInfo;
    [SerializeField] public TMP_Text PTOLives;
    [SerializeField] public UnityEngine.UI.Image PTOLivesImage;
    [SerializeField] public Sprite PTOLivesSprite;
    [SerializeField] public Sprite PTOPointsSprite;
    [SerializeField] public TMP_Text PTODamage;
    [SerializeField] public TMP_Text PTOAttack;
    [SerializeField] public TMP_Text PTODefense;
    [SerializeField] public TMP_Text PTOInvul;
    [SerializeField] public GameObject PTOTeamFlag;
    [SerializeField] public UnityEngine.UI.Image PTOTeamFlagImage;
    [SerializeField] public TMP_Text PTOTeamText;

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

        // Sort out better without all the debug
        if (gameController.round_state == (int)round_state_name.Start && PTSTopPanel.activeInHierarchy) { PTSTopPanel.SetActive(false); }
        else if (gameController.round_state != (int)round_state_name.Start && !PTSTopPanel.activeInHierarchy) { PTSTopPanel.SetActive(true); }

        var LivesText = "";
        if (gameController.round_state == (int)round_state_name.Start) { LivesText = ""; }
        else if (gameController.option_goal_points)
        {
            LivesText = Mathf.RoundToInt(playerAttributes.ply_points).ToString();
            PTOLivesImage.sprite = PTOPointsSprite;
        }
        else
        {
            LivesText = Mathf.RoundToInt(playerAttributes.ply_lives).ToString();
            PTOLivesImage.sprite = PTOLivesSprite;
        }
        PTOLives.text = LivesText;

        var DamageText = Mathf.RoundToInt(playerAttributes.ply_dp) + "%";
        if (gameController.round_state == (int)round_state_name.Start) { DamageText = ""; }
        PTODamage.text = DamageText;

        var InvulText = Mathf.Floor(playerAttributes.ply_respawn_duration - playerAttributes.ply_respawn_timer + 1.0f).ToString();
        if (gameController.round_state == (int)round_state_name.Start || playerAttributes.ply_state != (int)player_state_name.Respawning)
        {
            InvulText = "";
            PTOInvul.gameObject.transform.parent.gameObject.SetActive(false);
            PTODamage.gameObject.transform.parent.gameObject.SetActive(true);
        }
        else
        {
            PTOInvul.gameObject.transform.parent.gameObject.SetActive(true);
            PTODamage.gameObject.transform.parent.gameObject.SetActive(false);
        }
        PTOInvul.text = InvulText;

        var AttackVal = Mathf.RoundToInt(playerAttributes.ply_atk * (playerAttributes.ply_scale * gameController.scale_damage_factor) * 100.0f) / 100.0f;
        var AttackText = AttackVal + "x";
        if (gameController.round_state == (int)round_state_name.Start) { AttackText = ""; }
        if (AttackVal > gameController.plysettings_atk) { PTOAttack.color = new Color32(60, 255, 60, 255); }
        else if (AttackVal < gameController.plysettings_atk) { PTOAttack.color = new Color32(255, 60, 60, 255); }
        else { PTOAttack.color = new Color32(255, 255, 255, 255); }
        PTOAttack.text = AttackText;

        var DefenseVal = Mathf.RoundToInt(playerAttributes.ply_def * (playerAttributes.ply_scale * gameController.scale_damage_factor) * 100.0f) / 100.0f;
        var DefenseText = DefenseVal + "x";
        if (gameController.round_state == (int)round_state_name.Start) { DefenseText = ""; }
        if (DefenseVal > gameController.plysettings_def) { PTODefense.color = new Color32(60, 255, 60, 255); }
        else if (DefenseVal < gameController.plysettings_def) { PTODefense.color = new Color32(255, 60, 60, 255); }
        else { PTODefense.color = new Color32(255, 255, 255, 255); }
        PTODefense.text = DefenseText;

        if (playerAttributes.ply_team >= 0 && playerAttributes.ply_team < gameController.team_colors.Length) { PTOTeamFlagImage.color = gameController.team_colors[playerAttributes.ply_team]; }
        //var FlagText = "";
        //if (gameController.team_count >= 0) { FlagText = gameController.CheckSpecificTeamLives(playerAttributes.ply_team).ToString(); }
        //PTOTeamText.text = FlagText;


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
        transform.SetPositionAndRotation(owner.GetPosition() + new Vector3(0.0f, 2.6f * scaleUI, 0.0f), Networking.LocalPlayer.GetRotation());
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
