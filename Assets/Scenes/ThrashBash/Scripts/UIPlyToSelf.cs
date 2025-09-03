
using System;
using System.Linq;
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
    [SerializeField] public TMP_Text[] PTSTextStack;
    [SerializeField] public GameObject PTSTopPanel;
    [SerializeField] public TMP_Text PTSTimer;
    [SerializeField] public TMP_Text PTSLives;
    [SerializeField] public UnityEngine.UI.Image PTSLivesImage;
    [SerializeField] public Sprite PTSLivesSprite;
    [SerializeField] public Sprite PTSPointsSprite;
    [SerializeField] public Sprite PTSDeathsSprite;
    [SerializeField] public Sprite PTSFlagSprite;
    [SerializeField] public TMP_Text PTSDamage;
    [SerializeField] public TMP_Text PTSAttack;
    [SerializeField] public TMP_Text PTSDefense;
    [SerializeField] public TMP_Text PTSInvul;
    [SerializeField] public GameObject PTSTeamFlag;
    [SerializeField] public UnityEngine.UI.Image PTSTeamFlagImage;
    [SerializeField] public UnityEngine.UI.Image PTSTeamPoleImage;
    [SerializeField] public TMP_Text PTSTeamText;
    [SerializeField] public UnityEngine.UI.Image PTSTeamCBSpriteImage;

    [SerializeField] public Transform PTSPowerupPanel;
    [NonSerialized] public UnityEngine.UI.Image[] PTSPowerupSprites;


    [NonSerialized] public PlayerAttributes playerAttributes;

    // Fields used for demonstrating UI scale when modifying local options
    [SerializeField] public float ui_demo_duration = 5.0f;
    [NonSerialized] public float ui_demo_timer = 0.0f;
    [NonSerialized] public bool ui_demo_enabled = false;
    [NonSerialized] public bool ui_show_intro_text = true;

    [NonSerialized] public string text_queue_full_str = ""; // Queue system for local HUD messages, separated by the delineation character.
    [NonSerialized] public char text_queue_separator = '\r';
    [SerializeField] public int text_queue_limited_lines = 4; // Number of lines that will display at once from the text queue
    [SerializeField] public float text_queue_limited_duration = 5.0f; // How long should an active message be displayed?
    [SerializeField] public float text_queue_limited_fade_time_percent = 0.20f; // At what % of the the duration should the text begin fading? (i.e. if duration is 5.0f, 0.20f means fade at 4.0f)
    [SerializeField] public float text_queue_limited_extend = 0.5f; // How much longer should an active message be displayed if it is not the top message?
    [NonSerialized] public float[] text_queue_limited_timers;

    void Start()
    {
        var item_index = 0;
        var item_size = 0;
        text_queue_limited_timers = new float[text_queue_limited_lines];
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
        ui_show_intro_text = true;
        if (ui_show_intro_text) { AddToTextQueue("Welcome!"); }
        ui_demo_enabled = true;

    }

    public override void OnOwnershipTransferred(VRCPlayerApi newOwner)
    {
        owner = newOwner;
        playerAttributes = gameController.FindPlayerAttributes(newOwner);
    }

    public void AddToTextQueue(string input)
    {
        if (text_queue_full_str.Length > 0) { text_queue_full_str += text_queue_separator; }
        text_queue_full_str += input;
    }

    public void ProcessTextQueue()
    {
        if (text_queue_limited_timers == null) { return; }
        // First, check the string to see if we even have 4 lines
        string[] splitStr = text_queue_full_str.Split(text_queue_separator);
        int new_queue_size = splitStr.Length;
        float[] new_queue_timers = text_queue_limited_timers;
        int iterateAmount = Mathf.Min(splitStr.Length, text_queue_limited_lines);
        int iteration = 0;
        while (iteration < iterateAmount)
        {
            if (text_queue_full_str.Length == 0) { break; }
            if (new_queue_timers[iteration] < text_queue_limited_duration + (iteration * text_queue_limited_extend))
            {
                new_queue_timers[iteration] += Time.deltaTime;
            }
            else
            {
                // Shift timer entries up
                for (int j = iteration; j < iterateAmount - 1; j++)
                {
                    new_queue_timers[j] = new_queue_timers[j + 1];
                }
                new_queue_timers[iterateAmount - 1] = 0.0f;
                // Shift string entries up
                for (int k = iteration; k < splitStr.Length - 1; k++)
                {
                    splitStr[k] = splitStr[k + 1];
                }
                splitStr[splitStr.Length - 1] = "";
                new_queue_size--;
                iteration--; // Now that entries are shifted up, we need to check again
                iterateAmount--;
            }
            iteration++;
        }
        text_queue_full_str = String.Join(text_queue_separator, splitStr, 0, new_queue_size);
    }

    private void Update()
    {
        if (owner != Networking.LocalPlayer || owner == null) { return; }

        ProcessTextQueue();

        
        string[] splitStr = text_queue_full_str.Split(text_queue_separator);
        for (int i = 0; i < text_queue_limited_lines; i++)
        {
            if (i < splitStr.Length)
            {
                PTSTextStack[i].text = splitStr[i].ToUpper();
                float duration_modified = text_queue_limited_duration + (i * text_queue_limited_extend);
                float fade_time = duration_modified - (text_queue_limited_fade_time_percent * duration_modified);
                if (text_queue_limited_timers[i] >= fade_time) { PTSTextStack[i].alpha = 1 - ((text_queue_limited_timers[i] - fade_time) / (duration_modified - fade_time)); }
                else { PTSTextStack[i].alpha = 1.0f; }
            }
            else { PTSTextStack[i].text = ""; }
        }
        //if (ui_show_intro_text) { showTextSecondary += "Welcome! This game is in very early development; there may be major bugs or issues!"; }
        //else {  }
        //PTSPrimaryInfo.text = showTextPrimary;
        //PTSSecondaryInfo.text = showTextSecondary;

        // Tick down demo timer
        if (ui_demo_enabled && ui_demo_timer < ui_demo_duration)
        {
            ui_demo_timer += Time.deltaTime;
        }
        else if (ui_demo_enabled && ui_demo_timer >= ui_demo_duration)
        {
            ui_demo_enabled = false;
            if (ui_show_intro_text) {
                AddToTextQueue("Step in the square to join the game!");
                AddToTextQueue(" ");
                AddToTextQueue("This game is in very early development; there may be major bugs or issues!");
            }
            ui_show_intro_text = false;
            ui_demo_timer = 0.0f;
        }

        // Sort out better without all the debug
        if (ui_demo_enabled && !ui_show_intro_text) { PTSTopPanel.SetActive(true); }
        else if ((gameController.round_state == (int)round_state_name.Start || gameController.round_state == (int)round_state_name.Over || playerAttributes.ply_state == (int)player_state_name.Inactive || playerAttributes.ply_state == (int)player_state_name.Spectator) && PTSTopPanel.activeInHierarchy) { PTSTopPanel.SetActive(false); }
        else if (!(gameController.round_state == (int)round_state_name.Start || gameController.round_state == (int)round_state_name.Over || playerAttributes.ply_state == (int)player_state_name.Inactive || playerAttributes.ply_state == (int)player_state_name.Spectator) && !PTSTopPanel.activeInHierarchy) { PTSTopPanel.SetActive(true); }

        var TimerText = Mathf.Floor(gameController.round_length - gameController.round_timer + 1.0f).ToString();
        if (gameController.round_state == (int)round_state_name.Start) { TimerText = ""; }
        else if (gameController.round_state == (int)round_state_name.Ready) { TimerText = Mathf.Floor(gameController.ready_length - gameController.round_timer + 1.0f).ToString(); }
        PTSTimer.text = TimerText;

        var DamageText = Mathf.RoundToInt(playerAttributes.ply_dp) + "%";
        if (gameController.round_state == (int)round_state_name.Start) { DamageText = ""; }
        PTSDamage.text = DamageText;
        PTSDamage.color = new Color(Mathf.Min(Mathf.Max(0.2f, 1.0f - ((playerAttributes.ply_dp - 100) / 100)), 1.0f), Mathf.Min(Mathf.Max(0.2f, 1.0f - (playerAttributes.ply_dp/100)), 255), Mathf.Min(Mathf.Max(0.2f, 1.0f - (playerAttributes.ply_dp / 100)), 1.0f), 255);

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

        if (gameController.local_ppp_options != null && gameController.local_ppp_options.colorblind) { PTSTeamCBSpriteImage.enabled = true; }
        else { PTSTeamCBSpriteImage.enabled = false;  }
        PTSTeamFlagImage.sprite = PTSFlagSprite;
        PTSTeamFlagImage.enabled = !PTSTeamCBSpriteImage.enabled;
        PTSTeamPoleImage.enabled = PTSTeamFlagImage.enabled;

        if (playerAttributes.ply_team >= 0 && playerAttributes.ply_team < gameController.team_colors.Length) 
        { 
            if (gameController.option_teamplay) 
            { 
                PTSTeamFlagImage.color = gameController.team_colors[playerAttributes.ply_team];
            }
            else 
            { 
                PTSTeamFlagImage.color = new Color32(255,255,255,255);
            }
            PTSTeamCBSpriteImage.sprite = gameController.team_sprites[playerAttributes.ply_team];
            PTSTeamCBSpriteImage.color = PTSTeamFlagImage.color;
        }

        var FlagText = "";
        if (gameController.round_state != (int)round_state_name.Start && gameController.team_count >= 0) 
        {
            int total_lives = 0;
            int members_alive = gameController.CheckSpecificTeamLives(playerAttributes.ply_team, ref total_lives);
            int total_points = gameController.CheckSpecificTeamPoints(playerAttributes.ply_team);

            if (!gameController.option_teamplay || gameController.option_gamemode == (int)round_mode_name.Infection) { FlagText = members_alive.ToString(); }
            else if (gameController.option_gamemode == (int)round_mode_name.BossBash)
            {
                if (playerAttributes.ply_team == 1)
                { // If we are the boss, override the flag to instead display KOs
                    FlagText = playerAttributes.ply_points.ToString() + "/" + gameController.option_goal_value_a;
                    PTSTeamPoleImage.enabled = false;
                    PTSTeamFlagImage.sprite = PTSPointsSprite;
                    PTSTeamCBSpriteImage.sprite = PTSPointsSprite;
                }
                else { FlagText = total_points.ToString(); }
            }
            else if (gameController.option_goal_points_a) { FlagText = total_points.ToString(); }
            else { FlagText = total_lives.ToString(); }
        }
        PTSTeamText.text = FlagText;


        var LivesText = "";
        if (gameController.round_state == (int)round_state_name.Start) { LivesText = ""; }
        else if (gameController.option_goal_points_a && !(!gameController.option_goal_points_b && playerAttributes.ply_team == 1))
        {
            PTSLivesImage.sprite = PTSPointsSprite;
            if (gameController.option_gamemode == (int)round_mode_name.BossBash && gameController.gamemode_boss_id >= 0)
            {
                // If this is boss bash, display the boss's points as a total death counter
                var bossAttr = gameController.FindPlayerAttributes(VRCPlayerApi.GetPlayerById(gameController.gamemode_boss_id));
                if (bossAttr != null)
                {
                    LivesText = Mathf.RoundToInt(bossAttr.ply_points).ToString() + "/" + gameController.option_goal_value_a;
                }
                else { LivesText = "?"; }
                PTSLives.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
                PTSLivesImage.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
                PTSLivesImage.sprite = PTSDeathsSprite;
            }
            else if (gameController.option_gamemode == (int)round_mode_name.Infection)
            {
                LivesText = gameController.CheckSpecificTeamPoints(1).ToString();
                PTSLives.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
                PTSLivesImage.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
                PTSLivesImage.sprite = PTSDeathsSprite;
            }
            else
            {
                if (gameController.option_teamplay) { LivesText = Mathf.RoundToInt(gameController.CheckSpecificTeamPoints(playerAttributes.ply_team)).ToString() + "/" + gameController.option_goal_value_a; }
                else { LivesText = Mathf.RoundToInt(playerAttributes.ply_points).ToString() + "/" + gameController.option_goal_value_a; }
                PTSLives.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
                PTSLivesImage.color = PTSTeamFlagImage.color;
            }
        }
        else
        {
            LivesText = Mathf.RoundToInt(playerAttributes.ply_lives).ToString();
            PTSLivesImage.sprite = PTSLivesSprite;
            PTSLivesImage.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
            PTSLives.color = new Color(1.0f, (playerAttributes.ply_lives / gameController.plysettings_lives), (playerAttributes.ply_lives / gameController.plysettings_lives), 1.0f);
        }
        PTSLives.text = LivesText;


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
        var heightUI = 0.5f * (Networking.LocalPlayer.GetAvatarEyeHeightAsMeters() / 1.6f);
        var scaleUI = 1.0f;
        if (gameController.local_ppp_options != null) { 
            scaleUI *= (gameController.local_ppp_options.ui_scale * 1.0f); 
        }
        //if (!Networking.LocalPlayer.IsUserInVR()) { scaleUI *= 0.5f; }
        //else { scaleUI *= 0.5f; }
        transform.localScale = new Vector3(0.003f, 0.003f, 0.003f) * heightUI * scaleUI;
        transform.position = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position + (Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation * Vector3.forward * heightUI);
        transform.rotation = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation;
    }

}
