
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
using VRC.SDK3.Components;
using VRC.SDK3.Persistence;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;
using VRC.Udon.Common.Interfaces;

// A note on ENUM_LENGTH: this must ALWAYS BE LAST since we are using it as a shorthand for the length of an enumerator! (The typical method of typeof() is not supported in U#)
public enum game_sfx_name
{
    Death, Kill, HitSend, HitReceive, ENUM_LENGTH
}

public enum round_state_name
{
    Start, Ready, Ongoing, Over, ENUM_LENGTH
}

public enum player_tracking_name
{
    Unassigned = -3, WaitingForLobby = -2, Spectator = -1, ENUM_LENGTH
}

public enum weapon_stats_name
{
    Cooldown, Projectile_Distance, Projectile_Duration, Projectile_Type, Hurtbox_Damage, Hurtbox_Size, Hurtbox_Duration, Hurtbox_Damage_Type, ENUM_LENGTH
}

public enum round_mode_name
{
    Survival, Clash, BossBash, Infection, ENUM_LENGTH
}

public enum dict_compare_name
{
    Equals, GreaterThan, LessThan, GreaterThanOrEqualsTo, LessThanOrEqualsTo, ENUM_LENGTH
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
    [SerializeField] public UnityEngine.UI.Button ui_round_reset_button;
    [SerializeField] public UnityEngine.UI.Toggle ui_round_master_only_toggle;
    [SerializeField] public TextMeshProUGUI ui_round_master_text;
    [SerializeField] public UIRoundTeamPanel ui_round_team_panel;
    [SerializeField] public UnityEngine.UI.Toggle ui_round_teamplay_toggle;
    [SerializeField] public TMP_InputField ui_round_teamplay_count_input;
    [SerializeField] public UnityEngine.UI.Button ui_round_teamplay_sort_button;
    [SerializeField] public TMP_InputField ui_round_length_input;

    [SerializeField] public TMP_Text ui_round_option_description;
    [SerializeField] public TMP_Dropdown ui_round_option_dropdown;
    [SerializeField] public TextMeshProUGUI ui_round_option_goal_text_a;
    [SerializeField] public TextMeshProUGUI ui_round_option_goal_text_b;
    [SerializeField] public TMP_InputField ui_round_option_goal_input_a;
    [SerializeField] public TMP_InputField ui_round_option_goal_input_b;

    [SerializeField] public Room_Ready room_ready_script;
    [SerializeField] public GameObject room_ready_spawn;
    [SerializeField] public Collider[] room_game_spawnzones;
    [SerializeField] public TextMeshProUGUI room_ready_txt;
    [SerializeField] public AudioSource snd_game_music_source;
    [SerializeField] public AudioClip[] snd_game_music_clips;
    [SerializeField] public AudioSource snd_ready_music_source;
    [SerializeField] public AudioClip[] snd_ready_music_clips;
    [Tooltip("Needs to be in the exact order of game_sfx_name")]
    [SerializeField] public AudioSource[] snd_game_sfx_sources;

    [NonSerialized] public AudioClip[][] snd_game_sfx_clips; // Inspector doesn't like 2D arrays 
    [SerializeField] public AudioClip[] snd_game_sfx_clips_death;
    [SerializeField] public AudioClip[] snd_game_sfx_clips_kill;
    [SerializeField] public AudioClip[] snd_game_sfx_clips_hitsend; // NOTE: Corresponds to damage_type
    [SerializeField] public AudioClip[] snd_game_sfx_clips_hitreceive; // NOTE: Corresponds to damage_type

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
    [Tooltip("How often should the goal be checked during a round?")]
    [SerializeField][UdonSynced] public float goal_impulse = 5.0f;
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
    [UdonSynced] public float scale_damage_factor = 2.0f;

    [NonSerialized] public float round_timer = 0.0f;

    //[NonSerialized] [UdonSynced] public string ply_ready_str = "";
    //[NonSerialized] public int[] ply_ready_arr;
    //[NonSerialized] [UdonSynced] public string ply_in_game_str = "";
    //[NonSerialized] public int[] ply_in_game_arr;
    
    [UdonSynced] public bool option_teamplay = false;
    [UdonSynced] public byte team_count = 1;
    [SerializeField] public Color32[] team_colors; // Assign in inspector
    [SerializeField] public Sprite[] team_sprites; // MUST MATCH SIZE OF team_colors

    //[NonSerialized][UdonSynced] public string teams_assigned_synced_str = "";
    //[NonSerialized] public int[] teams_assigned_synced_arr;

    //[NonSerialized] public int[][] ply_teams_arr;

    [NonSerialized] public int[] ply_tracking_dict_keys_arr;
    [NonSerialized][UdonSynced] public string ply_tracking_dict_keys_str = "";
    [NonSerialized] public int[] ply_tracking_dict_values_arr;
    [NonSerialized][UdonSynced] public string ply_tracking_dict_values_str = "";

    [SerializeField] public string[] round_option_names; // NOTE: Corresponds to round_mode_name
    [SerializeField] public string[] round_option_descriptions; // NOTE: Corresponds to round_mode_name
    [UdonSynced] public byte option_gamemode = 0;
    [NonSerialized] public byte local_gamemode_count = 0; // For some reason, Udon doesn't support TMP_Dropwn.options, so we have to track this manually
    [NonSerialized][UdonSynced] public bool option_goal_time = false; // Should we not factor in points for the goal, but instead just wait out the timer?
    [NonSerialized][UdonSynced] public bool option_goal_points_a = false; // If true, points are used instead of lives
    [NonSerialized][UdonSynced] public ushort option_goal_value_a = 10;
    [NonSerialized][UdonSynced] public bool option_goal_points_b = false; // If true, points are used instead of lives
    [NonSerialized][UdonSynced] public ushort option_goal_value_b = 3;
    [NonSerialized][UdonSynced] public bool option_force_teams = false; // Should team limits be enforced? If so
    [NonSerialized][UdonSynced] public string option_team_limits_str = "0,1"; // What is the max # of players per team? Size must match team_count
    [NonSerialized] public int[] option_team_limits_arr;
    [NonSerialized][UdonSynced] public bool option_start_from_master_only = false;

    [NonSerialized] [UdonSynced] public int ply_master_id = 0;

    [NonSerialized] private string room_ready_status_text = "";

    [NonSerialized] private double networkJoinTime = 0;
    [NonSerialized] private bool wait_for_sync_for_round_start = false;

    [NonSerialized] [UdonSynced] public int gamemode_boss_id = 0; // the boss in big boss's ID

    [NonSerialized] public UIPlyToSelf local_uiplytoself;

    // Personal Player Persistence
    [NonSerialized] public PPP_Options local_ppp_options;

    // -- Initialization --
    private void Start()
    {
        //DebugTestDictFunctions();
        item_spawns = new ItemSpawner[0];
        //ply_ready_arr = new int[0];
        //ply_in_game_arr = new int[0];
        option_team_limits_arr = new int[0];
        //teams_assigned_synced_arr = new int[0];
        //ply_teams_arr = new int[0][];
        ply_tracking_dict_keys_arr = new int[0];
        ply_tracking_dict_values_arr = new int[0];

        ply_master_id = Networking.LocalPlayer.playerId;

        Networking.LocalPlayer.SetVoiceDistanceFar(50); // default 25
        Networking.LocalPlayer.SetVoiceGain(18); //default 15

        if (Networking.LocalPlayer.isMaster)
        {
            ResetPlyDicts();
        }

        snd_game_sfx_clips = new AudioClip[(int)game_sfx_name.ENUM_LENGTH][];
        snd_game_sfx_clips[(int)game_sfx_name.Death] = snd_game_sfx_clips_death;
        snd_game_sfx_clips[(int)game_sfx_name.Kill] = snd_game_sfx_clips_kill;
        snd_game_sfx_clips[(int)game_sfx_name.HitSend] = snd_game_sfx_clips_hitsend;
        snd_game_sfx_clips[(int)game_sfx_name.HitReceive] = snd_game_sfx_clips_hitreceive;

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
    
    /*private void DebugTestDictFunctions()
    {
        int[] testKeys = { 420, 69, 1337, 1234 };
        int[] testValues = { 111, 222, 222, 333 };
        UnityEngine.Debug.Log("--- TESTING DICT FUNCTIONS ---");
        UnityEngine.Debug.Log("Index from key 69: " + DictIndexFromKey(69, testKeys));
        UnityEngine.Debug.Log("Value from key 69: " + DictValueFromKey(69, testKeys, testValues));
        DictAddEntry(8008, 13, ref testKeys, ref testValues);
        UnityEngine.Debug.Log("Add new entry 8008 with value 13: " + ConvertIntArrayToString(testKeys) + " | " + ConvertIntArrayToString(testValues));
        DictRemoveEntry(69, ref testKeys, ref testValues);
        UnityEngine.Debug.Log("Remove entry 69: " + ConvertIntArrayToString(testKeys) + " | " + ConvertIntArrayToString(testValues));
        int[][] dictOut = DictFindAllWithValue(222, testKeys, testValues, (int)dict_compare_name.Equals);
        UnityEngine.Debug.Log("All entries with a value = 222: " + ConvertIntArrayToString(dictOut[0]) + " | " + ConvertIntArrayToString(dictOut[1]));
        dictOut = DictFindAllWithValue(222, testKeys, testValues, (int)dict_compare_name.LessThan);
        UnityEngine.Debug.Log("All entries with a value < 222: " + ConvertIntArrayToString(dictOut[0]) + " | " + ConvertIntArrayToString(dictOut[1]));
        dictOut = DictFindAllWithValue(222, testKeys, testValues, (int)dict_compare_name.GreaterThan);
        UnityEngine.Debug.Log("All entries with a value > 222: " + ConvertIntArrayToString(dictOut[0]) + " | " + ConvertIntArrayToString(dictOut[1]));
        dictOut = DictFindAllWithValue(222, testKeys, testValues, (int)dict_compare_name.LessThanOrEqualsTo);
        UnityEngine.Debug.Log("All entries with a value <= 222: " + ConvertIntArrayToString(dictOut[0]) + " | " + ConvertIntArrayToString(dictOut[1]));
        dictOut = DictFindAllWithValue(222, testKeys, testValues, (int)dict_compare_name.GreaterThanOrEqualsTo);
        UnityEngine.Debug.Log("All entries with a value >= 222: " + ConvertIntArrayToString(dictOut[0]) + " | " + ConvertIntArrayToString(dictOut[1]));
    }*/

    public float[] GetStatsFromWeaponType(int weapon_type)
    {
        var weapon_stats = new float[(int)weapon_stats_name.ENUM_LENGTH];
        switch (weapon_type)
        {
            case (int)weapon_type_name.PunchingGlove:
                weapon_stats[(int)weapon_stats_name.Cooldown] = 1.1f;
                weapon_stats[(int)weapon_stats_name.Projectile_Distance] = 1.8f;
                weapon_stats[(int)weapon_stats_name.Projectile_Duration] = 0.01f;
                weapon_stats[(int)weapon_stats_name.Projectile_Type] = (int)projectile_type_name.Bullet;
                weapon_stats[(int)weapon_stats_name.Hurtbox_Damage] = 10.0f;
                weapon_stats[(int)weapon_stats_name.Hurtbox_Size] = 0.8f;
                weapon_stats[(int)weapon_stats_name.Hurtbox_Duration] = 0.7f;
                weapon_stats[(int)weapon_stats_name.Hurtbox_Damage_Type] = (int)damage_type_name.Strike;
                break;
            case (int)weapon_type_name.BossGlove:
                weapon_stats[(int)weapon_stats_name.Cooldown] = 0.6f;
                weapon_stats[(int)weapon_stats_name.Projectile_Distance] = 1.8f;
                weapon_stats[(int)weapon_stats_name.Projectile_Duration] = 0.01f;
                weapon_stats[(int)weapon_stats_name.Projectile_Type] = (int)projectile_type_name.Bullet;
                weapon_stats[(int)weapon_stats_name.Hurtbox_Damage] = 15.0f;
                weapon_stats[(int)weapon_stats_name.Hurtbox_Size] = 1.4f;
                weapon_stats[(int)weapon_stats_name.Hurtbox_Duration] = 0.7f;
                weapon_stats[(int)weapon_stats_name.Hurtbox_Damage_Type] = (int)damage_type_name.Kapow;
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
        // If we are clients, sync our team arrays according to the incoming string
        if (!Networking.LocalPlayer.isMaster)
        {
            // To-do: optimization; we do not want this running every Update(), but it seems to fail with OnDeserialization(), for some reason
            //if (ConvertIntArrayToString(ply_tracking_dict_values_arr) != ply_tracking_dict_values_str) 
            //{ 
            //    ply_tracking_dict_keys_arr = ConvertStrToIntArray(ply_tracking_dict_keys_str);
            //    ply_tracking_dict_values_arr = ConvertStrToIntArray(ply_tracking_dict_values_str);
            //    RoundRefreshUI();
            //}
        }

        // Local handling
        round_timer = (float)Networking.CalculateServerDeltaTime(Networking.GetServerTimeInSeconds(), round_start_ms);
        room_ready_txt.text = "Game State: " + round_state.ToString()
            + "\nPlayers: " + ply_tracking_dict_values_str
            + room_ready_status_text
            ;

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
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "EnableReadyRoom");
            RequestSerialization();
            RoundRefreshUI();
        }
        else if (round_state == (int)round_state_name.Ongoing && round_timer >= round_length)
        {
            round_start_ms = Networking.GetServerTimeInSeconds();
            round_state = (int)round_state_name.Over;
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "RoundEnd", "");

        }
        else if (round_state == (int)round_state_name.Over && round_timer >= ready_length)
        {
            round_start_ms = Networking.GetServerTimeInSeconds();
            round_state = (int)round_state_name.Start;
            RoundRefreshUI();
            RequestSerialization();
        }

        /*else if (round_state == (int)round_state_name.Ongoing && Networking.CalculateServerDeltaTime(Networking.GetServerTimeInSeconds(), goalCheckTime) >= goal_impulse) 
        { 
            CheckForRoundGoal();
            goalCheckTime = Networking.GetServerTimeInSeconds();
        }*/

        //if (VRCPlayerApi.GetPlayerCount() > ply_tracking_dict_keys_arr.Length) { ResetPlyDicts(); }
    }

    public override void OnPostSerialization(SerializationResult result)
    {
        UnityEngine.Debug.Log("Serialized data: " + result.success + " with " + result.byteCount + " bytes");
    }

    public override void OnDeserialization()
    {
        int[] keys_from_network = ConvertStrToIntArray(ply_tracking_dict_keys_str);
        int[] values_from_network = ConvertStrToIntArray(ply_tracking_dict_values_str);

        if ((ply_tracking_dict_keys_arr != keys_from_network || ply_tracking_dict_values_arr != values_from_network)
            //&& Networking.CalculateServerDeltaTime(Networking.GetServerTimeInSeconds(), networkJoinTime) < 10.0f
            )
             {
            ply_tracking_dict_keys_arr = keys_from_network;
            ply_tracking_dict_values_arr = values_from_network;
            UnityEngine.Debug.Log("CLIENT: Syncing new arrays: " + ply_tracking_dict_keys_str + " | " + ply_tracking_dict_values_str);
            //ui_round_team_panel.CreateNewPanelList();
            ui_round_team_panel.PanelListCleanup();
        }

        var localPlyAttr = FindPlayerAttributes(Networking.LocalPlayer);
        if (localPlyAttr != null)
        {
            localPlyAttr.ply_team = GetGlobalTeam(Networking.LocalPlayer.playerId);
        }

        // Once we know our team variables have been fully synchronized, we can start the round on our end
        if (wait_for_sync_for_round_start)
        {
            LocalRoundStart();
            wait_for_sync_for_round_start = false;
        }

        RoundRefreshUI();
    }

    public void RoundRefreshUI()
    {
        ui_round_teamplay_toggle.isOn = option_teamplay;
        ui_round_master_only_toggle.isOn = option_start_from_master_only;

        ui_round_team_panel.SetAllTeamCounters();
        ui_round_team_panel.RefreshAllPanels();

        // This variable never updates if you aren't the master (updated in RoundOptionAdjust()), so let's update it for clients here
        if (!Networking.LocalPlayer.isMaster) 
        { 
            option_team_limits_arr = ConvertStrToIntArray(option_team_limits_str);
        }

        if (ui_round_option_dropdown != null && local_gamemode_count < round_option_names.Length)
        {
            ui_round_option_dropdown.ClearOptions();
            ui_round_option_dropdown.AddOptions(round_option_names);
            local_gamemode_count = (byte)round_option_names.Length;
        }

        // Room ready must occur AFTER SetAllTeamCounters to function correctly
        bool enableRoundStartButton = true;
        room_ready_status_text = "";
        if (round_state != (int)round_state_name.Start) { enableRoundStartButton = false; room_ready_status_text = "\n (Cannon start new round; a game is already ongoing!)"; }
        else if (option_gamemode == (int)round_mode_name.BossBash || option_gamemode == (int)round_mode_name.Infection)
        {
            if (ply_tracking_dict_keys_arr.Length < 2 || option_team_limits_arr.Length < 2 || ui_round_team_panel.team_count_arr.Length < 2) { enableRoundStartButton = false; room_ready_status_text = "\n (Cannot start game; there are fewer than 2 players / teams!)"; }
            else if (ui_round_team_panel.team_count_arr[1] > option_team_limits_arr[1]) { enableRoundStartButton = false; room_ready_status_text = "\n (Cannot start game; there are too many players on the 2nd team!)"; }
            else if (ui_round_team_panel.team_count_arr[1] < option_team_limits_arr[1]) { enableRoundStartButton = false; room_ready_status_text = "\n (Cannot start game; there are not enough players on the 2nd team!)"; }
        }
        else if (!Networking.LocalPlayer.isMaster && option_start_from_master_only) { enableRoundStartButton = false; room_ready_status_text = "\n [Only the game master may start the round!]"; }

        ui_round_start_button.interactable = enableRoundStartButton;
        ui_round_length_input.text = Mathf.FloorToInt(round_length).ToString();

        bool enableResetButton = false;
        if ((!option_start_from_master_only || (option_start_from_master_only && Networking.LocalPlayer.isMaster)) && round_state == (int)round_state_name.Ongoing) { enableResetButton = true; }
        ui_round_reset_button.interactable = enableResetButton;

        if (Networking.LocalPlayer.isMaster)
        {
            ui_round_master_only_toggle.interactable = true;
            ui_round_teamplay_toggle.interactable = true;
            ui_round_option_dropdown.interactable = true;
            ui_round_length_input.interactable = true;
            ui_round_option_goal_input_a.interactable = true;
            ui_round_option_goal_input_b.interactable = true;
            ui_round_teamplay_sort_button.interactable = option_teamplay;
            ui_round_teamplay_count_input.interactable = option_teamplay && !option_force_teams;
        }
        else
        {
            ui_round_master_only_toggle.interactable = false;
            ui_round_teamplay_toggle.interactable = false;
            ui_round_option_dropdown.interactable = false;
            ui_round_length_input.interactable = false;
            ui_round_option_goal_input_a.interactable = false;
            ui_round_option_goal_input_b.interactable = false;
            ui_round_teamplay_sort_button.interactable = false;
            ui_round_teamplay_count_input.interactable = false;
        }

        if (VRCPlayerApi.GetPlayerById(ply_master_id) != null)
        {
            ui_round_master_text.text = "Game Master:\n" + VRCPlayerApi.GetPlayerById(ply_master_id).displayName;
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
        ui_round_option_description.text = round_option_descriptions[option_gamemode];

    }

    public void RoundOptionAdjust()
    {
        if (!Networking.LocalPlayer.isMaster) { return; }
        if (round_state == (int)round_state_name.Ongoing || round_state == (int)round_state_name.Ready) { return; }

        option_gamemode = (byte)ui_round_option_dropdown.value;
        option_force_teams = false;
        option_goal_time = false;

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
                option_goal_time = true;
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

        int team_input_parse = 1;
        int prev_team_count = team_count;
        Int32.TryParse(ui_round_teamplay_count_input.text, out team_input_parse);
        team_input_parse = Mathf.Min(Mathf.Max(team_input_parse, 1), team_colors.Length, 255);
        if (option_teamplay) { team_count = (byte)team_input_parse; }
        else { team_count = 1; }
        if (prev_team_count != team_count)
        {
            PlayerAutoSortTeams();
        }
     
        
        int try_round_length_parse = Mathf.FloorToInt(round_length);
        Int32.TryParse(ui_round_length_input.text, out try_round_length_parse);
        try_round_length_parse = Mathf.Min(Mathf.Max(try_round_length_parse, 60), 2147483647);
        UnityEngine.Debug.Log("TIME TO CHANGE THE ROUND LENGTH FROM " + round_length + " TO " + try_round_length_parse);
        round_length = (float)try_round_length_parse;

        RequestSerialization();
        RoundRefreshUI();
    }

    public override void OnPlayerJoined(VRCPlayerApi player)
    {
        //base.OnPlayerJoined(player);
        
        var plyWeaponObj = FindPlayerOwnedObject(player, "PlayerWeapon");
        var plyAttributesObj = FindPlayerOwnedObject(player, "PlayerAttributes");
        var plyAttributesComponent = plyAttributesObj.GetComponent<PlayerAttributes>();
        var plyHitboxObj = FindPlayerOwnedObject(player, "PlayerHitbox");
        var plyUIToOthers = FindPlayerOwnedObject(player, "UIPlyToOthers");
        var plyUIToSelf = FindPlayerOwnedObject(player, "UIPlyToSelf");
        Networking.SetOwner(player, plyWeaponObj);
        Networking.SetOwner(player, plyAttributesObj);
        Networking.SetOwner(player, plyHitboxObj);
        plyHitboxObj.GetComponent<PlayerHitbox>().owner = player;
        plyHitboxObj.GetComponent<PlayerHitbox>().playerAttributes = plyAttributesComponent;
        Networking.SetOwner(player, plyUIToOthers);
        plyUIToOthers.GetComponent<UIPlyToOthers>().owner = player;
        plyUIToOthers.GetComponent<UIPlyToOthers>().playerAttributes = plyAttributesComponent;
        Networking.SetOwner(player, plyUIToSelf);
        plyUIToSelf.GetComponent<UIPlyToSelf>().owner = player;
        plyUIToSelf.GetComponent<UIPlyToSelf>().playerAttributes = plyAttributesComponent;

        if (player == Networking.LocalPlayer) { 
            networkJoinTime = Networking.GetServerTimeInSeconds();
            local_uiplytoself = plyUIToSelf.GetComponent<UIPlyToSelf>();
        }

        plyAttributesComponent.SendCustomNetworkEvent(NetworkEventTarget.Owner, "ResetDefaultEyeHeight");

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


        if (!(player == Networking.LocalPlayer && Networking.LocalPlayer.isMaster) && DictIndexFromKey(player.playerId, ply_tracking_dict_keys_arr) < 0 ) 
        { DictAddEntry(player.playerId, (int)player_tracking_name.Unassigned, ref ply_tracking_dict_keys_arr, ref ply_tracking_dict_values_arr); }
        else
        {
            UnityEngine.Debug.Log("New player (" + player.playerId + ") just dropped! Let's add them to the dictionary!" + ply_tracking_dict_keys_str);
        }

        if (Networking.LocalPlayer.isMaster)
        {
            ply_tracking_dict_keys_str = ConvertIntArrayToString(ply_tracking_dict_keys_arr);
            ply_tracking_dict_values_str = ConvertIntArrayToString(ply_tracking_dict_values_arr);
            ui_round_team_panel.CreateNewPanel(ply_tracking_dict_keys_arr.Length - 1);
            ply_master_id = Networking.LocalPlayer.playerId;
            RequestSerialization();
        }
        else
        {
            // Setup placeholder arrays until we receive data from the new ones
            if (ply_tracking_dict_keys_arr == null) { ply_tracking_dict_keys_arr = new int[VRCPlayerApi.GetPlayerCount()]; }
            if (ply_tracking_dict_values_arr == null) { ply_tracking_dict_values_arr = new int[VRCPlayerApi.GetPlayerCount()]; }
        }
        UnityEngine.Debug.Log("New player (" + player.playerId + ") is now in the dictionary! " + ply_tracking_dict_keys_str);

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
            ui_round_team_panel.RemovePanel(DictIndexFromKey(player.playerId, ply_tracking_dict_keys_arr));
            DictRemoveEntry(player.playerId, ref ply_tracking_dict_keys_arr, ref ply_tracking_dict_values_arr);
            ply_tracking_dict_keys_str = ConvertIntArrayToString(ply_tracking_dict_keys_arr);
            ply_tracking_dict_values_str = ConvertIntArrayToString(ply_tracking_dict_values_arr);
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
    // This function gets called only AFTER a client's team arrays have been fully synced up.
    public void LocalRoundStart()
    {
        // To-do: add gamemode variables
        int[][] ply_parent_arr = GetPlayersInGame();
        UnityEngine.Debug.Log("[DICT_TEST]: ROUND START - " + ConvertIntArrayToString(ply_parent_arr[0]) + " | " + ConvertIntArrayToString(ply_parent_arr[1]));
        for (var i = 0; i < ply_parent_arr[0].Length; i++)
        {
            var player = VRCPlayerApi.GetPlayerById(ply_parent_arr[0][i]);
            if (player == null) { continue; }
            var plyWeaponObj = FindPlayerOwnedObject(player, "PlayerWeapon");
            var plyHitboxObj = FindPlayerOwnedObject(player, "PlayerHitbox");
            plyWeaponObj.SetActive(true);
            plyHitboxObj.SetActive(true);

            if (ply_parent_arr[1][i] == 1 && option_gamemode == (int)round_mode_name.BossBash) { gamemode_boss_id = player.playerId; }

            if (!player.isLocal) { continue; }

            // To-do: IsNetworkSettled / IsClogged check for this player data
            PlayerAttributes playerData = FindPlayerAttributes(player);
            TeleportLocalPlayerToGameSpawnZone(i % room_game_spawnzones.Length);
            playerData.ply_deaths = 0;
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
            playerData.ply_team = ply_parent_arr[1][i];
            if (option_gamemode == (int)round_mode_name.BossBash && playerData.ply_team == 1)
            {
                playerData.ply_lives = option_goal_value_b;
                playerData.ply_scale = plysettings_scale * 3.0f;
                playerData.plyEyeHeight_desired = playerData.plyEyeHeight_default * 3.0f;
                playerData.ply_atk = plysettings_atk * (ply_parent_arr[0].Length / 4.0f);
                playerData.ply_def = plysettings_def * (ply_parent_arr[0].Length / 4.0f); 
                playerData.plyEyeHeight_change = true;
                plyWeaponObj.GetComponent<PlayerWeapon>().weapon_type = (int)weapon_type_name.BossGlove;
            }
            else 
            { 
                playerData.ply_lives = plysettings_lives;
                // To-do: have mapscript or game option override this
                plyWeaponObj.GetComponent<PlayerWeapon>().weapon_type = (int)weapon_type_name.PunchingGlove;
            }
            plyWeaponObj.GetComponent<PlayerWeapon>().UpdateStatsFromWeaponType();
        }

        snd_ready_music_source.Stop();
        PlaySFXFromArray(snd_game_music_source, snd_game_music_clips);
        Networking.LocalPlayer.Immobilize(false);

        AddToLocalTextQueue(round_option_descriptions[option_gamemode]);
        AddToLocalTextQueue(round_option_names[option_gamemode]);

        /*UnityEngine.Debug.Log("Everyone's teams should be: " + ply_tracking_dict_values_str);
        for (int i = 0; i < ply_parent_arr[0].Length; i++) {
            var player = VRCPlayerApi.GetPlayerById(ply_parent_arr[0][i]);
            if (player == null) { continue; }
            PlayerAttributes playerData = FindPlayerAttributes(player);
            UnityEngine.Debug.Log(player.displayName + " (" + ply_parent_arr[0][i] + ")'s team in array: " + ply_parent_arr[1][i] + " vs in plydata: " + playerData.ply_team);
        }*/

        if (!Networking.LocalPlayer.isMaster) { return; }
        round_state = (int)round_state_name.Ready;
        round_start_ms = Networking.GetServerTimeInSeconds();
        //ply_in_game_arr = ValidatePlayerArray(ply_parent_arr[0]);
        //ply_in_game_str = ConvertIntArrayToString(ply_in_game_arr);

        RequestSerialization();
    }

    [NetworkCallable]
    public void NetworkRoundStart()
    {
        room_ready_script.gameObject.SetActive(false); // We need to make sure player arrays don't get messed up while transferring over to the match
        Networking.LocalPlayer.Immobilize(true);
        if (Networking.IsMaster) { LocalRoundStart(); }
        else { wait_for_sync_for_round_start = true; }
    }

    public void SendRoundStart()
    {

        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "NetworkRoundStart");
    }

    [NetworkCallable]
    public void EnableReadyRoom()
    {
        AddToLocalTextQueue("GO!");
        room_ready_script.gameObject.SetActive(true);
    }

    public int[] CheckAllTeamLives(out int total_players_alive, out string player_names_alive)
    {
        int[][] players_in_game_dict = GetPlayersInGame();
        var plyAlivePerTeam = new int[team_count];
        total_players_alive = 0;
        player_names_alive = "";
        for (int i = 0; i < players_in_game_dict[0].Length; i++)
        {
            var player = VRCPlayerApi.GetPlayerById(players_in_game_dict[0][i]);
            if (player == null) { continue; }
            var plyAttributes = FindPlayerAttributes(player);
            if (plyAttributes == null) { continue; }
            if (plyAttributes.ply_lives > 0)
            {
                if (players_in_game_dict[1][i] > team_count) { UnityEngine.Debug.LogError("Player is on team " + players_in_game_dict[1][i] + ", but the game only has " + team_count + " teams!"); continue; }
                if (players_in_game_dict[1][i] >= 0) { plyAlivePerTeam[players_in_game_dict[1][i]]++; UnityEngine.Debug.Log("Player " + players_in_game_dict[0][i] + " on Team " + players_in_game_dict[1][i] + " is alive with " + plyAttributes.ply_lives + " lives"); } 
                total_players_alive++;
                player_names_alive += player.displayName;
            }
        }
        return plyAlivePerTeam;
    }

    public int CheckSpecificTeamLives(int team_id)
    {
        if (team_id > team_count) { UnityEngine.Debug.LogError("Attempted to check for team lives when the team (" + team_id + ") exceeds team count (" + team_count + ")!"); return 0; }
        int[][] players_in_game_dict = GetPlayersInGame();
        var members_alive = 0;
        for (int i = 0; i < players_in_game_dict[0].Length; i++)
        {
            var player = VRCPlayerApi.GetPlayerById(players_in_game_dict[0][i]);
            if (player == null) { continue; }
            var plyAttributes = FindPlayerAttributes(player);
            if (plyAttributes == null) { continue; }
            if (plyAttributes.ply_lives > 0 && players_in_game_dict[1][i] == team_id) { members_alive++; }
        }
        return members_alive;
    }

    public int[] CheckAllTeamPoints()
    {
        int[][] players_in_game_dict = GetPlayersInGame();
        var plyPointsPerTeam = new int[team_count];
        for (int i = 0; i < players_in_game_dict[0].Length; i++)
        {
            var player = VRCPlayerApi.GetPlayerById(players_in_game_dict[0][i]);
            if (player == null) { continue; }
            var plyAttributes = FindPlayerAttributes(player);
            if (plyAttributes == null) { continue; }
            if (players_in_game_dict[1][i] > team_count) { UnityEngine.Debug.LogError("Player is on team " + players_in_game_dict[1][i] + ", but the game only has " + team_count + " teams!"); continue; }
            if (players_in_game_dict[1][i] >= 0) { plyPointsPerTeam[players_in_game_dict[1][i]] += plyAttributes.ply_points; } 
        }
        return plyPointsPerTeam;
    }

    public int CheckSpecificTeamPoints(int team_id)
    {
        if (team_id > team_count) { UnityEngine.Debug.LogError("Attempted to check for team points when the team (" + team_id + ") exceeds team count (" + team_count + ")!"); return 0; }
        int[][] players_in_game_dict = GetPlayersInGame();
        var total_points = 0;
        for (int i = 0; i < players_in_game_dict[0].Length; i++)
        {
            var player = VRCPlayerApi.GetPlayerById(players_in_game_dict[0][i]);
            if (player == null) { continue; }
            var plyAttributes = FindPlayerAttributes(player);
            if (plyAttributes == null) { continue; }
            if (players_in_game_dict[1][i] == team_id) { total_points += plyAttributes.ply_points; }
        }
        return total_points;
    }

    [NetworkCallable]
    public void CheckForRoundGoal()
    {
        if (!Networking.LocalPlayer.isMaster) { return; }
        if (round_state != (int)round_state_name.Ongoing) { return; }
        if (team_count <= 0) { UnityEngine.Debug.LogError("Team count is <= 0!"); return; } // This should never happen

        int[][] players_in_game_dict = GetPlayersInGame();
        UnityEngine.Debug.Log("[DICT_TEST]: ROUND GOAL CHECK - " + ConvertIntArrayToString(players_in_game_dict[0]) + " | " + ConvertIntArrayToString(players_in_game_dict[1])
            + "\n COMPARE TO TRUE ARRAY: " + ply_tracking_dict_keys_str + " | "  + ply_tracking_dict_values_str
            );
        bool teamplayOver = false;
        bool freeForAllOver = false;
        string winner_name = "";

        // Points Check
        // Right now, all gamemodes use option_goal_value_a for points, but this may need support for future modes
        if (option_goal_points_a && !option_goal_time)
        {
            var plyPointsPerTeam = new int[team_count];
            plyPointsPerTeam = CheckAllTeamPoints();
            for (int j = 0; j < team_count; j++)
            {
                if (plyPointsPerTeam[j] >= option_goal_value_a) { teamplayOver = true; winner_name = "Team " + j;  break; }
            }
            UnityEngine.Debug.Log("Player points check: " + ConvertIntArrayToString(plyPointsPerTeam));
        }

        // Team count check
        //if (option_goal_time)
        //{
        var plyActivePerTeam = new int[team_count];
        for (int i = 0; i < players_in_game_dict[0].Length; i++)
        {
            var player = VRCPlayerApi.GetPlayerById(players_in_game_dict[0][i]);
            if (player == null) { continue; }
            if (players_in_game_dict[1][i] > team_count) { UnityEngine.Debug.LogError("Player is on team " + players_in_game_dict[1][i] + ", but the game only has " + team_count + " teams!"); continue; }
            if (players_in_game_dict[1][i] >= 0) { plyActivePerTeam[players_in_game_dict[1][i]]++;  }
        }
        var activeTeams = 0;
        for (int j = 0; j < team_count; j++)
        {
            if (plyActivePerTeam[j] > 0) { activeTeams++; winner_name = "Team " + j; }
        }
        if ((activeTeams <= 1 && option_teamplay) || activeTeams == 0) { teamplayOver = true; }
        //}

        // Lives check
        if (!teamplayOver && (!option_goal_points_a || !option_goal_points_b)) { 
            var plyAlivePerTeam = new int[team_count];
            var teams_alive = 0;
            var total_players_alive = 0;
            var player_names_alive = "";
            plyAlivePerTeam = CheckAllTeamLives(out total_players_alive, out player_names_alive);
            for (int j = 0; j < team_count; j++)
            {
                if (plyAlivePerTeam[j] > 0) { teams_alive++; winner_name = "Team " + j; }
            }
            if (!option_teamplay) { winner_name = player_names_alive; }
            
            UnityEngine.Debug.Log("Player lives check: {" + ConvertIntArrayToString(plyAlivePerTeam) + "}; teams alive: " + teams_alive + "/" + team_count + "; players alive: " + total_players_alive );
            teamplayOver = option_teamplay &&
            (
                (teams_alive <= 1 && team_count > 1) ||
                (teams_alive <= 0 && team_count == 1) ||
                (team_count <= 0)
            );
            freeForAllOver = !option_teamplay &&
                (
                    (total_players_alive <= 1 && players_in_game_dict[0].Length > 1) ||
                    (total_players_alive <= 0 && players_in_game_dict[0].Length == 1) ||
                    (players_in_game_dict[0].Length == 0)
                );
        }
 
        if (teamplayOver || freeForAllOver)
        {
            //round_state = (int)round_state_name.Over;
            //round_start_ms = Networking.GetServerTimeInSeconds();
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "RoundEnd", winner_name);
        }
    }

    [NetworkCallable]
    public void RoundEnd(string winner_name)
    {
        int[][] players_in_game_dict = GetPlayersInGame();
        UnityEngine.Debug.Log("[DICT_TEST]: ROUND END - " + ConvertIntArrayToString(players_in_game_dict[0]) + " | " + ConvertIntArrayToString(players_in_game_dict[1]));
        for (int i = 0; i < players_in_game_dict[0].Length; i++)
        {
            var player = VRCPlayerApi.GetPlayerById(players_in_game_dict[0][i]);
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

        if (option_gamemode == (int)round_mode_name.Infection)
        {
            if (winner_name == "Team 1") { winner_name = "Infected"; }
            else if (winner_name == "Team 0" || winner_name.Length == 0) { winner_name = "Survivors"; }
        }
        else if (option_gamemode == (int)round_mode_name.BossBash)
        {
            if (winner_name == "Team 1") { winner_name = "The Big Boss"; }
            else if (winner_name == "Team 0" || winner_name.Length == 0) { winner_name = "The Lil' Fellas"; }
        }
        if (winner_name != null && winner_name.Length >0)
        {
            AddToLocalTextQueue(winner_name + "!");
            AddToLocalTextQueue("THE WINNER IS:");
        }


        snd_game_music_source.Stop();
        PlaySFXFromArray(snd_ready_music_source, snd_ready_music_clips);

        if (Networking.LocalPlayer.isMaster)
        {
            round_state = (int)round_state_name.Over;
            round_start_ms = Networking.GetServerTimeInSeconds();
            // To-do: Make a leaderboard so people can feel good after the game
            //ply_in_game_arr = new int[0];
            //ply_in_game_str = "";
            RequestSerialization();
        }
        RoundRefreshUI();
    }

    public void SendRoundEnd()
    {
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "RoundEnd", "");
    }

    public void TeleportLocalPlayerToGameSpawnZone(int spawnZoneIndex = -1)
    {
        // If no spawnzone is specified, just use a random one
        if (spawnZoneIndex == -1) { spawnZoneIndex = UnityEngine.Random.Range(0, room_game_spawnzones.Length - 1); }

        var spawnZoneBounds = room_game_spawnzones[spawnZoneIndex].bounds;
        var rx = UnityEngine.Random.Range(spawnZoneBounds.min.x, spawnZoneBounds.max.x);
        var rz = UnityEngine.Random.Range(spawnZoneBounds.min.z, spawnZoneBounds.max.z);

        Networking.LocalPlayer.TeleportTo(new Vector3(rx, spawnZoneBounds.center.y, rz), Networking.LocalPlayer.GetRotation());
        UnityEngine.Debug.Log("Teleporting player to spawn zone " + spawnZoneIndex);
    }

    public void TeleportLocalPlayerToReadyRoom()
    {
        Networking.LocalPlayer.SetVelocity(new Vector3(0.0f, 0.0f, 0.0f));
        Networking.LocalPlayer.TeleportTo(room_ready_spawn.transform.position, Networking.LocalPlayer.GetRotation());
    }

    // -- Weapon Handling --
    [NetworkCallable]
    public void NetworkCreateProjectile(int weapon_type, Vector3 fire_start_pos, Quaternion fire_angle, float distance, double fire_start_ms, bool keep_parent, float player_scale, int player_id)
    {
        var newProjectileObj = Instantiate(template_WeaponProjectile, transform);
        newProjectileObj.transform.parent = null;
        newProjectileObj.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f) * player_scale;
        newProjectileObj.transform.SetPositionAndRotation(fire_start_pos, fire_angle);

        var projectile = newProjectileObj.GetComponent<WeaponProjectile>();
        projectile.weapon_type = weapon_type;
        projectile.projectile_type = (int)GetStatsFromWeaponType(weapon_type)[(int)weapon_stats_name.Projectile_Type];
        projectile.projectile_duration = GetStatsFromWeaponType(weapon_type)[(int)weapon_stats_name.Projectile_Duration];
        projectile.projectile_start_ms = fire_start_ms;
        projectile.pos_start = fire_start_pos;
        projectile.projectile_distance = distance;
        projectile.owner_id = player_id;
        projectile.owner_scale = player_scale;
        projectile.template_WeaponHurtbox = template_WeaponHurtbox;
        projectile.gameController = this;
        projectile.keep_parent = keep_parent;

        if (keep_parent)
        {
            var player = VRCPlayerApi.GetPlayerById(player_id);
            if (player != null)
            {
                var weaponObj = FindPlayerOwnedObject(player, "PlayerWeapon");
                if (!(weaponObj == null || weaponObj.GetComponent<PlayerWeapon>() == null))
                {
                    UnityEngine.Debug.Log("Found script, parenting");
                    newProjectileObj.transform.parent = weaponObj.transform;
                    projectile.weapon_parent = weaponObj;
                    projectile.pos_start = weaponObj.transform.position;
                }
            }
        }

        newProjectileObj.SetActive(true);

    }

    [NetworkCallable]
    public void NetworkCreateHurtBox(Vector3 position, float damage, double start_ms, bool keep_parent, float player_scale, int player_id, int weapon_type)
    {
        var newHurtboxObj = Instantiate(template_WeaponHurtbox, transform);

        newHurtboxObj.transform.parent = null;
        var hurtbox = newHurtboxObj.GetComponent<WeaponHurtbox>();

        newHurtboxObj.transform.localScale = GetStatsFromWeaponType(weapon_type)[(int)weapon_stats_name.Hurtbox_Size] * player_scale * new Vector3(1.0f, 1.0f, 1.0f);
        newHurtboxObj.transform.position = position;

        if (keep_parent)
        {
            var player = VRCPlayerApi.GetPlayerById(player_id);
            if (player != null)
            {
                var weaponObj = FindPlayerOwnedObject(player, "PlayerWeapon");
                if (weaponObj != null && weaponObj.GetComponent<PlayerWeapon>() != null)
                {
                    newHurtboxObj.transform.SetParent(weaponObj.transform);
                    hurtbox.weapon_parent = weaponObj;
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

    public int[][] GetPlayersInGame()
    {
        if (ply_tracking_dict_keys_arr == null || ply_tracking_dict_keys_arr.Length <= 0 || ply_tracking_dict_values_arr == null || ply_tracking_dict_values_arr.Length <= 0 || ply_tracking_dict_keys_arr.Length != ply_tracking_dict_values_arr.Length) { UnityEngine.Debug.Log("Invalid dictionary!"); return null; }
        return DictFindAllWithValue(0, ply_tracking_dict_keys_arr, ply_tracking_dict_values_arr, (int)dict_compare_name.GreaterThanOrEqualsTo);
    }

    [NetworkCallable]
    public void ChangeTeam(int player_id, int new_team, bool from_ready_room_enter)
    {
        if (!Networking.LocalPlayer.isMaster) { return; }
        UnityEngine.Debug.Log("Change team request received :" + player_id + " -> " + new_team + " (" + from_ready_room_enter + ")");
        int change_index = DictIndexFromKey(player_id, ply_tracking_dict_keys_arr);
        if (change_index < 0) { UnityEngine.Debug.LogWarning("Couldn't find player to change teams for: " + player_id); return; }
        if (from_ready_room_enter && ply_tracking_dict_values_arr[change_index] >= 0) { return; } // Don't assign to 0 from ready room if they're already in-game
        ply_tracking_dict_values_arr[change_index] = new_team;
        ply_tracking_dict_values_str = ConvertIntArrayToString(ply_tracking_dict_values_arr);
        UnityEngine.Debug.Log("[DICT_TEST]: Changing player " + player_id + " to team " + new_team + " (" + ply_tracking_dict_keys_str + " | " + ply_tracking_dict_values_str + ")");
        RoundRefreshUI();
        RequestSerialization();

        if (round_state == (int)round_state_name.Ongoing) { CheckForRoundGoal(); }
    }

    // This is a last resort method used if we lose so much synchronization that we need to rebuild the player dictionaries entirely.
    public void ResetPlyDicts()
    {
        //if (!Networking.LocalPlayer.isMaster) { return; }
        var players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
        VRCPlayerApi.GetPlayers(players);
        ply_tracking_dict_keys_arr = new int[players.Length];
        ply_tracking_dict_values_arr = new int[players.Length];
        byte index_iter = 0;
        foreach (var player in players)
        {
            ply_tracking_dict_keys_arr[index_iter] = player.playerId;
            ply_tracking_dict_values_arr[index_iter] = FindPlayerAttributes(player).ply_team;
            index_iter++;
        }
        ply_tracking_dict_keys_str = ConvertIntArrayToString(ply_tracking_dict_keys_arr);
        ply_tracking_dict_values_str = ConvertIntArrayToString(ply_tracking_dict_values_arr);
        UnityEngine.Debug.Log("BUILT NEW PLAYER ARRAY: " + ply_tracking_dict_keys_str + " (" + ply_tracking_dict_values_str + ")");
        RequestSerialization();
    }

    [NetworkCallable]
    public void PlayerAutoSortTeams()
    {
        if (!Networking.LocalPlayer.isMaster) { return; }
        int[][] players_in_game_dict = GetPlayersInGame();
        UnityEngine.Debug.Log("[DICT_TEST]: TEAM SORTING - " + ConvertIntArrayToString(players_in_game_dict[0]) + " | " + ConvertIntArrayToString(players_in_game_dict[1]));
        int[] team_list_sorted = ply_tracking_dict_values_arr;
        int[] counts_per_team = new int[team_count];
        ushort iterate_index = 0;
        int[] shrinking_ply_array = ply_tracking_dict_keys_arr;

        // How this works: we create a shrinking array of players, shrinking_ply_array, that removes entries for each player we iterate through.
        // We roll for a random index from this shrinking array, giving us the player ID of a random player.
        // We check if this player is already assigned to one of the unassigned roles (i.e. spectator, not ready, etc.), if so, we make sure they stay that way and move on.
        // Otherwise, we can do two searches: softcap and hardcap.
        // Softcap searches are based on "don't overload a single team". We take the max count of all teams and set that as the limit for the # of players that can be on any team (in addition to the hardcap).
        // If we search through all the teams and find that they're all at the softcap, we then iterate a 2nd time, but only looking at the hardcap.
        // The hardcap is based on the an average team count: # players in game / # of teams.
        // If team limits are enforced (i.e. Boss Battle & Infection), this hardcap is set to those caps instead.
        // If we are still unable to find a team even with the hardcap, default the player to team 0 and move on. This is to prevent infinite loops.

        // Because our indices need to match the dictionary, we use the original arrays for processing.
        while (iterate_index < ply_tracking_dict_values_arr.Length)
        {
            // To-do: change ints to bytes or ushorts to save on memory
            int randIndex = UnityEngine.Random.Range((int)0, (int)shrinking_ply_array.Length);
            int randPly = shrinking_ply_array[randIndex];
            // If player is not registered to be in-game, skip them
            int team_dict_index = DictIndexFromKey(randPly, ply_tracking_dict_keys_arr);
            int prevTeam = ply_tracking_dict_values_arr[team_dict_index];

            if (prevTeam < 0) 
            {
                team_list_sorted[team_dict_index] = prevTeam;
                iterate_index++;
                shrinking_ply_array = RemoveIndexFromIntArray(randIndex, shrinking_ply_array);
                continue;
            }

            int team_size_softcap = Mathf.Max(1,Mathf.Max(counts_per_team));
            // However, we use players_in_game_dict[0] instead of ply_tracking_dict_keys_arr because we only want to cap based on ACTIVE players
            int team_size_hardcap = (int)Mathf.Max(1.0f, Mathf.Ceil((float)players_in_game_dict[0].Length / (float)team_count));

            bool found_team = false;
            for (int j = team_count - 1; j >= 0; j--)
            {
                // If we are enforcing team limits, only distribute up to the limit for each team. Otherwise, distribute evenly.
                if (option_force_teams) 
                { 
                    team_size_hardcap = option_team_limits_arr[j]; 
                    team_size_softcap = Mathf.Min(team_size_softcap, team_size_hardcap); 
                }

                if (counts_per_team[j] < team_size_softcap) {
                    team_list_sorted[team_dict_index] = j;
                    counts_per_team[j]++;
                    iterate_index++;
                    found_team = true;
                    shrinking_ply_array = RemoveIndexFromIntArray(randIndex, shrinking_ply_array);
                    break;
                }
            }
            if (!found_team) 
            {
                for (int j = team_count - 1; j >= 0; j--)
                {
                    if (option_force_teams) { team_size_hardcap = option_team_limits_arr[j]; }

                    if (counts_per_team[j] < team_size_hardcap)
                    {
                        team_list_sorted[team_dict_index] = j;
                        counts_per_team[j]++;
                        iterate_index++;
                        found_team = true;
                        shrinking_ply_array = RemoveIndexFromIntArray(randIndex, shrinking_ply_array);
                        break;
                    }
                }
                if (!found_team) // This looks silly, but that's because we're checking the variable a second time after potential reassignment
                {
                    UnityEngine.Debug.LogError("Tried to assign a team for " + randPly + ", but autosort failed! Assigning to Team 0!");
                    team_list_sorted[randIndex] = 0;
                    counts_per_team[0]++;
                    iterate_index++;
                }
            }
        }

        ply_tracking_dict_values_arr = team_list_sorted;
        ply_tracking_dict_values_str = ConvertIntArrayToString(team_list_sorted);
        //ui_round_team_panel.RequestSerialization();
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

    public int GetGlobalTeam(int player_id)
    {
        return DictValueFromKey(player_id, ply_tracking_dict_keys_arr, ply_tracking_dict_values_arr);
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

    public void AddToLocalTextQueue(string input)
    {
        if (local_uiplytoself != null) { local_uiplytoself.AddToTextQueue(input); }
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

    public int DictIndexFromKey(int key, int[] keys)
    {
        if (keys == null || keys.Length <= 0) { UnityEngine.Debug.Log("Invalid dictionary!"); return -999; }
        int key_index = -999;
        for (int i = 0; i < keys.Length; i++)
        {
            if (key == keys[i]) { key_index = i; break; }
        }
        return key_index;
    }

    public int DictValueFromKey(int key, int[] keys, int[] values)
    {
        if (keys == null || keys.Length <= 0 || values == null || values.Length <= 0 || keys.Length != values.Length) { UnityEngine.Debug.Log("Invalid dictionary!"); return -999; }
        int key_index = DictIndexFromKey(key, keys);
        if (key_index < 0) { return -999; }
        return values[key_index];
    }

    public void DictAddEntry(int key, int value, ref int[] keys, ref int[] values)
    {
        if (keys == null || keys.Length <= 0 || values == null || values.Length <= 0 || keys.Length != values.Length) { UnityEngine.Debug.Log("Invalid dictionary!"); return; }
        keys = AddToIntArray(key, keys);
        values = AddToIntArray(value, values);
    }

    public void DictRemoveEntry(int key, ref int[] keys, ref int[] values)
    {
        if (keys == null || keys.Length <= 0 || values == null || values.Length <= 0 || keys.Length != values.Length) { UnityEngine.Debug.Log("Invalid dictionary!"); return; }
        int key_index = DictIndexFromKey(key, keys);
        keys = RemoveIndexFromIntArray(key_index, keys);
        values = RemoveIndexFromIntArray(key_index, values);
    }

    public int[][] DictFindAllWithValue(int value, int[] keys, int[] values, int compare_op = 0)
    {
        if (keys == null || keys.Length <= 0 || values == null || values.Length <= 0 || keys.Length != values.Length) { UnityEngine.Debug.Log("Invalid dictionary!"); return null; }
        int out_arr_size = 0;
        for (int i = 0; i < values.Length; i++)
        {
            if ((compare_op == (int)dict_compare_name.Equals && values[i] == value) 
            || (compare_op == (int)dict_compare_name.GreaterThan && values[i] > value) 
            || (compare_op == (int)dict_compare_name.LessThan && values[i] < value) 
            || (compare_op == (int)dict_compare_name.GreaterThanOrEqualsTo && values[i] >= value) 
            || (compare_op == (int)dict_compare_name.LessThanOrEqualsTo && values[i] <= value))
            {
                out_arr_size++;
            }
        }
        int[] keys_out = new int[out_arr_size];
        int[] values_out = new int[out_arr_size];
        int index_iter = 0;
        for (int j = 0; j < values.Length; j++)
        {
            if ((compare_op == (int)dict_compare_name.Equals && values[j] == value)
            || (compare_op == (int)dict_compare_name.GreaterThan && values[j] > value)
            || (compare_op == (int)dict_compare_name.LessThan && values[j] < value)
            || (compare_op == (int)dict_compare_name.GreaterThanOrEqualsTo && values[j] >= value)
            || (compare_op == (int)dict_compare_name.LessThanOrEqualsTo && values[j] <= value))
            {
                keys_out[index_iter] = keys[j];
                values_out[index_iter] = values[j];
                index_iter++;
            }
        }
        int[][] dict_out = new int[2][];
        dict_out[0] = keys_out; dict_out[1] = values_out;
        //UnityEngine.Debug.Log("Dictionary output: " + ConvertIntArrayToString(keys_out) + " | " + ConvertIntArrayToString(values_out));
        return dict_out;
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
        else if (cleanStr == "partialheal") { output = (int)powerup_type_name.PartialHeal; }
        else if (cleanStr == "fullheal") { output = (int)powerup_type_name.FullHeal; }
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
        else if (cleanStr == "bossglove") { output = (int)weapon_type_name.BossGlove; }
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

