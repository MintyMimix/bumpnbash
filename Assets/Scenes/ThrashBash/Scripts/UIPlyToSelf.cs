
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
    [SerializeField] public GameObject PTSTopPanel;
    [SerializeField] public TMP_Text PTSTimer;
    [SerializeField] public TMP_Text PTSLives;
    [SerializeField] public UnityEngine.UI.Image PTSLivesImage;
    [SerializeField] public Sprite PTSLivesSprite;
    [SerializeField] public Sprite PTSPointsSprite;
    [SerializeField] public TMP_Text PTSDamage;
    [SerializeField] public TMP_Text PTSAttack;
    [SerializeField] public TMP_Text PTSDefense;
    [SerializeField] public TMP_Text PTSInvul;
    [SerializeField] public GameObject PTSTeamFlag;
    [SerializeField] public UnityEngine.UI.Image PTSTeamFlagImage;
    [SerializeField] public TMP_Text PTSTeamText;

    [SerializeField] public Transform PTSPowerupPanel;
    [NonSerialized] public UnityEngine.UI.Image[] PTSPowerupSprites;


    [NonSerialized] public PlayerAttributes playerAttributes;

    void Start()
    {
        var item_index = 0;
        var item_size = 0;
        RectTransform temp_parent = null;
        for (var i = 0; i < PTSPowerupPanel.transform.childCount; i++)
        {
            var child = (RectTransform)transform.GetChild(i);
            if (child.name.Contains("PTSPowerupPanel")) { temp_parent = child; break;  }
        }

        foreach (GameObject child in (Transform)temp_parent) 
        {
            if (child.name.Contains("PTSPowerupSprite")) { item_size++; }
        }

        PTSPowerupSprites = new UnityEngine.UI.Image[item_size];
        foreach (Transform child in (Transform)temp_parent)
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

        // Sort out better without all the debug

        if ((gameController.round_state == (int)round_state_name.Start || gameController.round_state == (int)round_state_name.Over || playerAttributes.ply_state == (int)player_state_name.Inactive || playerAttributes.ply_state == (int)player_state_name.Spectator) && PTSTopPanel.activeInHierarchy) { PTSTopPanel.SetActive(false); }
        else if (!(gameController.round_state == (int)round_state_name.Start || gameController.round_state == (int)round_state_name.Over || playerAttributes.ply_state == (int)player_state_name.Inactive || playerAttributes.ply_state == (int)player_state_name.Spectator) && !PTSTopPanel.activeInHierarchy) { PTSTopPanel.SetActive(true); }

        var TimerText = Mathf.Floor(gameController.round_length - gameController.round_timer + 1.0f).ToString();
        if (gameController.round_state == (int)round_state_name.Start) { TimerText = ""; }
        else if (gameController.round_state == (int)round_state_name.Ready) { TimerText = Mathf.Floor(gameController.ready_length - gameController.round_timer + 1.0f).ToString(); }
        PTSTimer.text = TimerText;

        var LivesText = "";
        if (gameController.round_state == (int)round_state_name.Start) { LivesText = ""; }
        else if (gameController.option_goal_points)
        {
            LivesText = Mathf.RoundToInt(playerAttributes.ply_points).ToString();
            PTSLivesImage.sprite = PTSPointsSprite;
        }
        else
        {
            LivesText = Mathf.RoundToInt(playerAttributes.ply_lives).ToString();
            PTSLivesImage.sprite = PTSLivesSprite;
        }
        PTSLives.text = LivesText;

        var DamageText = Mathf.RoundToInt(playerAttributes.ply_dp) + "%";
        if (gameController.round_state == (int)round_state_name.Start) { DamageText = ""; }
        PTSDamage.text = DamageText;

        var InvulText = Mathf.Floor(playerAttributes.ply_respawn_duration - playerAttributes.ply_respawn_timer + 1.0f).ToString();
        if (gameController.round_state == (int)round_state_name.Start || playerAttributes.ply_state != (int)player_state_name.Respawning)
        {
            InvulText = ""; 
            PTSInvul.gameObject.transform.parent.gameObject.SetActive(false);
            PTSDamage.gameObject.transform.parent.gameObject.SetActive(true);
        }
        else
        {
            PTSInvul.gameObject.transform.parent.gameObject.SetActive(true);
            PTSDamage.gameObject.transform.parent.gameObject.SetActive(false);
        }
        PTSInvul.text = InvulText;

        var AttackVal = Mathf.RoundToInt(playerAttributes.ply_atk * (playerAttributes.ply_scale * gameController.scale_damage_factor) * 100.0f) / 100.0f;
        var AttackText = AttackVal + "x";
        if (gameController.round_state == (int)round_state_name.Start) { AttackText = ""; }
        if (AttackVal > gameController.plysettings_atk) { PTSAttack.color = new Color32(60, 255, 60, 255); }
        else if (AttackVal < gameController.plysettings_atk) { PTSAttack.color = new Color32(255, 60, 60, 255); }
        else { PTSAttack.color = new Color32(255, 255, 255, 255); }
        PTSAttack.text = AttackText;

        var DefenseVal = Mathf.RoundToInt(playerAttributes.ply_def * (playerAttributes.ply_scale * gameController.scale_damage_factor) * 100.0f) / 100.0f;
        var DefenseText = DefenseVal + "x";
        if (gameController.round_state == (int)round_state_name.Start) { DefenseText = ""; }
        if (DefenseVal > gameController.plysettings_def) { PTSDefense.color = new Color32(60, 255, 60, 255); }
        else if (DefenseVal < gameController.plysettings_def) { PTSDefense.color = new Color32(255, 60, 60, 255); }
        else { PTSDefense.color = new Color32(255, 255, 255, 255); }
        PTSDefense.text = DefenseText;

        if (playerAttributes.ply_team >= 0 && playerAttributes.ply_team < gameController.team_colors.Length) { PTSTeamFlagImage.color = gameController.team_colors[playerAttributes.ply_team]; }
        var FlagText = "";
        if (gameController.team_count >= 0) { FlagText = gameController.CheckSpecificTeamLives(playerAttributes.ply_team).ToString(); }
        PTSTeamText.text = FlagText;



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
                PTSPowerupSprites[i].GetComponentInChildren<TMP_Text>().text = 
                    (Mathf.Floor((float)(powerup.powerup_duration - powerup.powerup_timer_network)*10.0f)/10.0f).ToString().PadRight(2, '.').PadRight(3,'0');
            }

        }
    }

    private void FixedUpdate()
    {
        if (owner != Networking.LocalPlayer || owner == null) { return; }
        var scaleUI = (Networking.LocalPlayer.GetAvatarEyeHeightAsMeters() / 1.6f);
        if (!Networking.LocalPlayer.IsUserInVR()) { scaleUI *= 0.5f; }
        transform.localScale = new Vector3(0.003f, 0.003f, 0.003f) * scaleUI;
        transform.position = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position + (Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation * Vector3.forward * scaleUI);
        transform.rotation = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation;
    }

}
