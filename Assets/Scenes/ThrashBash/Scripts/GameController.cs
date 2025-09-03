
using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities.IO;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.ProBuilder;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;
using static UnityEngine.InputSystem.LowLevel.InputStateHistory;
using static UnityEngine.UIElements.UxmlAttributeDescription;
using static VRC.Core.ApiPendingTransaction;

// A note on ENUM_LENGTH: this must ALWAYS BE LAST since we are using it as a shorthand for the length of an enumerator! (The typical method of typeof() is not supported in U#)
public enum game_sfx_index
{
    Death, Kill, HitSend, HitReceive, WeaponFire, ENUM_LENGTH
}

public enum round_state_name
{
    Start, Ready, Ongoing, Over, ENUM_LENGTH
}

public enum ply_tracking_arr_name
{
    Ready, InGame, ENUM_LENGTH
}

public enum weapon_stats_name
{
    Cooldown, Projectile_Distance, Projectile_Duration, Projectile_Type, Hurtbox_Damage, Hurtbox_Size, Hurtbox_Duration, Hurtbox_Damage_Type, ENUM_LENGTH
}

public enum round_mode_name
{
    Survival, Clash, BossBash, Infection, ENUM_LENGTH
}

// shorts and unsigneds
public class GameController : UdonSharpBehaviour
{

    [Header("References")]
    [SerializeField] public GameObject template_WeaponProjectile;
    [SerializeField] public GameObject template_WeaponHurtbox;
    [SerializeField] public GameObject template_ItemPowerup;
    [SerializeField] public Sprite Sprite_None;

    [SerializeField] public UnityEngine.UI.Button ui_round_start_button;
    [SerializeField] public UnityEngine.UI.Toggle ui_round_master_only_toggle;
    [SerializeField] public TextMeshProUGUI ui_round_master_text;
    [SerializeField] public UIRoundTeamPanel ui_round_team_panel;
    [SerializeField] public UnityEngine.UI.Toggle ui_round_teamplay_toggle;
    [SerializeField] public TMP_InputField ui_round_teamplay_count_input;
    [SerializeField] public UnityEngine.UI.Button ui_round_teamplay_sort_button;
    [SerializeField] public TMP_InputField ui_round_length_input;

    [SerializeField] public TMP_Dropdown ui_round_option_dropdown;
    [SerializeField] public TextMeshProUGUI ui_round_option_goal_text_a;
    [SerializeField] public TextMeshProUGUI ui_round_option_goal_text_b;
    [SerializeField] public TMP_InputField ui_round_option_goal_input_a;
    [SerializeField] public TMP_InputField ui_round_option_goal_input_b;

    [SerializeField] public GameObject room_ready_spawn;
    [SerializeField] public Collider[] room_game_spawnzones;
    [SerializeField] public TextMeshProUGUI room_ready_txt;
    [SerializeField] public AudioSource snd_game_music_source;
    [SerializeField] public AudioClip[] snd_game_music_clips;
    [SerializeField] public AudioSource snd_ready_music_source;
    [SerializeField] public AudioClip[] snd_ready_music_clips;
    [SerializeField] public AudioSource[] snd_game_sfx_sources;

    [NonSerialized] public AudioClip[][] snd_game_sfx_clips; // Inspector doesn't like 2D arrays 
    [SerializeField] public AudioClip[] snd_game_sfx_clips_death;
    [SerializeField] public AudioClip[] snd_game_sfx_clips_kill;
    [SerializeField] public AudioClip[] snd_game_sfx_clips_hitsend; // NOTE: Corresponds to damage_type
    [SerializeField] public AudioClip[] snd_game_sfx_clips_hitreceive; // NOTE: Corresponds to damage_type
    [SerializeField] public AudioClip[] snd_game_sfx_clips_weaponfire; // NOTE: Corresponds to weapon_type

    [SerializeField] public Transform item_spawns_parent;
    [NonSerialized] public ItemSpawner[] item_spawns;

    [NonSerialized][UdonSynced] public byte round_state = 0;
    [Header("Game Settings")]
    [Tooltip("How long a round should last, in seconds")]
    [SerializeField][UdonSynced] public float round_length = 300.0f;
    [Tooltip("How long players are on the 'Ready...' screen before a round begins. This time is needed so they can pick up their weapon.")]
    [SerializeField][UdonSynced] public float ready_length = 5.0f;
    [Tooltip("How long a round should be in 'Game Over' state before someone can start a new round.")]
    [SerializeField][UdonSynced] public float over_length = 10.0f;
    [NonSerialized][UdonSynced] public double round_start_ms = 0.0f;

    [Header("Player Settings")]
    [Tooltip("Starting Damage %")]
    [UdonSynced] public float plysettings_dp = 0.0f;
    [Tooltip("Respawn Invulnerability Time, in seconds")]
    [UdonSynced] public float plysettings_respawn_duration = 3.0f;
    [Tooltip("Starting Lives")]
    [UdonSynced] public ushort plysettings_lives = 3;
    [Tooltip("Starting Points")]
    [UdonSynced] public ushort plysettings_points = 0;
    [Tooltip("Starting Scale, as a multiplier (default: 1.0x)")]
    [UdonSynced] public float plysettings_scale = 1.0f;
    [Tooltip("Starting Speed, as a multiplier (default: 1.0x)")]
    [UdonSynced] public float plysettings_speed = 1.0f;
    [Tooltip("Starting Attack, as a multiplier (default: 1.0x)")]
    [UdonSynced] public float plysettings_atk = 1.0f;
    [Tooltip("Starting Defense, as a multiplier (default: 1.0x)")]
    [UdonSynced] public float plysettings_def = 1.0f;
    [Tooltip("Starting Gravity, as a multiplier (default: 1.0x)")]
    [UdonSynced] public float plysettings_grav = 1.0f;
    [Tooltip("Starting Scale damage factor, which determines how much player size affects abilities, as a multiplier (default: 1.0x)")]
    [UdonSynced] public float scale_damage_factor = 1.0f;

    [NonSerialized] public float round_timer = 0.0f;

    [NonSerialized] [UdonSynced] public string ply_ready_str = "";
    [NonSerialized] public int[] ply_ready_arr;
    [NonSerialized] [UdonSynced] public string ply_in_game_str = "";
    [NonSerialized] public int[] ply_in_game_arr;

    [UdonSynced] public bool option_teamplay = false;
    [UdonSynced] public byte team_count = 1;
    [SerializeField] public Color32[] team_colors; // Assign in inspector
    [NonSerialized][UdonSynced] public string teams_assigned_synced_str = "";
    [NonSerialized] public int[] teams_assigned_synced_arr;

    //[NonSerialized] public int[][] ply_teams_arr;


    [UdonSynced] public byte option_gamemode = 0;
    [NonSerialized][UdonSynced] public bool option_goal_points_a = false; // If true, points are used instead of lives
    [NonSerialized][UdonSynced] public ushort option_goal_value_a = 10;
    [NonSerialized][UdonSynced] public bool option_goal_points_b = false; // If true, points are used instead of lives
    [NonSerialized][UdonSynced] public ushort option_goal_value_b = 3;
    [NonSerialized][UdonSynced] public bool option_force_teams = false; // Should team limits be enforced? If so
    [NonSerialized][UdonSynced] public string option_team_limits_str = "0,1"; // What is the max # of players per team? Size must match team_count
    [NonSerialized] public int[] option_team_limits_arr;
    [NonSerialized][UdonSynced] public bool option_start_from_master_only = false;

    [NonSerialized] [UdonSynced] int ply_master_id = 0;

    [NonSerialized] string room_ready_status_text = "";

    // -- Initialization --
    private void Start()
    {
        item_spawns = new ItemSpawner[0];
        ply_ready_arr = new int[0];
        ply_in_game_arr = new int[0];
        option_team_limits_arr = new int[0];
        teams_assigned_synced_arr = new int[0];
        //ply_teams_arr = new int[0][];

        ply_master_id = Networking.LocalPlayer.playerId;

        snd_game_sfx_clips = new AudioClip[(int)game_sfx_index.ENUM_LENGTH][];
        snd_game_sfx_clips[(int)game_sfx_index.Death] = snd_game_sfx_clips_death;
        snd_game_sfx_clips[(int)game_sfx_index.Kill] = snd_game_sfx_clips_kill;
        snd_game_sfx_clips[(int)game_sfx_index.HitSend] = snd_game_sfx_clips_hitsend;
        snd_game_sfx_clips[(int)game_sfx_index.HitReceive] = snd_game_sfx_clips_hitreceive;
        snd_game_sfx_clips[(int)game_sfx_index.WeaponFire] = snd_game_sfx_clips_weaponfire;

        PlaySFXFromArray(snd_ready_music_source, snd_ready_music_clips);
        // To-do: make this map-specific
        if (item_spawns_parent != null)
        {
            item_spawns = new ItemSpawner[item_spawns_parent.transform.childCount];
            var item_index = 0;
            foreach (Transform t in item_spawns_parent.transform)
            {
                item_spawns[item_index] = t.GetComponent<ItemSpawner>();
                item_spawns[item_index].item_spawn_global_index = item_index;
                item_index++;
            }
        }
        RoundRefreshUI();
    }

    public float[] GetStatsFromWeaponType(int weapon_type)
    {
        var weapon_stats = new float[(int)weapon_stats_name.ENUM_LENGTH];
        switch (weapon_type)
        {
            case (int)weapon_type_name.PunchingGlove:
                weapon_stats[(int)weapon_stats_name.Cooldown] = 1.1f;
                weapon_stats[(int)weapon_stats_name.Projectile_Distance] = 1.8f;
                weapon_stats[(int)weapon_stats_name.Projectile_Duration] = 0.001f;
                weapon_stats[(int)weapon_stats_name.Projectile_Type] = (int)projectile_type_name.Bullet;
                weapon_stats[(int)weapon_stats_name.Hurtbox_Damage] = 10.0f;
                weapon_stats[(int)weapon_stats_name.Hurtbox_Size] = 0.8f;
                weapon_stats[(int)weapon_stats_name.Hurtbox_Duration] = 1.0f;
                weapon_stats[(int)weapon_stats_name.Hurtbox_Damage_Type] = (int)damage_type_name.Strike;
                break;
            default:
                weapon_stats[(int)weapon_stats_name.Cooldown] = 1.0f;
                weapon_stats[(int)weapon_stats_name.Projectile_Distance] = 1.0f;
                weapon_stats[(int)weapon_stats_name.Projectile_Duration] = 1.0f;
                weapon_stats[(int)weapon_stats_name.Projectile_Type] = (int)projectile_type_name.Bullet;
                weapon_stats[(int)weapon_stats_name.Hurtbox_Damage] = 1.0f;
                weapon_stats[(int)weapon_stats_name.Hurtbox_Size] = 1.0f;
                weapon_stats[(int)weapon_stats_name.Hurtbox_Duration] = 1.0f;
                weapon_stats[(int)weapon_stats_name.Hurtbox_Damage_Type] = (int)damage_type_name.Strike;
                break;
        }
        return weapon_stats;
    }

    // -- Continously Running --
    private void Update()
    {
        // Local handling
        round_timer = (float)Networking.CalculateServerDeltaTime(Networking.GetServerTimeInSeconds(), round_start_ms);
        room_ready_txt.text = "Game State: " + round_state.ToString()
            + "\nPlayers Ready: " + ply_ready_arr.Length.ToString() + "\n {" + ply_ready_str + "}"
            + "\n Players In-Game: " + ply_in_game_arr.Length.ToString() + "\n {" + ply_in_game_str + "}"
            + room_ready_status_text
            + "\n Teams: " + teams_assigned_synced_str
            ;

        /*
        if (round_state == (int)round_state_name.Ready || round_state == (int)round_state_name.Ongoing) { ply_teams_arr = DebugTeamArray(ply_in_game_arr); }
        else { ply_teams_arr = DebugTeamArray(ply_ready_arr); }
        if (ply_teams_arr != null)
        {
            room_ready_txt.text += "\n Teams: ";
            for (int i = 0; i < ply_teams_arr.Length; i++)
            {
                room_ready_txt.text += "\n" + i.ToString() + ": ";
                if (i > 0) { room_ready_txt.text += "\n"; }
                for (int j = 0; j < ply_teams_arr[i].Length; j++)
                {
                    if (j > 0) { room_ready_txt.text += ","; }
                    else { room_ready_txt.text += "{"; }
                    room_ready_txt.text += ply_teams_arr[i][j].ToString();
                    if (j == ply_teams_arr[i].Length - 1) { room_ready_txt.text += "}"; }
                }
            }
        }*/

        // Master handling
        if (!Networking.LocalPlayer.isMaster) { return; }
        if (round_state == (int)round_state_name.Ready && round_timer >= ready_length)
        {
            round_start_ms = Networking.GetServerTimeInSeconds();
            round_state = (int)round_state_name.Ongoing;

            for (int j = 0; j < item_spawns.Length; j++)
            {
                // Don't spawn the item if it is FFA-only and we are in teamplay mode
                if (option_teamplay && item_spawns[j].item_spawn_team == -2) { item_spawns[j].item_spawn_state = (int)item_spawn_state_name.Disabled; }
                else { item_spawns[j].item_spawn_state = (int)item_spawn_state_name.Spawnable; }
            }

            RequestSerialization();
        }
        else if (round_state == (int)round_state_name.Ongoing && round_timer >= round_length)
        {
            round_start_ms = Networking.GetServerTimeInSeconds();
            round_state = (int)round_state_name.Over;
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "RoundEnd");
        }
        else if (round_state == (int)round_state_name.Over && round_timer >= ready_length)
        {
            round_start_ms = Networking.GetServerTimeInSeconds();
            round_state = (int)round_state_name.Start;
            RoundRefreshUI();
            RequestSerialization();
        }
    }

    public override void OnDeserialization()
    {
        ply_ready_arr = ConvertStrToIntArray(ply_ready_str);
        ply_in_game_arr = ConvertStrToIntArray(ply_in_game_str);

        RoundRefreshUI();
    }

    public void RoundRefreshUI()
    {
        ui_round_teamplay_toggle.isOn = option_teamplay;
        ui_round_master_only_toggle.isOn = option_start_from_master_only;
        ui_round_team_panel.UpdateFlags();

        bool enableRoundStartButton = true;
        room_ready_status_text = "";
        if (!Networking.LocalPlayer.isMaster && option_start_from_master_only) { enableRoundStartButton = false; room_ready_status_text = "\n (Cannot start round; user is not instance owner!)"; }
        else if (round_state != (int)round_state_name.Start) { enableRoundStartButton = false; room_ready_status_text = "\n (Cannon start new round; a game is already ongoing!)"; }
        else if (option_gamemode == (int)round_mode_name.BossBash || option_gamemode == (int)round_mode_name.Infection)
        {
            if (ui_round_team_panel.team_count_arr.Length < 2 || option_team_limits_arr.Length < 2) { enableRoundStartButton = false; room_ready_status_text = "\n (Cannot start game; there are fewer than 2 teams!)"; }
            else if (ui_round_team_panel.team_count_arr[1] > option_team_limits_arr[1]) { enableRoundStartButton = false; room_ready_status_text = "\n (Cannot start game; there are too many players on the 2nd team!)"; }
            else if (ui_round_team_panel.team_count_arr[1] <= 0) { enableRoundStartButton = false; room_ready_status_text = "\n (Cannot start game; there are not enough players on the 2nd team!)"; }
        }

        ui_round_start_button.enabled = enableRoundStartButton;
        ui_round_length_input.text = Mathf.FloorToInt(round_length).ToString();

        if (Networking.LocalPlayer.isMaster)
        {
            ui_round_master_only_toggle.enabled = true;
            ui_round_teamplay_toggle.enabled = true;
            ui_round_option_dropdown.enabled = true;
            ui_round_length_input.enabled = true;
            ui_round_option_goal_input_a.enabled = true;
            ui_round_option_goal_input_b.enabled = true;
            ui_round_teamplay_sort_button.enabled = option_teamplay && !option_force_teams;
            ui_round_teamplay_count_input.enabled = option_teamplay && !option_force_teams;
        }
        else
        {
            ui_round_master_only_toggle.enabled = false;
            ui_round_teamplay_toggle.enabled = false;
            ui_round_option_dropdown.enabled = false;
            ui_round_length_input.enabled = false;
            ui_round_option_goal_input_a.enabled = false;
            ui_round_option_goal_input_b.enabled = false;
            ui_round_teamplay_sort_button.enabled = false;
            ui_round_teamplay_count_input.enabled = false;
        }

        if (VRCPlayerApi.GetPlayerById(ply_master_id) != null)
        {
            ui_round_master_text.text = "Instance Owner:\n" + VRCPlayerApi.GetPlayerById(ply_master_id).displayName;
        }

        ui_round_teamplay_count_input.text = team_count.ToString();
        ui_round_option_dropdown.value = option_gamemode;
        if (option_gamemode == (int)round_mode_name.Survival)
        {
            ui_round_option_goal_text_a.text = "Lives";
            ui_round_option_goal_input_a.text = plysettings_lives.ToString();
            ui_round_option_goal_text_b.gameObject.SetActive(false);
            ui_round_option_goal_input_b.gameObject.SetActive(false);

        }
        else if (option_gamemode == (int)round_mode_name.Clash)
        {
            ui_round_option_goal_text_a.text = "Points to Win";
            ui_round_option_goal_input_a.text = option_goal_value_a.ToString();
            ui_round_option_goal_text_b.gameObject.SetActive(false);
            ui_round_option_goal_input_b.gameObject.SetActive(false);
        }
        else if (option_gamemode == (int)round_mode_name.BossBash)
        {
            ui_round_option_goal_text_a.text = "Boss Eliminations";
            ui_round_option_goal_input_a.text = option_goal_value_a.ToString();
            ui_round_option_goal_text_b.text = "Boss Lives";
            ui_round_option_goal_input_b.text = option_goal_value_b.ToString();
            ui_round_option_goal_text_b.gameObject.SetActive(true);
            ui_round_option_goal_input_b.gameObject.SetActive(true);

        }
        else if (option_gamemode == (int)round_mode_name.Infection)
        {
            ui_round_option_goal_text_a.text = "Starting Infected";
            ui_round_option_goal_input_a.text = option_goal_value_a.ToString();
            ui_round_option_goal_text_b.gameObject.SetActive(false);
            ui_round_option_goal_input_b.gameObject.SetActive(false);
        }

    }

    public void RoundOptionAdjust()
    {
        if (!Networking.LocalPlayer.isMaster) { return; }

        option_gamemode = (byte)ui_round_option_dropdown.value;

        int try_goal_parse = 1;
        if (option_gamemode == (int)round_mode_name.Survival)
        {
            option_goal_points_a = false;
            option_goal_points_b = true;
            Int32.TryParse(ui_round_option_goal_input_a.text, out try_goal_parse);
            try_goal_parse = Mathf.Min(Mathf.Max(try_goal_parse, 1), 65535);
            plysettings_lives = (ushort)try_goal_parse;
        }
        else 
        {
            plysettings_lives = 1; // Just set lives to 1 if we're in points mode
            option_goal_points_a = true;
            option_goal_points_b = true;
            Int32.TryParse(ui_round_option_goal_input_a.text, out try_goal_parse);
            try_goal_parse = Mathf.Min(Mathf.Max(try_goal_parse, 1), 65535);
            option_goal_value_a = (ushort)try_goal_parse;
            if (option_gamemode == (int)round_mode_name.Infection)
            {
                ui_round_teamplay_toggle.isOn = true;
                ui_round_teamplay_count_input.text = "2";
                option_force_teams = true;
                option_team_limits_str = "80," + option_goal_value_a;
                option_team_limits_arr = ConvertStrToIntArray(option_team_limits_str);
            }
            else if (option_gamemode == (int)round_mode_name.BossBash)
            {
                ui_round_teamplay_toggle.isOn = true;
                ui_round_teamplay_count_input.text = "2";
                option_force_teams = true;
                option_team_limits_str = "80,1";
                option_team_limits_arr = ConvertStrToIntArray(option_team_limits_str);

                option_goal_points_b = false;
                Int32.TryParse(ui_round_option_goal_input_b.text, out try_goal_parse);
                try_goal_parse = Mathf.Min(Mathf.Max(try_goal_parse, 1), 65535);
                option_goal_value_b = (ushort)try_goal_parse;
            }
        }

        option_teamplay = ui_round_teamplay_toggle.isOn;
        option_start_from_master_only = ui_round_master_only_toggle.isOn;
        option_force_teams = false;

        int team_input_parse = 1;
        Int32.TryParse(ui_round_teamplay_count_input.text, out team_input_parse);
        team_input_parse = Mathf.Min(Mathf.Max(team_input_parse, 1), team_colors.Length - 1, 255);
        if (option_teamplay) { team_count = (byte)team_input_parse; }
        else { team_count = 1; }

        
        int try_round_length_parse = Mathf.FloorToInt(round_length);
        Int32.TryParse(ui_round_length_input.text, out try_round_length_parse);
        try_round_length_parse = Mathf.Min(Mathf.Max(try_round_length_parse, 60), 2147483647);
        round_length = (float)try_round_length_parse;

        if (round_state == (int)round_state_name.Start) 
        {
            teams_assigned_synced_arr = ui_round_team_panel.player_team_list_arr;
            teams_assigned_synced_str = ui_round_team_panel.player_team_list_str;
        }

        ui_round_team_panel.UpdateFlags();
        RequestSerialization();
        RoundRefreshUI();
    }

    public override void OnPlayerJoined(VRCPlayerApi player)
    {
        base.OnPlayerJoined(player);
        var plyWeaponObj = FindPlayerOwnedObject(player, "PlayerWeapon");
        var plyAttributesObj = FindPlayerOwnedObject(player, "PlayerAttributes");
        var plyHitboxObj = FindPlayerOwnedObject(player, "PlayerHitbox");
        var plyUIToOthers = FindPlayerOwnedObject(player, "UIPlyToOthers");
        var plyUIToSelf = FindPlayerOwnedObject(player, "UIPlyToSelf");
        Networking.SetOwner(player, plyWeaponObj);
        Networking.SetOwner(player, plyAttributesObj);
        Networking.SetOwner(player, plyHitboxObj);
        plyHitboxObj.GetComponent<PlayerHitbox>().owner = player;
        plyHitboxObj.GetComponent<PlayerHitbox>().playerAttributes = plyAttributesObj.GetComponent<PlayerAttributes>();
        Networking.SetOwner(player, plyUIToOthers);
        plyUIToOthers.GetComponent<UIPlyToOthers>().owner = player;
        plyUIToOthers.GetComponent<UIPlyToOthers>().playerAttributes = plyAttributesObj.GetComponent<PlayerAttributes>();
        Networking.SetOwner(player, plyUIToSelf);
        plyUIToSelf.GetComponent<UIPlyToSelf>().owner = player;
        plyUIToSelf.GetComponent<UIPlyToSelf>().playerAttributes = plyAttributesObj.GetComponent<PlayerAttributes>();


        plyAttributesObj.GetComponent<PlayerAttributes>().plyEyeHeight_default = player.GetAvatarEyeHeightAsMeters();
        plyAttributesObj.GetComponent<PlayerAttributes>().plyEyeHeight_desired = player.GetAvatarEyeHeightAsMeters();

        plyWeaponObj.SetActive(false);
        plyHitboxObj.SetActive(false);
        if (Networking.LocalPlayer == player)
        {
            plyUIToOthers.SetActive(false);
            plyUIToSelf.SetActive(true);
        }
        else
        {
            plyUIToOthers.SetActive(true);
            plyUIToSelf.SetActive(false);
        }
        if (Networking.LocalPlayer.isMaster) { RequestSerialization(); }
    }

    public override void OnPlayerLeft(VRCPlayerApi player)
    {
        var objects = Networking.GetPlayerObjects(player);
        for (int i = 0; i < objects.Length; i++)
        {
            if (!Utilities.IsValid(objects[i])) continue;
            if (Utilities.IsValid(objects[i]))
            {
                Destroy(objects[i]);
            }
        }
        if (Networking.LocalPlayer.isMaster)
        {
            ManipulatePlyTrackingArray(player.playerId, (int)ply_tracking_arr_name.Ready, false);
            ManipulatePlyTrackingArray(player.playerId, (int)ply_tracking_arr_name.InGame, false);
            ply_master_id = Networking.LocalPlayer.playerId;
            RequestSerialization();
        }
    }

    /*public int[][] DebugTeamArray(int[] trackingArr)
    {
        if (!Networking.LocalPlayer.isMaster) { return new int[0][]; }
        if (team_count <= 0) { UnityEngine.Debug.LogError("Team count is <= 0!"); return new int[0][]; } // This should never happen
        var ply_teams = new int[team_count][];
        for (int j = 0; j < team_count; j++)
        {
            var ply_team = new int[trackingArr.Length];
            for (int i = 0; i < trackingArr.Length; i++)
            {
                var player = VRCPlayerApi.GetPlayerById(trackingArr[i]);
                if (player == null) { continue; }
                var plyAttributes = FindPlayerAttributes(player);
                if (plyAttributes == null) { continue; }
                ply_team[i] = trackingArr[i];
            }
            ply_teams[j] = ply_team;
        }
        return ply_teams;
    }*/

    // -- Round Management --
    [NetworkCallable]
    public void NetworkRoundStart()
    {
        // To-do: add gamemode variables
        for (var i = 0; i < ply_ready_arr.Length; i++)
        {
            var player = VRCPlayerApi.GetPlayerById(ply_ready_arr[i]);
            if (player == null) { continue; }
            var plyWeaponObj = FindPlayerOwnedObject(player, "PlayerWeapon");
            var plyHitboxObj = FindPlayerOwnedObject(player, "PlayerHitbox");
            plyWeaponObj.SetActive(true);
            plyHitboxObj.SetActive(true);

            if (!player.isLocal) { continue; }

            PlayerAttributes playerData = FindPlayerAttributes(player);
            TeleportLocalPlayerToGameSpawnZone(i % room_game_spawnzones.Length);
            playerData.ply_dp = plysettings_dp;
            playerData.ply_dp_default = plysettings_dp;
            playerData.ply_points = plysettings_points;
            playerData.ply_respawn_duration = plysettings_respawn_duration;
            playerData.ply_scale = plysettings_scale;
            playerData.ply_speed = plysettings_speed;
            playerData.ply_atk = plysettings_atk;
            playerData.ply_def = plysettings_def;
            playerData.ply_grav = plysettings_grav;
            playerData.ply_state = (int)player_state_name.Alive;
            if (!option_teamplay) { playerData.ply_team = 0; }
            else { playerData.ply_team = teams_assigned_synced_arr[i]; }
            if (ui_round_option_dropdown.value == (int)round_mode_name.BossBash && playerData.ply_team == 1) { playerData.ply_lives = option_goal_value_b; }
            else { playerData.ply_lives = plysettings_lives; }

            if (ui_round_option_dropdown.value == (int)round_mode_name.BossBash && playerData.ply_team == 1) 
            {
                playerData.ply_scale = plysettings_scale * 3.0f;
                playerData.plyEyeHeight_desired = playerData.plyEyeHeight_default * 3.0f;
                playerData.plyEyeHeight_change = true;
            }
        }

        snd_ready_music_source.Stop();
        PlaySFXFromArray(snd_game_music_source, snd_game_music_clips);

        if (!Networking.LocalPlayer.isMaster) { return; }
        round_state = (int)round_state_name.Ready;
        round_start_ms = Networking.GetServerTimeInSeconds();
        ply_in_game_arr = ValidatePlayerArray(ply_ready_arr);
        ply_in_game_str = ConvertIntArrayToString(ply_in_game_arr);
        RequestSerialization();
    }

    public void SendRoundStart()
    {

        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "NetworkRoundStart");
    }


    public int[] CheckAllTeamLives(out int total_players_alive)
    {
        var plyAlivePerTeam = new int[team_count];
        total_players_alive = 0;
        for (int i = 0; i < ply_in_game_arr.Length; i++)
        {
            var player = VRCPlayerApi.GetPlayerById(ply_in_game_arr[i]);
            if (player == null) { continue; }
            var plyAttributes = FindPlayerAttributes(player);
            if (plyAttributes == null) { continue; }
            if (plyAttributes.ply_lives > 0)
            {
                if (plyAttributes.ply_team > team_count) { UnityEngine.Debug.LogError("Player is on team " + plyAttributes.ply_team.ToString() + ", but the game only has " + team_count.ToString() + " teams!"); continue; }
                if (plyAttributes.ply_team >= 0) { plyAlivePerTeam[plyAttributes.ply_team]++; } // Should only ever error out in ClientSim, but just in case, let's make sure it's not index < 0
                total_players_alive++;
            }
        }
        return plyAlivePerTeam;
    }

    public int CheckSpecificTeamLives(int team_id)
    {
        var members_alive = 0;
        if (team_id > team_count) { UnityEngine.Debug.LogError("Attempted to check for team lives when the team (" + team_id + ") exceeds team count (" + team_count + ")!"); return 0; }
        for (int i = 0; i < ply_in_game_arr.Length; i++)
        {
            var player = VRCPlayerApi.GetPlayerById(ply_in_game_arr[i]);
            if (player == null) { continue; }
            var plyAttributes = FindPlayerAttributes(player);
            if (plyAttributes == null) { continue; }
            if (plyAttributes.ply_lives > 0 && plyAttributes.ply_team == team_id) { members_alive++; }
        }
        return members_alive;
    }

    public int[] CheckAllTeamPoints()
    {
        var plyPointsPerTeam = new int[team_count];
        for (int i = 0; i < ply_in_game_arr.Length; i++)
        {
            var player = VRCPlayerApi.GetPlayerById(ply_in_game_arr[i]);
            if (player == null) { continue; }
            var plyAttributes = FindPlayerAttributes(player);
            if (plyAttributes == null) { continue; }
            if (plyAttributes.ply_team > team_count) { UnityEngine.Debug.LogError("Player is on team " + plyAttributes.ply_team.ToString() + ", but the game only has " + team_count.ToString() + " teams!"); continue; }
            if (plyAttributes.ply_team >= 0) { plyPointsPerTeam[plyAttributes.ply_team] += plyAttributes.ply_points; } // Should only ever error out in ClientSim, but just in case, let's make sure it's not index < 0     
        }
        return plyPointsPerTeam;
    }

    public int CheckSpecificTeamPoints(int team_id)
    {
        var total_points = 0;
        if (team_id > team_count) { UnityEngine.Debug.LogError("Attempted to check for team lives when the team (" + team_id + ") exceeds team count (" + team_count + ")!"); return 0; }
        for (int i = 0; i < ply_in_game_arr.Length; i++)
        {
            var player = VRCPlayerApi.GetPlayerById(ply_in_game_arr[i]);
            if (player == null) { continue; }
            var plyAttributes = FindPlayerAttributes(player);
            if (plyAttributes == null) { continue; }
            if (plyAttributes.ply_team == team_id) { total_points = plyAttributes.ply_points; }
        }
        return total_points;
    }

    [NetworkCallable]
    public void CheckForRoundGoal()
    {
        if (!Networking.LocalPlayer.isMaster) { return; }
        if (round_state != (int)round_state_name.Ongoing) { return; }
        if (team_count <= 0) { UnityEngine.Debug.LogError("Team count is <= 0!"); return; } // This should never happen

        bool teamplayOver = false;
        bool freeForAllOver = false;

        // Points Check
        // Right now, all gamemodes use option_goal_value_a for points, but this may need support for future modes
        if (option_goal_points_a)
        {
            var plyPointsPerTeam = new int[team_count];
            plyPointsPerTeam = CheckAllTeamPoints();
            for (int j = 0; j < team_count; j++)
            {
                if (plyPointsPerTeam[j] > option_goal_value_a) { teamplayOver = true; break; }
            }
        }

        // Lives check
        if (!teamplayOver && (!option_goal_points_a || !option_goal_points_b)) { 
            var plyAlivePerTeam = new int[team_count];
            var teams_alive = 0;
            var total_players_alive = 0;
            plyAlivePerTeam = CheckAllTeamLives(out total_players_alive);
            for (int j = 0; j < team_count; j++)
            {
                if (plyAlivePerTeam[j] > 0) { teams_alive++; }
            }

            teamplayOver = option_teamplay &&
            (
                (teams_alive <= 1 && team_count > 1) ||
                (teams_alive <= 0 && team_count == 1) ||
                (team_count <= 0)
            );
            freeForAllOver = !option_teamplay &&
                (
                    (total_players_alive <= 1 && ply_in_game_arr.Length > 1) ||
                    (total_players_alive <= 0 && ply_in_game_arr.Length == 1) ||
                    (ply_in_game_arr.Length == 0)
                );
        }
 
        if (teamplayOver || freeForAllOver)
        {
            round_state = (int)round_state_name.Over;
            round_start_ms = Networking.GetServerTimeInSeconds();
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "RoundEnd");
        }
    }

    public void RoundEnd()
    {
        for (int i = 0; i < ply_in_game_arr.Length; i++)
        {
            var player = VRCPlayerApi.GetPlayerById(ply_in_game_arr[i]);
            if (player == null) { continue; }
            var plyAttributes = FindPlayerAttributes(player);
            var plyWeapon = FindPlayerOwnedObject(player, "PlayerWeapon");
            var plyHitboxObj = FindPlayerOwnedObject(player, "PlayerHitbox");
            plyWeapon.SetActive(false);
            plyHitboxObj.SetActive(false);
            if (player == Networking.LocalPlayer)
            {
                if (plyWeapon.GetComponent<VRC_Pickup>() != null) { plyWeapon.GetComponent<VRC_Pickup>().Drop(); }
                if (plyAttributes.ply_state != (int)player_state_name.Spectator) { plyAttributes.ply_state = (int)player_state_name.Inactive; }
                plyAttributes.ResetPowerups();
                plyAttributes.ply_scale = 1.0f;
                plyAttributes.plyEyeHeight_desired = plyAttributes.plyEyeHeight_default;
                plyAttributes.plyEyeHeight_change = true;
                TeleportLocalPlayerToReadyRoom();

            }
        }

        // To-do: make this map-specific
        for (int j = 0; j < item_spawns.Length; j++)
        {
            item_spawns[j].item_spawn_state = (int)item_spawn_state_name.Disabled;
        }

        /*
        for (int j = 0; j < powerup_list.Length; j++) 
        {
            if (powerup_list[j] == null) { continue; }
            SendDestroyPowerup(powerup_list[j].GetComponent<ItemPowerup>().powerup_stored_global_index, (int)item_powerup_destroy_reason_code.ItemExpire, false ); 
        }
        */

        snd_game_music_source.Stop();
        PlaySFXFromArray(snd_ready_music_source, snd_ready_music_clips);

        if (Networking.LocalPlayer.isMaster)
        {
            // To-do: Make a leaderboard so people can feel good after the game
            //ply_in_game_arr = new int[0];
            //ply_in_game_str = "";
            RequestSerialization();
        }
    }

    public void TeleportLocalPlayerToGameSpawnZone(int spawnZoneIndex = -1)
    {
        // If no spawnzone is specified, just use a random one
        if (spawnZoneIndex == -1) { spawnZoneIndex = UnityEngine.Random.Range(0, room_game_spawnzones.Length - 1); }

        var spawnZoneBounds = room_game_spawnzones[spawnZoneIndex].bounds;
        var rx = UnityEngine.Random.Range(spawnZoneBounds.min.x, spawnZoneBounds.max.x);
        var rz = UnityEngine.Random.Range(spawnZoneBounds.min.z, spawnZoneBounds.max.z);

        Networking.LocalPlayer.TeleportTo(new Vector3(rx, spawnZoneBounds.center.y, rz), Networking.LocalPlayer.GetRotation());
    }

    public void TeleportLocalPlayerToReadyRoom()
    {
        Networking.LocalPlayer.SetVelocity(new Vector3(0.0f, 0.0f, 0.0f));
        Networking.LocalPlayer.TeleportTo(room_ready_spawn.transform.position, Networking.LocalPlayer.GetRotation());
    }

    // -- Weapon Handling --
    [NetworkCallable]
    public void NetworkCreateProjectile(int weapon_type, Vector3 fire_start_pos, Quaternion fire_angle, float distance, double fire_start_ms, bool keep_parent, int player_id)
    {
        var newProjectileObj = Instantiate(template_WeaponProjectile, transform);
        newProjectileObj.transform.parent = null;
        newProjectileObj.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        newProjectileObj.transform.SetPositionAndRotation(fire_start_pos, fire_angle);

        var projectile = newProjectileObj.GetComponent<WeaponProjectile>();
        projectile.weapon_type = weapon_type;
        projectile.projectile_type = (int)GetStatsFromWeaponType(weapon_type)[(int)weapon_stats_name.Projectile_Type];
        projectile.projectile_duration = GetStatsFromWeaponType(weapon_type)[(int)weapon_stats_name.Projectile_Duration];
        projectile.projectile_start_ms = fire_start_ms;
        projectile.pos_start = fire_start_pos;
        projectile.projectile_distance = distance;
        projectile.owner_id = player_id;
        projectile.template_WeaponHurtbox = template_WeaponHurtbox;
        projectile.gameController = this;
        projectile.keep_parent = keep_parent;

        if (keep_parent)
        {
            var player = VRCPlayerApi.GetPlayerById(player_id);
            if (player != null)
            {
                var weaponObj = FindPlayerOwnedObject(player, "PlayerWeapon");
                if (weaponObj == null || weaponObj.GetComponent<PlayerWeapon>() == null)
                {
                    newProjectileObj.transform.parent = weaponObj.transform;
                    //projectile.projectile_distance = 
                }
            }
        }

        newProjectileObj.SetActive(true);

    }

    [NetworkCallable]
    public void NetworkCreateHurtBox(Vector3 position, float damage, double start_ms, bool keep_parent, int player_id, int weapon_type)
    {
        var newHurtboxObj = Instantiate(template_WeaponHurtbox, transform);

        newHurtboxObj.transform.parent = null;
        var hurtbox = newHurtboxObj.GetComponent<WeaponHurtbox>();

        newHurtboxObj.transform.localScale = GetStatsFromWeaponType(weapon_type)[(int)weapon_stats_name.Hurtbox_Size] * new Vector3(1.0f, 1.0f, 1.0f);
        newHurtboxObj.transform.position = position;

        if (keep_parent)
        {
            var player = VRCPlayerApi.GetPlayerById(player_id);
            if (player != null)
            {
                var weaponObj = FindPlayerOwnedObject(player, "PlayerWeapon");
                if (weaponObj != null && weaponObj.GetComponent<PlayerWeapon>() != null)
                {
                    newHurtboxObj.transform.parent = weaponObj.transform;
                }
            }
        }

        hurtbox.hurtbox_damage = damage;
        hurtbox.hurtbox_duration = GetStatsFromWeaponType(weapon_type)[(int)weapon_stats_name.Hurtbox_Duration];
        hurtbox.hurtbox_start_ms = start_ms;
        hurtbox.damage_type = (int)GetStatsFromWeaponType(weapon_type)[(int)weapon_stats_name.Hurtbox_Damage_Type];
        hurtbox.owner_id = player_id;
        hurtbox.gameController = this;
        newHurtboxObj.SetActive(true);

    }

    // -- Player Tracking --
    [NetworkCallable]
    public void ManipulatePlyTrackingArray(int player_id, int ply_tracking_arr_id, bool op_add)
    {
        if (op_add) 
        { 
            AddPlyToTrackingArray(player_id, ply_tracking_arr_id);
            if (ply_tracking_arr_id == (int)ply_tracking_arr_name.Ready) { ui_round_team_panel.ManipulateTeamArray(player_id, op_add); }
        }
        else 
        {
            if (ply_tracking_arr_id == (int)ply_tracking_arr_name.Ready) { ui_round_team_panel.ManipulateTeamArray(player_id, op_add); }
            RemovePlyFromTrackingArray(player_id, ply_tracking_arr_id); 
        }
        RequestSerialization();
    }

    public void AddPlyToTrackingArray(int player_id, int ply_tracking_arr_id)
    {
        switch (ply_tracking_arr_id)
        {
            case (int)ply_tracking_arr_name.Ready:
                ply_ready_arr = ValidatePlayerArray(AddToIntArray(player_id, ply_ready_arr));
                ply_ready_str = ConvertIntArrayToString(ply_ready_arr);
                break;
            case (int)ply_tracking_arr_name.InGame:
                ply_in_game_arr = ValidatePlayerArray(AddToIntArray(player_id, ply_in_game_arr));
                ply_in_game_str = ConvertIntArrayToString(ply_in_game_arr);
                break;
            default:
                break;
        }
    }

    [NetworkCallable]
    public void RemovePlyFromTrackingArray(int player_id, int ply_tracking_arr_id)
    {
        switch (ply_tracking_arr_id)
        {
            case (int)ply_tracking_arr_name.Ready:
                ply_ready_arr = ValidatePlayerArray(RemoveValueFromIntArray(player_id, ply_ready_arr));
                ply_ready_str = ConvertIntArrayToString(ply_ready_arr);
                break;
            case (int)ply_tracking_arr_name.InGame:
                ply_in_game_arr = ValidatePlayerArray(RemoveValueFromIntArray(player_id, ply_in_game_arr));
                ply_in_game_str = ConvertIntArrayToString(ply_in_game_arr);
                break;
            default:
                break;
        }
    }

    public int[] ValidatePlayerArray(int[] arrIn)
    {
        if (arrIn.Length <= 0) { return arrIn; }
        var valid_indices = new int[arrIn.Length];
        var valid_count = 0;
        for (var i = 0; i < arrIn.Length; i++)
        {
            if (VRCPlayerApi.GetPlayerById(arrIn[i]) == null) { continue; }
            valid_indices[valid_count] = arrIn[i];
            valid_count++;
        }
        var arrOut = new int[valid_count];
        for (var j = 0; j < arrOut.Length; j++)
        {
            arrOut[j] = valid_indices[j];
        }
        return arrOut;
    }

    [NetworkCallable]
    public void PlayerAutoSortTeams()
    {
        if (!Networking.LocalPlayer.isMaster) { return; }
        if (round_state == (int)round_state_name.Start)
        {
            teams_assigned_synced_arr = ui_round_team_panel.player_team_list_arr;
            teams_assigned_synced_str = ui_round_team_panel.player_team_list_str;
        }
        var team_list_sorted = teams_assigned_synced_arr;
        var counts_per_team = new int[team_count];

        var iterate_index = 0;
        var player_count_to_sort = teams_assigned_synced_arr.Length;
        var shrinking_index_array = new int[teams_assigned_synced_arr.Length];
        for (int i = 0; i < team_list_sorted.Length; i++)
        {
            shrinking_index_array[i] = i;
        }

        while (iterate_index < player_count_to_sort)
        {
            var randIndex = shrinking_index_array[UnityEngine.Random.Range((int)0, (int)shrinking_index_array.Length)];

            // If we are enforcing team limits, only distribute up to the limit for each team, in reverse order. Otherwise, distribute evenly.
            // To-do: Why this fails: if I have 4 players, wanting to distribute to 3 teams, it will assign the first 3, but never assign the 4th due to exceeding limit, thus failing.
            UnityEngine.Debug.Log("max_team_size calc: " + player_count_to_sort / team_count);
            var max_team_size = Mathf.Min(1,Mathf.Ceil(player_count_to_sort / team_count));
            UnityEngine.Debug.Log("max_team_size: " + max_team_size);
            for (int j = counts_per_team.Length - 1; j >= 0; j--)
            {
                if (option_force_teams) { max_team_size = option_team_limits_arr[j]; }

                if (counts_per_team[j] < max_team_size) {
                    team_list_sorted[randIndex] = j;
                    counts_per_team[j]++;
                    iterate_index++;
                    shrinking_index_array = RemoveIndexFromIntArray(randIndex, shrinking_index_array);
                    break;
                }
            }
        }

        teams_assigned_synced_arr = team_list_sorted;
        teams_assigned_synced_str = ConvertIntArrayToString(team_list_sorted);
        ui_round_team_panel.RequestSerialization();
        RequestSerialization();
    }

    // -- Player Helper Functions --
    public GameObject FindPlayerOwnedObject(VRCPlayerApi player, string objName)
    {
        var objects = Networking.GetPlayerObjects(player);
        for (int i = 0; i < objects.Length; i++)
        {
            if (!Utilities.IsValid(objects[i])) continue;
            if (!objects[i].name.Contains(objName)) continue;
            if (Utilities.IsValid(objects[i]))
            {
                return objects[i];
            }
        }
        return null;
    }

    public PlayerAttributes FindPlayerAttributes(VRCPlayerApi player)
    {
        var objects = Networking.GetPlayerObjects(player);
        for (int i = 0; i < objects.Length; i++)
        {
            if (!Utilities.IsValid(objects[i])) continue;
            if (!objects[i].name.Contains("PlayerAttributes")) continue;
            PlayerAttributes foundScript = objects[i].GetComponentInChildren<PlayerAttributes>();
            if (Utilities.IsValid(foundScript))
            {
                return foundScript;
            }
        }
        return null;
    }

    // -- Game Helper Functions --
    public void PlaySFXFromArray(AudioSource source, AudioClip[] clips, int index = -1, float pitch = 1.0f)
    {
        if (clips.Length <= 0) { return; }
        if (index < 0)
        {
            var randMusic = UnityEngine.Random.Range(0, clips.Length - 1);
            source.clip = clips[randMusic];
        }
        else if (index < clips.Length && clips[index] != null)
        {
            source.clip = clips[index];
        }
        source.Stop();
        source.pitch = pitch;
        source.Play();
    }

    // -- Internal Helper Functions --
    public int StringToInt(string str)
    {
        int result = -404;
        int.TryParse(str, out result); // UdonSharp supports TryParse
        return result;
    }

    public int[] ConvertStrToIntArray(string str)
    {
        string[] splitStr = str.Split(',');
        int[] arrOut = new int[splitStr.Length];

        for (int i = 0; i < splitStr.Length; i++)
        {
            var intAttempt = StringToInt(splitStr[i]);
            if (intAttempt != 404) { arrOut[i] = intAttempt; }
        }
        return arrOut;
    }

    public string ConvertIntArrayToString(int[] arrIn)
    {
        if (arrIn == null || arrIn.Length == 0) return "";

        string result = arrIn[0].ToString();
        for (int i = 1; i < arrIn.Length; i++)
        {
            result += ',';
            result += arrIn[i].ToString();
        }
        return result;
    }

    public int[] AddToIntArray(int inValue, int[] inArr)
    {
        var arrOut = new int[inArr.Length + 1];
        for (var i = 0; i < inArr.Length; i++)
        {
            arrOut[i] = inArr[i];
        }
        arrOut[inArr.Length] = inValue;
        return arrOut;
    }

    public int[] RemoveIndexFromIntArray(int inIndex, int[] inArr)
    {
        // If we are removing the last entry or there are no entries, just return empty array
        if (inArr.Length <= 1) { return new int[0]; }
        var arrOut = new int[inArr.Length - 1];
        for (var i = 0; i < inArr.Length; i++)
        {
            if (inIndex == i) { continue; }
            if (i > inIndex) { arrOut[i - 1] = inArr[i]; }
            else if (i < arrOut.Length) { arrOut[i] = inArr[i]; }
        }
        return arrOut;
    }

    public int[] RemoveValueFromIntArray(int inValue, int[] inArr)
    {
        // If we are removing the last entry or there are no entries, just return empty array
        if (inArr.Length <= 1) { return new int[0]; }
        var arrOut = new int[inArr.Length - 1];
        var found_value = false;
        for (var i = 0; i < inArr.Length; i++)
        {
            if (inValue == inArr[i]) { found_value = true; continue; }
            if (found_value) { arrOut[i - 1] = inArr[i]; }
            else if (i < arrOut.Length) { arrOut[i] = inArr[i]; }
        }
        return arrOut;
    }

    public GameObject[] AddToGameObjectArray(GameObject inValue, GameObject[] inArr)
    {
        var arrOut = new GameObject[inArr.Length + 1];
        for (var i = 0; i < inArr.Length; i++)
        {
            arrOut[i] = inArr[i];
        }
        arrOut[inArr.Length] = inValue;
        return arrOut;
    }

    public GameObject[] RemoveEntryFromGameObjectArray(GameObject inValue, GameObject[] inArr)
    {
        // If we are removing the last entry or there are no entries, just return empty array
        if (inArr.Length <= 1) { return new GameObject[0]; }
        var arrOut = new GameObject[inArr.Length - 1];
        var found_value = false;
        for (var i = 0; i < inArr.Length; i++)
        {
            if (inValue == inArr[i]) { found_value = true; continue; }
            if (found_value) { arrOut[i - 1] = inArr[i]; }
            else if (i < arrOut.Length) { arrOut[i] = inArr[i]; }
        }
        return arrOut;
    }

    public GameObject[] RemoveIndexFromGameObjectArray(int index, GameObject[] inArr)
    {
        // If we are removing the last entry or there are no entries, just return empty array
        if (inArr.Length <= 1) { return new GameObject[0]; }
        var arrOut = new GameObject[inArr.Length - 1];
        for (var i = 0; i < inArr.Length; i++)
        {
            if (i == index) { continue; }
            else if (i > index) { arrOut[i - 1] = inArr[i]; }
            else if (i < index) { arrOut[i] = inArr[i]; }
        }
        return arrOut;
    }

    public GameObject[] AddToStaticGameObjectArray(GameObject inValue, GameObject[] inArr, out int useIndex)
    {
        var arrOut = inArr;
        useIndex = -1;
        for (var i = 0; i < inArr.Length; i++)
        {
            if (inArr[i] == null) { 
                useIndex = i; 
                arrOut[i] = inValue;
                break; 
            }
        }
        return arrOut;
    }

    // Enum replacement helper
    public int KeyToPowerupType(string enum_str_name)
    {
        var cleanStr = enum_str_name.Trim().ToLower();
        var output = (int)powerup_type_name.Fallback;
        if (cleanStr == "sizeup") { output = (int)powerup_type_name.SizeUp; }
        else if (cleanStr == "sizedown") { output = (int)powerup_type_name.SizeDown; }
        else if (cleanStr == "speedup") { output = (int)powerup_type_name.SpeedUp; }
        else if (cleanStr == "atkup") { output = (int)powerup_type_name.AtkUp; }
        else if (cleanStr == "defup") { output = (int)powerup_type_name.DefUp; }
        else if (cleanStr == "atkdown") { output = (int)powerup_type_name.AtkDown; }
        else if (cleanStr == "defdown") { output = (int)powerup_type_name.DefDown; }
        else if (cleanStr == "lowgrav") { output = (int)powerup_type_name.LowGrav; }
        //UnityEngine.Debug.Log("Attempted to match key '" + cleanStr + "' to value: " + output);
        return output;
    }
    public int KeyToWeaponType(string enum_str_name)
    {
        var cleanStr = enum_str_name.Trim().ToLower();
        var output = 0;
        if (cleanStr == "punchingglove") { output = (int)weapon_type_name.PunchingGlove; }
        else if (cleanStr == "bomb") { output = (int)weapon_type_name.Bomb; }
        else if (cleanStr == "rocket") { output = (int)weapon_type_name.Rocket; }
        return output;
    }
    public string DebugPrintFloatArray(float[] inArr)
    {
        var strOut = "{";
        for (int i = 0; i < inArr.Length; i++)
        {
            strOut += inArr[i].ToString();
            if (i < inArr.Length - 1) { strOut += ","; }
        }
        strOut += "}";
        return strOut;
    }
    public string DebugPrintIntArray(int[] inArr)
    {
        var strOut = "{";
        for (int i = 0; i < inArr.Length; i++)
        {
            strOut += inArr[i].ToString();
            if (i < inArr.Length - 1) { strOut += ","; }
        }
        strOut += "}";
        return strOut;
    }
}

