
using Newtonsoft.Json.Linq;
using Superbstingray;
using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.UIElements;
using UnityEngine.Windows;
using VRC.SDK3.Components;
using VRC.SDK3.Persistence;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;
using VRC.Udon.Serialization.OdinSerializer.Utilities;

// A note on ENUM_LENGTH: this must ALWAYS BE LAST since we are using it as a shorthand for the length of an enumerator! (The typical method of typeof() is not supported in U#)
public enum game_sfx_name
{
    Death, Kill, HitSend, HitReceive, Announcement, ENUM_LENGTH
}
public enum ready_sfx_name
{
    QueueStart, TimerTick, QueueCancel, LoadStart, JoinGame, JoinMidGame, LeaveGame, WarningPressNext, WarningPressDone, ENUM_LENGTH
}
public enum announcement_sfx_name
{
    KOTH_Capture_Team, KOTH_Capture_Other, KOTH_Unlock, KOTH_Contest_Progress, KOTH_Contest_Start_Team, KOTH_Contest_Start_Other, KOTH_Victory_Near, HurryUp, ENUM_LENGTH 
}
public enum infection_music_name
{
    Start, ZombigSpawn, ZombigSuper, FinalSeconds, LastSurvivor, ENUM_LENGTH
}
public enum round_state_name
{
    Start, Ready, Ongoing, Over, Queued, Loading, ENUM_LENGTH
}

public enum player_tracking_name
{
    Unassigned = -3, WaitingForLobby = -2, Spectator = -1, ENUM_LENGTH
}

public enum weapon_stats_name
{
    Cooldown, Projectile_Distance, Projectile_Duration, Projectile_Type, Hurtbox_Damage, Hurtbox_Size, Hurtbox_Duration, Hurtbox_Damage_Type, Projectile_Size, ChargeTime, IsMelee, ENUM_LENGTH
}
public enum gamemode_name
{
    Survival, Clash, BossBash, Infection, FittingIn, KingOfTheHill, ENUM_LENGTH
}
public enum dict_compare_name
{
    Equals, GreaterThan, LessThan, GreaterThanOrEqualsTo, LessThanOrEqualsTo, ENUM_LENGTH
}
public enum prealloc_obj_name
{
    WeaponProjectile, WeaponHurtbox, ItemPowerup, UIHarmNumber, ENUM_LENGTH
}
public enum GLOBAL_CONST
{
    UDON_MAX_PLAYERS=100, PROJECTILE_LIMIT_PER_PLAYER=64, POWERUP_LIMIT_PER_PLAYER=24, PREALLOC_BATCH_SIZE=128, TICK_RATE_MS=25
        , RENDER_DISTANCE_FAR_QUEST = 165, RENDER_DISTANCE_NEAR_QUEST = 95, RENDER_DISTANCE_FAR_PC = 205, RENDER_DISTANCE_NEAR_PC = 135
        , INFECTION_HEAL_AMOUNT = 10
}

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class GameController : GlobalHelperFunctions
{
    // Note to self: Keep an eye out for getplayerbyids returning null from disconnecting players, especially when getting .displayname
    [Header("References")]
    [SerializeField] public Localizer localizer;
    [SerializeField] public GameObject template_ItemSpawner; // Note: this will be overriden by the player-owned instance of the object, but we need to have the template available for early reference
    [SerializeField] public Sprite Sprite_None;
    [SerializeField] public Material skybox;
    [SerializeField] public Texture default_skybox_tex;
    [SerializeField] public UdonPlatformHook platformHook;
    [SerializeField] public GameObject flag_for_mobile_vr;
    //[SerializeField] public PlayerWeapon boss_weapon; // the boss's secondary weapon
    [SerializeField] public Megaphone megaphone;
    [SerializeField] public LocalMotionSicknessHelper localMotionSicknessHelper;
    [SerializeField] public GameObject audiolink_obj;
    [SerializeField] public ClockDisplay[] clock_displays;
    [SerializeField] public GameObject victory_camera_plane;

    [SerializeField] public Camera[] highlightCameras;
    [SerializeField] public GameObject ui_spectatorcanvas;
    [SerializeField] public GameObject ui_highlightcanvas;
    //[NonSerialized] public sbyte highlight_camera_active = -1; // If -1, none; otherwise, camera at that index
    [NonSerialized] public bool[] highlight_cameras_waiting_on_sync;
    [NonSerialized] public bool[] highlight_cameras_active;
    [NonSerialized] public int highlight_cameras_active_cnt;
    [NonSerialized] public bool[] highlight_cameras_snapped;
    [NonSerialized] public double[] highlight_cameras_ms;
    [NonSerialized] public bool highlight_cameras_resetting = false;

    [SerializeField] public UnityEngine.UI.Button ui_round_start_button;
    [SerializeField] public UnityEngine.UI.Button ui_round_reset_button;
    [SerializeField] public UnityEngine.UI.Toggle ui_round_master_only_toggle;
    [SerializeField] public TextMeshProUGUI ui_round_master_text;
    [SerializeField] public UIRoundTeamPanel ui_round_team_panel;
    [SerializeField] public UnityEngine.UI.Toggle ui_round_teamplay_toggle;
    [SerializeField] public TMP_InputField ui_round_teamplay_count_input;
    [SerializeField] public UnityEngine.UI.Button ui_round_teamplay_sort_button;
    [SerializeField] public UnityEngine.UI.Toggle ui_round_teamplay_personal_toggle;
    [SerializeField] public UnityEngine.UI.Toggle ui_round_length_toggle;
    [SerializeField] public TMP_InputField ui_round_length_input;
    [SerializeField] public Transform ui_round_scoreboard_canvas;
    [SerializeField] public MapSelectPanel ui_round_mapselect;

    [SerializeField] public UnityEngine.UI.Button ui_round_option_default_button;
    [SerializeField] public TMP_Text ui_round_option_description;
    [SerializeField] public TMP_Dropdown ui_round_option_dropdown;
    [SerializeField] public UnityEngine.UI.Image ui_round_option_image;
    //[SerializeField] public TMP_Text ui_round_map_description;
    //[SerializeField] public TMP_Dropdown ui_round_map_dropdown;
    [SerializeField] public TextMeshProUGUI ui_round_option_goal_text_a;
    [SerializeField] public TextMeshProUGUI ui_round_option_goal_text_b;
    [SerializeField] public TMP_InputField ui_round_option_goal_input_a;
    [SerializeField] public TMP_InputField ui_round_option_goal_input_b;

    [SerializeField] public TMP_InputField ui_ply_option_dp;
    [SerializeField] public TMP_InputField ui_ply_option_scale;
    [SerializeField] public TMP_InputField ui_ply_option_atk;
    [SerializeField] public TMP_InputField ui_ply_option_def;
    [SerializeField] public TMP_InputField ui_ply_option_speed;
    [SerializeField] public TMP_InputField ui_ply_option_grav;
    [SerializeField] public TMP_Dropdown ui_ply_option_weapon_dropdown;
    [SerializeField] public UnityEngine.UI.Toggle ui_ply_option_weapon_toggle;
    [SerializeField] public UnityEngine.UI.Toggle ui_ply_option_powerup_toggle;

    [SerializeField] public TMP_InputField ui_adv_option_respawn_duration;
    [SerializeField] public TMP_InputField ui_adv_option_item_frequency;
    [SerializeField] public TMP_InputField ui_adv_option_item_duration;
    [SerializeField] public TMP_InputField ui_adv_option_boss_atk_mod;
    [SerializeField] public TMP_InputField ui_adv_option_boss_def_mod;
    [SerializeField] public TMP_InputField ui_adv_option_boss_speed_mod;
    [SerializeField] public TMP_InputField ui_adv_option_boss_scale_mod;
    [SerializeField] public UnityEngine.UI.Toggle ui_adv_option_item_debuff_toggle;

    [SerializeField] public UnityEngine.UI.Button ui_ppp_reset_pos_button;

    [SerializeField] public Room_Ready room_ready_script;
    [SerializeField] public GameObject room_ready_spawn;
    [SerializeField] public GameObject room_training;
    [SerializeField] public GameObject room_training_portal;
    [SerializeField] public GameObject room_training_hallway_spawn;
    [SerializeField] public GameObject room_training_arena_spawn;
    [SerializeField] public GameObject room_spectator_portal;
    [NonSerialized] public bool room_ready_should_render = true;

    [SerializeField] public TMP_Text[] ui_tutorial_gamemodes_txt;

    [SerializeField] public Mapscript[] mapscript_list;
    [NonSerialized][UdonSynced] public sbyte map_selected = -1;
    [NonSerialized][UdonSynced] public string maps_active_str = "";
    [NonSerialized] public sbyte map_selected_local = -1;
    [NonSerialized] public string maps_active_local = "";
    [NonSerialized] public bool restrict_map_change = false;
    [SerializeField] public TextMeshProUGUI room_ready_txt;

    [SerializeField] public AudioSource snd_game_music_source;
    //[SerializeField] public AudioClip[] snd_game_music_clips;
    //[SerializeField] public AudioSource snd_ready_music_source;
    [SerializeField] public AudioClip[] snd_ready_music_clips;
    [Tooltip("Needs to be in the exact order of game_sfx_name")]
    [SerializeField] public AudioSource[] snd_game_sfx_sources;

    [NonSerialized] public AudioClip[][] snd_game_sfx_clips; // Inspector doesn't like 2D arrays 
    [SerializeField] public AudioClip[] snd_game_sfx_clips_death;
    [SerializeField] public AudioClip[] snd_game_sfx_clips_kill;
    [SerializeField] public AudioClip[] snd_game_sfx_clips_hitsend; // NOTE: Corresponds to damage_type
    [SerializeField] public AudioClip[] snd_game_sfx_clips_hitreceive; // NOTE: Corresponds to damage_type
    [SerializeField] public AudioClip[] snd_game_sfx_clips_announcement; // NOTE: Corresponds to announcement_sfx_name

    [SerializeField] public AudioSource snd_voiceover_sfx_source;
    [SerializeField] public VoiceoverPack[] voiceover_packs;
    [NonSerialized] public VoiceoverPack vopack_selected;
    [NonSerialized] public bool voiceover_interruptable = true;
    [NonSerialized] public float voiceover_countdown = 0.0f;
    [NonSerialized] public float voiceover_volume_default = 1.0f;

    [SerializeField] public AudioSource snd_ready_sfx_source;
    [SerializeField] public AudioClip[] snd_ready_sfx_clips;

    [SerializeField] public AudioClip[] snd_victory_music_clips;
    [SerializeField] public AudioClip[] snd_defeat_music_clips;
    [SerializeField] public AudioClip[] snd_boss_music_clips;
    [SerializeField] public AudioClip[] snd_infection_music_clips;

    [SerializeField] public AudioClip[] snd_override_music_clips; // List of tracks that can override the current music playing
    [SerializeField] public string[] snd_override_music_names; // And their list of names (MUST MATCH IN LENGTH)

    [NonSerialized] public AudioClip music_clip_desired;
    [NonSerialized] public AudioClip music_clip_playing;
    [NonSerialized] public float music_clip_ts = 0.0f;
    [NonSerialized] public float music_volume_default = 1.0f;

    //[SerializeField] public Transform item_spawns_parent;
    //[NonSerialized] public ItemSpawner[] item_spawns;
    //[SerializeField] public Transform map_bouncepads_parent;
    //[NonSerialized] public BouncePad[] map_bouncepads;

    [NonSerialized][UdonSynced] public byte round_state = 0;
    [Header("Game Settings")]
    [Tooltip("How long a round should last, in seconds")]
    [SerializeField][UdonSynced] public float round_length = 300.0f;
    [Tooltip("Should a round have a time limit at all?")]
    [SerializeField][UdonSynced] public bool round_length_enabled = true;
    [Tooltip("How long players are on the 'Ready...' screen before a round begins. This time is needed so they can pick up their weapon.")]
    [SerializeField][UdonSynced] public float ready_length = 5.0f;
    [Tooltip("How long a round should be in 'Game Over' state before someone can start a new round.")]
    [SerializeField][UdonSynced] public float queue_length = 4.0f;
    [Tooltip("How long does a game master have to stop the game from ongoing?")]
    [SerializeField][UdonSynced] public float load_length = 4.0f;
    [Tooltip("How long should players be given to load in the map?")]
    [SerializeField][UdonSynced] public float over_length = 10.0f;
    [Tooltip("How often should the goal be checked during a round?")]
    [SerializeField][UdonSynced] public float goal_impulse = 5.0f;
    [NonSerialized][UdonSynced] public double round_start_ms = 0.0f;

    [Header("Player Settings")]
    [Tooltip("Starting Damage %")]
    [UdonSynced] public float plysettings_dp = 0.0f;
    [Tooltip("Respawn Invulnerability Time, in seconds")]
    [UdonSynced] public float plysettings_respawn_duration = 7.0f;
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
    [Tooltip("Starting Weapon")]
    [UdonSynced] public int plysettings_weapon = (int)weapon_type_name.PunchingGlove;
    [Tooltip("Spawn Weapon Pickups")]
    [UdonSynced] public bool plysettings_iweapons = true;
    [Tooltip("Spawn Powerup Pickups")]
    [UdonSynced] public bool plysettings_powerups = true;
    [Tooltip("Item Spawn Frequency, as a multiplier (default 1.0x)")]
    [UdonSynced] public float plysettings_item_frequency = 1.0f;
    [Tooltip("Item Spawn Duration, as a multiplier (default: 1.0x)")]
    [UdonSynced] public float plysettings_item_duration = 1.0f;
    [Tooltip("Should the throwable item only spawn debuffs?")]
    [UdonSynced] public bool plysettings_item_debuff = true;

    //[Tooltip("Starting Scale damage factor, which determines how much player size affects abilities, as a multiplier (default: 1.0x)")]
    //[UdonSynced] public float scale_damage_factor = 2.0f;

    [Tooltip("(Boss Bash) How big should The Big Boss be?")]
    [UdonSynced] public float plysettings_boss_scale_mod = 3.5f;
    [Tooltip("(Boss Bash) How much should The Big Boss's attack be modified, after scale factor is applied?")]
    [UdonSynced] public float plysettings_boss_atk_mod = 0.0f;
    [Tooltip("(Boss Bash) How much should The Big Boss's defense be modified, after scale factor is applied?")]
    [UdonSynced] public float plysettings_boss_def_mod = 1.5f;
    [Tooltip("(Boss Bash) How much should The Big Boss's speed be modified, after scale factor is applied?")]
    [UdonSynced] public float plysettings_boss_speed_mod = 0.0f;

    [Header("Voice Settings")]
    [Tooltip("Range 0 - 1,000,000. The near radius, in meters, where volume begins to fall off. (VRC Default: 0; Game Default: 25)")]
    [SerializeField] public int voice_distance_near = 0; // default 0
    [Tooltip("Range 0 - 1,000,000. This sets the end of the range for hearing the user's voice, in meters. Default is 25 meters. You can lower this to make another player's voice not travel as far, all the way to 0 to effectively 'mute' the player. (VRC Default: 25; Game Default: 250)")]
    [SerializeField] public int voice_distance_far = 25; // default 25
    //[Tooltip("Range 0-24. Add boost to the Player's voice in decibels. (VRC Default: 15; Game Default: 18)")]
    //[SerializeField] public int voice_gain = 15; //default 15 //20

    [NonSerialized] public float round_timer = 0.0f;
    [NonSerialized] public float local_every_second_timer = 0.0f;
    [NonSerialized] public float local_tick_timer = 0.0f;
    [NonSerialized] public float local_queue_timer = 0.0f;
    //[NonSerialized] public float (int)GLOBAL_CONST.TICK_RATE_MS = (int)GLOBAL_CONST.TICK_RATE_MS;

    [NonSerialized][UdonSynced] public float largest_ply_scale = 1.0f;
    [NonSerialized][UdonSynced] public bool megaphone_active = false;
    [NonSerialized] public bool local_megaphone_active = false;
    [NonSerialized] public bool local_announced_last_member = false;

    [NonSerialized] public bool debug_run_update = true;

    //[NonSerialized] public bool f;

    //[NonSerialized] [UdonSynced] public string ply_ready_str = "";
    //[NonSerialized] public int[] ply_ready_arr;
    //[NonSerialized] [UdonSynced] public string ply_in_game_str = "";
    //[NonSerialized] public int[] ply_in_game_arr;

    [UdonSynced] public bool option_teamplay = false;
    [UdonSynced] public byte team_count = 1;
    [UdonSynced] public bool option_personal_teams = true;
    [SerializeField] public Color32[] team_colors; // Active colors. In inspector, consider this base
    [SerializeField] public Color32[] team_colors_protanopia; // Can't distinguish reds
    [SerializeField] public Color32[] team_colors_deuteranopia; // Can't distinguish greens
    [SerializeField] public Color32[] team_colors_tritanopia; // Can't distinguish blues
    [SerializeField] public Color32[] team_colors_achromatopsia; // Can't distinguish hues
    [NonSerialized] public Color32[] team_colors_bright; // Active text colors
    [NonSerialized] public Color32[] team_colors_base;
    [NonSerialized] public Color32[][] team_colors_all_options_arr;

    [SerializeField] public string[] team_names; // MUST MATCH SIZE OF team_colors
    [SerializeField] public Sprite[] team_sprites; // MUST MATCH SIZE OF team_colors

    //[NonSerialized] [UdonSynced] public string teams_assigned_synced_str = "";
    //[NonSerialized] public int[] teams_assigned_synced_arr;

    //[NonSerialized] public int[][] ply_teams_arr;

    // Player tracking
    [NonSerialized] public int[] ply_tracking_dict_keys_arr;
    [NonSerialized][UdonSynced] public string ply_tracking_dict_keys_str = "";
    [NonSerialized] public int[] ply_tracking_dict_values_arr;
    [NonSerialized][UdonSynced] public string ply_tracking_dict_values_str = "";

    // Player owned objects
    [NonSerialized] public int[] ply_object_owners;
    [NonSerialized] public int ply_owners_cnt = 0;
    [NonSerialized] public PlayerAttributes[] ply_object_plyattr;
    [NonSerialized] public PlayerHitbox[] ply_object_plyhitbox;
    [NonSerialized] public PlayerWeapon[] ply_object_plyweapon;
    [NonSerialized] public PlayerWeapon[] ply_object_secondaryweapon;
    [NonSerialized] public UIPlyToOthers[] ply_object_uiplytoothers;

    [SerializeField] public int[][] cached_ply_in_game_dict;
    [SerializeField] public int[] cached_leaderboard_arr;
    [SerializeField] public int[] cached_progress_arr;

    [SerializeField] public string[] round_option_names; // NOTE: Corresponds to gamemode_name
    [SerializeField] public string[] round_option_descriptions; // NOTE: Corresponds to gamemode_name
    [SerializeField] public Sprite[] round_option_images; // NOTE: Corresponds to gamemode_name
    [NonSerialized][UdonSynced] public byte option_gamemode = 0;
    [NonSerialized] public byte local_gamemode_count = 0; // For some reason, Udon doesn't support TMP_Dropown.options, so we have to track this manually
    [NonSerialized] public byte local_weapon_count = 0; // For some reason, Udon doesn't support TMP_Dropown.options, so we have to track this manually
    [NonSerialized][UdonSynced] public ushort option_gm_goal; // The goal for each gamemode, such as points to win, boss KOs, or time to capture
    [NonSerialized][UdonSynced] public ushort option_gm_config_a; // A variable for additional configuration options for a gamemode, such as starting infected or boss lives
    //[NonSerialized] [UdonSynced] public ushort option_gm_config_b; // A variable for additional configuration options for a gamemode, such as starting infected or boss lives
    //[NonSerialized] [UdonSynced] public bool option_goal_time = false; // Should we not factor in points for the goal, but instead just wait out the timer?
    //[NonSerialized] [UdonSynced] public bool option_goal_points_a = false; // If true, points are used instead of lives
    //[NonSerialized] [UdonSynced] public ushort option_goal_value_a = 10;
    //[NonSerialized] [UdonSynced] public bool option_goal_points_b = false; // If true, points are used instead of lives
    //[NonSerialized] [UdonSynced] public ushort option_goal_value_b = 3;
    [NonSerialized][UdonSynced] public bool option_force_teamplay = false; // Should teamplay be forced on?
    [NonSerialized][UdonSynced] public bool option_enforce_team_limits = false; // Should team limits be enforced? If so...
    [NonSerialized][UdonSynced] public string option_team_limits_str = "0,1"; // What is the max # of players per team? Size must match team_count
    [NonSerialized] public int[] option_team_limits_arr;
    [NonSerialized][UdonSynced] public bool option_start_from_master_only = true;

    [NonSerialized][UdonSynced] public int ply_master_id = 0;
    [NonSerialized] public bool master_in_transit = false;

    [NonSerialized] private string room_ready_status_text = "";

    [NonSerialized] private bool wait_for_sync_for_round_start = false;
    [NonSerialized] private bool wait_for_sync_for_player_join = false;

    [NonSerialized][UdonSynced] public int gamemode_boss_id = 0; // the boss in big boss's ID

    //[NonSerialized] public int koth_decimal_division = 1000;
    [NonSerialized] public int[][] koth_progress_dict;
    [NonSerialized][UdonSynced] public double koth_respawn_wave_start_ms;
    [NonSerialized] public double koth_respawn_wave_duration;
    [NonSerialized][UdonSynced] public byte infection_zombigs_spawned = 0;
    [NonSerialized][UdonSynced] public byte infection_zombig_active = 0; // 0 = no, 1 = yes, 2 = super
    [NonSerialized][UdonSynced] public double infection_zombig_spawn_time = 0.0d;
    [NonSerialized][UdonSynced] public int round_extra_data = 0; // Extra tracking data for a gamemode, such as number of Survivor deaths in Infected

    [NonSerialized] public bool ui_initialized = false;
    [NonSerialized] public bool ui_updating = false;

    // Personal Player Variables
    [NonSerialized] public PPP_Options local_ppp_options;
    [NonSerialized] public UIPlyToSelf local_uiplytoself;
    [NonSerialized] public PlayerAttributes local_plyAttr;
    [NonSerialized] public PlayerWeapon local_plyweapon;
    [NonSerialized] public PlayerWeapon local_secondaryweapon;
    [NonSerialized] public PlayerHitbox local_plyhitbox;

    // Preallocated prefab objects used in place of create-destroy events
    //[NonSerialized] public int generate_prealloc_stage = 0;
    [NonSerialized] public WeaponProjectile[] global_projectile_arr;
    [NonSerialized] public int[] global_projectile_refs;
    [NonSerialized] public int global_projectile_cnt = 0;
    [NonSerialized] public int global_lowest_available_projectile_index = 0;
    //[NonSerialized] public WeaponHurtbox[] global_hurtbox_arr;
    //[NonSerialized] public int[] global_hurtbox_refs;
    //[NonSerialized] public int global_hurtbox_cnt = 0;
    //[NonSerialized] public int global_lowest_available_hurtbox_index = 0;
    [NonSerialized] public ItemPowerup[] global_powerup_arr;
    [NonSerialized] public int[] global_powerup_refs;
    [NonSerialized] public int global_powerup_cnt = 0;
    [NonSerialized] public int global_lowest_available_powerup_index = 0;
    [NonSerialized] public UIHarmNumber[] global_harmnumber_arr;
    [NonSerialized] public int[] global_harmnumber_refs;
    [NonSerialized] public int global_harmnumber_cnt = 0;
    [NonSerialized] public int global_lowest_available_harmnumber_index = 0;
    [NonSerialized] public Color32 COLOR_CHARGE = new Color32(70, 197, 101, 255); 
    [NonSerialized] public Color32 COLOR_COOLDOWN = new Color32(170, 65, 65, 255);  
    [NonSerialized] public Color32 COLOR_AIRTHRUST = new Color32(196, 227, 255, 255);

    // -- Initialization --
    private void Start()
    {
        debug_run_update = true;
        ui_initialized = false;
        skybox.mainTexture = default_skybox_tex;

        victory_camera_plane.SetActive(false);

        // Initialize team colors
        // We want to store the base colors from the inspector. Due to legacy issues, we have to do in this strnage order (colors -> base -> colors)
        team_colors_base = new Color32[team_colors.Length];
        for (int j = 0; j < team_colors.Length; j++)
        {
            team_colors_base[j] = team_colors[j];
        }
        team_colors_all_options_arr = new Color32[5][];
        team_colors_all_options_arr[0] = team_colors_base;
        team_colors_all_options_arr[1] = team_colors_protanopia;
        team_colors_all_options_arr[2] = team_colors_deuteranopia;
        team_colors_all_options_arr[3] = team_colors_tritanopia;
        team_colors_all_options_arr[4] = team_colors_achromatopsia;

        SetColorOptions(0);

        //DebugTestDictFunctions();
        //item_spawns = new ItemSpawner[0];
        //ply_ready_arr = new int[0];
        //ply_in_game_arr = new int[0];
        option_team_limits_arr = new int[0];
        //teams_assigned_synced_arr = new int[0];
        //ply_teams_arr = new int[0][];
        ply_tracking_dict_keys_arr = new int[0];
        ply_tracking_dict_values_arr = new int[0];
        koth_progress_dict = new int[2][];
        highlight_cameras_waiting_on_sync = new bool[highlightCameras.Length];
        highlight_cameras_active = new bool[highlightCameras.Length];
        highlight_cameras_snapped = new bool[highlightCameras.Length];
        highlight_cameras_ms = new double[highlightCameras.Length];

        ply_object_owners = new int[(int)GLOBAL_CONST.UDON_MAX_PLAYERS];
        ply_object_plyattr = new PlayerAttributes[(int)GLOBAL_CONST.UDON_MAX_PLAYERS];
        ply_object_plyhitbox = new PlayerHitbox[(int)GLOBAL_CONST.UDON_MAX_PLAYERS];
        ply_object_plyweapon = new PlayerWeapon[(int)GLOBAL_CONST.UDON_MAX_PLAYERS];
        ply_object_secondaryweapon = new PlayerWeapon[(int)GLOBAL_CONST.UDON_MAX_PLAYERS];
        ply_object_uiplytoothers = new UIPlyToOthers[(int)GLOBAL_CONST.UDON_MAX_PLAYERS];

        ply_master_id = Networking.LocalPlayer.playerId; // To-do: should this be here?

        //(int)GLOBAL_CONST.TICK_RATE_MS = (int)GLOBAL_CONST.TICK_RATE_MS;

        AdjustVoiceRange();

        for (int i = 0; i < mapscript_list.Length; i++)
        {
            mapscript_list[i].gameObject.SetActive(false);
            mapscript_list[i].gameObject.SetActive(false);
        }
        //room_training.SetActive(false);
        room_training_portal.SetActive(false);

        if (Networking.IsOwner(gameObject))
        {
            ResetPlyDicts();
            ResetGameOptionsToDefault(false);
            if (mapscript_list != null && mapscript_list.Length > 0)
            {
                foreach (ItemSpawner itemSpawner in mapscript_list[0].GetItemSpawnerFromParent(room_training.transform))
                {
                    if (itemSpawner == null || itemSpawner.gameObject == null) { continue; }
                    itemSpawner.item_spawn_powerups_enabled = true;
                    itemSpawner.item_spawn_weapons_enabled = true;
                    itemSpawner.item_spawn_frequency_mul = 1.0f;
                    itemSpawner.item_spawn_duration_mul = 1.0f;
                    itemSpawner.SetSpawnChances();
                    if (itemSpawner.item_spawn_state == (int)item_spawn_state_name.Disabled) { itemSpawner.item_spawn_state = (int)item_spawn_state_name.Spawnable; }
                    else { itemSpawner.SyncSpawns(); }
                }
            }
        }

        global_projectile_arr = new WeaponProjectile[(int)GLOBAL_CONST.PROJECTILE_LIMIT_PER_PLAYER * (int)GLOBAL_CONST.UDON_MAX_PLAYERS];
        global_projectile_refs = new int[(int)GLOBAL_CONST.PROJECTILE_LIMIT_PER_PLAYER * (int)GLOBAL_CONST.UDON_MAX_PLAYERS];
        for (int i = 0; i < global_projectile_refs.Length; i++) { global_projectile_refs[i] = -1; }
        PreallocGlobalObj((int)prealloc_obj_name.WeaponProjectile);
        /*global_hurtbox_arr = new WeaponHurtbox[(int)GLOBAL_CONST.PROJECTILE_LIMIT_PER_PLAYER * (int)GLOBAL_CONST.UDON_MAX_PLAYERS];
        global_hurtbox_refs = new int[(int)GLOBAL_CONST.PROJECTILE_LIMIT_PER_PLAYER * (int)GLOBAL_CONST.UDON_MAX_PLAYERS];
        for (int i = 0; i < global_hurtbox_refs.Length; i++) { global_hurtbox_refs[i] = -1; }
        PreallocGlobalObj((int)prealloc_obj_name.WeaponHurtbox);*/
        global_powerup_arr = new ItemPowerup[(int)GLOBAL_CONST.POWERUP_LIMIT_PER_PLAYER];
        global_powerup_refs = new int[(int)GLOBAL_CONST.POWERUP_LIMIT_PER_PLAYER];
        for (int i = 0; i < global_powerup_refs.Length; i++) { global_powerup_refs[i] = -1; }
        PreallocGlobalObj((int)prealloc_obj_name.ItemPowerup);
        global_harmnumber_arr = new UIHarmNumber[(int)GLOBAL_CONST.UDON_MAX_PLAYERS + 1]; // We want one for each player + 1 for the training dummy
        global_harmnumber_refs = new int[(int)GLOBAL_CONST.UDON_MAX_PLAYERS + 1];
        for (int i = 0; i < global_harmnumber_refs.Length; i++) { global_harmnumber_refs[i] = -1; }
        //PreallocGlobalObj((int)prealloc_obj_name.UIHarmNumber); // Can't allocate at this stage since we haven't defined local_uiplytoself yet

        music_volume_default = snd_game_music_source.volume;
        voiceover_volume_default = snd_voiceover_sfx_source.volume;

        snd_game_sfx_clips = new AudioClip[(int)game_sfx_name.ENUM_LENGTH][];
        snd_game_sfx_clips[(int)game_sfx_name.Death] = snd_game_sfx_clips_death;
        snd_game_sfx_clips[(int)game_sfx_name.Kill] = snd_game_sfx_clips_kill;
        snd_game_sfx_clips[(int)game_sfx_name.HitSend] = snd_game_sfx_clips_hitsend;
        snd_game_sfx_clips[(int)game_sfx_name.HitReceive] = snd_game_sfx_clips_hitreceive;
        snd_game_sfx_clips[(int)game_sfx_name.Announcement] = snd_game_sfx_clips_announcement;
        vopack_selected = voiceover_packs[0];

        room_spectator_portal.SetActive(false);
        ui_highlightcanvas.SetActive(false);
        ui_spectatorcanvas.SetActive(true);

        cached_ply_in_game_dict = GetPlayersInGame();
        ResetLeaderboardDictInCheck(cached_ply_in_game_dict, ref cached_leaderboard_arr, ref cached_progress_arr);

        RefreshSetupUI();
        ui_initialized = true;
    }

    public int PreallocGlobalObj(int prealloc_obj_type)
    {
        int return_index = -1;
        // Preallocate a batch of a specific object type so we can have them ready for future use
        if (prealloc_obj_type == (int)prealloc_obj_name.ItemPowerup && template_ItemSpawner != null)
        {
            for (int i = global_powerup_cnt; i < Mathf.Min(global_powerup_refs.Length, global_powerup_cnt + (int)GLOBAL_CONST.PREALLOC_BATCH_SIZE); i++)
            {
                GameObject powerup_obj = Instantiate(template_ItemSpawner.GetComponent<ItemSpawner>().child_powerup.gameObject, transform);
                global_powerup_arr[i] = powerup_obj.GetComponent<ItemPowerup>();
                global_powerup_arr[i].item_is_template = true;
                global_powerup_arr[i].ResetPowerup();
                global_powerup_arr[i].global_index = i;
                global_powerup_arr[i].gameObject.SetActive(false);
                global_powerup_refs[i] = -1;
            }
            return_index = global_powerup_refs[global_powerup_cnt];
        }
        else if (prealloc_obj_type == (int)prealloc_obj_name.UIHarmNumber && local_uiplytoself != null && local_uiplytoself.PTSHarmNumberTemplate != null)
        {
            for (int i = global_harmnumber_cnt; i < Mathf.Min(global_harmnumber_refs.Length, global_harmnumber_cnt + (int)GLOBAL_CONST.PREALLOC_BATCH_SIZE); i++)
            {
                GameObject harmnumber_obj = Instantiate(local_uiplytoself.PTSHarmNumberTemplate, local_uiplytoself.transform);
                global_harmnumber_arr[i] = harmnumber_obj.GetComponent<UIHarmNumber>();
                global_harmnumber_arr[i].ResetDisplay();
                //global_harmnumber_arr[i].gameObject.SetActive(false);
                global_harmnumber_arr[i].global_index = i;
                global_harmnumber_refs[i] = -1;
            }
            return_index = global_powerup_refs[global_harmnumber_cnt];
        }
        return return_index;
    }

    public void SetColorOptions(int color_type)
    {
        team_colors = new Color32[team_colors_base.Length];
        for (int j = 0; j < team_colors.Length; j++)
        {
            team_colors[j] = team_colors_all_options_arr[color_type][j];
        }
        ProcessBrightColors(color_type);
    }

    private void ProcessBrightColors(int color_type)
    {
        team_colors_bright = new Color32[team_colors.Length];
        byte brightness_add = 80; // default
        if (color_type == 1) { brightness_add = 35; } // protranopia has trouble distinguishing at too bright colors
        else if (color_type == 4) { brightness_add = 35; } // achromatopsia becomes all bright grays at default level

        for (int j = 0; j < team_colors.Length; j++)
        {
            team_colors_bright[j] = new Color32(
                        (byte)Mathf.Max(0, Mathf.Min((byte)255, brightness_add + team_colors[j].r)),
                        (byte)Mathf.Max(0, Mathf.Min((byte)255, brightness_add + team_colors[j].g)),
                        (byte)Mathf.Max(0, Mathf.Min((byte)255, brightness_add + team_colors[j].b)),
                        (byte)team_colors[j].a);
        }
    }

    // -- Continously Running --
    private void Update()
    {
        if (!debug_run_update) { return; }
        // Local handling
        double server_ms = Networking.GetServerTimeInSeconds();
        round_timer = (float)Networking.CalculateServerDeltaTime(server_ms, round_start_ms);
        /*room_ready_txt.text = "Game State: " + round_state.ToString()
            + "\nPlayers: " + ply_tracking_dict_values_str
            + room_ready_status_text
            ;*/

        if (round_state == (int)round_state_name.Queued)
        {
            local_queue_timer += Time.deltaTime;
        }

        local_every_second_timer += Time.deltaTime;
        if (local_every_second_timer >= 1.0f)
        {
            LocalPerSecondUpdate();
            local_every_second_timer = 0.0f;
        }
        local_tick_timer += Time.deltaTime;
        if (local_tick_timer >= ((int)GLOBAL_CONST.TICK_RATE_MS / 1000.0f))
        {
            LocalPerTickUpdate();
            local_tick_timer = 0.0f;
        }

        if (option_gamemode == (int)gamemode_name.KingOfTheHill && koth_respawn_wave_duration >= 0.0f) //koth_respawn_wave_start_ms != null && koth_respawn_wave_start_ms.Length > 0
        {
            if (!Networking.IsOwner(gameObject) && koth_respawn_wave_start_ms == 0.0f) { koth_respawn_wave_start_ms = server_ms; }
            float koth_full_timer = (float)Networking.CalculateServerDeltaTime(server_ms, koth_respawn_wave_start_ms);
            float koth_mod_timer = koth_full_timer % (float)koth_respawn_wave_duration;

            if (local_plyAttr != null) {
                local_plyAttr.ply_respawn_duration = (float)koth_respawn_wave_duration;
                if (local_plyAttr.ply_state == (int)player_state_name.Respawning) 
                { 
                    local_plyAttr.ply_respawn_timer = koth_mod_timer + 1.0f;
                    //UnityEngine.Debug.Log("[KOTH_TEST]: Respawn wave timer @ " + (koth_mod_timer + 1.0f) + " / " + koth_respawn_wave_duration);
                }
            }
            /*for (int i = 0; i < koth_respawn_wave_start_ms.Length; i++)
            {
                float koth_timer = (float)Networking.CalculateServerDeltaTime(server_ms, koth_respawn_wave_start_ms[i]);
                if (local_plyAttr != null && local_plyAttr.ply_state == (int)player_state_name.Respawning) 
                {
                    if (koth_timer >= koth_respawn_wave_duration[i]) // && Networking.IsOwner(gameObject)
                    {
                        koth_respawn_wave_start_ms[i] = server_ms;
                        koth_timer = 0.0f;
                    }
                    
                    if (option_teamplay && local_plyAttr.ply_team == i) 
                    { 
                        local_plyAttr.ply_respawn_timer = koth_timer;
                        UnityEngine.Debug.Log("[KOTH_TEST]: Respawn wave timer @ " + koth_timer + " / " + koth_respawn_wave_duration[i]);
                    }
                    else if (!option_teamplay && ply_tracking_dict_keys_arr != null && ply_tracking_dict_keys_arr.Length > 0 && i < ply_tracking_dict_keys_arr.Length && ply_tracking_dict_keys_arr[i] == Networking.LocalPlayer.playerId) 
                    { 
                        local_plyAttr.ply_respawn_timer = koth_timer;
                        UnityEngine.Debug.Log("[KOTH_TEST]: Respawn wave timer @ " + koth_timer + " / " + koth_respawn_wave_duration[i]);
                    }
                }
            }*/
        }

        if (voiceover_countdown > 0)
        {
            voiceover_countdown -= Time.deltaTime;
        }
        else if (!voiceover_interruptable)
        {
            voiceover_interruptable = true;
        }
    }

    private void LocalPerTickUpdate()
    {
        // If our local map is desynced with the true map, resync it
        if (map_selected_local != map_selected && round_state != (int)round_state_name.Queued && !restrict_map_change) { SetupMap(); }

        OOBFailsafe();

        // If ready room should render mismatches with active state and we are not the host, sync it
        if (room_ready_script != null && room_ready_script.gameObject.activeInHierarchy != room_ready_should_render && !Networking.IsOwner(gameObject)) { room_ready_script.gameObject.SetActive(room_ready_should_render); }

        double server_ms = Networking.GetServerTimeInSeconds();
        float photo_timer = 0.0f; byte reset_count = 0; byte active_count = 0;
        for (int i = 0; i < highlightCameras.Length; i++)
        {
            photo_timer = (float)Networking.CalculateServerDeltaTime(server_ms, highlight_cameras_ms[i]);
            //if (camera_id == 5) { victory_camera_plane.SetActive(true); }
            double photo_length = i == 5 ? over_length - 1 : ((int)GLOBAL_CONST.TICK_RATE_MS / 1000.0f);
            if (highlight_cameras_active[i] && photo_timer >= photo_length)
            {
                highlight_cameras_snapped[i] = !highlight_cameras_resetting;
                highlightCameras[i].gameObject.SetActive(false);
                highlight_cameras_active[i] = false;
                highlight_cameras_waiting_on_sync[i] = false; // We received the network event, thus we are no longer waiting on sync (active & snapped instead do the work)
                if (i == 5) { victory_camera_plane.SetActive(false); }
            }
            if (highlight_cameras_resetting && !highlight_cameras_active[i] && !highlight_cameras_waiting_on_sync[i]) { reset_count++; }

            if (highlight_cameras_active[i]) { active_count++; }
        }
        if (highlight_cameras_resetting && reset_count >= highlight_cameras_active.Length) { highlight_cameras_resetting = false; }
        highlight_cameras_active_cnt = active_count;

        // UI handling
        if (local_uiplytoself != null)
        {
            // To-do: check to see if this is too many updates or not
            local_uiplytoself.UI_Timer();
            local_uiplytoself.UI_Powerups();
            local_uiplytoself.UI_Weapons();
            if (option_gamemode == (int)gamemode_name.KingOfTheHill)
            {
                local_uiplytoself.UI_Capturezones();
            }
        }

        // Master handling
        if (!Networking.IsOwner(gameObject)) { return; }

        if (round_state == (int)round_state_name.Ready && round_timer >= ready_length)
        {
            round_start_ms = server_ms;
            round_state = (int)round_state_name.Ongoing;
            int[][] players_in_game = GetPlayersInGame();
            for (int j = 0; j < mapscript_list[map_selected].map_item_spawns.Length; j++)
            {
                // Only spawn if we have enough players and it isn't effectively disabled
                if (players_in_game[1].Length >= mapscript_list[map_selected].map_item_spawns[j].item_spawn_min_players && mapscript_list[map_selected].map_item_spawns[j].item_spawn_chance_total > 0)
                {
                    // Don't spawn the item if it is FFA-only and we are in teamplay mode
                    if (option_teamplay && mapscript_list[map_selected].map_item_spawns[j].item_spawn_team == -2) { mapscript_list[map_selected].map_item_spawns[j].item_spawn_state = (int)item_spawn_state_name.Disabled; }
                    else { mapscript_list[map_selected].map_item_spawns[j].item_spawn_state = (int)item_spawn_state_name.Spawnable; }
                }
                else { mapscript_list[map_selected].map_item_spawns[j].item_spawn_state = (int)item_spawn_state_name.Disabled; }
            }
            for (int k = 0; k < mapscript_list[map_selected].map_bouncepads.Length; k++)
            {
                // Only spawn if we have enough players
                if (players_in_game[1].Length >= mapscript_list[map_selected].map_bouncepads[k].min_players)
                {
                    mapscript_list[map_selected].map_bouncepads[k].gameObject.SetActive(true);
                }
                else { mapscript_list[map_selected].map_bouncepads[k].gameObject.SetActive(false); }
            }
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "RoundCommence");
            RequestSerialization();
            RefreshSetupUI();
        }
        else if (round_state == (int)round_state_name.Ongoing && !round_length_enabled)
        {
            round_timer = 0.0f;
        }
        else if (round_state == (int)round_state_name.Ongoing && round_timer >= round_length)
        {
            round_start_ms = server_ms;
            round_state = (int)round_state_name.Over;
            CheckRoundGoalProgress(ref cached_leaderboard_arr, ref cached_progress_arr, out string leader_name);
            int winning_team = -1;
            if (option_teamplay && cached_leaderboard_arr.Length > 0) { winning_team = Mathf.Max(0, cached_leaderboard_arr[0]); }
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "RoundEnd", leader_name, winning_team);
            RefreshSetupUI();
        }
        else if (round_state == (int)round_state_name.Over && round_timer >= over_length)
        {
            if (local_uiplytoself != null && round_state != (int)round_state_name.Start) { local_uiplytoself.gamevars_force_refresh_on_next_tick = true; }
            round_start_ms = server_ms;
            round_state = (int)round_state_name.Start;
            if (ply_tracking_dict_values_arr != null) { 
                for (int i = 0; i < ply_tracking_dict_keys_arr.Length; i++)
                {
                    if (ply_tracking_dict_values_arr[i] == (int)player_tracking_name.WaitingForLobby)
                    {
                        ChangeTeam(ply_tracking_dict_keys_arr[i], 0, false);
                    }
                }
            }
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "NetworkTeleportToReadyRoom"); // Failsafe in case the master gets left behind in the arena or someone gets stuck in the Training Room. Sometimes occurs if they are the last Infected.
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "NetworkPlayReadyMusic");
            RequestSerialization();
            RefreshSetupUI();
        }
        else if (round_state == (int)round_state_name.Queued && local_queue_timer >= queue_length)
        {
            round_start_ms = server_ms;
            round_state = (int)round_state_name.Loading;
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetupMap");
            SetupMapSpecificParams();
            RequestSerialization();
            //RefreshSetupUI();
            //RequestSerialization();
        }
        else if (round_state == (int)round_state_name.Loading && round_timer >= load_length)
        {
            round_start_ms = server_ms;
            round_state = (int)round_state_name.Ready;
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "NetworkRoundStart");
            //RequestSerialization();
        }
    }

    private void LocalPerSecondUpdate()
    {
        // Events that occur once per second based on local time (will always fire perfectly, but may not be synced with the server's game state)
        megaphone.SetVisible();

        // Round state
        float TimeLeft = (round_length - round_timer);
        if (round_state == (int)round_state_name.Queued && (queue_length - local_queue_timer) > 0)
        {
            RefreshSetupUI();
            PlaySFXFromArray(snd_ready_sfx_source, snd_ready_sfx_clips, (int)ready_sfx_name.TimerTick);
        }
        else if (round_state == (int)round_state_name.Ongoing)
        {
            cached_ply_in_game_dict = GetPlayersInGame();
            // Check if you're the last member of the team, and announce it if so
            if (!local_announced_last_member && option_teamplay && local_plyAttr != null && local_plyAttr.ply_team >= 0 && local_plyAttr.ply_state == (int)player_state_name.Alive && !local_plyAttr.ply_training && (option_gamemode == (int)gamemode_name.Survival || option_gamemode == (int)gamemode_name.FittingIn))
            {
                int total_players_alive = 0; int sum_lives = 0;
                int[] plyAliveLocalTeam = CheckSingleTeamLives(local_plyAttr.ply_team, cached_ply_in_game_dict, ref total_players_alive, ref sum_lives);
                if (total_players_alive == 1)
                {
                    AddToLocalTextQueue(localizer.FetchText("NOTIFICATION_LIVESMODE_LASTALIVE_SELF", "You are the last one alive!"), Color.red, 10.0f);
                    snd_game_music_source.pitch = 1.1f;
                    local_announced_last_member = true;
                }
            }

            // Time-based event SFX
            float calc_volume = music_volume_default;
            if (local_ppp_options != null) { calc_volume = music_volume_default * local_ppp_options.music_volume; }
            if (Mathf.RoundToInt(TimeLeft) == 30 && round_length_enabled && option_gamemode != (int)gamemode_name.Infection)
            {
                PlaySFXFromArray(snd_game_sfx_sources[(int)game_sfx_name.Announcement], snd_game_sfx_clips[(int)game_sfx_name.Announcement], (int)announcement_sfx_name.HurryUp);
                // Defean the music temporarily
                snd_game_music_source.volume = calc_volume * 0.05f;
            }
            else if ((Mathf.RoundToInt(TimeLeft) <= 28 || Mathf.RoundToInt(TimeLeft) > 30) && round_length_enabled && snd_game_music_source.volume != calc_volume && option_gamemode != (int)gamemode_name.Infection)
            {
                // Undefean the music. We'll also check this on RoundEnd() just in case the round ends before this fires
                snd_game_music_source.volume = calc_volume;
            }

            if (TimeLeft < 10.0f && round_length_enabled)
            {
                PlaySFXFromArray(snd_game_sfx_sources[(int)game_sfx_name.Announcement], snd_game_sfx_clips[(int)game_sfx_name.Announcement], (int)announcement_sfx_name.KOTH_Victory_Near, 0.8f);
            }

            // Time-based music speed, unless it's Infection, where its custom music cues require regular playback
            if (TimeLeft < 30.0f & round_length_enabled && option_gamemode != (int)gamemode_name.Infection)
            {
                snd_game_music_source.pitch = 1.1f;
            }
            else
            {
                snd_game_music_source.pitch = 1.0f;
            }

            // Photo Highlights
            if (Networking.IsOwner(gameObject))
            {
                if (round_length - TimeLeft >= 3
                    && highlight_cameras_snapped != null && highlight_cameras_snapped.Length > 0
                    && highlight_cameras_waiting_on_sync != null && highlight_cameras_waiting_on_sync.Length > 0
                    && highlight_cameras_snapped[0] == false && highlight_cameras_waiting_on_sync[0] == false)
                {
                    // First 3 seconds
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SnapHighlightPhoto", 0, mapscript_list[map_selected].map_campoints[0].position, mapscript_list[map_selected].map_campoints[0].rotation, Vector3.zero, Vector3.forward, false, 1.0f);
                    highlight_cameras_waiting_on_sync[0] = true;
                }
                else if (round_length - TimeLeft >= 30 
                    && highlight_cameras_snapped != null && highlight_cameras_snapped.Length > 2 
                    && highlight_cameras_waiting_on_sync != null && highlight_cameras_waiting_on_sync.Length > 2 
                    && highlight_cameras_snapped[2] == false && highlight_cameras_waiting_on_sync[2] == false)
                {
                    // First 30 seconds
                    VRCPlayerApi lead_ply = GetLeaderPlayer(); PlayerAttributes lead_attr = FindPlayerAttributes(lead_ply);
                    if (lead_ply == null || lead_attr == null) { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SnapHighlightPhoto", 2, mapscript_list[map_selected].map_campoints[0].position, mapscript_list[map_selected].map_campoints[0].rotation, Vector3.zero, Vector3.forward, false, 1.0f); }
                    else { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SnapHighlightPhoto", 2, Vector3.zero, Quaternion.identity, lead_ply.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position, lead_ply.GetRotation() * (Vector3.forward - Vector3.right), true, lead_attr.ply_scale); }
                    highlight_cameras_waiting_on_sync[2] = true;
                }
                else if (TimeLeft <= Mathf.RoundToInt(round_length / 2.0f) 
                    && highlight_cameras_snapped != null && highlight_cameras_snapped.Length > 3 
                    && highlight_cameras_waiting_on_sync != null && highlight_cameras_waiting_on_sync.Length > 3
                    && highlight_cameras_snapped[3] == false && highlight_cameras_waiting_on_sync[3] == false)
                {
                    // Halftime
                    VRCPlayerApi lead_ply = GetLeaderPlayer(); PlayerAttributes lead_attr = FindPlayerAttributes(lead_ply);
                    if (lead_ply == null || lead_attr == null) { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SnapHighlightPhoto", 3, mapscript_list[map_selected].map_campoints[0].position, mapscript_list[map_selected].map_campoints[0].rotation, Vector3.zero, Vector3.forward, false, 1.0f); }
                    else { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SnapHighlightPhoto", 3, Vector3.zero, Quaternion.identity, lead_ply.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position, lead_ply.GetRotation() * (Vector3.forward - Vector3.right), true, lead_attr.ply_scale); }
                    highlight_cameras_waiting_on_sync[3] = true;
                }
                else if (TimeLeft <= 30
                    && highlight_cameras_snapped != null && highlight_cameras_snapped.Length > 4 
                    && highlight_cameras_waiting_on_sync != null && highlight_cameras_waiting_on_sync.Length > 4 
                    && highlight_cameras_snapped[4] == false && highlight_cameras_waiting_on_sync[4] == false)
                {
                    // Last 30 seconds
                    VRCPlayerApi lead_ply = GetLeaderPlayer(); PlayerAttributes lead_attr = FindPlayerAttributes(lead_ply);
                    if (lead_ply == null || lead_attr == null) { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SnapHighlightPhoto", 4, mapscript_list[map_selected].map_campoints[0].position, mapscript_list[map_selected].map_campoints[0].rotation, Vector3.zero, Vector3.forward, false, 1.0f); }
                    else { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SnapHighlightPhoto", 4, Vector3.zero, Quaternion.identity, lead_ply.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position, lead_ply.GetRotation() * (Vector3.forward - Vector3.right), true, lead_attr.ply_scale); }
                    highlight_cameras_waiting_on_sync[4] = true;
                }
            }
        }
        /*else if (round_state == (int)round_state_name.Over)
        {
            if (Networking.IsOwner(gameObject) && Mathf.RoundToInt(over_length - round_timer) <= 3
                && highlight_cameras_snapped != null && highlight_cameras_snapped.Length > 5
                && highlight_cameras_waiting_on_sync != null && highlight_cameras_waiting_on_sync.Length > 5
                && highlight_cameras_snapped[5] == false && highlight_cameras_waiting_on_sync[5] == false)
            {
                // Victory
                VRCPlayerApi lead_ply = GetLeaderPlayer(); PlayerAttributes lead_attr = FindPlayerAttributes(lead_ply);
                if (option_teamplay || lead_ply == null || lead_attr == null) { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SnapHighlightPhoto", 5, Vector3.zero, Quaternion.identity, room_ready_spawn.transform.position, Vector3.right, true, 1.0f); }
                else { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SnapHighlightPhoto", 5, Vector3.zero, Quaternion.identity, lead_ply.GetPosition(), lead_ply.GetRotation() * Vector3.forward, true, lead_attr.ply_scale); }
                highlight_cameras_waiting_on_sync[5] = true;
            }
        }*/

        // Infection-specific handling
        if (round_state == (int)round_state_name.Ongoing && option_gamemode == (int)gamemode_name.Infection)
        {
            int[][] survivors = GetPlayersOnTeam(0);
            int[][] infected = GetPlayersOnTeam(1);

            // If there is only one survivor left, play the last survivor music
            if (survivors != null && survivors.Length > 0 && survivors[0] != null && survivors[0].Length == 1)
            {
                if (music_clip_playing != snd_infection_music_clips[(int)infection_music_name.LastSurvivor])
                {
                    PlaySFXFromArray(snd_game_music_source, snd_infection_music_clips, (int)infection_music_name.LastSurvivor, 1.0f, true);
                    VRCPlayerApi last_survivor = VRCPlayerApi.GetPlayerById(survivors[0][0]);
                    // And if we're the last survivor, buff ourselves and give ourselves an infinite ammo rocket launcher
                    if (last_survivor != null && last_survivor == Networking.LocalPlayer)
                    {
                        //local_plyAttr.ply_speed *= 1.5f;
                        local_plyAttr.ply_atk *= 4.5f;
                        local_plyAttr.ply_def *= 1.0f;
                        local_plyweapon.weapon_temp_ammo = -1;
                        local_plyweapon.weapon_temp_duration = -1;
                        local_plyweapon.weapon_temp_timer = 0.0f;
                        local_plyweapon.weapon_type = (int)weapon_type_name.HyperGlove;
                        local_plyweapon.weapon_type_default = local_plyweapon.weapon_type;
                        local_plyweapon.weapon_extra_data = 0;
                        if (local_uiplytoself != null && template_ItemSpawner != null)
                        {
                            ItemWeapon iweapon = template_ItemSpawner.GetComponent<ItemSpawner>().child_weapon;
                            local_uiplytoself.PTSWeaponSprite.sprite = iweapon.iweapon_sprites[(int)weapon_type_name.Rocket];
                            PlaySFXFromArray(local_plyweapon.snd_source_weaponcharge, iweapon.iweapon_snd_clips, iweapon.iweapon_type);
                        }
                        local_plyweapon.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "UpdateStatsFromWeaponType");
                        AddToLocalTextQueue(localizer.FetchText("NOTIFICATION_INFECTION_LASTALIVE_SELF", "You are the last one alive!"), Color.red, 10.0f);
                    }
                    else if (last_survivor != null)
                    {
                        AddToLocalTextQueue(localizer.FetchText("NOTIFICATION_INFECTION_LASTALIVE_OTHER", "$ARG0 is the last survivor!", last_survivor.displayName), Color.red, 10.0f);
                    }
                }
            }
            // At the last few seconds, play the finale music, if we aren't already playing something else
            else if (TimeLeft < 30.0f)
            {
                if (music_clip_playing != snd_infection_music_clips[(int)infection_music_name.FinalSeconds])
                {
                    PlaySFXFromArray(snd_game_music_source, snd_infection_music_clips, (int)infection_music_name.FinalSeconds, 1.0f, true);
                }
            }
            else if (infection_zombig_active == 1)
            {
                if (music_clip_playing != snd_infection_music_clips[(int)infection_music_name.ZombigSpawn]) 
                {
                    music_clip_ts = snd_game_music_source.time;
                    PlaySFXFromArray(snd_game_music_source, snd_infection_music_clips, (int)infection_music_name.ZombigSpawn, 1.0f, true); 
                }
            }
            else if (infection_zombig_active == 2)
            {
                if (music_clip_playing != snd_infection_music_clips[(int)infection_music_name.ZombigSuper])
                {
                    music_clip_ts = snd_game_music_source.time;
                    PlaySFXFromArray(snd_game_music_source, snd_infection_music_clips, (int)infection_music_name.ZombigSuper, 1.0f, true);
                }
            }
            else
            {
                if (music_clip_playing != snd_infection_music_clips[(int)infection_music_name.Start] && (local_ppp_options == null || (local_ppp_options != null && local_ppp_options.music_override < 0))) 
                { 
                    PlaySFXFromArray(snd_game_music_source, snd_infection_music_clips, (int)infection_music_name.Start, 1.0f, true, music_clip_ts); 
                }
            }

            // Reset patient zero once 124 of survivors are dead
            if (local_plyAttr.infection_special == 1
                && survivors != null && survivors.Length > 0 && survivors[0] != null
                && infected != null && infected.Length > 0 && infected[0] != null
                && infected[0].Length >= survivors[0].Length)
            {
                local_plyAttr.infection_special = 0;
                local_plyAttr.InfectionStatReset(false);
                AddToLocalTextQueue(localizer.FetchText("NOTIFICATION_INFECTION_HALFTIME", "Half of the Survivors are dead! You are no longer buffed!"));
            }

            // -- Master only below --
            if (Networking.IsOwner(gameObject))
            {
                int survivor_count = survivors != null && survivors.Length > 0 && survivors[0] != null ? survivors[0].Length : 0;
                // If we hit a certain threshold of players and timer is met and we have not yet spawned X number of zombigs, spawn one
                if (infection_zombig_active == 0 && ((TimeLeft <= (round_length / 1.5f) && infection_zombigs_spawned < 1) || (TimeLeft <= (round_length / 3.0f) && infection_zombigs_spawned < 2 && survivor_count >= 4)) && infected != null && infected.Length > 0 && infected[0] != null && infected[0].Length > 0)
                {
                    int pick_player_id = UnityEngine.Random.Range(0, infected[0].Length);
                    VRCPlayerApi pick_player = VRCPlayerApi.GetPlayerById(infected[0][pick_player_id]);
                    if (pick_player != null)
                    {
                        PlayerAttributes zombig_attr = FindPlayerAttributes(pick_player);
                        if (zombig_attr != null && zombig_attr.infection_special < 2)
                        {
                            bool zombig_super = survivor_count >= 10;
                            zombig_attr.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "BecomeZombig", true, zombig_super);
                            infection_zombig_spawn_time = Networking.GetServerTimeInSeconds();
                            infection_zombig_active = (byte)(zombig_super ? 2 : 1);
                            infection_zombigs_spawned++;
                            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "NetworkAddToTextQueue", "NOTIFICATION_INFECTION_ZOMBIG_GLOBAL", pick_player.displayName, Color.red, 5.0f);
                            RequestSerialization();
                        }
                    }
                }
            }
        }

        // Clock displays
        if (clock_displays != null && clock_displays.Length > 0)
        {
            for (int i = 0; i < clock_displays.Length; i++)
            {
                if (localizer != null && localizer.language_type == (int)language_type_name.English) { clock_displays[i].UpdateTime(true); }
                else { clock_displays[i].UpdateTime(false); }
            }
        }

        //UnityEngine.Debug.Log("[VOICE_TEST] " + Networking.LocalPlayer.displayName + " [" + Networking.LocalPlayer.playerId + "]"
        //    + ": near = " + Networking.LocalPlayer.GetVoiceDistanceNear() + ", far = " + Networking.LocalPlayer.GetVoiceDistanceFar() + ", gain = " + Networking.LocalPlayer.GetVoiceGain() + "; "
        //    + Networking.GetOwner(gameObject).displayName + " [" + Networking.GetOwner(gameObject).playerId + "]"
        //    + ": near = " + Networking.GetOwner(gameObject).GetVoiceDistanceNear() + ", far = " + Networking.GetOwner(gameObject).GetVoiceDistanceFar() + ", gain = " + Networking.GetOwner(gameObject).GetVoiceGain()
        //    );

        if (local_plyAttr != null) { local_plyAttr.LocalPerSecondUpdate(); } // Run per second update for local player attribute as well, so we don't have to keep as many timers
       
    }

    public VRCPlayerApi GetLeaderPlayer()
    {
        if (option_teamplay || cached_ply_in_game_dict == null || cached_ply_in_game_dict.Length == 0 || cached_leaderboard_arr == null || cached_leaderboard_arr.Length == 0) { return null; }
        /*if (option_teamplay)
        {
            for (int i = 0; i < cached_ply_in_game_dict.Length; i++)
            {
                if (cached_ply_in_game_dict[i][1] == cached_leaderboard_arr[0]) { return VRCPlayerApi.GetPlayerById(cached_ply_in_game_dict[i][0]); }
            }
            return null;
        }
        else
        {*/
        return VRCPlayerApi.GetPlayerById(cached_leaderboard_arr[0]);
        //}
    }

    [NetworkCallable]
    public void CheckForZombigs(int exclude_player_id)
    {
        // Only the game master should have this function return a value
        if (!Networking.IsOwner(gameObject)) { return; }
        
        int[][] infected = GetPlayersOnTeam(1);
        byte found_zombig = 0;
        if (infected != null && infected.Length > 0 && infected[0] != null && infected[0].Length > 0)
        {
            for (int i = 0; i < infected[0].Length; i++)
            {
                if (infected[0][i] == exclude_player_id) { continue; }
                VRCPlayerApi check_player = VRCPlayerApi.GetPlayerById(infected[0][i]);
                if (check_player != null)
                {
                    PlayerAttributes zombig_attr = FindPlayerAttributes(check_player);
                    if (zombig_attr != null && zombig_attr.infection_special >= 2)
                    {
                        found_zombig = zombig_attr.infection_special;
                        break;
                    }
                }
            }
        }
        infection_zombig_active = found_zombig;
        if (found_zombig == 0) { infection_zombig_spawn_time = 0.0d; }
        RequestSerialization();
    }


    public override void OnPostSerialization(SerializationResult result)
    {
        UnityEngine.Debug.Log("Serialized data: " + result.success + " with " + result.byteCount + " bytes");
    }

    public override void OnDeserialization()
    {
        int[] keys_from_network = ConvertStrToIntArray(ply_tracking_dict_keys_str);
        int[] values_from_network = ConvertStrToIntArray(ply_tracking_dict_values_str);

        if (ply_tracking_dict_keys_arr == null || ply_tracking_dict_values_arr == null || ply_tracking_dict_keys_arr != keys_from_network || ply_tracking_dict_values_arr != values_from_network
            //&& Networking.CalculateServerDeltaTime(Networking.GetServerTimeInSeconds(), networkJoinTime) < 10.0f
            )
        {
            ply_tracking_dict_keys_arr = keys_from_network;
            ply_tracking_dict_values_arr = values_from_network;
            cached_ply_in_game_dict = GetPlayersInGame();
            //UnityEngine.Debug.Log("CLIENT: Syncing new arrays: " + ply_tracking_dict_keys_str + " | " + ply_tracking_dict_values_str);
        }

        if (local_plyAttr == null) { local_plyAttr = FindPlayerAttributes(Networking.LocalPlayer); }
        if (local_plyAttr != null)
        {
            // Check previous team, if going from waiting for lobby to something new, then play a nice noise
            int old_team = local_plyAttr.ply_team;
            local_plyAttr.ply_team = GetGlobalTeam(Networking.LocalPlayer.playerId);
            if (old_team == (int)player_tracking_name.WaitingForLobby && local_plyAttr.ply_team >= 0) { PlaySFXFromArray(snd_ready_sfx_source, snd_ready_sfx_clips, (int)ready_sfx_name.JoinGame); }
            else if ((old_team == (int)player_tracking_name.WaitingForLobby || old_team >= 0) && local_plyAttr.ply_team < 0 && old_team != local_plyAttr.ply_team) { PlaySFXFromArray(snd_ready_sfx_source, snd_ready_sfx_clips, (int)ready_sfx_name.LeaveGame); }
            else if (old_team != (int)player_tracking_name.WaitingForLobby && local_plyAttr.ply_team == (int)player_tracking_name.WaitingForLobby) { PlaySFXFromArray(snd_ready_sfx_source, snd_ready_sfx_clips, (int)ready_sfx_name.JoinMidGame); }
            else if (old_team != (int)player_tracking_name.Spectator && local_plyAttr.ply_team == (int)player_tracking_name.Spectator) { PlaySFXFromArray(snd_ready_sfx_source, snd_ready_sfx_clips, (int)ready_sfx_name.LeaveGame); }
            else if (old_team != (int)player_tracking_name.Unassigned && local_plyAttr.ply_team == (int)player_tracking_name.Unassigned) { PlaySFXFromArray(snd_ready_sfx_source, snd_ready_sfx_clips, (int)ready_sfx_name.LeaveGame); }

            if (local_plyAttr.ply_state == (int)player_state_name.Spectator) {
                if (local_plyAttr.ply_team >= 0 || local_plyAttr.ply_team == (int)player_tracking_name.WaitingForLobby) { local_plyAttr.ply_state = (int)player_state_name.Joined; }
                else if (local_plyAttr.ply_team != (int)player_tracking_name.Spectator) { local_plyAttr.ply_state = (int)player_state_name.Inactive; }
                else { } // If state & team are already spectator, leave as-is
            }
            else if (local_plyAttr.ply_team == (int)player_tracking_name.Spectator && !local_plyAttr.ply_training) { local_plyAttr.ply_state = (int)player_state_name.Spectator; }
            if (room_ready_script.warning_acknowledged) { room_ready_script.ZoneMarkerHighlight(true); }
        }

        if (ui_round_mapselect != null)
        {
            if (maps_active_local == null || maps_active_str == null) { maps_active_str = ""; maps_active_local = ""; }
            if (maps_active_local.Length != maps_active_str.Length) // broke here, somehow.
            {
                ui_round_mapselect.BuildMapList();
            }
            maps_active_local = maps_active_str;
            ui_round_mapselect.RefreshMapList();
        }

        // Once we know our team variables have been fully synchronized, we can start the round on our end
        if (wait_for_sync_for_round_start)
        {
            SetupMap();
            LocalRoundStart();
            wait_for_sync_for_round_start = false;
        }

        // Once a new player has received data, setup the correct map and teleport them as well as sync any hitbox or weapon active states
        if (wait_for_sync_for_player_join && ply_tracking_dict_keys_arr != null)
        {
            SetupMap();

            for (int i = 0; i < ply_tracking_dict_keys_arr.Length; i++)
            {
                if (ply_tracking_dict_keys_arr == null || ply_tracking_dict_keys_arr.Length == 0) { break; }
                if (ply_tracking_dict_keys_arr[i] < 0) { continue; }
                VRCPlayerApi player = VRCPlayerApi.GetPlayerById(ply_tracking_dict_keys_arr[i]);
                if (player == null) { continue; }
                var plyAttributesObj = FindPlayerOwnedObject(player, "PlayerAttributes");
                var plyAttributesComponent = plyAttributesObj.GetComponent<PlayerAttributes>();

                if (plyAttributesComponent.ply_team >= 0 && plyAttributesComponent.ply_state != (int)player_state_name.Dead && plyAttributesComponent.ply_state != (int)player_state_name.Inactive)
                {
                    var plyWeaponObj = FindPlayerOwnedObject(player, "PlayerWeapon");
                    var plyHitboxObj = FindPlayerOwnedObject(player, "PlayerHitbox");
                    plyWeaponObj.SetActive(true);
                    plyHitboxObj.SetActive(true);
                }
            }

            if (round_state == (int)round_state_name.Ongoing || round_state == (int)round_state_name.Ready) { room_spectator_portal.SetActive(true); }
            else { room_spectator_portal.SetActive(false); }

            wait_for_sync_for_player_join = false;
        }

        if (local_uiplytoself == null)
        {
            var plyUIToSelf = FindPlayerOwnedObject(Networking.LocalPlayer, "UIPlyToSelf");
            local_uiplytoself = plyUIToSelf.GetComponent<UIPlyToSelf>();
            PreallocGlobalObj((int)prealloc_obj_name.UIHarmNumber);
        }

        if (local_megaphone_active != megaphone_active) 
        {
            AdjustVoiceRange();
        }

        RefreshSetupUI();
    }

    public void RefreshSetupUI()
    {
        ui_updating = true;

        ui_round_teamplay_toggle.isOn = option_teamplay;
        ui_round_master_only_toggle.isOn = option_start_from_master_only;
        ui_round_teamplay_personal_toggle.isOn = option_personal_teams;

        ui_round_team_panel.SetAllTeamCounters();
        ui_round_team_panel.RefreshAllPanels();
        ui_round_mapselect.RefreshMapList();

        // This variable never updates if you aren't the master (updated in RoundOptionAdjust()), so let's update it for clients here
        if (!Networking.IsOwner(gameObject))
        {
            option_team_limits_arr = ConvertStrToIntArray(option_team_limits_str);
        }

        if (ui_round_option_dropdown != null && local_gamemode_count < round_option_names.Length)
        {
            // In case we are resetting the options [due to language change] instead of setting them, we store the value of the dropdown and set it after refreshing the options
            int stored_gamemode_value = ui_round_option_dropdown.value;
            ui_round_option_dropdown.ClearOptions();
            ui_round_option_dropdown.AddOptions(round_option_names);
            ui_round_option_dropdown.value = stored_gamemode_value;
            local_gamemode_count = (byte)round_option_names.Length;

            string gamemode_tutorial_description = "";
            for (int i = 0; i < round_option_names.Length; i++)
            {
                if (ui_tutorial_gamemodes_txt == null || i >= ui_tutorial_gamemodes_txt.Length) { break; }
                gamemode_tutorial_description = round_option_descriptions[i];
                gamemode_tutorial_description = gamemode_tutorial_description.Replace("$TIMER", "X");
                gamemode_tutorial_description = gamemode_tutorial_description.Replace("$POINTS_A", "X");
                gamemode_tutorial_description = gamemode_tutorial_description.Replace("$LIVES", "X");
                ui_tutorial_gamemodes_txt[i].text = gamemode_tutorial_description;
            }
        }

        if (ui_ply_option_weapon_dropdown != null && local_weapon_count < (int)weapon_type_name.ENUM_LENGTH)
        {
            int stored_weapon_value = ui_ply_option_weapon_dropdown.value;
            ui_ply_option_weapon_dropdown.ClearOptions();
            string[] weapon_options = new string[(int)weapon_type_name.ENUM_LENGTH];
            for (int i = 0; i < weapon_options.Length; i++) {
                weapon_options[i] = WeaponTypeToStr(i);
            }
            ui_ply_option_weapon_dropdown.AddOptions(weapon_options);
            local_weapon_count = (byte)weapon_options.Length;
            ui_ply_option_weapon_dropdown.value = stored_weapon_value;
        }

        /*if (ui_round_map_dropdown != null && (map_selected >= mapscript_list.Length || map_selected < 0))
        {
            string[] map_names = new string[mapscript_list.Length];
            for (int i = 0; i < mapscript_list.Length; i++)
            {
                map_names[i] = mapscript_list[i].map_name;
            }
            ui_round_map_dropdown.ClearOptions();
            ui_round_map_dropdown.AddOptions(map_names);
            map_selected = 0;
        }*/

        // Room ready must occur AFTER SetAllTeamCounters to function correctly
        cached_ply_in_game_dict = GetPlayersInGame();
        bool enableRoundStartButton = true;

        float ReadyTimerDisplay = Mathf.Floor(queue_length - local_queue_timer);
        if (ReadyTimerDisplay < 0 || ReadyTimerDisplay >= queue_length) { ReadyTimerDisplay = queue_length - 1; }

        room_ready_status_text = localizer.FetchText("GAMESETTINGS_START_START", "START");
        if (round_state == (int)round_state_name.Queued) {
            room_ready_status_text = ReadyTimerDisplay.ToString();
        }
        else if (round_state == (int)round_state_name.Loading) { enableRoundStartButton = false; room_ready_status_text = localizer.FetchText("GAMESETTINGS_START_LOADING", "--LOADING--"); }
        else if (round_state != (int)round_state_name.Start) { enableRoundStartButton = false; room_ready_status_text = localizer.FetchText("GAMESETTINGS_START_INPROGRESS", "MATCH IN PROGRESS"); }
        else if (ui_round_mapselect != null && ui_round_mapselect.GetActiveMaps().Length <= 0) { enableRoundStartButton = false; room_ready_status_text = localizer.FetchText("GAMESETTINGS_START_NOMAPS", "NO MAPS SELECTED"); }
        else if (cached_ply_in_game_dict != null && cached_ply_in_game_dict[0].Length <= 0) { enableRoundStartButton = false; }
        else if (option_gamemode == (int)gamemode_name.BossBash || option_gamemode == (int)gamemode_name.Infection)
        {
            if (ply_tracking_dict_keys_arr.Length < 2 || option_team_limits_arr.Length < 2) { enableRoundStartButton = false; room_ready_status_text = "X"; }
            else if (ui_round_team_panel.team_count_arr.Length < 2) { enableRoundStartButton = false; room_ready_status_text = "X"; }
            else if (ui_round_team_panel.team_count_arr[1] > option_team_limits_arr[1]) { enableRoundStartButton = false; room_ready_status_text = localizer.FetchText("GAMESETTINGS_START_FSMAX", "TOO MANY ON RED TEAM"); }
            else if (ui_round_team_panel.team_count_arr[1] < option_team_limits_arr[1]) { enableRoundStartButton = false; room_ready_status_text = localizer.FetchText("GAMESETTINGS_START_FSMIN", "NOT ENOUGH ON RED TEAM"); }
        }
        else if (option_gamemode == (int)gamemode_name.FittingIn)
        {
            if (option_gm_goal < 1) { enableRoundStartButton = false; room_ready_status_text = localizer.FetchText("GAMESETTINGS_START_SIZEMIN", "MAX SIZE < 100%"); }
            if (option_gm_config_a < 0) { enableRoundStartButton = false; room_ready_status_text = "X"; }
        }

        // No matter what, if we aren't the master and master-only is toggled, do not allow them to start it
        if (!Networking.IsOwner(gameObject) && option_start_from_master_only)
        {
            enableRoundStartButton = false;
            if (room_ready_status_text == localizer.FetchText("GAMESETTINGS_START_START", "START")) { room_ready_status_text = localizer.FetchText("GAMESETTINGS_START_MASTERONLY", "(MASTER ONLY)"); }
        }

        ui_round_start_button.GetComponentInChildren<TMP_Text>().text = room_ready_status_text;
        ui_round_start_button.interactable = enableRoundStartButton;
        ui_round_length_input.text = Mathf.FloorToInt(round_length).ToString();
        ui_round_length_toggle.isOn = round_length_enabled;

        bool enableResetButton = false;
        if ((!option_start_from_master_only || (option_start_from_master_only && Networking.IsOwner(gameObject))) && round_state == (int)round_state_name.Ongoing) { enableResetButton = true; }
        ui_round_reset_button.interactable = enableResetButton;

        if (Networking.IsOwner(gameObject) && round_state == (int)round_state_name.Start && !master_in_transit)
        {
            ui_round_master_only_toggle.interactable = true;
            ui_round_teamplay_toggle.interactable = true && !option_force_teamplay;
            ui_round_teamplay_personal_toggle.interactable = option_teamplay;
            ui_round_option_dropdown.interactable = true;
            ui_round_option_default_button.interactable = true;
            ui_round_length_toggle.interactable = option_gamemode != (int)gamemode_name.Infection; // Infection requires a time limit, so don't allow people to toggle it
            ui_round_length_input.interactable = round_length_enabled;
            ui_round_option_goal_input_a.interactable = true;
            ui_round_option_goal_input_b.interactable = true;
            ui_round_teamplay_sort_button.interactable = option_teamplay;
            ui_round_teamplay_count_input.interactable = option_teamplay && !option_enforce_team_limits;

            ui_ply_option_dp.interactable = true;
            ui_ply_option_scale.interactable = true;
            ui_ply_option_atk.interactable = true;
            ui_ply_option_def.interactable = true;
            ui_ply_option_speed.interactable = true;
            ui_ply_option_grav.interactable = true;
            ui_ply_option_weapon_dropdown.interactable = true;
            ui_ply_option_weapon_toggle.interactable = true;
            ui_ply_option_powerup_toggle.interactable = true;

            ui_adv_option_respawn_duration.interactable = true;
            ui_adv_option_item_frequency.interactable = true;
            ui_adv_option_item_duration.interactable = true;
            ui_adv_option_boss_atk_mod.interactable = true;
            ui_adv_option_boss_def_mod.interactable = true;
            ui_adv_option_boss_speed_mod.interactable = true;
            ui_adv_option_boss_scale_mod.interactable = true;
            ui_adv_option_item_debuff_toggle.interactable = true;
        }
        else
        {
            ui_round_master_only_toggle.interactable = false;
            ui_round_teamplay_toggle.interactable = false;
            ui_round_teamplay_personal_toggle.interactable = false;
            ui_round_option_dropdown.interactable = false;
            ui_round_option_default_button.interactable = false;
            ui_round_length_toggle.interactable = false;
            ui_round_length_input.interactable = false;
            ui_round_option_goal_input_a.interactable = false;
            ui_round_option_goal_input_b.interactable = false;
            ui_round_teamplay_sort_button.interactable = Networking.IsOwner(gameObject) && round_state == (int)round_state_name.Queued && option_teamplay;
            ui_round_teamplay_count_input.interactable = false;

            ui_ply_option_dp.interactable = false;
            ui_ply_option_scale.interactable = false;
            ui_ply_option_atk.interactable = false;
            ui_ply_option_def.interactable = false;
            ui_ply_option_speed.interactable = false;
            ui_ply_option_grav.interactable = false;
            ui_ply_option_weapon_dropdown.interactable = false;
            ui_ply_option_weapon_toggle.interactable = false;
            ui_ply_option_powerup_toggle.interactable = false;

            ui_adv_option_respawn_duration.interactable = false;
            ui_adv_option_item_frequency.interactable = false;
            ui_adv_option_item_duration.interactable = false;
            ui_adv_option_boss_atk_mod.interactable = false;
            ui_adv_option_boss_def_mod.interactable = false;
            ui_adv_option_boss_speed_mod.interactable = false;
            ui_adv_option_boss_scale_mod.interactable = false;
            ui_adv_option_item_debuff_toggle.interactable = false;
        }

        if (VRCPlayerApi.GetPlayerById(ply_master_id) != null)
        {
            ui_round_master_text.text = localizer.FetchText("GAMESETTINGS_MASTER_LABEL", "Game Master:\n$ARG0", VRCPlayerApi.GetPlayerById(ply_master_id).displayName);
        }

        ui_round_teamplay_count_input.text = team_count.ToString();
        ui_round_option_dropdown.value = option_gamemode;
        //ui_round_map_dropdown.value = map_selected;

        if (option_gamemode == (int)gamemode_name.Survival)
        {
            ui_round_option_goal_text_a.text = localizer.FetchText("GAMESETTINGS_GAMEMODE_LIVES", "Lives");
            ui_round_option_goal_input_a.text = plysettings_lives.ToString();
            ui_round_option_goal_text_b.gameObject.SetActive(false);
            ui_round_option_goal_input_b.gameObject.SetActive(false);

        }
        else if (option_gamemode == (int)gamemode_name.Clash)
        {
            ui_round_option_goal_text_a.text = localizer.FetchText("GAMESETTINGS_GAMEMODE_POINTSTOWIN", "Points to Win");
            ui_round_option_goal_input_a.text = option_gm_goal.ToString();
            ui_round_option_goal_text_b.gameObject.SetActive(false);
            ui_round_option_goal_input_b.gameObject.SetActive(false);
        }
        else if (option_gamemode == (int)gamemode_name.BossBash)
        {
            ui_round_option_goal_text_a.text = localizer.FetchText("GAMESETTINGS_GAMEMODE_BOSSKOSTOWIN", "Boss KOs to Win");
            ui_round_option_goal_input_a.text = option_gm_goal.ToString();
            ui_round_option_goal_text_b.text = localizer.FetchText("GAMESETTINGS_GAMEMODE_BOSSLIVES", "Boss Lives");
            ui_round_option_goal_input_b.text = plysettings_lives.ToString();
            ui_round_option_goal_text_b.gameObject.SetActive(true);
            ui_round_option_goal_input_b.gameObject.SetActive(true);
        }
        else if (option_gamemode == (int)gamemode_name.Infection)
        {
            ui_round_option_goal_text_a.text = localizer.FetchText("GAMESETTINGS_GAMEMODE_STARTINGINFECTED", "Starting Infected");
            ui_round_option_goal_input_a.text = option_gm_goal.ToString();
            ui_round_option_goal_text_b.gameObject.SetActive(false);
            ui_round_option_goal_input_b.gameObject.SetActive(false);
        }
        else if (option_gamemode == (int)gamemode_name.KingOfTheHill)
        {
            ui_round_option_goal_text_a.text = localizer.FetchText("GAMESETTINGS_GAMEMODE_HOLDTIME", "Hold Time");
            ui_round_option_goal_input_a.text = option_gm_goal.ToString();
            ui_round_option_goal_text_b.text = localizer.FetchText("GAMESETTINGS_GAMEMODE_TURNOVERTIME", "Turnover Time");
            ui_round_option_goal_input_b.text = option_gm_config_a.ToString();
            ui_round_option_goal_text_b.gameObject.SetActive(true);
            ui_round_option_goal_input_b.gameObject.SetActive(true);
        }
        else if (option_gamemode == (int)gamemode_name.FittingIn)
        {
            ui_round_option_goal_text_a.text = localizer.FetchText("GAMESETTINGS_GAMEMODE_MAXSIZE", "Max Size (%)");
            ui_round_option_goal_input_a.text = option_gm_goal.ToString();
            ui_round_option_goal_text_b.text = localizer.FetchText("GAMESETTINGS_GAMEMODE_SIZEINCREASE", "Size Increase upon Fall (%)");
            ui_round_option_goal_input_b.text = option_gm_config_a.ToString();
            ui_round_option_goal_text_b.gameObject.SetActive(true);
            ui_round_option_goal_input_b.gameObject.SetActive(true);
        }
        ui_round_option_description.text = ModifyModeDescription(round_option_descriptions[option_gamemode]);
        ui_round_option_image.sprite = round_option_images[option_gamemode];

        // Starting & Advanced Options Parsing
        ui_ply_option_dp.text = Mathf.RoundToInt(plysettings_dp).ToString();
        ui_ply_option_scale.text = Mathf.RoundToInt(plysettings_scale * 100.0f).ToString();
        ui_ply_option_atk.text = Mathf.RoundToInt(plysettings_atk * 100.0f).ToString();
        ui_ply_option_def.text = Mathf.RoundToInt(plysettings_def * 100.0f).ToString();
        ui_ply_option_speed.text = Mathf.RoundToInt(plysettings_speed * 100.0f).ToString();
        ui_ply_option_grav.text = Mathf.RoundToInt(plysettings_grav * 100.0f).ToString();

        ui_ply_option_weapon_dropdown.value = plysettings_weapon;
        ui_ply_option_weapon_toggle.isOn = plysettings_iweapons;
        ui_ply_option_powerup_toggle.isOn = plysettings_powerups;

        ui_adv_option_respawn_duration.text = Mathf.RoundToInt(plysettings_respawn_duration).ToString();
        ui_adv_option_boss_scale_mod.text = Mathf.RoundToInt(plysettings_boss_scale_mod * 100.0f).ToString();
        ui_adv_option_boss_atk_mod.text = Mathf.RoundToInt(plysettings_boss_atk_mod * 100.0f).ToString();
        ui_adv_option_boss_def_mod.text = Mathf.RoundToInt(plysettings_boss_def_mod * 100.0f).ToString();
        ui_adv_option_boss_speed_mod.text = Mathf.RoundToInt(plysettings_boss_speed_mod * 100.0f).ToString();
        ui_adv_option_item_frequency.text = Mathf.RoundToInt(plysettings_item_frequency * 100.0f).ToString();
        ui_adv_option_item_duration.text = Mathf.RoundToInt(plysettings_item_duration * 100.0f).ToString();

        ui_adv_option_item_debuff_toggle.isOn = plysettings_item_debuff;

        //if (map_selected < mapscript_list.Length) { ui_round_map_description.text = mapscript_list[map_selected].map_description; }
        RefreshGameUI();

        ui_updating = false;
    }

    public void RefreshGameUI()
    {
        if (local_uiplytoself != null && round_state != (int)round_state_name.Start) { local_uiplytoself.gamevars_force_refresh_on_next_tick = true; }
        if (local_plyweapon != null) { local_plyweapon.SetTeamColor(); }
        if (local_secondaryweapon != null) { local_secondaryweapon.SetTeamColor(); }
    }

    [NetworkCallable]
    public void ChangeHost(int new_owner_id)
    {
        // Note: This function needs to be updated we add any scripts that would ordinarily use Networking.IsMaster
        master_in_transit = true;
        //if (!Networking.IsOwner(gameObject)) { return; }
        RequestSerialization();
        VRCPlayerApi new_owner = VRCPlayerApi.GetPlayerById(new_owner_id);
        Networking.SetOwner(new_owner, ui_round_mapselect.gameObject);
        if (mapscript_list != null && mapscript_list.Length > 0)
        {
            foreach (ItemSpawner itemSpawner in mapscript_list[0].GetItemSpawnerFromParent(room_training.transform))
            {
                if (itemSpawner == null || itemSpawner.gameObject == null) { continue; }
                Networking.SetOwner(new_owner, itemSpawner.gameObject);
            }
        }
        for (int m = 0; m < mapscript_list.Length; m++)
        {
            foreach (ItemSpawner itemSpawner in mapscript_list[m].map_item_spawns)
            {
                if (itemSpawner == null || itemSpawner.gameObject == null) { continue; }
                Networking.SetOwner(new_owner, itemSpawner.gameObject);
                if (itemSpawner.child_powerup != null) { Networking.SetOwner(new_owner, itemSpawner.child_powerup.gameObject); }
                if (itemSpawner.child_weapon != null) { Networking.SetOwner(new_owner, itemSpawner.child_weapon.gameObject); }

            }
            foreach (CaptureZone capturezone in mapscript_list[m].map_capturezones)
            {
                if (capturezone == null || capturezone.gameObject == null) { continue; }
                Networking.SetOwner(new_owner, capturezone.gameObject);
            }
        }
        Networking.SetOwner(new_owner, megaphone.gameObject);
        Networking.SetOwner(new_owner, gameObject);
        if (local_plyhitbox != null) { local_plyhitbox.network_active = true; } // To-do: failsafe for original owner not having hitbox on transfer. Figure out root cause (alongside hitboxes appearing on join for users in ready room)
    }

    public override void OnOwnershipTransferred(VRCPlayerApi newOwner)
    {
        ply_master_id = Networking.GetOwner(gameObject).playerId;
        // If we are the new owner setup accordingly
        if (newOwner == Networking.LocalPlayer)
        {
            ply_tracking_dict_keys_str = ConvertIntArrayToString(ply_tracking_dict_keys_arr);
            ply_tracking_dict_values_str = ConvertIntArrayToString(ply_tracking_dict_values_arr);
            ply_master_id = Networking.LocalPlayer.playerId;
            ToggleReadyRoomCollisions(true);
            ToggleTrainingRoom(true);

            RequestSerialization();
            RefreshSetupUI();
            /*if (round_state == (int)round_state_name.Ready || round_state == (int)round_state_name.Ongoing)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "RoundEnd", "", -1);
            }*/
        }
        else
        {
            // Reset all item spawners
            if (mapscript_list != null && mapscript_list.Length > 0)
            {
                foreach (ItemSpawner itemSpawner in mapscript_list[0].GetItemSpawnerFromParent(room_training.transform))
                {
                    if (itemSpawner == null || itemSpawner.gameObject == null) { continue; }
                    itemSpawner.item_spawn_powerups_enabled = true;
                    itemSpawner.item_spawn_weapons_enabled = true;
                    itemSpawner.item_spawn_frequency_mul = 1.0f;
                    itemSpawner.item_spawn_duration_mul = 1.0f;
                    itemSpawner.DespawnItem((int)item_sfx_index.ItemExpire, -1, false);
                }
                /*for (int m = 0; m < mapscript_list.Length; m++)
                {
                    foreach (ItemSpawner itemSpawner in mapscript_list[m].map_item_spawns)
                    {
                        if (itemSpawner == null || itemSpawner.gameObject == null) { continue; }
                        itemSpawner.DespawnItem((int)item_sfx_index.ItemExpire, -1, false);
                    }
                }*/
            }
        }

        // Enable the debugger menu if the dev
        if (local_ppp_options != null)
        {
            bool is_dev = Networking.LocalPlayer.displayName.ToLower() == "mintymimix" && newOwner == Networking.LocalPlayer;
            local_ppp_options.debuggerPanel.gameObject.SetActive(is_dev);
        }

        master_in_transit = false;
    }

    public void ChangeGamemode()
    {
        // Function called by the dropdown when the gamemode value changes
        // Set these all to some default value
        int ply_count = 0;
        int[][] plyInGame = GetPlayersInGame();
        if (plyInGame != null && plyInGame[0] != null) { ply_count = plyInGame[0].Length; }
        UnityEngine.Debug.Log("Players in game: " + ply_count);
        int goal_input_a = 0; int goal_input_b = 0;
        option_gamemode = (byte)ui_round_option_dropdown.value;

        
        if (option_gamemode == (int)gamemode_name.Survival)
        {
            goal_input_a = 3;
            ui_round_length_input.text = "240";
            ui_round_length_toggle.isOn = true;
            ui_adv_option_respawn_duration.text = "5";
            if (ui_ply_option_atk.text == "250") { ui_ply_option_atk.text = "100"; } // Reset from KOTH
        }
        else if (option_gamemode == (int)gamemode_name.Clash)
        {
            goal_input_a = 3 + Mathf.FloorToInt((float)ply_count / (float)3.0f);
            if (option_teamplay) { goal_input_a *= Mathf.Max(1, Mathf.RoundToInt(ply_count / (team_count * 3.0f))); }
            ui_round_length_input.text = "240";
            ui_round_length_toggle.isOn = true;
            ui_adv_option_respawn_duration.text = "5";
            if (ui_ply_option_atk.text == "250") { ui_ply_option_atk.text = "100"; } // Reset from KOTH
        }
        else if (option_gamemode == (int)gamemode_name.BossBash)
        {
            goal_input_a = Mathf.FloorToInt((float)Mathf.Pow((float)ply_count, 1.33f));
            goal_input_b = 2 + Mathf.FloorToInt((float)ply_count / (float)10.0f);
            ui_round_length_input.text = "300";
            ui_round_length_toggle.isOn = true;
            ui_adv_option_respawn_duration.text = "5";
            if (ui_ply_option_atk.text == "250") { ui_ply_option_atk.text = "100"; } // Reset from KOTH
        }
        else if (option_gamemode == (int)gamemode_name.Infection)
        {
            goal_input_a = 1 + Mathf.FloorToInt((float)ply_count / (float)8.0f);
            ui_round_length_input.text = "150";
            ui_round_length_toggle.isOn = true;
            ui_adv_option_respawn_duration.text = "5";
            if (ui_ply_option_atk.text == "250") { ui_ply_option_atk.text = "100"; } // Reset from KOTH

        }
        else if (option_gamemode == (int)gamemode_name.KingOfTheHill)
        {
            if (option_teamplay) { goal_input_a = Mathf.RoundToInt(90.0f / Mathf.Max(1, team_count)); }
            else { goal_input_a = Mathf.RoundToInt(Mathf.Lerp(60, 20, Mathf.Min(1.0f, ply_count / 8))); }
            goal_input_b = Mathf.FloorToInt(15 / (float)Mathf.Max(1, ply_count));
            ui_round_length_input.text = "300";
            ui_round_length_toggle.isOn = true;
            ui_adv_option_respawn_duration.text = "17"; // Respawn duration will be higher on KOTH by default
            if (ui_ply_option_atk.text == "100") { ui_ply_option_atk.text = "250"; } 
        }

        else if (option_gamemode == (int)gamemode_name.FittingIn)
        {
            goal_input_a = 400;
            goal_input_b = 100;
            if (mapscript_list != null && map_selected >= 0 && map_selected < mapscript_list.Length && mapscript_list[map_selected].map_name == localizer.FetchText("MAP_NAME_RATS", "Borrowed Space"))
            {
                goal_input_a = 800;
                goal_input_b = 200;
            }
            ui_round_length_input.text = "240";
            ui_round_length_toggle.isOn = true;
            ui_adv_option_respawn_duration.text = "5";
            if (ui_ply_option_atk.text == "250") { ui_ply_option_atk.text = "100"; } // Reset from KOTH
        }
        ui_round_option_goal_input_a.text = goal_input_a.ToString();
        ui_round_option_goal_input_b.text = goal_input_b.ToString();
        ui_adv_option_item_debuff_toggle.isOn = !option_teamplay;
        RoundOptionAdjust();
    }

    public void ResetGameOptionsToDefault()
    {

        if (!Networking.IsOwner(gameObject)) { return; }

        ui_updating = true;

        ui_ply_option_dp.text = "0";
        ui_ply_option_scale.text = "100";
        ui_ply_option_atk.text = "100";
        if (option_gamemode == (int)gamemode_name.KingOfTheHill) { ui_ply_option_atk.text = "250"; }
        ui_ply_option_def.text = "100";
        ui_ply_option_speed.text = "100";
        ui_ply_option_grav.text = "100";

        ui_ply_option_weapon_dropdown.value = (int)weapon_type_name.PunchingGlove;
        ui_ply_option_weapon_toggle.isOn = true;
        ui_ply_option_powerup_toggle.isOn = true;

        ui_adv_option_respawn_duration.text = "3";
        ui_adv_option_boss_scale_mod.text = "400";
        ui_adv_option_boss_atk_mod.text = "50";
        ui_adv_option_boss_def_mod.text = "150";
        ui_adv_option_boss_speed_mod.text = "50";
        ui_adv_option_item_frequency.text = "100";
        ui_adv_option_item_duration.text = "100";
        ui_adv_option_item_debuff_toggle.isOn = !option_teamplay;

        if (option_gamemode == (int)gamemode_name.KingOfTheHill) { ui_round_teamplay_toggle.isOn = true; } // by default, KOTH is a team-based gamemode, but this is not strictly enforced

        ui_updating = false;
        ChangeGamemode();
    }

    public void RoundOptionAdjust()
    {
        if (!Networking.IsOwner(gameObject)) { return; }
        if (round_state != (int)round_state_name.Start) { return; }
        if (!ui_initialized || ui_updating) { return; }

        option_gamemode = (byte)ui_round_option_dropdown.value;


        // Parse the first input as the points goal
        int try_goal_parse = 1;
        Int32.TryParse(ui_round_option_goal_input_a.text, out try_goal_parse);
        try_goal_parse = Mathf.Min(Mathf.Max(try_goal_parse, 1), 65535);
        option_gm_goal = (ushort)try_goal_parse;

        // Then handle additional parameters
        if (option_gamemode == (int)gamemode_name.Survival)
        {
            plysettings_lives = (ushort)try_goal_parse;
            if (option_force_teamplay || option_enforce_team_limits) { ui_round_teamplay_toggle.isOn = false; } // if the previously selected gamemode enforced teamplay, set back to FFA
            option_force_teamplay = false;
            option_enforce_team_limits = false;
        }
        else if (option_gamemode == (int)gamemode_name.BossBash)
        {
            Int32.TryParse(ui_round_option_goal_input_b.text, out try_goal_parse);
            try_goal_parse = Mathf.Min(Mathf.Max(try_goal_parse, 1), 65535);
            plysettings_lives = (ushort)try_goal_parse;

            ui_round_teamplay_toggle.isOn = true;
            ui_round_teamplay_count_input.text = "2";
            option_force_teamplay = true;
            option_enforce_team_limits = true;
            option_team_limits_str = "80,1";
            option_team_limits_arr = ConvertStrToIntArray(option_team_limits_str);
        }
        else if (option_gamemode == (int)gamemode_name.Infection)
        {
            ui_round_teamplay_toggle.isOn = true;
            ui_round_teamplay_count_input.text = "2";
            plysettings_lives = 2;
            option_force_teamplay = true;
            option_enforce_team_limits = true;
            option_team_limits_str = "80," + option_gm_goal;
            option_team_limits_arr = ConvertStrToIntArray(option_team_limits_str);
        }
        else if (option_gamemode == (int)gamemode_name.KingOfTheHill)
        {
            Int32.TryParse(ui_round_option_goal_input_b.text, out try_goal_parse);
            try_goal_parse = Mathf.Min(Mathf.Max(try_goal_parse, 1), 65535);
            option_gm_config_a = (ushort)try_goal_parse;
            ui_round_teamplay_toggle.isOn = true;
            option_force_teamplay = true;
            option_enforce_team_limits = false;
            if (ui_round_teamplay_count_input.text == "1") { ui_round_teamplay_count_input.text = "2"; }
            //option_force_teamplay = option_gamemode == (int)gamemode_name.ENUM_LENGTH; // KOTH is a team-forced gamemode
        }
        else if (option_gamemode == (int)gamemode_name.FittingIn)
        {
            option_gm_goal = (ushort)Mathf.Clamp(try_goal_parse, 101, 65535); // Make sure our goal % is > 100%
            Int32.TryParse(ui_round_option_goal_input_b.text, out try_goal_parse);
            try_goal_parse = Mathf.Clamp(try_goal_parse, 10, 65535); // Make sure our size increase % is > 10%
            option_gm_config_a = (ushort)try_goal_parse;
            if (option_force_teamplay || option_enforce_team_limits) { ui_round_teamplay_toggle.isOn = false; } // if the previously selected gamemode enforced teamplay, set back to FFA
            option_force_teamplay = false;
            option_enforce_team_limits = false;
        }
        else
        {
            if (option_force_teamplay || option_enforce_team_limits) { ui_round_teamplay_toggle.isOn = false; } // if the previously selected gamemode enforced teamplay, set back to FFA
            option_force_teamplay = false;
            option_enforce_team_limits = false;
        }

        option_teamplay = ui_round_teamplay_toggle.isOn;
        option_start_from_master_only = ui_round_master_only_toggle.isOn;
        option_personal_teams = ui_round_teamplay_personal_toggle.isOn;

        int team_input_parse = 1;
        int prev_team_count = team_count;
        Int32.TryParse(ui_round_teamplay_count_input.text, out team_input_parse);
        team_input_parse = Mathf.Min(Mathf.Max(team_input_parse, 1), team_colors.Length, 255);
        if (option_teamplay) 
        { 
            team_count = (byte)team_input_parse;
            if (ui_round_teamplay_count_input.text == "1") { ui_round_teamplay_count_input.text = "2"; }
        }
        else { team_count = 1; }
        if (prev_team_count != team_count)
        {
            PlayerAutoSortTeams();
            ChangeGamemode();
        }


        int try_round_length_parse = Mathf.FloorToInt(round_length);
        Int32.TryParse(ui_round_length_input.text, out try_round_length_parse);
        try_round_length_parse = Mathf.Min(Mathf.Max(try_round_length_parse, 20), 2147483647);
        //UnityEngine.Debug.Log("TIME TO CHANGE THE ROUND LENGTH FROM " + round_length + " TO " + try_round_length_parse);
        round_length = (float)try_round_length_parse;

        round_length_enabled = ui_round_length_toggle.isOn;

        // Starting & Advanced Options Parsing
        int try_option_parse_dp = 0; Int32.TryParse(ui_ply_option_dp.text, out try_option_parse_dp); try_option_parse_dp = Mathf.Min(Mathf.Max(try_option_parse_dp, 0), 99999);
        plysettings_dp = (ushort)try_option_parse_dp;
        int try_option_parse_scale = 1; Int32.TryParse(ui_ply_option_scale.text, out try_option_parse_scale); try_option_parse_scale = Mathf.Min(Mathf.Max(try_option_parse_scale, 1), 1000);
        plysettings_scale = 0.01f * (ushort)try_option_parse_scale;
        int try_option_parse_atk = 1; Int32.TryParse(ui_ply_option_atk.text, out try_option_parse_atk); try_option_parse_atk = Mathf.Min(Mathf.Max(try_option_parse_atk, 1), 1000);
        plysettings_atk = 0.01f * (ushort)try_option_parse_atk;
        int try_option_parse_def = 1; Int32.TryParse(ui_ply_option_def.text, out try_option_parse_def); try_option_parse_def = Mathf.Min(Mathf.Max(try_option_parse_def, 0), 1000);
        plysettings_def = 0.01f * (ushort)try_option_parse_def;
        int try_option_parse_speed = 1; Int32.TryParse(ui_ply_option_speed.text, out try_option_parse_speed); try_option_parse_speed = Mathf.Min(Mathf.Max(try_option_parse_speed, 1), 1000);
        plysettings_speed = 0.01f * (ushort)try_option_parse_speed;
        int try_option_parse_grav = 1; Int32.TryParse(ui_ply_option_grav.text, out try_option_parse_grav); try_option_parse_grav = Mathf.Min(Mathf.Max(try_option_parse_grav, -1000), 1000);
        plysettings_grav = 0.01f * (ushort)try_option_parse_grav;

        plysettings_weapon = ui_ply_option_weapon_dropdown.value;
        plysettings_iweapons = ui_ply_option_weapon_toggle.isOn;
        plysettings_powerups = ui_ply_option_powerup_toggle.isOn;

        int try_option_parse_respawn_duration = 3; Int32.TryParse(ui_adv_option_respawn_duration.text, out try_option_parse_respawn_duration); try_option_parse_respawn_duration = Mathf.Min(Mathf.Max(try_option_parse_respawn_duration, 0), Mathf.RoundToInt(round_length - 1.0f));
        plysettings_respawn_duration = 1.0f * (ushort)try_option_parse_respawn_duration;
        int try_option_parse_boss_scale_mod = 100; Int32.TryParse(ui_adv_option_boss_scale_mod.text, out try_option_parse_boss_scale_mod); try_option_parse_boss_scale_mod = Mathf.Min(Mathf.Max(try_option_parse_boss_scale_mod, 0), 1000);
        plysettings_boss_scale_mod = 0.01f * (ushort)try_option_parse_boss_scale_mod;
        int try_option_parse_boss_atk_mod = 0; Int32.TryParse(ui_adv_option_boss_atk_mod.text, out try_option_parse_boss_atk_mod); try_option_parse_boss_atk_mod = Mathf.Min(Mathf.Max(try_option_parse_boss_atk_mod, 0), 1000);
        plysettings_boss_atk_mod = 0.01f * (ushort)try_option_parse_boss_atk_mod;
        int try_option_parse_boss_def_mod = 0; Int32.TryParse(ui_adv_option_boss_def_mod.text, out try_option_parse_boss_def_mod); try_option_parse_boss_def_mod = Mathf.Min(Mathf.Max(try_option_parse_boss_def_mod, 0), 1000);
        plysettings_boss_def_mod = 0.01f * (ushort)try_option_parse_boss_def_mod;
        int try_option_parse_boss_speed_mod = 150; Int32.TryParse(ui_adv_option_boss_speed_mod.text, out try_option_parse_boss_speed_mod); try_option_parse_boss_speed_mod = Mathf.Min(Mathf.Max(try_option_parse_boss_speed_mod, 0), 1000);
        plysettings_boss_speed_mod = 0.01f * (ushort)try_option_parse_boss_speed_mod;
        int ui_ply_option_item_frequency = 0; Int32.TryParse(ui_adv_option_item_frequency.text, out ui_ply_option_item_frequency); ui_ply_option_item_frequency = Mathf.Min(Mathf.Max(ui_ply_option_item_frequency, 1), 1000);
        plysettings_item_frequency = 0.01f * (ushort)ui_ply_option_item_frequency;
        int ui_ply_option_item_duration = 0; Int32.TryParse(ui_adv_option_item_duration.text, out ui_ply_option_item_duration); ui_ply_option_item_duration = Mathf.Min(Mathf.Max(ui_ply_option_item_duration, 1), 1000);
        plysettings_item_duration = 0.01f * (ushort)ui_ply_option_item_duration;

        plysettings_item_debuff = ui_adv_option_item_debuff_toggle.isOn;

        if (room_ready_script.warning_acknowledged) { room_ready_script.ZoneMarkerHighlight(true); }

        RequestSerialization();
        RefreshSetupUI();
    }

    public void ResetGameOptionsToDefault(bool play_sfx)
    {
        ResetGameOptionsToDefault();
        if (play_sfx) { PlaySFXFromArray(snd_ready_sfx_source, snd_ready_sfx_clips, (int)ready_sfx_name.QueueCancel); }
    }

    public override void OnPlayerJoined(VRCPlayerApi player)
    {
        if (player == null) { return; }

        if (ply_object_owners == null || ply_object_owners.Length < ply_owners_cnt)
        {
            ply_object_owners = new int[(int)GLOBAL_CONST.UDON_MAX_PLAYERS];
            ply_object_plyattr = new PlayerAttributes[(int)GLOBAL_CONST.UDON_MAX_PLAYERS];
            ply_object_plyhitbox = new PlayerHitbox[(int)GLOBAL_CONST.UDON_MAX_PLAYERS];
            ply_object_plyweapon = new PlayerWeapon[(int)GLOBAL_CONST.UDON_MAX_PLAYERS];
            ply_object_secondaryweapon = new PlayerWeapon[(int)GLOBAL_CONST.UDON_MAX_PLAYERS];
            ply_object_uiplytoothers = new UIPlyToOthers[(int)GLOBAL_CONST.UDON_MAX_PLAYERS];
            ply_owners_cnt = 0;
        }
        if (ply_owners_cnt >= (int)GLOBAL_CONST.UDON_MAX_PLAYERS)
        {
            ResetAllObjOwners();
        }

        UnityEngine.Debug.Log("[PLAYER_JOIN_TEST] Player " + player.playerId + " (" + player.displayName + ") joined. ply_owners_cnt = " + ply_owners_cnt);

        bool game_is_functional = true;
        if (ply_owners_cnt >= (int)GLOBAL_CONST.UDON_MAX_PLAYERS) { UnityEngine.Debug.LogError("[OWNERS_TEST] Owners count exceeds maximum udon player limit (100); this game will cease function!"); game_is_functional = false;  }

        bool found_player_in_owner_arr = false;
        for (int i = 0; i < ply_object_owners.Length; i++)
        {
            if (ply_object_owners[i] == player.playerId) { found_player_in_owner_arr = true; break; }
        }
        if (!found_player_in_owner_arr && game_is_functional)
        {
            if (ply_owners_cnt < ply_object_owners.Length) { ply_object_owners[ply_owners_cnt] = player.playerId; }
        }

        var plyWeaponObj = FindPlayerOwnedObject(player, "PlayerWeapon");
        var plySecondaryObj = FindPlayerOwnedObject(player, "SecondaryWeapon");
        var plyAttributesObj = FindPlayerOwnedObject(player, "PlayerAttributes");
        var plyAttributesComponent = plyAttributesObj.GetComponent<PlayerAttributes>();
        var plyHitboxObj = FindPlayerOwnedObject(player, "PlayerHitbox");
        var plyUIToOthers = FindPlayerOwnedObject(player, "UIPlyToOthers");
        var plyUIToSelf = FindPlayerOwnedObject(player, "UIPlyToSelf");
        //var plyUIMessagesToSelf = FindPlayerOwnedObject(player, "UIMessagesToSelf");
        var plyPPPCanvas = FindPlayerOwnedObject(player, "PPPCanvas");
        var plyLandingCircleObj = FindPlayerOwnedObject(player, "PlayerLandingCircle");

        Networking.SetOwner(player, plyAttributesObj);
        if (game_is_functional) { ply_object_plyattr[ply_owners_cnt] = plyAttributesComponent; }
        Networking.SetOwner(player, plyWeaponObj);
        if (game_is_functional) { ply_object_plyweapon[ply_owners_cnt] = plyWeaponObj.GetComponent<PlayerWeapon>(); }
        if (game_is_functional) { ply_object_plyweapon[ply_owners_cnt].owner_attributes = plyAttributesComponent; }
        Networking.SetOwner(player, plySecondaryObj);
        if (game_is_functional) { ply_object_secondaryweapon[ply_owners_cnt] = plySecondaryObj.GetComponent<PlayerWeapon>(); }
        if (game_is_functional) { ply_object_secondaryweapon[ply_owners_cnt].owner_attributes = plyAttributesComponent; }
        Networking.SetOwner(player, plyHitboxObj);
        if (game_is_functional) { ply_object_plyhitbox[ply_owners_cnt] = plyHitboxObj.GetComponent<PlayerHitbox>(); }
        if (game_is_functional) { ply_object_plyhitbox[ply_owners_cnt].owner = player; }
        if (game_is_functional) { ply_object_plyhitbox[ply_owners_cnt].playerAttributes = plyAttributesComponent; }
        Networking.SetOwner(player, plyUIToOthers);
        if (game_is_functional) { ply_object_uiplytoothers[ply_owners_cnt] = plyUIToOthers.GetComponent<UIPlyToOthers>();}
        if (game_is_functional) { ply_object_uiplytoothers[ply_owners_cnt].owner = player; }
        if (game_is_functional) { ply_object_uiplytoothers[ply_owners_cnt].playerAttributes = plyAttributesComponent; }
        Networking.SetOwner(player, plyUIToSelf);
        plyUIToSelf.GetComponent<UIPlyToSelf>().owner = player;
        plyUIToSelf.GetComponent<UIPlyToSelf>().playerAttributes = plyAttributesComponent;
        //Networking.SetOwner(player, plyUIMessagesToSelf);
        //plyUIMessagesToSelf.GetComponent<UIMessagesToSelf>().owner = player;
        Networking.SetOwner(player, plyLandingCircleObj);
        plyLandingCircleObj.GetComponent<PlayerLandingCircle>().owner = player;
        plyLandingCircleObj.GetComponent<PlayerLandingCircle>().playerAttributes = plyAttributesComponent;
        Networking.SetOwner(player, plyPPPCanvas);

        if (player == Networking.LocalPlayer) {
            //networkJoinTime = Networking.GetServerTimeInSeconds();
            local_uiplytoself = plyUIToSelf.GetComponent<UIPlyToSelf>();
            PreallocGlobalObj((int)prealloc_obj_name.UIHarmNumber);
            //local_uiplytoself.local_uimessagestoself = plyUIMessagesToSelf.GetComponent<UIMessagesToSelf>();
            local_plyAttr = plyAttributesComponent;
            if (game_is_functional) { local_plyweapon = ply_object_plyweapon[ply_owners_cnt]; }
            if (local_plyweapon != null) 
            { 
                local_plyweapon.OnDrop(); 
                local_plyweapon.pickup_component.pickupable = true;
            }
            if (game_is_functional) { local_secondaryweapon = ply_object_secondaryweapon[ply_owners_cnt]; }
            if (local_secondaryweapon != null)
            {
                local_secondaryweapon.OnDrop();
                local_secondaryweapon.pickup_component.pickupable = true;
            }
            if (game_is_functional) { local_plyhitbox = ply_object_plyhitbox[ply_owners_cnt]; }
            local_ppp_options = plyPPPCanvas.GetComponent<PPP_Options>();

            local_ppp_options.ColorblindTemplateInit(ref local_ppp_options.template_colorblind_flag, ref local_ppp_options.ui_colorblind_grid, ref local_ppp_options.ui_colorblind_dropdown_caption, ref local_ppp_options.colorblind_flags);
            local_ppp_options.ColorblindTemplateInit(ref room_ready_script.template_colorblind_flag, ref room_ready_script.ui_colorblind_grid, ref room_ready_script.ui_colorblind_dropdown_caption, ref room_ready_script.colorblind_flags);

            //local_ppp_options.RefreshAllOptions();

            template_ItemSpawner = FindPlayerOwnedObject(player, "ItemSpawnerTemplate");

            if (Networking.IsOwner(gameObject)) { ui_round_mapselect.BuildMapList(); SetupMap(); }
        }

        plyAttributesComponent.SetupTutorialMessages();

        plyWeaponObj.SetActive(false);
        plySecondaryObj.SetActive(false);
        plyHitboxObj.SetActive(false);
        plyPPPCanvas.SetActive(false);
        if (Networking.LocalPlayer == player)
        {
            plyUIToOthers.SetActive(false);
            plyUIToSelf.SetActive(true);
            plyLandingCircleObj.SetActive(true);
            //plyUIMessagesToSelf.SetActive(true);
            plyPPPCanvas.SetActive(true);
            if (game_is_functional) { ply_object_plyhitbox[ply_owners_cnt].ToggleHitbox(false); }
            ForceResetHitboxes();
        }
        else
        {
            plyUIToOthers.SetActive(true);
            plyUIToSelf.SetActive(false);
            plyLandingCircleObj.SetActive(false);
            //plyUIMessagesToSelf.SetActive(false);
        }


        if (!(player == Networking.LocalPlayer && Networking.IsOwner(gameObject)) && DictIndexFromKey(player.playerId, ply_tracking_dict_keys_arr) < 0)
        { DictAddEntry(player.playerId, (int)player_tracking_name.Unassigned, ref ply_tracking_dict_keys_arr, ref ply_tracking_dict_values_arr); }
        else
        {
            UnityEngine.Debug.Log("[PLAYER_JOIN_TEST] New player (" + player.playerId + ") just dropped! Let's add them to the dictionary! " + ply_tracking_dict_keys_str);
        }

        if (Networking.IsOwner(gameObject))
        {
            ply_tracking_dict_keys_str = ConvertIntArrayToString(ply_tracking_dict_keys_arr);
            ply_tracking_dict_values_str = ConvertIntArrayToString(ply_tracking_dict_values_arr);
            //ui_round_team_panel.CreateNewPanel(ply_tracking_dict_keys_arr.Length - 1);
            ply_master_id = Networking.LocalPlayer.playerId;
            //(int)GLOBAL_CONST.TICK_RATE_MS =  Mathf.Clamp(ply_tracking_dict_keys_arr.Length * 10.0f, (int)GLOBAL_CONST.TICK_RATE_MS, 1000.0f);
            RequestSerialization();
        }
        else
        {
            // Setup placeholder arrays until we receive data from the new ones
            if (ply_tracking_dict_keys_arr == null) { ply_tracking_dict_keys_arr = new int[VRCPlayerApi.GetPlayerCount()]; }
            if (ply_tracking_dict_values_arr == null) { ply_tracking_dict_values_arr = new int[VRCPlayerApi.GetPlayerCount()]; }
        }
        UnityEngine.Debug.Log("[PLAYER_JOIN_TEST] New player (" + player.playerId + ") is now in the dictionary! " + ply_tracking_dict_keys_str);
        if (Networking.LocalPlayer == player) { wait_for_sync_for_player_join = true; }

        ply_owners_cnt++;
        UnityEngine.Debug.Log("[PLAYER_JOIN_TEST] Join complete for " + player.displayName + ". ply_owners_cnt = " + ply_owners_cnt + " [" + ConvertIntArrayToString(ply_object_owners) + "]");

    }

    public override void OnPlayerLeft(VRCPlayerApi player)
    {
        RemovePlayerFromObjOwners(player.playerId);
        var objects = Networking.GetPlayerObjects(player);
        for (int i = 0; i < objects.Length; i++)
        {
            if (!Utilities.IsValid(objects[i])) continue;
            if (Utilities.IsValid(objects[i]))
            {
                Destroy(objects[i]);
            }
        }
        if (Networking.IsOwner(gameObject))
        {
            //ui_round_team_panel.RemovePanel(DictIndexFromKey(player.playerId, ply_tracking_dict_keys_arr));
            DictRemoveEntry(player.playerId, ref ply_tracking_dict_keys_arr, ref ply_tracking_dict_values_arr);
            ply_tracking_dict_keys_str = ConvertIntArrayToString(ply_tracking_dict_keys_arr);
            ply_tracking_dict_values_str = ConvertIntArrayToString(ply_tracking_dict_values_arr);
            ply_master_id = Networking.LocalPlayer.playerId;
            UnityEngine.Debug.Log("Host disconnected; changing host to self: " + Networking.LocalPlayer.playerId);
            ChangeHost(Networking.LocalPlayer.playerId);
            //(int)GLOBAL_CONST.TICK_RATE_MS = Mathf.Clamp(ply_tracking_dict_keys_arr.Length * 10.0f, (int)GLOBAL_CONST.TICK_RATE_MS, 1000.0f);
            RequestSerialization();
            RefreshSetupUI();
        }

    }
    public override void OnPlayerRespawn(VRCPlayerApi player)
    {
        // Enable collision on ready room
        if (!player.isLocal) { return; }
        platformHook.custom_force_unhook = false;
        if (local_plyAttr != null)
        {
            local_plyAttr.ply_scale = 1.0f;
            local_plyAttr.plyEyeHeight_lerp_start_ms = Networking.GetServerTimeInSeconds();
            local_plyAttr.plyEyeHeight_desired = local_plyAttr.plyEyeHeight_default;
            local_plyAttr.plyEyeHeight_change = true;
            local_plyAttr.ply_training = false;
            local_plyAttr.in_spectator_area = false;
            local_plyAttr.in_ready_room = true;
            //local_plyAttr.air_thrust_enabled = false;
            local_plyAttr.ResetPowerups();
            local_plyAttr.ResetToDefaultStats();
            if (local_plyweapon != null) { local_plyweapon.ResetWeaponToDefault(); }
            if (local_secondaryweapon != null) { local_secondaryweapon.ResetWeaponToDefault(); }
        }

        if (local_plyAttr != null) {
            if (local_plyAttr.ply_team == (int)player_tracking_name.Spectator) { local_plyAttr.ply_state = (int)player_state_name.Spectator; }
            else if (local_plyAttr.ply_state != (int)player_state_name.Spectator) {
                local_plyAttr.ply_team = (int)player_tracking_name.Unassigned;
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "ChangeTeam", Networking.LocalPlayer.playerId, (int)player_tracking_name.Unassigned, false);
                local_plyAttr.ply_state = (int)player_state_name.Inactive;
                if (room_ready_script.warning_acknowledged) { room_ready_script.ZoneMarkerHighlight(false); }
            }
            else 
            {
                local_plyAttr.ply_team = (int)player_tracking_name.Spectator;
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "ChangeTeam", Networking.LocalPlayer.playerId, (int)player_tracking_name.Spectator, false);
                if (room_ready_script.warning_acknowledged) { room_ready_script.ZoneMarkerHighlight(true); }
            }
        }
        if (local_plyweapon != null && local_plyweapon.pickup_component != null) { local_plyweapon.pickup_component.Drop(); local_plyweapon.gameObject.SetActive(false); }
        if (local_secondaryweapon != null && local_secondaryweapon.pickup_component != null) { local_secondaryweapon.pickup_component.Drop(); local_secondaryweapon.gameObject.SetActive(false); }
        if (!(mapscript_list == null || map_selected >= mapscript_list.Length || map_selected < 0)) { mapscript_list[map_selected].room_spectator_area.SetActive(false); }
        
        room_ready_script.gameObject.GetComponent<Collider>().enabled = true;
        ToggleReadyRoomCollisions(true);
        ToggleTrainingRoom(false);
        room_training_portal.SetActive(true);
    }

    public void ToggleReadyRoomCollisions(bool toggle)
    {
        // Since the ready room no longer dangles over everyone, we can just toggle the ready room's primary collider
        // This also prevents us from accidentally disabling the canvas colliders (which would prevent interacting with them!)
        room_ready_script.trigger_player.enabled = toggle;

        // If we aren't the host, we can easily cull the ready room as needed
        if (!Networking.IsOwner(gameObject)) 
        { 
            room_ready_should_render = toggle;
            room_ready_script.gameObject.SetActive(toggle);
        }
        // If we are the host, however, we'll need to cull only if the game is ongoing and we are a participant. Otherwise, all functionality must remain in-tact.
        else if (round_state == (int)round_state_name.Ongoing && local_plyAttr != null && local_plyAttr.ply_team >= 0 && !local_plyAttr.ply_training && (local_plyAttr.ply_state == (byte)player_state_name.Alive || local_plyAttr.ply_state == (byte)player_state_name.Respawning))
        {
            room_ready_should_render = toggle;
            room_ready_script.gameObject.SetActive(toggle);
        }
        else
        {
            room_ready_should_render = true;
            if (!room_ready_script.gameObject.activeInHierarchy) { room_ready_script.gameObject.SetActive(true); }
        }

    }

    public void ToggleTrainingRoom(bool toggle)
    {
        // If we aren't the host, we can easily cull the training room as needed
        if (!Networking.IsOwner(gameObject))
        {
            room_training.SetActive(toggle);
        }
        // If we are the host, however, it must always remain enabled in the interest of synchronization.
        else if (!room_training.activeInHierarchy)
        { 
           room_training.gameObject.SetActive(true); 
        }
    }

    // -- Round Management --
    public void SetupMap()
    {
        if (map_selected < 0) { RefreshSetupUI(); return; }

        if ((round_state == (int)round_state_name.Queued && !Networking.IsOwner(gameObject)) || round_state == (int)round_state_name.Loading)
        {
            Color mapColor = new Color(0.09803922f, 0.8862745f, 0.5254902f, 1.0f);
            AddToLocalTextQueue(localizer.FetchText("NOTIFICATION_ROUNDSTART_MAP", "You're Going To:"), Color.white, load_length);
            AddToLocalTextQueue(mapscript_list[map_selected].map_name.Replace("\n", ""), mapColor, load_length);
            PlaySFXFromArray(snd_ready_sfx_source, snd_ready_sfx_clips, (int)ready_sfx_name.LoadStart);
            vopack_selected.PlayVoiceover((int)voiceover_event_name.Round, (int)voiceover_round_sfx_name.MapAnnounce);

            if (local_plyAttr != null)
            {
                if (local_plyAttr.ply_team >= 0 || local_plyAttr.ply_team == (int)player_tracking_name.WaitingForLobby)
                {
                    room_training_portal.SetActive(false);
                    if (local_plyAttr.ply_training)
                    {
                        local_plyAttr.ply_training = false;
                        TeleportLocalPlayerToReadyRoom();
                    }
                }
                else if (local_plyAttr.in_spectator_area)
                {
                    TeleportLocalPlayerToReadyRoom();
                }
            }
        }

        for (int i = 0; i < mapscript_list.Length; i++)
        {
            if (i == map_selected) { mapscript_list[i].gameObject.SetActive(true); }
            else { mapscript_list[i].gameObject.SetActive(false); }
        }

        if (map_selected >= 0) { skybox.mainTexture = mapscript_list[map_selected].skybox_tex; }
        else { skybox.mainTexture = default_skybox_tex; }

        // Old code for when the ready room itself was teleporting around
        //Vector3 plyPosRelativeToReadyRoom = Networking.LocalPlayer.GetPosition() - room_ready_script.gameObject.transform.position;
        //room_ready_script.gameObject.transform.position = mapscript_list[map_selected].map_readyroom_center.position; 
        //platformHook.custom_force_unhook = true;
        //Networking.LocalPlayer.TeleportTo(mapscript_list[map_selected].map_readyroom_center.position + plyPosRelativeToReadyRoom, Networking.LocalPlayer.GetRotation());
        //platformHook.custom_force_unhook = false;

        snd_game_music_source.transform.position = mapscript_list[map_selected].transform.position;
        snd_game_music_source.maxDistance = mapscript_list[map_selected].map_snd_radius;
        snd_game_music_source.minDistance = snd_game_music_source.maxDistance;
        for (int i = 0; i < snd_game_sfx_sources.Length; i++)
        {
            snd_game_sfx_sources[i].transform.position = mapscript_list[map_selected].transform.position;
            snd_game_sfx_sources[i].maxDistance = mapscript_list[map_selected].map_snd_radius;
            snd_game_sfx_sources[i].minDistance = snd_game_sfx_sources[i].maxDistance;
        }

        // Refresh item spawn chances
        if (mapscript_list != null && map_selected >= 0 && map_selected < mapscript_list.Length && mapscript_list[map_selected].map_item_spawns != null && mapscript_list[map_selected].map_item_spawns.Length > 0)
        {
            foreach (ItemSpawner itemSpawner in mapscript_list[map_selected].map_item_spawns)
            {
                if (itemSpawner == null || itemSpawner.gameObject == null) { continue; }
                itemSpawner.item_spawn_powerups_enabled = plysettings_powerups;
                itemSpawner.item_spawn_weapons_enabled = plysettings_iweapons;
                itemSpawner.item_spawn_frequency_mul = plysettings_item_frequency;
                itemSpawner.item_spawn_duration_mul = plysettings_item_duration;
                itemSpawner.SetSpawnChances();
            }
        }

        if (mapscript_list != null && map_selected >= 0 && map_selected < mapscript_list.Length && mapscript_list[map_selected].map_capturezones != null && mapscript_list[map_selected].map_capturezones.Length > 0)
        {
            // KOTH Capture Zone handling
            foreach (CaptureZone capturezone in mapscript_list[map_selected].map_capturezones)
            {
                if (capturezone == null || capturezone.gameObject == null) { continue; }
                capturezone.gameObject.SetActive(option_gamemode == (int)gamemode_name.KingOfTheHill);
            }
        }

        map_selected_local = map_selected;

        // Setup gamemode-specific objects
        SetupGamemodeMapObjects();

        //if (mapscript_list[map_selected].voice_distance >= 0)
        //{
        //voice_distance_near = mapscript_list[map_selected].voice_distance;
        //voice_distance_far = (1 + voice_distance_near) * 25;
        AdjustVoiceRange();
        //}

        if (mapscript_list != null && map_selected >= 0 && map_selected < mapscript_list.Length && mapscript_list[map_selected].room_spectator_area != null) { mapscript_list[map_selected].room_spectator_area.SetActive(false); }

        RefreshSetupUI();
    }

    public void SetupGamemodeMapObjects()
    {
        if (map_selected >= 0 && mapscript_list != null && map_selected < mapscript_list.Length)
        {
            map_element_gamemode_specific[] gm_specific_list = mapscript_list[map_selected].map_gm_specific_list;
            if (gm_specific_list != null && gm_specific_list.Length > 0)
            {
                for (int i = 0; i < gm_specific_list.Length; i++)
                {
                    if (gm_specific_list[i].gamemode_enabled != null && option_gamemode >= 0 && option_gamemode < gm_specific_list[i].gamemode_enabled.Length && gm_specific_list[i].gamemode_enabled[option_gamemode])
                    {
                        gm_specific_list[i].gameObject.SetActive(true);
                    }
                    else
                    {
                        gm_specific_list[i].gameObject.SetActive(false);
                    }
                }
            }
        }
    }

    // This function gets called only AFTER a client's team arrays have been fully synced up.
    public void LocalRoundStart()
    {
        restrict_map_change = false;
        int[][] ply_parent_arr = GetPlayersInGame();
        UnityEngine.Debug.Log("[DICT_TEST]: ROUND START - " + ConvertIntArrayToString(ply_parent_arr[0]) + " | " + ConvertIntArrayToString(ply_parent_arr[1]));

        // We need to remove the extended map BEFORE we teleport the player
        if (ply_parent_arr[0].Length >= mapscript_list[map_selected].min_players_to_extend_room && mapscript_list[map_selected].room_game_extended != null) { mapscript_list[map_selected].room_game_extended.SetActive(true); }
        else if (ply_parent_arr[0].Length < mapscript_list[map_selected].min_players_to_extend_room && mapscript_list[map_selected].room_game_extended != null) { mapscript_list[map_selected].room_game_extended.SetActive(false); }
        SetupGamemodeMapObjects();

        //if (option_enforce_team_limits && mapscript_list[map_selected].room_game_asymmetrical_blocker != null) { mapscript_list[map_selected].room_game_asymmetrical_blocker.SetActive(true); }
        //else if (!option_enforce_team_limits && mapscript_list[map_selected].room_game_asymmetrical_blocker != null) { mapscript_list[map_selected].room_game_asymmetrical_blocker.SetActive(false); }

        var gamemode_description = ModifyModeDescription(round_option_descriptions[option_gamemode]);
        var personal_description = "";
        var player_description = "";

        // -- Local Only Below --
        if (GetGlobalTeam(Networking.LocalPlayer.playerId) >= 0)
        {
            // Failsafes in case locally setting active/inactive fails. Inefficient, but the alternative is a game where someone is permanently invulnerable
            //local_plyweapon.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "ToggleActive", true);
            //local_plyhitbox.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "ToggleHitbox", true);
            local_plyweapon.ToggleActive(true);
            local_plyhitbox.ToggleHitbox(true);
            //local_secondaryweapon.ToggleActive(false);

            //room_training.SetActive(false);
            room_training_portal.SetActive(false);
            // To-do: IsNetworkSettled / IsClogged check for this player data
            PlayerAttributes playerData = FindPlayerAttributes(Networking.LocalPlayer);
            playerData.ply_training = false;
            //playerData.ResetTutorialMessage();

            playerData.ply_deaths = 0;
            playerData.ply_dp = plysettings_dp;
            playerData.ply_dp_default = plysettings_dp;
            playerData.ply_damage_dealt = 0;
            playerData.ply_lives = plysettings_lives;
            playerData.ply_points = plysettings_points;
            playerData.ply_respawn_duration = plysettings_respawn_duration;
            playerData.ply_scale = plysettings_scale;
            playerData.ply_speed = plysettings_speed;
            playerData.ply_atk = plysettings_atk;
            playerData.ply_def = plysettings_def;
            playerData.ply_grav = plysettings_grav * mapscript_list[map_selected].map_gravity_scale;
            playerData.ply_desktop_applied_dual_compensation_buff = false;
            playerData.killstreak = 0;
            playerData.killbo = 0;
            playerData.killbo_timer = 0.0f;
            playerData.combo_receive = 0;
            playerData.combo_send = 0;
            playerData.combo_send_timer = 0.0f;
            playerData.combo_send_duration = 2.0f;
            playerData.ply_state = (int)player_state_name.Alive;
            playerData.ply_team = GetGlobalTeam(Networking.LocalPlayer.playerId);
            if (room_ready_script.warning_acknowledged) { room_ready_script.ZoneMarkerHighlight(true); }
            playerData.air_thrust_enabled = true;

            if (option_gamemode == (int)gamemode_name.BossBash && playerData.ply_team == 1)
            {
                //playerData.ply_lives = option_goal_value_b;
                playerData.ply_scale = plysettings_scale * plysettings_boss_scale_mod;
                playerData.ply_atk = plysettings_atk + plysettings_boss_atk_mod; // (ply_parent_arr[0].Length / 4.0f);
                playerData.ply_def = plysettings_def + plysettings_boss_def_mod; //+ Mathf.Max(0.0f, -0.2f + (ply_parent_arr[0].Length * 0.2f));
                playerData.ply_speed = plysettings_speed + plysettings_boss_speed_mod;
                personal_description = localizer.FetchText("NOTIFICATION_ROUNDSTART_BOSSBASH_BOSS", "You are THE BIG BOSS! Crush everyone who stands in your way!");
                //vopack_selected.PlayVoiceover((int)voiceover_event_name.Tutorial, (int)voiceover_tutorial_sfx_name.Mode_BossBash_B);
                if (local_secondaryweapon != null && Networking.LocalPlayer.IsUserInVR())
                {
                    playerData.ply_dual_wield = true;
                    local_secondaryweapon.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "ToggleActive", true);
                    //secondaryWeapon.ToggleActive(true);
                }
            }
            else
            {
                if (option_gamemode == (int)gamemode_name.BossBash)
                {
                    personal_description = localizer.FetchText("NOTIFICATION_ROUNDSTART_BOSSBASH_TINY", "You are a Tiny Trooper! Work together and defeat $ARG0!", "$BOSS");
                    //vopack_selected.PlayVoiceover((int)voiceover_event_name.Tutorial, (int)voiceover_tutorial_sfx_name.Mode_BossBash_A);
                }
                else if (option_gamemode == (int)gamemode_name.Infection)
                {
                    if (playerData.ply_team == 1)
                    {
                        personal_description = localizer.FetchText("NOTIFICATION_ROUNDSTART_INFECTION_INFECTED", "You are an Infected!");
                        playerData.infection_special = 1;
                        playerData.InfectionStatReset(false);
                        //vopack_selected.PlayVoiceover((int)voiceover_event_name.Tutorial, (int)voiceover_tutorial_sfx_name.Mode_Infection_B);

                    }
                    else
                    {
                        personal_description = localizer.FetchText("NOTIFICATION_ROUNDSTART_INFECTION_SURVIVOR", "You are a Survivor!");
                        //vopack_selected.PlayVoiceover((int)voiceover_event_name.Tutorial, (int)voiceover_tutorial_sfx_name.Mode_Infection_A);
                    }
                    playerData.ply_lives = 2; // We set lives to 2 so that you always have at least one left upon death (although this should never decrement, it never hurts to be safe)
                }
            }

            bool is_boss = (option_gamemode == (int)gamemode_name.BossBash && playerData.ply_team == 1);
            if (local_plyweapon == null || local_secondaryweapon == null)
            {
                local_plyweapon = GetPlayerWeaponFromID(Networking.LocalPlayer.playerId);
                local_secondaryweapon = GetSecondaryWeaponFromID(Networking.LocalPlayer.playerId);
            }
            if (local_plyweapon != null)
            {
                if (is_boss)
                {
                    gamemode_boss_id = Networking.LocalPlayer.playerId;
                    local_plyweapon.weapon_type_default = (int)weapon_type_name.BossGlove;
                    local_plyweapon.weapon_type = local_plyweapon.weapon_type_default;
                }
                else
                {
                    local_plyweapon.weapon_type_default = plysettings_weapon;
                    local_plyweapon.weapon_type = local_plyweapon.weapon_type_default;
                }
                //if (local_plyweapon.weapon_type_default != (int)weapon_type_name.PunchingGlove)
                //{
                local_plyweapon.weapon_temp_ammo = -1;
                local_plyweapon.weapon_temp_duration = -1;
                //}
                local_plyweapon.UpdateStatsFromWeaponType();
                local_plyweapon.CacheWeaponPos(true);
                is_boss = is_boss || local_plyweapon.weapon_type == (int)weapon_type_name.BossGlove;
            }
            if (local_secondaryweapon != null)
            {
                if (is_boss)
                {
                    gamemode_boss_id = Networking.LocalPlayer.playerId;
                    local_secondaryweapon.weapon_type_default = (int)weapon_type_name.BossGlove;
                    local_secondaryweapon.weapon_type = local_plyweapon.weapon_type_default;
                }
                else
                {
                    local_secondaryweapon.weapon_type_default = plysettings_weapon;
                    local_secondaryweapon.weapon_type = local_secondaryweapon.weapon_type_default;
                }
                //if (local_secondaryweapon.weapon_type_default != (int)weapon_type_name.PunchingGlove)
                //{
                local_secondaryweapon.weapon_temp_ammo = -1;
                local_secondaryweapon.weapon_temp_duration = -1;
                //}
                is_boss = is_boss || local_secondaryweapon.weapon_type == (int)weapon_type_name.BossGlove;
                if (local_secondaryweapon.gameObject.activeInHierarchy) { local_secondaryweapon.UpdateStatsFromWeaponType(); }
            }

            if (is_boss && Networking.LocalPlayer.IsUserInVR()) { playerData.ply_dual_wield = true; }
            else { playerData.ply_dual_wield = false; }

            playerData.plyEyeHeight_desired = playerData.plyEyeHeight_default * playerData.ply_scale;
            playerData.plyEyeHeight_lerp_start_ms = Networking.GetServerTimeInSeconds();
            playerData.plyEyeHeight_change = true;

            if (local_uiplytoself != null)
            {
                local_uiplytoself.ResetCache();
                local_uiplytoself.ForceUpdateAllUI();
            }

            if (option_teamplay) { TeleportLocalPlayerToGameSpawnZone(-1, true); }
            ToggleReadyRoomCollisions(false);
            ToggleTrainingRoom(false);

            //if (option_gamemode == (int)gamemode_name.Survival) { vopack_selected.PlayVoiceover((int)voiceover_event_name.Tutorial, (int)voiceover_tutorial_sfx_name.Mode_Survival); }
            //else if (option_gamemode == (int)gamemode_name.Clash) { vopack_selected.PlayVoiceover((int)voiceover_event_name.Tutorial, (int)voiceover_tutorial_sfx_name.Mode_Clash); }
            //else if (option_gamemode == (int)gamemode_name.FittingIn) { vopack_selected.PlayVoiceover((int)voiceover_event_name.Tutorial, (int)voiceover_tutorial_sfx_name.Mode_FittingIn); }
            //else if (option_gamemode == (int)gamemode_name.KingOfTheHill) { vopack_selected.PlayVoiceover((int)voiceover_event_name.Tutorial, (int)voiceover_tutorial_sfx_name.Mode_KingOfTheHill); }
        }

        // -- Local Only End (In Loop) --

        for (var i = 0; i < ply_parent_arr[0].Length; i++)
        {
            var player = VRCPlayerApi.GetPlayerById(ply_parent_arr[0][i]);
            if (player == null) { continue; }

            PlayerWeapon plyWeapon = GetPlayerWeaponFromID(player.playerId);
            PlayerHitbox plyHitbox = GetPlayerHitboxFromID(player.playerId);
            PlayerWeapon secondaryWeapon = GetSecondaryWeaponFromID(player.playerId);
            GameObject plyWeaponObj;
            if (plyWeapon == null) { plyWeaponObj = FindPlayerOwnedObject(player, "PlayerWeapon"); plyWeapon = plyWeaponObj.GetComponent<PlayerWeapon>(); }
            else { plyWeaponObj = plyWeapon.gameObject; }
            GameObject plyHitboxObj;
            if (plyHitbox == null) { plyHitboxObj = FindPlayerOwnedObject(player, "PlayerHitbox"); plyHitbox = plyHitboxObj.GetComponent<PlayerHitbox>(); }
            else { plyHitboxObj = plyHitbox.gameObject; }
            GameObject plySecondaryObj;
            if (secondaryWeapon == null) { plySecondaryObj = FindPlayerOwnedObject(player, "SecondaryWeapon"); secondaryWeapon = plySecondaryObj.GetComponent<PlayerWeapon>(); }
            else { plySecondaryObj = secondaryWeapon.gameObject; }

            plyWeaponObj.SetActive(true);
            plyHitboxObj.SetActive(true);
            plySecondaryObj.SetActive(false);
            secondaryWeapon.network_active = false;

            if (option_gamemode == (int)gamemode_name.BossBash && ply_parent_arr[1][i] == 1)
            {
                gamemode_boss_id = player.playerId;
                plyWeapon.weapon_type_default = (int)weapon_type_name.BossGlove;
                plyWeapon.weapon_type = plyWeapon.weapon_type_default;
                secondaryWeapon.weapon_type_default = (int)weapon_type_name.BossGlove;
                secondaryWeapon.weapon_type = secondaryWeapon.weapon_type_default;
                //ply_dual_wield
            }

            UIPlyToOthers plytoothers = GetUIPlyToOthersFromID(player.playerId);
            if (plytoothers != null) { plytoothers.ResetCache(); }

            if (player.isLocal) {
                if (!option_teamplay) { TeleportLocalPlayerToGameSpawnZone(i % mapscript_list[map_selected].map_spawnzones.Length); }
                continue; 
            }
        }

        if (local_plyAttr.ply_team < 0) { room_training_portal.SetActive(true); }

        music_clip_ts = 0.0f;
        if (option_gamemode == (int)gamemode_name.BossBash)
        {
            PlaySFXFromArray(snd_game_music_source, snd_boss_music_clips, -1, 1, true);
        }
        else if (option_gamemode == (int)gamemode_name.Infection)
        {
            PlaySFXFromArray(snd_game_music_source, snd_infection_music_clips, (int)infection_music_name.Start, 1, true);
        }
        else
        {
            PlaySFXFromArray(snd_game_music_source, mapscript_list[map_selected].snd_game_music_clips, -1, 1, true);
        }

        if (option_gamemode == (int)gamemode_name.Survival) { vopack_selected.PlayVoiceover((int)voiceover_event_name.Round, (int)voiceover_round_sfx_name.Mode_Survival); }
        else if (option_gamemode == (int)gamemode_name.Clash) { vopack_selected.PlayVoiceover((int)voiceover_event_name.Round, (int)voiceover_round_sfx_name.Mode_Clash); }
        else if (option_gamemode == (int)gamemode_name.BossBash) { vopack_selected.PlayVoiceover((int)voiceover_event_name.Round, (int)voiceover_round_sfx_name.Mode_BossBash); }
        else if (option_gamemode == (int)gamemode_name.Infection) { vopack_selected.PlayVoiceover((int)voiceover_event_name.Round, (int)voiceover_round_sfx_name.Mode_Infection); }
        else if (option_gamemode == (int)gamemode_name.FittingIn) { vopack_selected.PlayVoiceover((int)voiceover_event_name.Round, (int)voiceover_round_sfx_name.Mode_FittingIn); }
        else if (option_gamemode == (int)gamemode_name.KingOfTheHill) { vopack_selected.PlayVoiceover((int)voiceover_event_name.Round, (int)voiceover_round_sfx_name.Mode_KingOfTheHill); }

        //Networking.LocalPlayer.Immobilize(false);

        int team_to_search = GetGlobalTeam(Networking.LocalPlayer.playerId);
        Color color_team = Color.white; Color color_personal = Color.white;
        if (option_teamplay && team_colors_bright != null && team_to_search < team_colors_bright.Length && team_to_search >= 0) { color_personal = (Color)team_colors_bright[Mathf.Max(0, team_to_search)]; }
        if (option_teamplay && option_gamemode != (int)gamemode_name.BossBash && team_to_search >= 0)
        {
            player_description = localizer.FetchText("NOTIFICATION_ROUNDSTART_TEAM", "Your team: $ARG0", "");
            if (option_gamemode == (int)gamemode_name.Infection) { team_to_search = 1; player_description = localizer.FetchText("NOTIFICATION_ROUNDSTART_INFECTION_TEAM", "The Infected are: $ARG0", ""); }
            int[] players_on_team = DictFindAllWithValue(team_to_search, ply_tracking_dict_keys_arr, ply_tracking_dict_values_arr, (int)dict_compare_name.Equals)[0];
            string players_on_team_names = "";
            for (int i = 0; i < players_on_team.Length; i++)
            {
                var player = VRCPlayerApi.GetPlayerById(players_on_team[i]);
                if (player == null) { continue; }
                if (i > 0) { players_on_team_names += ", "; }
                players_on_team_names += player.displayName;
            }
            player_description += players_on_team_names;
        }
        if (option_teamplay && team_colors_bright != null && team_to_search < team_colors_bright.Length && team_to_search >= 0) { color_team = (Color)team_colors_bright[Mathf.Max(0, team_to_search)]; }

        AddToLocalTextQueue(round_option_names[option_gamemode], Color.yellow, ready_length);
        AddToLocalTextQueue(gamemode_description, Color.white, ready_length);

        if (VRCPlayerApi.GetPlayerById(gamemode_boss_id) != null) { personal_description = personal_description.Replace("$BOSS", VRCPlayerApi.GetPlayerById(gamemode_boss_id).displayName); }
        if (personal_description.Length > 0) { AddToLocalTextQueue(personal_description, color_personal, ready_length); }
        if (player_description.Length > 0) { AddToLocalTextQueue(player_description, color_team, ready_length); }

        // KOTH Capture Zone handling
        if (option_gamemode == (int)gamemode_name.KingOfTheHill)
        {
            foreach (CaptureZone capturezone in mapscript_list[map_selected].map_capturezones)
            {
                if (capturezone == null || capturezone.gameObject == null) { continue; }
                capturezone.gameObject.SetActive(ply_parent_arr[0].Length >= capturezone.min_players);

                if (!Networking.IsOwner(gameObject)) { continue; }
                double server_ms = Networking.GetServerTimeInSeconds();
                if (option_teamplay)
                {
                    // If we have teams on, the leaderboard will be a list of team IDs
                    capturezone.dict_points_keys_arr = new int[team_count];
                    capturezone.dict_points_values_arr = new int[team_count];
                    //koth_respawn_wave_start_ms = new double[team_count];
                    //koth_respawn_wave_duration = new double[team_count];
                    for (int i = 0; i < team_count; i++)
                    {
                        capturezone.dict_points_keys_arr[i] = i;
                        capturezone.dict_points_values_arr[i] = 0;
                        //koth_respawn_wave_start_ms[i] = server_ms;
                        //koth_respawn_wave_duration[i] = plysettings_respawn_duration;
                    }
                }
                else
                {
                    capturezone.dict_points_keys_arr = ply_parent_arr[0];
                    capturezone.dict_points_values_arr = ply_parent_arr[1];
                    // koth_respawn_wave_start_ms = new double[ply_parent_arr[0].Length];
                    //koth_respawn_wave_duration = new double[team_count];
                    for (int i = 0; i < ply_parent_arr[0].Length; i++)
                    {
                        //koth_respawn_wave_start_ms[i] = server_ms;
                        //koth_respawn_wave_duration[i] = plysettings_respawn_duration;
                    }
                }
                capturezone.ResetZone();
                capturezone.RequestSerialization(); // Continous sync probably doesn't require this, but just in case
                koth_respawn_wave_start_ms = server_ms;
                koth_respawn_wave_duration = plysettings_respawn_duration;
            }
        }

        // Reset the highlight cameras 
        for (int i = 0; i < highlight_cameras_snapped.Length; i++)
        {
            highlight_cameras_resetting = true;
            highlight_cameras_active[i] = false;
            highlight_cameras_snapped[i] = false;
            highlight_cameras_waiting_on_sync[i] = false;
            // Ensure they don't have photos from the previous round and that they're reset after
            SnapHighlightPhoto(i, Vector3.zero, Quaternion.LookRotation(Vector3.down), Vector3.zero, Vector3.zero, false, 1.0f);
        }
        highlight_cameras_active_cnt = 0;

        room_spectator_portal.SetActive(true);
        ui_highlightcanvas.SetActive(false);
        ui_spectatorcanvas.SetActive(true);

        // -- Master Handling Below --
        if (!Networking.IsOwner(gameObject)) { return; }
        round_state = (int)round_state_name.Ready;
        round_start_ms = Networking.GetServerTimeInSeconds();
        largest_ply_scale = plysettings_scale;
        infection_zombig_spawn_time = 0.0d;
        infection_zombigs_spawned = 0;
        infection_zombig_active = 0;
        round_extra_data = 0;

        RequestSerialization();
    }

    public void SetupMapSpecificParams()
    {
        if (Networking.IsOwner(gameObject))
        {
            if (map_selected >= mapscript_list.Length || map_selected < 0) { map_selected = 0; }
            // If we're on Facing Worlds, increase default speed to compensate for map size
            if (mapscript_list[map_selected].map_name == localizer.FetchText("MAP_NAME_FACINGWORLDS", "Facing Worlds"))
            {
                if (plysettings_speed < 1.91f || plysettings_atk < 3.0f)
                {
                    plysettings_speed = 1.91f;
                    plysettings_atk = 3.0f;
                }
                if (option_gamemode == (int)gamemode_name.FittingIn && option_gm_goal == 801)
                {
                    option_gm_goal = 400;
                    option_gm_config_a = 100;
                    ui_round_option_goal_input_a.text = option_gm_goal.ToString();
                    ui_round_option_goal_input_b.text = option_gm_config_a.ToString();
                }
            }
            else if (mapscript_list[map_selected].map_name == localizer.FetchText("MAP_NAME_UNDERWATER", "Deep Blues"))
            {
                if (plysettings_speed < 1.61f)
                {
                    plysettings_speed = 1.61f;
                }
                if (option_gamemode == (int)gamemode_name.FittingIn && option_gm_goal == 801)
                {
                    option_gm_goal = 400;
                    option_gm_config_a = 100;
                    ui_round_option_goal_input_a.text = option_gm_goal.ToString();
                    ui_round_option_goal_input_b.text = option_gm_config_a.ToString();
                }
            }
            else if (mapscript_list[map_selected].map_name == localizer.FetchText("MAP_NAME_RATS", "Borrowed Space"))
            {
                if (plysettings_speed < 1.91f || plysettings_atk < 2.0f)
                {
                    plysettings_speed = 1.91f;
                    plysettings_atk = 2.0f;
                }
                if (option_gamemode == (int)gamemode_name.FittingIn)
                {
                    option_gm_goal = (ushort)Mathf.Max(option_gm_goal, 801);
                    option_gm_config_a = (ushort)Mathf.Max(option_gm_config_a, 200);
                    ui_round_option_goal_input_a.text = option_gm_goal.ToString();
                    ui_round_option_goal_input_b.text = option_gm_config_a.ToString();
                }
            }
            else if (mapscript_list[map_selected].map_name == localizer.FetchText("MAP_NAME_CASTLE", "Stone's Throw"))
            {
                if (plysettings_speed < 1.61f || plysettings_atk < 1.5f)
                {
                    plysettings_speed = 1.61f;
                    plysettings_atk = 1.5f;
                }
                if (option_gamemode == (int)gamemode_name.FittingIn && option_gm_goal == 801)
                {
                    option_gm_goal = 400;
                    option_gm_config_a = 100;
                    ui_round_option_goal_input_a.text = option_gm_goal.ToString();
                    ui_round_option_goal_input_b.text = option_gm_config_a.ToString();
                }
            }
            // And reset if the game master forgot to
            else if (plysettings_speed == 1.61f || plysettings_speed == 1.91f)
            {
                plysettings_speed = 1.0f;
                plysettings_atk = 1.0f;
                if (option_gamemode == (int)gamemode_name.FittingIn && option_gm_goal == 801)
                {
                    option_gm_goal = 400;
                    option_gm_config_a = 100;
                    ui_round_option_goal_input_a.text = option_gm_goal.ToString();
                    ui_round_option_goal_input_b.text = option_gm_config_a.ToString();
                }
            }
        }
    }

    [NetworkCallable]
    public void NetworkRoundStart()
    {
        room_ready_script.gameObject.GetComponent<Collider>().enabled = false; // We need to make sure player arrays don't get messed up while transferring over to the match
        //Networking.LocalPlayer.Immobilize(true); <-- this forces the player to 0,0,0(!)
        // Ensure the players in game is updated
        cached_ply_in_game_dict = GetPlayersInGame();
        ResetLeaderboardDictInCheck(cached_ply_in_game_dict, ref cached_leaderboard_arr, ref cached_progress_arr);

        if (Networking.IsOwner(gameObject))
        {
            if (map_selected >= mapscript_list.Length || map_selected < 0) { map_selected = 0; }

            SetupMapSpecificParams();
            LocalRoundStart();
        }
        else { wait_for_sync_for_round_start = true; }
    }


    [NetworkCallable]
    public void NetworkRoundQueueStart()
    {
        round_start_ms = Networking.GetServerTimeInSeconds();
        round_state = (int)round_state_name.Queued;
        local_queue_timer = 0.0f;
        local_every_second_timer = 0.0f;
        PlaySFXFromArray(snd_ready_sfx_source, snd_ready_sfx_clips, (int)ready_sfx_name.QueueStart);
        if (Networking.IsOwner(gameObject))
        {
            if (ui_round_mapselect != null) { map_selected = (sbyte)ui_round_mapselect.SelectRandomActiveMap(); }
            RequestSerialization();
        }
        restrict_map_change = true;
        if (ui_round_scoreboard_canvas != null)
        {
            ui_round_scoreboard_canvas.GetComponent<Scoreboard>().scoreboard_header_text.text = localizer.FetchText("SCOREBOARD_HEADER_DEFAULT", "Scoreboard");
            ui_round_scoreboard_canvas.GetComponent<Scoreboard>().scoreboard_header_text.color = Color.white;
        }

        if (Networking.IsOwner(gameObject))
        {
            if (map_selected >= mapscript_list.Length || map_selected < 0) { map_selected = 0; }
            /*// If we're on Facing Worlds, increase default speed to compensate for map size
            if (mapscript_list[map_selected].map_name == localizer.FetchText("MAP_NAME_FACINGWORLDS", "Facing Worlds")
                && (plysettings_speed < 1.91f || plysettings_atk < 2.99f))
            {
                plysettings_speed = 1.91f;
                plysettings_atk = 2.99f;
            }
            // And reset if the game master forgot to
            else if (mapscript_list[map_selected].map_name != localizer.FetchText("MAP_NAME_FACINGWORLDS", "Facing Worlds")
                && (plysettings_speed == 1.91f || plysettings_atk == 2.99f))
            {
                plysettings_speed = 1.0f;
                plysettings_atk = 1.0f;
            }*/
            SetupMapSpecificParams();
            RequestSerialization();
        }
        RefreshSetupUI();
    }

    [NetworkCallable]
    public void NetworkRoundQueueStop()
    {
        round_start_ms = Networking.GetServerTimeInSeconds();
        round_state = (int)round_state_name.Start;
        local_queue_timer = 0.0f;
        local_every_second_timer = 0.0f;
        PlaySFXFromArray(snd_ready_sfx_source, snd_ready_sfx_clips, (int)ready_sfx_name.QueueCancel);
        // If we have anyone waiting for lobby, they should now be in the game
        if (ply_tracking_dict_values_arr != null && Networking.IsOwner(gameObject))
        {
            for (int i = 0; i < ply_tracking_dict_keys_arr.Length; i++)
            {
                if (ply_tracking_dict_values_arr[i] == (int)player_tracking_name.WaitingForLobby)
                {
                    ChangeTeam(ply_tracking_dict_keys_arr[i], 0, false);
                }
            }
        }
        RefreshSetupUI();
        if (Networking.IsOwner(gameObject)) { RequestSerialization(); }
    }

    public void SendRoundStart()
    {
        if (round_state == (int)round_state_name.Start) { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "NetworkRoundQueueStart"); }
        else if (round_state == (int)round_state_name.Queued) { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "NetworkRoundQueueStop"); }
    }

    [NetworkCallable]
    public void RoundCommence()
    {
        AddToLocalTextQueue(localizer.FetchText("NOTIFICATION_ROUNDSTART_BEGIN", "GO!"));

        if (vopack_selected != null && local_ppp_options != null && !vopack_selected.AllowPlay((int)voiceover_event_name.Tutorial)) { vopack_selected.PlayVoiceover((int)voiceover_event_name.Round, (int)voiceover_round_sfx_name.Start); }
        else if (vopack_selected != null)
        {
            if (option_gamemode == (int)gamemode_name.BossBash && local_plyAttr != null && local_plyAttr.ply_team == 0) { vopack_selected.PlayVoiceover((int)voiceover_event_name.Tutorial, (int)voiceover_tutorial_sfx_name.Mode_BossBash_A); }
            else if (option_gamemode == (int)gamemode_name.BossBash && local_plyAttr != null && local_plyAttr.ply_team == 1) { vopack_selected.PlayVoiceover((int)voiceover_event_name.Tutorial, (int)voiceover_tutorial_sfx_name.Mode_BossBash_B); }
            else if (option_gamemode == (int)gamemode_name.Infection && local_plyAttr != null && local_plyAttr.ply_team == 0) { vopack_selected.PlayVoiceover((int)voiceover_event_name.Tutorial, (int)voiceover_tutorial_sfx_name.Mode_Infection_A); }
            else if (option_gamemode == (int)gamemode_name.Infection && local_plyAttr != null && local_plyAttr.ply_team == 1) { vopack_selected.PlayVoiceover((int)voiceover_event_name.Tutorial, (int)voiceover_tutorial_sfx_name.Mode_Infection_B); }
            else if (option_gamemode == (int)gamemode_name.Survival) { vopack_selected.PlayVoiceover((int)voiceover_event_name.Tutorial, (int)voiceover_tutorial_sfx_name.Mode_Survival); }
            else if (option_gamemode == (int)gamemode_name.Clash) { vopack_selected.PlayVoiceover((int)voiceover_event_name.Tutorial, (int)voiceover_tutorial_sfx_name.Mode_Clash); }
            else if (option_gamemode == (int)gamemode_name.FittingIn) { vopack_selected.PlayVoiceover((int)voiceover_event_name.Tutorial, (int)voiceover_tutorial_sfx_name.Mode_FittingIn); }
            else if (option_gamemode == (int)gamemode_name.KingOfTheHill) { vopack_selected.PlayVoiceover((int)voiceover_event_name.Tutorial, (int)voiceover_tutorial_sfx_name.Mode_KingOfTheHill); }
        }

        room_ready_script.gameObject.GetComponent<Collider>().enabled = true;

        // To be ABSOLUTELY CERTAIN, let's also re-enable everyone's hitboxes and weapons
        if (local_plyweapon == null) 
        { 
            var plyWeaponFind = FindPlayerOwnedObject(Networking.LocalPlayer, "PlayerWeapon"); 
            if (plyWeaponFind != null) { local_plyweapon = plyWeaponFind.GetComponent<PlayerWeapon>(); } 
        }
        if (local_secondaryweapon == null)
        {
            var plyWeaponFind = FindPlayerOwnedObject(Networking.LocalPlayer, "SecondaryWeapon");
            if (plyWeaponFind != null) { local_secondaryweapon = plyWeaponFind.GetComponent<PlayerWeapon>(); }
        }

        if (local_plyweapon != null && local_plyAttr != null && local_plyAttr.ply_team >= 0 && !local_plyAttr.ply_training) {
            if (!local_plyweapon.gameObject.activeInHierarchy) { local_plyweapon.ToggleActive(true); }
            local_plyweapon.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "UpdateStatsFromWeaponType");
        }
        if (local_secondaryweapon != null && local_secondaryweapon.gameObject.activeInHierarchy)
        {
            local_secondaryweapon.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "UpdateStatsFromWeaponType");
        }

        cached_ply_in_game_dict = GetPlayersInGame();
        if (cached_ply_in_game_dict != null && cached_ply_in_game_dict.Length > 0 && cached_ply_in_game_dict[0] != null)
        {
            for (var i = 0; i < cached_ply_in_game_dict[0].Length; i++)
            {
                var player = VRCPlayerApi.GetPlayerById(cached_ply_in_game_dict[0][i]);
                if (player == null) { continue; }
                var plyWeaponObj = FindPlayerOwnedObject(player, "PlayerWeapon");
                var plyHitboxObj = FindPlayerOwnedObject(player, "PlayerHitbox");
                var plySecondaryWeaponObj = FindPlayerOwnedObject(player, "SecondaryWeapon");
                var plyAttr = FindPlayerAttributes(player);

                plyWeaponObj.SetActive(true);
                plyHitboxObj.SetActive(true);
                if (plySecondaryWeaponObj != null && plySecondaryWeaponObj.GetComponent<PlayerWeapon>() != null)
                {
                    bool is_boss = (option_gamemode == (int)gamemode_name.BossBash && GetGlobalTeam(player.playerId) == 1) || plyAttr.ply_dual_wield;
                    if (is_boss && player.IsUserInVR()) { plySecondaryWeaponObj.SetActive(true); }
                    else { plySecondaryWeaponObj.SetActive(false); }
                }

            }
        }
        //CheckPlyObjsActive();
    }

    /*public void CheckPlyObjsActive()
    {
        for (int i = 0; i < ply_owners_cnt; i++)
        {
            if (ply_object_owners == null || ply_object_owners.Length < i) { return; }
            if (ply_object_owners[i] == Networking.LocalPlayer.playerId) { continue; }
            if (ply_object_plyhitbox != null && i < ply_object_plyhitbox.Length && ply_object_plyhitbox[i] != null) { ply_object_plyhitbox[i].gameObject.SetActive(ply_object_plyhitbox[i].network_active); }
            if (ply_object_plyweapon != null && i < ply_object_plyweapon.Length && ply_object_plyweapon[i] != null) { ply_object_plyweapon[i].gameObject.SetActive(ply_object_plyweapon[i].network_active); }
            if (ply_object_secondaryweapon != null && i < ply_object_secondaryweapon.Length && ply_object_secondaryweapon[i] != null) { ply_object_secondaryweapon[i].gameObject.SetActive(ply_object_secondaryweapon[i].network_active); }
        }
    }*/

    [NetworkCallable]
    public void UpdateLargestPlayer(float in_ply_scale)
    {
        if (ply_object_owners != null && ply_owners_cnt > 0)
        {
            float refreshed_largest_scale = -1.0f;
            for (int i = 0; i < ply_owners_cnt; i++)
            {
                if (i < 0 || i >= ply_object_owners.Length || ply_object_owners[i] < 0) { break; }
                PlayerAttributes plyAttr = GetPlayerAttributesFromID(ply_object_owners[i]);
                if (plyAttr == null) { continue; }
                if (plyAttr.ply_scale > refreshed_largest_scale) { refreshed_largest_scale = plyAttr.ply_scale; }
            }
            largest_ply_scale = refreshed_largest_scale;
        }

        if (in_ply_scale > largest_ply_scale)
        {
            largest_ply_scale = in_ply_scale;
            RequestSerialization();
            AdjustVoiceRange();
        }
    }

    [NetworkCallable]
    public void ForceSerialization()
    {
        RequestSerialization();
    }

    public void AdjustVoiceRange()
    {
        VRCPlayerApi[] all_players = new VRCPlayerApi[(int)GLOBAL_CONST.UDON_MAX_PLAYERS];
        VRCPlayerApi.GetPlayers(all_players);
        VRCPlayerApi host = Networking.GetOwner(gameObject);
        if (megaphone_active)
        {
            foreach (VRCPlayerApi player in all_players)
            {
                if (player == null) continue;
                if (player == host)
                {
                    player.SetVoiceDistanceNear(0); // default 0
                    player.SetVoiceDistanceFar(1000000); // default 25
                    player.SetVoiceGain(10); //default 15
                }
                else
                {
                    player.SetVoiceDistanceNear(0); // default 0
                    player.SetVoiceDistanceFar(1); // default 25
                    player.SetVoiceGain(15); //default 15
                }
            }
        }
        else
        {
            float largest_voice_scale = largest_ply_scale;
            foreach (VRCPlayerApi player in all_players)
            {
                if (player == null) continue;
                PlayerAttributes plyAttr = FindPlayerAttributes(player);
                largest_voice_scale = largest_ply_scale;
                //largest_voice_scale = Mathf.Pow(2.0f, Mathf.Min(0.0f, largest_ply_scale - 1.0f));
                if (plyAttr != null && plyAttr.in_spectator_area) { largest_voice_scale *= 2.0f; } // Spectators should be able to hear a wider range
                else if (plyAttr != null && plyAttr.in_ready_room) { largest_voice_scale = 1.0f; } // Ready room players only need to hear each other
                player.SetVoiceDistanceNear(voice_distance_near * largest_voice_scale); // default 0
                player.SetVoiceDistanceFar(voice_distance_far * largest_voice_scale); // default 25
                //player.SetVoiceGain(15); //default 15
            }
            //UnityEngine.Debug.Log("[MEGAPHONE_TEST]: distanceNear = " + (voice_distance_near * largest_voice_scale) + "; distanceFar = " + (voice_distance_far * largest_voice_scale));

        }
        local_megaphone_active = megaphone_active;
    }

    public int[] CheckAllTeamLives(int[][] players_in_game_dict, ref byte total_players_alive, ref byte total_teams_alive)
    {
        int[] plyLivesPerTeam = new int[team_count];
        total_players_alive = 0;
        total_teams_alive = 0;
        if (players_in_game_dict == null || players_in_game_dict[0] == null || players_in_game_dict[0].Length == 0 || plyLivesPerTeam == null) { return null; }
        for (int i = 0; i < players_in_game_dict[0].Length; i++)
        {
            if (players_in_game_dict == null || players_in_game_dict.Length < 2 || players_in_game_dict[0] == null || players_in_game_dict[0].Length == 0 || players_in_game_dict[1] == null || players_in_game_dict[1].Length == 0 || players_in_game_dict[0].Length != players_in_game_dict[1].Length) { return null; }

            var player = VRCPlayerApi.GetPlayerById(players_in_game_dict[0][i]);
            if (player == null) { continue; }
            var plyAttributes = FindPlayerAttributes(player);
            if (plyAttributes == null) { continue; }
            if (plyAttributes.ply_lives > 0)
            {
                if (players_in_game_dict[1][i] > team_count) { UnityEngine.Debug.LogError("Player is on team " + players_in_game_dict[1][i] + ", but the game only has " + team_count + " teams!"); continue; }
                if (players_in_game_dict[1][i] < 0) { UnityEngine.Debug.LogError("Player " + players_in_game_dict[0][i] + " is on team " + players_in_game_dict[1][i] + ", but is considered in game!"); continue; }

                if (players_in_game_dict[1][i] >= 0)
                {
                    if (plyLivesPerTeam[players_in_game_dict[1][i]] <= 0) { total_teams_alive++; }
                    plyLivesPerTeam[players_in_game_dict[1][i]] += plyAttributes.ply_lives;
                    //UnityEngine.Debug.Log("Player " + players_in_game_dict[0][i] + " on Team " + players_in_game_dict[1][i] + " is alive with " + plyAttributes.ply_lives + " lives"); 
                }
                total_players_alive++;
            }
        }
        return plyLivesPerTeam;
    }

    public int[] CheckSingleTeamLives(int team_id, int[][] players_in_game_dict, ref int members_alive, ref int total_lives)
    {
        if (team_id >= team_count) { UnityEngine.Debug.LogError("Attempted to check for team lives when the team (" + team_id + ") exceeds team count (" + team_count + ")!"); return null; }
        if (players_in_game_dict == null || players_in_game_dict[0] == null || players_in_game_dict[0].Length == 0) { return null; }
        members_alive = 0;
        int[] players_alive = new int[players_in_game_dict[1].Length];
        Array.Copy(players_in_game_dict[1], players_alive, players_in_game_dict[1].Length);
        for (int i = 0; i < players_in_game_dict[0].Length; i++)
        {
            if (players_in_game_dict == null || players_in_game_dict.Length < 2 || players_in_game_dict[0] == null || players_in_game_dict[0].Length == 0 || players_in_game_dict[1] == null || players_in_game_dict[1].Length == 0 || players_in_game_dict[0].Length != players_in_game_dict[1].Length) { return null; }

            var player = VRCPlayerApi.GetPlayerById(players_in_game_dict[0][i]);
            if (player == null) { continue; }
            var plyAttributes = FindPlayerAttributes(player);
            if (plyAttributes == null) { continue; }
            if (players_in_game_dict[1][i] == team_id)
            {
                players_alive[i] = plyAttributes.ply_lives;
                if (plyAttributes.ply_lives > 0) { members_alive++; total_lives += plyAttributes.ply_lives; }
            }
            else { players_alive[i] = -4; }
        }
        return players_alive;
    }

    public int[] CheckAllTeamPoints(int[][] players_in_game_dict, ref int highest_team, ref int highest_points, ref int highest_ply_id, bool check_deaths_instead = false)
    {
        var plyPointsPerTeam = new int[team_count]; var highest_points_in_team = 0;
        if (players_in_game_dict == null || players_in_game_dict[0] == null || players_in_game_dict[0].Length == 0 || plyPointsPerTeam == null) { return null; }
        for (int i = 0; i < players_in_game_dict[0].Length; i++)
        {
            if (players_in_game_dict == null || players_in_game_dict.Length < 2 || players_in_game_dict[0] == null || players_in_game_dict[0].Length == 0 || players_in_game_dict[1] == null || players_in_game_dict[1].Length == 0 || plyPointsPerTeam == null || players_in_game_dict[0].Length != players_in_game_dict[1].Length) { return null; }

            VRCPlayerApi player = VRCPlayerApi.GetPlayerById(players_in_game_dict[0][i]);
            if (player == null) { continue; }
            PlayerAttributes plyAttributes = FindPlayerAttributes(player);
            if (plyAttributes == null) { continue; }

            int ply_point_value = plyAttributes.ply_points;
            if (check_deaths_instead) { ply_point_value = plyAttributes.ply_deaths; }

            if (players_in_game_dict[1][i] > team_count) { UnityEngine.Debug.LogError("Player " + players_in_game_dict[0][i] + " is on team " + players_in_game_dict[1][i] + ", but the game only has " + team_count + " teams!"); continue; }
            if (players_in_game_dict[1][i] < 0 ) { UnityEngine.Debug.LogError("Player " + players_in_game_dict[0][i] + " is on team " + players_in_game_dict[1][i] + ", but is considered in game!"); continue; }

            if (players_in_game_dict[1][i] >= 0) { plyPointsPerTeam[players_in_game_dict[1][i]] += ply_point_value; }

            if (!check_deaths_instead)
            {
                if (ply_point_value > highest_points) { highest_points = ply_point_value; highest_ply_id = players_in_game_dict[0][i]; }
                if (plyPointsPerTeam[players_in_game_dict[1][i]] > highest_points_in_team) { highest_points_in_team = plyPointsPerTeam[players_in_game_dict[1][i]]; highest_team = players_in_game_dict[1][i]; }
            }
            else
            {
                // For deaths, we want to check lowest value
                if (ply_point_value < highest_points) { highest_points = ply_point_value; highest_ply_id = players_in_game_dict[0][i]; }
                if (plyPointsPerTeam[players_in_game_dict[1][i]] < highest_points_in_team) { highest_points_in_team = plyPointsPerTeam[players_in_game_dict[1][i]]; highest_team = players_in_game_dict[1][i]; }
            }
        }

        if (check_deaths_instead) {
            for (int i = 0; i < plyPointsPerTeam.Length; i++)
            {
                int[][] team_dict = GetPlayersOnTeam(i);
                if (team_dict[0].Length == 0) { plyPointsPerTeam[i] = 99999; }
            }
        }

        return plyPointsPerTeam;
    }

    public int[] CheckSingleTeamPoints(int team_id, int[][] players_in_game_dict, ref int total_points, bool check_deaths_instead = false)
    {
        if (team_id > team_count) { UnityEngine.Debug.LogError("Attempted to check for team points when the team (" + team_id + ") exceeds team count (" + team_count + ")!"); return null; }
        if (players_in_game_dict == null || players_in_game_dict[0] == null || players_in_game_dict[0].Length == 0) { return null; }
        total_points = 0;
        int[] player_points = new int[players_in_game_dict[1].Length];
        Array.Copy(players_in_game_dict[1], player_points, players_in_game_dict[1].Length);
        for (int i = 0; i < players_in_game_dict[0].Length; i++)
        {
            if (players_in_game_dict == null || players_in_game_dict.Length < 2 || players_in_game_dict[0] == null || players_in_game_dict[0].Length == 0 || players_in_game_dict[1] == null || players_in_game_dict[1].Length == 0 || players_in_game_dict[0].Length != players_in_game_dict[1].Length) { return null; }

            var player = VRCPlayerApi.GetPlayerById(players_in_game_dict[0][i]);
            if (player == null) { continue; }
            var plyAttributes = FindPlayerAttributes(player);
            if (plyAttributes == null) { continue; }

            int ply_point_value = plyAttributes.ply_points;
            if (check_deaths_instead) { ply_point_value = plyAttributes.ply_deaths; }

            if (players_in_game_dict[1][i] == team_id) { player_points[i] = ply_point_value; total_points += ply_point_value; }
            //else { player_points[i] = -4; }
        }
        return player_points;
    }

    public string GetLeaderName(int[] leaderboard_arr, int[] progress_arr, bool ascending = false)
    {
        if (leaderboard_arr == null || progress_arr == null || leaderboard_arr.Length == 0 || progress_arr.Length == 0) { return ""; }
        string leader_name = "";
        VRCPlayerApi leaderPly;
        if (!option_teamplay) 
        { 
            leaderPly = VRCPlayerApi.GetPlayerById(leaderboard_arr[0]);
            if (leaderPly != null) { leader_name = leaderPly.displayName; }
        }
        else if (leaderboard_arr[0] < team_names.Length)
        {
            //leader_name = team_names[leaderboard_arr[0]];
            leader_name = localizer.FetchTeamNameKey(leaderboard_arr[0]);
        }

        int point_compare = 0;
        for (int i = 0; i < progress_arr.Length; i++)
        {
            if (i == 0) { point_compare = progress_arr[0]; continue; }
            if ((!ascending && progress_arr[i] >= point_compare) || (ascending && progress_arr[i] <= point_compare))
            {
                if (option_teamplay && leaderboard_arr[0] < team_names.Length)
                {
                    leader_name += ", " + localizer.FetchTeamNameKey(leaderboard_arr[i]);
                }
                else if (!option_teamplay)
                {
                    leaderPly = VRCPlayerApi.GetPlayerById(leaderboard_arr[i]);
                    if (leaderPly != null) { leader_name += ", " + leaderPly.displayName; }
                }
            }
            else { break; }
        }
        return leader_name;
    }

    internal void ResetLeaderboardDictInCheck(int[][] players_in_game_dict, ref int[] leaderboard_arr, ref int[] progress_arr)
    {
        if (players_in_game_dict == null) 
        { 
            cached_ply_in_game_dict = GetPlayersInGame(); 
            players_in_game_dict = cached_ply_in_game_dict; 
            if (cached_ply_in_game_dict == null) { return; }
        }

        if (option_teamplay)
        {
            // If we have teams on, the leaderboard will be a list of team IDs
            leaderboard_arr = new int[team_count];
            progress_arr = new int[team_count];
            for (int i = 0; i < team_count; i++)
            {
                leaderboard_arr[i] = i;
                progress_arr[i] = 0;
            }
        }
        else
        {
            // If we have FFA on, the leaderboard will be a list of player IDs
            leaderboard_arr = new int[players_in_game_dict[0].Length];
            progress_arr = new int[players_in_game_dict[0].Length];
            for (int i = 0; i < players_in_game_dict[0].Length; i++)
            {
                leaderboard_arr[i] = players_in_game_dict[0][i];
                progress_arr[i] = 0;
            }
        }
    }

    public bool CheckRoundGoalProgress(ref int[] leaderboard_arr, ref int[] progress_arr, out string leader_name)
    {
        leader_name = "";
        if (cached_ply_in_game_dict == null || cached_ply_in_game_dict.Length < 2 || cached_ply_in_game_dict[0] == null || cached_ply_in_game_dict[0].Length == 0 || cached_ply_in_game_dict[1] == null || cached_ply_in_game_dict[1].Length == 0 || cached_ply_in_game_dict[0].Length != cached_ply_in_game_dict[1].Length) { cached_ply_in_game_dict = GetPlayersInGame(); }
        int[][] players_in_game_dict = cached_ply_in_game_dict; // Warning: this is a pointer, not a copy; make sure not to modify this array
        bool declare_victor = false;

        if (team_count <= 0 || players_in_game_dict == null || players_in_game_dict[0] == null || players_in_game_dict[0].Length == 0 || players_in_game_dict[1] == null || players_in_game_dict[1].Length == 0 || players_in_game_dict[0].Length != players_in_game_dict[1].Length) { UnityEngine.Debug.LogWarning("Attempted to check for round goal with invalid player array or less than 1 team!"); return true; }

        ResetLeaderboardDictInCheck(players_in_game_dict, ref leaderboard_arr, ref progress_arr);

        // To-do: check for null results from CheckSingleTeam<>()

        // For survival and fitting in, we need to check lives
        if (option_gamemode == (int)gamemode_name.Survival || option_gamemode == (int)gamemode_name.FittingIn)
        {
            if (option_teamplay)
            {
                byte total_teams_alive = 0;
                byte total_players_alive = 0;
                int[] plyLivesPerTeam = CheckAllTeamLives(players_in_game_dict, ref total_players_alive, ref total_teams_alive);
                progress_arr = plyLivesPerTeam;
                DictSort(ref leaderboard_arr, ref progress_arr, false);
                leader_name = GetLeaderName(leaderboard_arr, progress_arr);
                declare_victor =
                    (total_players_alive <= 1 && players_in_game_dict[0].Length > 1) ||
                    (total_players_alive <= 0 && players_in_game_dict[0].Length == 1) ||
                    (total_teams_alive <= 1 && team_count > 1) ||
                    (total_teams_alive <= 0 && team_count == 1) ||
                    (team_count <= 0);
            }
            else
            {
                int total_players_alive = 0; int sum_lives = 0;
                int[] plyAlive = CheckSingleTeamLives(0, players_in_game_dict, ref total_players_alive, ref sum_lives);
                progress_arr = plyAlive;
                DictSort(ref leaderboard_arr, ref progress_arr, false);
                leader_name = GetLeaderName(leaderboard_arr, progress_arr);
                declare_victor =
                    (total_players_alive <= 1 && players_in_game_dict[0].Length > 1) ||
                    (total_players_alive <= 0 && players_in_game_dict[0].Length == 1) ||
                    (players_in_game_dict[0].Length == 0);
            }
        }
        // For Clash, we just need to check points
        else if (option_gamemode == (int)gamemode_name.Clash)
        {
            if (option_teamplay)
            {
                int highest_team = -3; int highest_points = -1; int highest_ply_id = -1;
                int[] pointsPerTeam = CheckAllTeamPoints(players_in_game_dict, ref highest_team, ref highest_points, ref highest_ply_id);
                progress_arr = pointsPerTeam;
                DictSort(ref leaderboard_arr, ref progress_arr, false);
                leader_name = GetLeaderName(leaderboard_arr, progress_arr);
                declare_victor = pointsPerTeam[0] >= option_gm_goal;
            }
            else
            {
                int total_points = 0;
                int[] pointsPerPlayer = CheckSingleTeamPoints(0, players_in_game_dict, ref total_points);
                progress_arr = pointsPerPlayer;
                DictSort(ref leaderboard_arr, ref progress_arr, false);
                leader_name = GetLeaderName(leaderboard_arr, progress_arr);
                declare_victor = pointsPerPlayer[0] >= option_gm_goal;
            }
        }
        // For Boss Bash, we need to check points on Tiny Troopers and points & lives on Boss
        else if (option_gamemode == (int)gamemode_name.BossBash)
        {
            int total_points = 0;
            int[] pointsPerPlayer = CheckSingleTeamPoints(0, players_in_game_dict, ref total_points);
            progress_arr = pointsPerPlayer;
            DictSort(ref leaderboard_arr, ref progress_arr, false);

            int boss_alive = 0; int boss_lives = 0;
            int[] bossLivesArr = CheckSingleTeamLives(1, players_in_game_dict, ref boss_alive, ref boss_lives);
            int boss_points = 0;
            int[] bossPointsArr = CheckSingleTeamPoints(1, players_in_game_dict, ref boss_points);

            if (boss_alive == 0 || bossLivesArr.Length == 0 || bossPointsArr.Length == 0)
            {
                leader_name = "TEAM_NAME_BOSSBASH_0";
                declare_victor = true;
            }
            else if (boss_lives > 0 && bossPointsArr.Length > 0 && boss_points >= option_gm_goal)
            {
                leader_name = "TEAM_NAME_BOSSBASH_1";
                declare_victor = true;
            }
        }
        // For Infection, we need to check players alive on Survivors and points on Infected
        else if (option_gamemode == (int)gamemode_name.Infection)
        {
            int total_points = 0;
            int[] pointsPerInfected = CheckSingleTeamPoints(1, players_in_game_dict, ref total_points);
            progress_arr = pointsPerInfected;
            DictSort(ref leaderboard_arr, ref progress_arr, false);

            int survivors_alive = 0; int survivor_lives = 0;
            int[] livesPerSurvivor = CheckSingleTeamLives(0, players_in_game_dict, ref survivors_alive, ref survivor_lives);

            if (survivors_alive == 0 || livesPerSurvivor.Length == 0)
            {
                leader_name = "TEAM_NAME_INFECTION_1";
                declare_victor = true;
            }
            else if (round_timer >= round_length)
            {
                leader_name = "TEAM_NAME_INFECTION_0";
                declare_victor = true;
            }
        }
        // For King of the Hill, we need make an aggregated dictionary formed from all of the capture zone dicts and use sort as if they were team/player points
        else if (option_gamemode == (int)gamemode_name.KingOfTheHill)
        {
            if (mapscript_list == null || map_selected < 0 || map_selected > mapscript_list.Length) { return true; }
            if (mapscript_list[map_selected].map_capturezones == null || mapscript_list[map_selected].map_capturezones.Length <= 0) { UnityEngine.Debug.LogWarning("KOTH: No capture zones found!"); return true; }
            if (mapscript_list[map_selected].map_capturezones[0].dict_points_keys_arr == null) { return true; }
            int keys_len = mapscript_list[map_selected].map_capturezones[0].dict_points_keys_arr.Length;
            koth_progress_dict[0] = new int[keys_len];
            koth_progress_dict[1] = new int[keys_len];
            Array.Copy(mapscript_list[map_selected].map_capturezones[0].dict_points_keys_arr, koth_progress_dict[0], keys_len);
            Array.Copy(mapscript_list[map_selected].map_capturezones[0].dict_points_values_arr, koth_progress_dict[1], keys_len);
            // To-Do: I have no idea why, but for some reason, all of the points between capture zones are synchronized, so this block below ends up not working as intended.
            /*for (int i = 1; i < mapscript_list[map_selected].map_capturezones.Length; i++)
            {
                CaptureZone capturezone = mapscript_list[map_selected].map_capturezones[i];
                if (capturezone == null || capturezone.gameObject == null || !capturezone.gameObject.activeInHierarchy) { continue; }
                if (capturezone.dict_points_keys_arr.Length != koth_progress_dict[0].Length || capturezone.dict_points_values_arr.Length != koth_progress_dict[1].Length) { UnityEngine.Debug.LogWarning("Capture zone dict " + capturezone.transform.name + " does not match length of " + mapscript_list[map_selected].map_capturezones[0].transform.name); continue; }
                for (int j = 0; j < capturezone.dict_points_values_arr.Length; j++)
                {
                    koth_progress_dict[1][j] += capturezone.dict_points_values_arr[j];
                }
            }*/

            if (koth_progress_dict[0] != null && koth_progress_dict[0].Length > 0) {
                // Normally, we wouldn't set the leaderboard array, but because the KOTH arrays are static at round start, we need to be consistent
                leaderboard_arr = koth_progress_dict[0];
                progress_arr = koth_progress_dict[1];
                DictSort(ref leaderboard_arr, ref progress_arr, false);
                if (leaderboard_arr[0] >= 0)
                {
                    if (option_teamplay && leaderboard_arr.Length > 1)
                    {
                        leader_name = GetLeaderName(leaderboard_arr, progress_arr);
                    }
                    else if (!option_teamplay)
                    {
                        leader_name = GetLeaderName(leaderboard_arr, progress_arr);
                    }
                }
                declare_victor = progress_arr[0] >= option_gm_goal;

                //UnityEngine.Debug.Log("[PROGRESS_TEST] KOTH: " + ConvertIntArrayToString(progress_arr) + "; LEADERBOARD: " + ConvertIntArrayToString(leaderboard_arr));
            }
            
        }

        // After we've checked Lives for Fitting In (victory condition), we need to check deaths on players (leaderboard)
        if (option_gamemode == (int)gamemode_name.FittingIn)
        {
            ResetLeaderboardDictInCheck(players_in_game_dict, ref leaderboard_arr, ref progress_arr);
            if (option_teamplay)
            {
                int lowest_team = -3; int lowest_deaths = 0; int lowest_ply_id = -1;
                int[] pointsPerTeam = CheckAllTeamPoints(players_in_game_dict, ref lowest_team, ref lowest_deaths, ref lowest_ply_id, true);
                UnityEngine.Debug.Log("[PROGRESS_TEST] FITTING IN DEATHS PER TEAM: " + ConvertIntArrayToString(pointsPerTeam) + "; LOWEST TEAM: " + lowest_team + " LOWEST DEATHS: " + lowest_deaths + " LOWEST PLY ID: " + lowest_ply_id);
                progress_arr = pointsPerTeam;
                DictSort(ref leaderboard_arr, ref progress_arr, true);
                UnityEngine.Debug.Log("[PROGRESS_TEST] FITTING IN DEATHS PER TEAM SORTED: " + ConvertIntArrayToString(progress_arr) + "; LEADERBOARD: " + ConvertIntArrayToString(leaderboard_arr));
                leader_name = GetLeaderName(leaderboard_arr, progress_arr, true);
            }
            else
            {
                int total_deaths = 0;
                int[] pointsPerPlayer = CheckSingleTeamPoints(0, players_in_game_dict,ref total_deaths, true);
                progress_arr = pointsPerPlayer;
                //UnityEngine.Debug.Log("[PROGRESS_TEST] FITTING IN DEATHS PER PLAYER: " + ConvertIntArrayToString(pointsPerPlayer) + "; TOTAL DEATHS: " + total_deaths);
                DictSort(ref leaderboard_arr, ref progress_arr, true);
                //UnityEngine.Debug.Log("[PROGRESS_TEST] FITTING IN DEATHS PER PLAYER SORTED: " + ConvertIntArrayToString(progress_arr) + "; LEADERBOARD: " + ConvertIntArrayToString(leaderboard_arr));
                leader_name = GetLeaderName(leaderboard_arr, progress_arr, true);
            }
        }

        if (declare_victor)
        {
            UnityEngine.Debug.Log("[VICTORY_TEST] LEADERBOARD ARR: " + ConvertIntArrayToString(leaderboard_arr));
            UnityEngine.Debug.Log("[VICTORY_TEST] PROGRESS ARR: " + ConvertIntArrayToString(progress_arr));
            UnityEngine.Debug.Log("[VICTORY_TEST] LEADER NAME: " + leader_name);
            //if (option_teamplay) { UnityEngine.Debug.Log("[VICTORY TEST] LEADER NAME CAME FROM TEAM #" + leaderboard_arr[0] + " (" + team_names[leaderboard_arr[0]] + ") WITH PROGRESS: " + progress_arr[0]); }
        }

        return declare_victor;
    }

    [NetworkCallable]
    public void CheckForRoundGoal()
    {
        bool declare_victor = CheckRoundGoalProgress(ref cached_leaderboard_arr, ref cached_progress_arr, out string leader_name);
        int winning_team = -1;
        if (option_teamplay && cached_leaderboard_arr != null && cached_leaderboard_arr.Length > 0) { winning_team = Mathf.Max(0, cached_leaderboard_arr[0]); }
        if (!Networking.IsOwner(gameObject)) { return; }
        if (declare_victor && round_state == (int)round_state_name.Ongoing)
        {
            //round_state = (int)round_state_name.Over;
            //round_start_ms = Networking.GetServerTimeInSeconds();
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "RoundEnd", leader_name, winning_team);
        }
        else
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "RefreshGameUI");
        }
    }

    [NetworkCallable]
    public void RoundEnd(string winner_name_input, int winning_team)
    {
        //UnityEngine.Debug.Log("[DICT_TEST]: ROUND END - " + ConvertIntArrayToString(players_in_game_dict[0]) + " | " + ConvertIntArrayToString(players_in_game_dict[1]));
        if (local_uiplytoself != null && round_state != (int)round_state_name.Start) { local_uiplytoself.gamevars_force_refresh_on_next_tick = true; }

        for (int i = 0; i < ply_tracking_dict_keys_arr.Length; i++)
        {
            var player = VRCPlayerApi.GetPlayerById(ply_tracking_dict_keys_arr[i]);
            if (player == null) { continue; }

            PlayerAttributes plyAttributes = FindPlayerAttributes(player);
            PlayerWeapon plyWeapon = GetPlayerWeaponFromID(player.playerId);
            PlayerHitbox plyHitbox = GetPlayerHitboxFromID(player.playerId);
            PlayerWeapon secondaryWeapon = GetSecondaryWeaponFromID(player.playerId);
            GameObject plyWeaponObj;
            if (plyWeapon == null) { plyWeaponObj = FindPlayerOwnedObject(player, "PlayerWeapon"); plyWeapon = plyWeaponObj.GetComponent<PlayerWeapon>(); }
            else { plyWeaponObj = plyWeapon.gameObject; }
            GameObject plyHitboxObj;
            if (plyHitbox == null) { plyHitboxObj = FindPlayerOwnedObject(player, "PlayerHitbox"); plyHitbox = plyHitboxObj.GetComponent<PlayerHitbox>(); }
            else { plyHitboxObj = plyHitbox.gameObject; }
            GameObject plySecondaryObj;
            if (secondaryWeapon == null) { plySecondaryObj = FindPlayerOwnedObject(player, "SecondaryWeapon"); secondaryWeapon = plySecondaryObj.GetComponent<PlayerWeapon>(); }
            else { plySecondaryObj = secondaryWeapon.gameObject; }

            plyWeaponObj.SetActive(false);
            plyHitboxObj.SetActive(false);
            plySecondaryObj.SetActive(false);

            if (player == Networking.LocalPlayer)
            {
                if (plyWeapon.GetComponent<VRC_Pickup>() != null) { plyWeapon.GetComponent<VRC_Pickup>().Drop(); }
                if (plyAttributes.ply_state != (int)player_state_name.Spectator) { plyAttributes.ply_state = (int)player_state_name.Inactive; plyAttributes.ply_training = false; }
                ToggleReadyRoomCollisions(true);
                if (!plyAttributes.in_ready_room && (plyAttributes.ply_team >= 0 || plyAttributes.ply_team == (int)player_tracking_name.WaitingForLobby)) { TeleportLocalPlayerToReadyRoom(); }
            }
        }
                
        for (int j = 0; j < mapscript_list[map_selected].map_item_spawns.Length; j++)
        {
            mapscript_list[map_selected].map_item_spawns[j].DespawnItem((int)item_sfx_index.ItemExpire, -1, false);
            mapscript_list[map_selected].map_item_spawns[j].item_spawn_state = (int)item_spawn_state_name.Disabled;
        }

        string winner_name_localized = winner_name_input;
        if (option_teamplay) { winner_name_localized = localizer.LocalizeTeamName(winner_name_input); }

        if (winner_name_localized == localizer.FetchText("TEAM_NAME_INFECTION_1", "Infected")) { winning_team = 1; }
        else if (winner_name_localized == localizer.FetchText("TEAM_NAME_BOSSBASH_1", "The Big Boss")) { winning_team = 1; }

        Color winning_color = Color.white;
        if (winning_team >= 0 && team_colors_bright != null && winning_team < team_colors_bright.Length && !winner_name_localized.Contains(',')) 
        { 
            winning_color = (Color)team_colors_bright[winning_team]; 
        }

        if (winner_name_localized != null && winner_name_localized.Length > 0)
        {
            AddToLocalTextQueue(localizer.FetchText("NOTIFICATION_ROUNDEND_WINNER", "WINNER:", "").Replace("!","").Replace("！",""), Color.white, over_length + 2.0f);
            AddToLocalTextQueue(winner_name_localized + "!", winning_color, over_length + 2.0f);
            if (ui_round_scoreboard_canvas != null)
            {
                ui_round_scoreboard_canvas.GetComponent<Scoreboard>().scoreboard_header_text.text = localizer.FetchText("NOTIFICATION_ROUNDEND_WINNER", "WINNER: $ARG0!", winner_name_localized);
                ui_round_scoreboard_canvas.GetComponent<Scoreboard>().scoreboard_header_text.color = winning_color;
            }
        }
        else
        {
            AddToLocalTextQueue(localizer.FetchText("NOTIFICATION_ROUNDEND_STALEMATE_0", "STALEMATE!"));
            AddToLocalTextQueue(localizer.FetchText("NOTIFICATION_ROUNDEND_STALEMATE_1", "no one wins :("));
            if (ui_round_scoreboard_canvas != null)
            {
                ui_round_scoreboard_canvas.GetComponent<Scoreboard>().scoreboard_header_text.text = localizer.FetchText("NOTIFICATION_ROUNDEND_STALEMATE_0", "STALEMATE!");
                ui_round_scoreboard_canvas.GetComponent<Scoreboard>().scoreboard_header_text.color = Color.white;
            }
            vopack_selected.PlayVoiceover((int)voiceover_event_name.Round, (int)voiceover_round_sfx_name.Stalemate);
        }

        if (room_ready_script.warning_acknowledged) 
        {
            bool local_is_winner = false;
            local_is_winner = (option_gamemode == (int)gamemode_name.Infection && winning_team == 0);
            local_is_winner = local_is_winner || (option_gamemode == (int)gamemode_name.BossBash && winning_team == local_plyAttr.ply_team);
            local_is_winner = local_is_winner || (option_gamemode != (int)gamemode_name.Infection && option_teamplay && winner_name_input.Contains(local_plyAttr.ply_team.ToString()));
            local_is_winner = local_is_winner || (option_gamemode != (int)gamemode_name.Infection && !option_teamplay && winner_name_localized.Contains(Networking.LocalPlayer.displayName));
            UnityEngine.Debug.Log("[ROUND_END]: winning_team = " + winning_team + "; winner_name_input = " + winner_name_input + "; localized = " + winner_name_localized + "; is_local = " + local_is_winner);

            //if (snd_game_music_source.volume < music_volume_stored) { snd_game_music_source.volume = music_volume_stored; }
            snd_game_music_source.pitch = 1.0f;
            if (!option_teamplay && winner_name_localized != null && winner_name_localized.Length > 0)
            {
                PlaySFXFromArray(snd_game_music_source, snd_victory_music_clips, option_gamemode, 1, true);
                vopack_selected.PlayVoiceover((int)voiceover_event_name.Round, (int)voiceover_round_sfx_name.End);
            }
            else if (winner_name_localized != null && winner_name_localized.Length > 0 && local_is_winner)
            {
                PlaySFXFromArray(snd_game_music_source, snd_victory_music_clips, option_gamemode, 1, true);
                if (option_gamemode == (int)gamemode_name.Infection) { vopack_selected.PlayVoiceover((int)voiceover_event_name.Round, (int)voiceover_round_sfx_name.Infection_End_Victory); }
                else if (local_plyAttr != null && local_plyAttr.ply_team < 0) { vopack_selected.PlayVoiceover((int)voiceover_event_name.Round, (int)voiceover_round_sfx_name.End); }
                else { vopack_selected.PlayVoiceover((int)voiceover_event_name.Round, (int)voiceover_round_sfx_name.Victory); }
            }
            else
            {
                PlaySFXFromArray(snd_game_music_source, snd_defeat_music_clips, option_gamemode, 1, true); 
                if (option_gamemode == (int)gamemode_name.Infection) { vopack_selected.PlayVoiceover((int)voiceover_event_name.Round, (int)voiceover_round_sfx_name.Infection_End_Defeat); }
                else if (local_plyAttr != null && local_plyAttr.ply_team < 0) { vopack_selected.PlayVoiceover((int)voiceover_event_name.Round, (int)voiceover_round_sfx_name.End); }
                else { vopack_selected.PlayVoiceover((int)voiceover_event_name.Round, (int)voiceover_round_sfx_name.Defeat); }
            }
        }

        if (Networking.IsOwner(gameObject))
        {
            round_state = (int)round_state_name.Over;
            round_start_ms = Networking.GetServerTimeInSeconds();
            largest_ply_scale = 1.0f;
            RequestSerialization();

        }

        if (option_gamemode == (int)gamemode_name.KingOfTheHill)
        {
            // KOTH Capture Zone handling
            foreach (CaptureZone capturezone in mapscript_list[map_selected].map_capturezones)
            {
                if (capturezone == null || capturezone.gameObject == null) { continue; }
                if (Networking.IsOwner(gameObject))
                {
                    //capturezone.dict_points_keys_arr = new int[0];
                    //capturezone.dict_points_keys_str = "";
                    //capturezone.dict_points_values_arr = new int[0];
                    //capturezone.dict_points_values_str = "";
                    //capturezone.RequestSerialization(); // Continous sync probably doesn't require this, but just in case
                    capturezone.hold_id = -1;
                    capturezone.contest_id = -1;
                    capturezone.is_locked = true;
                    capturezone.RequestSerialization(); // Continous sync probably doesn't require this, but just in case

                }
                
                capturezone.gameObject.SetActive(false);
            }
        }

        room_training_portal.SetActive(true);
        room_spectator_portal.SetActive(false);
        ui_spectatorcanvas.SetActive(false);
        ui_highlightcanvas.SetActive(true);

        if (Networking.IsOwner(gameObject) 
            && highlight_cameras_snapped != null && highlight_cameras_snapped.Length > 5
            && highlight_cameras_waiting_on_sync != null && highlight_cameras_waiting_on_sync.Length > 5
            && highlight_cameras_snapped[5] == false && highlight_cameras_waiting_on_sync[5] == false)
        {
            // Victory
            VRCPlayerApi lead_ply = GetLeaderPlayer(); PlayerAttributes lead_attr = FindPlayerAttributes(lead_ply);
            //if (option_teamplay || lead_ply == null || lead_attr == null) { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SnapHighlightPhoto", 5, Vector3.zero, Quaternion.identity, victory_camera_plane.transform.position, -Vector3.right, true, 1.0f); }
            //else { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SnapHighlightPhoto", 5, Vector3.zero, Quaternion.identity, victory_camera_plane.transform.position, -Vector3.right, true, 1.0f); } //lead_ply.GetRotation() * Vector3.forward
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SnapHighlightPhoto", 5, Vector3.zero, Quaternion.identity, victory_camera_plane.transform.position, -Vector3.right, true, 1.0f);
            highlight_cameras_waiting_on_sync[5] = true;
        }

        restrict_map_change = false;
        RefreshSetupUI();
    }

    public void SendRoundEnd()
    {
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "RoundEnd", "", -1);
    }

    [NetworkCallable]
    public void NetworkPlayReadyMusic()
    {
        if (room_ready_script != null && room_ready_script.warning_acknowledged)
        {
            PlaySFXFromArray(snd_game_music_source, snd_ready_music_clips, -1, 1, true);
        }
    }

    public void TeleportLocalPlayerToGameSpawnZone(int spawnZoneIndex = -1, bool force_use_team = false)
    {
        if (mapscript_list == null || map_selected >= mapscript_list.Length || mapscript_list[map_selected].map_spawnzones == null || mapscript_list[map_selected].map_spawnzones.Length == 0) { return; }
        map_element_spawn spawnzone = null;
        if (spawnZoneIndex >= 0 && spawnZoneIndex < mapscript_list[map_selected].map_spawnzones.Length)
        {
            spawnzone = mapscript_list[map_selected].map_spawnzones[spawnZoneIndex];
            // Note: we need to ensure KO
            if (spawnzone == null
                || (option_teamplay && (force_use_team || !option_enforce_team_limits) && spawnzone.team_id != GetGlobalTeam(Networking.LocalPlayer.playerId) && spawnzone.team_id >= 0) 
                || (option_teamplay && (force_use_team || !option_enforce_team_limits) && spawnzone.team_id == -2) // FFA-only
                || (GetPlayersInGame()[0].Length < spawnzone.min_players && spawnzone.min_players > 0)
                || !spawnzone.gameObject.activeInHierarchy || !spawnzone.gameObject.activeSelf
                ) { spawnZoneIndex = -1; }
        }
        // If no spawn is specified, try gettting the farthest away from all other players
        else
        {
            int[] eligible_spawns = GetEligibleSpawnsForPlayer(Networking.LocalPlayer.playerId, force_use_team);
            int index_within_eligible_spawns = -1;
            if (eligible_spawns != null && eligible_spawns.Length > 0) 
            { 
                index_within_eligible_spawns = GetSpawnFarthestFromPlayers(Networking.LocalPlayer.playerId, eligible_spawns);
                if (index_within_eligible_spawns < eligible_spawns.Length) { spawnZoneIndex = eligible_spawns[index_within_eligible_spawns]; }
            }
            if (spawnZoneIndex >= 0 && spawnZoneIndex < mapscript_list[map_selected].map_spawnzones.Length) 
            {
                spawnzone = mapscript_list[map_selected].map_spawnzones[spawnZoneIndex]; 
            }
            UnityEngine.Debug.Log("Eligible spawns: " + ConvertIntArrayToString(eligible_spawns) + "; farthest from players: " + spawnZoneIndex);
        }

        // If we can't find the farthest spawn, pick one at random
        if (spawnZoneIndex == -1 || spawnzone == null)
        {
            spawnZoneIndex = UnityEngine.Random.Range(0, mapscript_list[map_selected].map_spawnzones.Length);
            spawnzone = mapscript_list[map_selected].map_spawnzones[spawnZoneIndex];
        }

        float scaleFactor = 1.0f; float scaleMaxBias = 4.0f;
        if (local_plyAttr != null) { local_plyAttr.ResetPowerups(); }
        Bounds spawnZoneBounds = spawnzone.GetComponent<Collider>().bounds;
        scaleFactor = (spawnZoneBounds.center.x - spawnZoneBounds.min.x) / Networking.LocalPlayer.GetAvatarEyeHeightAsMeters();
        float min_x = Mathf.Lerp(spawnZoneBounds.min.x, spawnZoneBounds.center.x, scaleFactor);
        float max_x = Mathf.Lerp(spawnZoneBounds.max.x, spawnZoneBounds.center.x, scaleFactor);
        scaleFactor = (spawnZoneBounds.center.z - spawnZoneBounds.min.z) / Networking.LocalPlayer.GetAvatarEyeHeightAsMeters();
        float min_z = Mathf.Lerp(spawnZoneBounds.min.z, spawnZoneBounds.center.z, scaleFactor);
        float max_z = Mathf.Lerp(spawnZoneBounds.max.z, spawnZoneBounds.center.z, scaleFactor);
        var rx = UnityEngine.Random.Range(min_x, max_x);
        var rz = UnityEngine.Random.Range(min_z, max_z);

        platformHook.custom_force_unhook = true;
        Quaternion rotateTo = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation;
        if (spawnzone.orient_towards != null)
        {
            rotateTo = RotateTowards(Networking.LocalPlayer.GetPosition(), spawnzone.orient_towards.position) * Quaternion.Euler(new Vector3(0, 90.0f, 0));
        }
        Networking.LocalPlayer.TeleportTo(new Vector3(rx, spawnZoneBounds.center.y, rz), rotateTo);
        UnityEngine.Debug.Log("Teleporting player to spawn zone " + spawnZoneIndex);
        platformHook.custom_force_unhook = false;

        if (local_plyAttr != null) 
        { 
            local_plyAttr.in_ready_room = false; local_plyAttr.air_thrust_enabled = true; 
        }
    }

    public void TeleportLocalPlayerToKOTHtainer()
    {
        if (mapscript_list == null || map_selected >= mapscript_list.Length || mapscript_list[map_selected].map_spawnzones == null || mapscript_list[map_selected].map_spawnzones.Length == 0) { return; }
        bool found_kothtainer = false;
        foreach (map_element_kothtainer kothtainer in mapscript_list[map_selected].map_kothtainers)
        {
            if (option_teamplay && kothtainer.team_id == GetGlobalTeam(Networking.LocalPlayer.playerId) && kothtainer.start_zone != null) 
            { 
                found_kothtainer = true;
                kothtainer.gameObject.SetActive(true);
                platformHook.custom_force_unhook = true;
                float scaleFactor = 1.0f; float scaleMaxBias = 4.0f;
                if (local_plyAttr != null) 
                { 
                    scaleFactor = local_plyAttr.ply_scale; 
                    local_plyAttr.ResetPowerups();
                    local_plyAttr.cached_kothtainer = kothtainer;
                }
                scaleFactor *= 2.0f;
                Bounds zoneBounds = kothtainer.start_zone.bounds;
                float min_x = Mathf.Lerp(zoneBounds.min.x, zoneBounds.center.x, (scaleFactor - 1.0f) / scaleMaxBias);
                float max_x = Mathf.Lerp(zoneBounds.max.x, zoneBounds.center.x, (scaleFactor - 1.0f) / scaleMaxBias);
                float min_z = Mathf.Lerp(zoneBounds.min.z, zoneBounds.center.z, (scaleFactor - 1.0f) / scaleMaxBias);
                float max_z = Mathf.Lerp(zoneBounds.max.z, zoneBounds.center.z, (scaleFactor - 1.0f) / scaleMaxBias);
                var rx = UnityEngine.Random.Range(min_x, max_x);
                var rz = UnityEngine.Random.Range(min_z, max_z);

                platformHook.custom_force_unhook = true;
                Quaternion rotateTo = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation;
                Networking.LocalPlayer.TeleportTo(new Vector3(rx, zoneBounds.center.y, rz), rotateTo);
                platformHook.custom_force_unhook = false;
            }
        }
        if (!found_kothtainer) { TeleportLocalPlayerToGameSpawnZone(); }
    }

    [NetworkCallable]
    public void NetworkTeleportToReadyRoom()
    {
        bool should_teleport = false;
        if (local_plyAttr != null)
        {
            int global_team = GetGlobalTeam(Networking.LocalPlayer.playerId);
            should_teleport = (local_plyAttr.ply_training || (Networking.IsOwner(gameObject) && !local_plyAttr.in_ready_room)) && (local_plyAttr.ply_team >= 0 || global_team >= 0);
            should_teleport = should_teleport || (local_plyAttr.ply_team == (int)player_tracking_name.WaitingForLobby || global_team == (int)player_tracking_name.WaitingForLobby);
            should_teleport = should_teleport || local_plyAttr.in_spectator_area;
            if (should_teleport) { TeleportLocalPlayerToReadyRoom(); }
        }
    }

    public void TeleportLocalPlayerToReadyRoom()
    {
        if (!room_ready_script.warning_acknowledged) { return; }

        Networking.LocalPlayer.SetVelocity(new Vector3(0.0f, 0.0f, 0.0f));
        //room_training.SetActive(false);
        //var plyWeaponObj = FindPlayerOwnedObject(Networking.LocalPlayer, "PlayerWeapon");
        //var plyHitboxObj = FindPlayerOwnedObject(Networking.LocalPlayer, "PlayerHitbox");
        //plyWeapon.SetActive(false);

        if (local_plyweapon != null && local_plyweapon.gameObject.activeInHierarchy) { local_plyweapon.GetComponent<VRCPickup>().Drop(); local_plyweapon.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "ToggleActive", false); }
        if (local_secondaryweapon != null && local_secondaryweapon.gameObject.activeInHierarchy) { local_secondaryweapon.GetComponent<VRCPickup>().Drop(); local_secondaryweapon.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "ToggleActive", false); }
        if (local_plyhitbox != null && local_plyhitbox.gameObject.activeInHierarchy) { local_plyhitbox.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "ToggleHitbox", false); }

        if (local_plyAttr != null)
        {
            local_plyAttr.ply_training = false;
            local_plyAttr.in_spectator_area = false;
            local_plyAttr.in_ready_room = true;
            //local_plyAttr.air_thrust_enabled = false;
            if (local_plyAttr.ply_team == (int)player_tracking_name.Spectator) { local_plyAttr.ply_state = (int)player_state_name.Spectator; }
            else if (local_plyAttr.ply_state == (int)player_state_name.Spectator) 
            {
                local_plyAttr.ply_team = (int)player_tracking_name.Spectator;
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "ChangeTeam", Networking.LocalPlayer.playerId, (int)player_tracking_name.Spectator, false); 
            }
            else if (local_plyAttr.ply_state != (int)player_state_name.Dead) { local_plyAttr.ply_state = (int)player_state_name.Joined; }
            local_plyAttr.ResetPowerups();
            local_plyAttr.ResetToDefaultStats();
            if (local_plyweapon != null) { local_plyweapon.ResetWeaponToDefault(); }
            if (local_secondaryweapon != null) { local_secondaryweapon.ResetWeaponToDefault(); }
            local_plyAttr.ply_scale = 1.0f;
            local_plyAttr.plyEyeHeight_desired = local_plyAttr.plyEyeHeight_default;
            local_plyAttr.plyEyeHeight_change = true;
            local_plyAttr.ply_dp = local_plyAttr.ply_dp_default;
            UnityEngine.Debug.Log("[TELEPORT_TEST]: Teleporting to Ready Room with new state " + local_plyAttr.ply_state + " and team " + local_plyAttr.ply_team);
        }
        ToggleReadyRoomCollisions(true);
        ToggleTrainingRoom(false);
        Quaternion rotateTo = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation;
        if (ui_round_scoreboard_canvas != null)
        {
            rotateTo = Quaternion.Inverse(RotateTowards(Networking.LocalPlayer.GetPosition(), ui_round_scoreboard_canvas.transform.position));
        }
        platformHook.custom_force_unhook = true;
        Networking.LocalPlayer.TeleportTo(room_ready_spawn.transform.position, rotateTo); //faceScoreboard * Networking.LocalPlayer.GetRotation()
        platformHook.custom_force_unhook = false;

        if (!(mapscript_list == null || map_selected >= mapscript_list.Length || map_selected < 0)) { mapscript_list[map_selected].room_spectator_area.SetActive(false); }
    }

    public void TeleportLocalPlayerToTrainingHall()
    {
        Networking.LocalPlayer.SetVelocity(new Vector3(0.0f, 0.0f, 0.0f));
        //room_training.SetActive(true);
        if (local_plyAttr != null) 
        { 
            local_plyAttr.ply_training = true;
            local_plyAttr.in_ready_room = false;
            local_plyAttr.air_thrust_enabled = true;
            if (local_plyAttr.ply_team == (int)player_tracking_name.Spectator) { local_plyAttr.ply_state = (int)player_state_name.Spectator; }
            else if (local_plyAttr.ply_state == (int)player_state_name.Spectator) 
            {
                local_plyAttr.ply_team = (int)player_tracking_name.Spectator;
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "ChangeTeam", Networking.LocalPlayer.playerId, (int)player_tracking_name.Spectator, false); 
            }
            else if (local_plyAttr.ply_team >= 0 || local_plyAttr.ply_team == (int)player_tracking_name.WaitingForLobby) { local_plyAttr.ply_state = (int)player_state_name.Joined; }
            else { local_plyAttr.ply_state = (int)player_state_name.Inactive; }
            UnityEngine.Debug.Log("[TELEPORT_TEST]: Teleporting to Training Hall with new state " + local_plyAttr.ply_state + " and team " + local_plyAttr.ply_team);
        }
        //var plyWeaponObj = FindPlayerOwnedObject(Networking.LocalPlayer, "PlayerWeapon");
        //var plyHitboxObj = FindPlayerOwnedObject(Networking.LocalPlayer, "PlayerHitbox");

        ToggleReadyRoomCollisions(false);
        ToggleTrainingRoom(true);
        platformHook.custom_force_unhook = true;
        Networking.LocalPlayer.TeleportTo(room_training_hallway_spawn.transform.position, room_training_hallway_spawn.transform.rotation); //faceScoreboard * Networking.LocalPlayer.GetRotation()
        platformHook.custom_force_unhook = false;

        // We want these to occur after the teleport, particular for CacheWeaponPos() so our player is already rotated when that runs and the weapon is properly placed in front of us rather than in a weird pre-teleport position.
        if (local_plyweapon != null && !local_plyweapon.gameObject.activeInHierarchy) { local_plyweapon.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "ToggleActive", true); local_plyweapon.UpdateStatsFromWeaponType(); local_plyweapon.CacheWeaponPos(true); }
        if (local_plyhitbox != null && local_plyhitbox.gameObject.activeInHierarchy) { local_plyhitbox.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "ToggleHitbox", false); }
        if (local_uiplytoself != null) { local_uiplytoself.UI_Damage(); }
        if (local_secondaryweapon != null && !local_secondaryweapon.gameObject.activeInHierarchy)
        {
            bool is_boss = (option_gamemode == (int)gamemode_name.BossBash && local_plyAttr.ply_team == 1) || local_plyAttr.ply_dual_wield;
            if (is_boss && Networking.LocalPlayer.IsUserInVR()) { local_secondaryweapon.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "ToggleActive", true); local_secondaryweapon.UpdateStatsFromWeaponType(); }
            local_secondaryweapon.CacheWeaponPos(true);
        }
    }

    public void TeleportLocalPlayerToTrainingArena()
    {
        Networking.LocalPlayer.SetVelocity(new Vector3(0.0f, 0.0f, 0.0f));
        //room_training.SetActive(true);
        if (local_plyAttr != null) 
        { 
            local_plyAttr.ply_training = true;
            local_plyAttr.in_ready_room = false;
            local_plyAttr.air_thrust_enabled = true;
            local_plyAttr.ply_state = (int)player_state_name.Alive;
            UnityEngine.Debug.Log("[TELEPORT_TEST]: Teleporting to Training Arena with new state " + local_plyAttr.ply_state + " and team " + local_plyAttr.ply_team);
        }
        //var plyWeaponObj = FindPlayerOwnedObject(Networking.LocalPlayer, "PlayerWeapon");
        // plyHitboxObj = FindPlayerOwnedObject(Networking.LocalPlayer, "PlayerHitbox");
        if (local_plyweapon != null && !local_plyweapon.gameObject.activeInHierarchy) { local_plyweapon.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "ToggleActive", true); local_plyweapon.UpdateStatsFromWeaponType(); }
        if (local_plyhitbox != null && !local_plyhitbox.gameObject.activeInHierarchy) { local_plyhitbox.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "ToggleHitbox", true); }
        if (local_secondaryweapon != null && !local_secondaryweapon.gameObject.activeInHierarchy) 
        {
            bool is_boss = local_secondaryweapon.weapon_type == (int)weapon_type_name.BossGlove || local_plyAttr.ply_dual_wield || (option_gamemode == (int)gamemode_name.BossBash && local_plyAttr.ply_team == 1);
            if (is_boss && Networking.LocalPlayer.IsUserInVR()) { local_secondaryweapon.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "ToggleActive", true); local_plyweapon.UpdateStatsFromWeaponType(); }
        }

        ToggleReadyRoomCollisions(false);
        ToggleTrainingRoom(true);
        platformHook.custom_force_unhook = true;
        Networking.LocalPlayer.TeleportTo(room_training_arena_spawn.transform.position, Networking.LocalPlayer.GetRotation()); //faceScoreboard * Networking.LocalPlayer.GetRotation()
        platformHook.custom_force_unhook = false;
    }

    public void TeleportLocalPlayerToSpectatorArea()
    {
        if (mapscript_list == null || map_selected >= mapscript_list.Length || map_selected < 0) { return; }
        mapscript_list[map_selected].room_spectator_area.SetActive(true);

        if (local_plyAttr != null) { local_plyAttr.in_spectator_area = true; local_plyAttr.in_ready_room = false; UnityEngine.Debug.Log("[TELEPORT_TEST]: Teleporting to Spectator Area with new state " + local_plyAttr.ply_state + " and team " + local_plyAttr.ply_team); } //local_plyAttr.air_thrust_enabled = false;
        if (local_plyweapon != null && local_plyweapon.gameObject.activeInHierarchy) { local_plyweapon.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "ToggleActive", false); }
        if (local_secondaryweapon != null && local_secondaryweapon.gameObject.activeInHierarchy) { local_secondaryweapon.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "ToggleActive", false); }
        if (local_plyhitbox != null && local_plyhitbox.gameObject.activeInHierarchy) { local_plyhitbox.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "ToggleHitbox", false); }

        //ToggleReadyRoomCollisions(false);
        ToggleTrainingRoom(false);
        platformHook.custom_force_unhook = true;
        Networking.LocalPlayer.TeleportTo(mapscript_list[map_selected].room_spectator_spawn.transform.position, Networking.LocalPlayer.GetRotation()); //faceScoreboard * Networking.LocalPlayer.GetRotation()
        platformHook.custom_force_unhook = false;
    }

    [NetworkCallable]
    public void SnapHighlightPhoto(int camera_id, Vector3 camera_pos, Quaternion camera_rot, Vector3 target_pos, Vector3 target_vec_rot, bool look_at_target, float scaleDist)
    {
        if (camera_id < 0 || camera_id > highlightCameras.Length) { return; }
        if (map_selected < 0 || map_selected > mapscript_list.Length || mapscript_list[map_selected] == null || mapscript_list[map_selected].map_campoints == null || mapscript_list[map_selected].map_campoints.Length == 0) { return; }

        if (highlight_cameras_snapped[camera_id] == true || highlight_cameras_active[camera_id] == true) { return; }

        highlight_cameras_waiting_on_sync[camera_id] = true; // Camera #1 is from plyAttr, which may not match owners with gameController. We put this is in as a failsafe so others don't try to overwrite it with their first KOs. It's still possible for desync to occur (i.e. Player #2 KOs & receives own network event -> Player #3 KOs & receives own network event -> Player #3 receives 2's event due to ping -> photo is now 2's on 2's screen, and 3 on 3's screen. Could be 2's or 3's for all other players.

        highlightCameras[camera_id].gameObject.SetActive(true);
        if (camera_id == 5) { victory_camera_plane.SetActive(true); }

        if (look_at_target)
        {
            //Vector3 new_pos = target_pos + (target_vec_rot * 5.0f * scaleDist) + (Vector3.up * 2.5f * scaleDist);
            Vector3 new_pos = target_pos + (target_vec_rot * 1.0f * scaleDist);
            if (camera_id != 5)
            {
                new_pos += target_vec_rot * 1.5f * scaleDist;
            }
            /*else if (flag_for_mobile_vr) 
            {
                new_pos -= target_vec_rot * 1.5f * scaleDist;
            }*/
            highlightCameras[camera_id].transform.position = new_pos;
            highlightCameras[camera_id].transform.rotation = RotateTowards(target_pos, new_pos);
        }
        else
        {
            highlightCameras[camera_id].transform.position = camera_pos;
            highlightCameras[camera_id].transform.rotation = camera_rot;
        }

        highlight_cameras_active[camera_id] = true;
        highlight_cameras_active_cnt++;
        highlight_cameras_ms[camera_id] = Networking.GetServerTimeInSeconds();
    }

    public void LocalDeclareSpectatorIntent(bool spectator_desired)
    {
        // Handle spectator intent
        if (spectator_desired && GetGlobalTeam(Networking.LocalPlayer.playerId) != (int)player_tracking_name.Spectator)
        {
            local_plyAttr.ply_team = (int)player_tracking_name.Spectator;
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "ChangeTeam", Networking.LocalPlayer.playerId, (int)player_tracking_name.Spectator, false);
        }
        else if (!spectator_desired && GetGlobalTeam(Networking.LocalPlayer.playerId) == (int)player_tracking_name.Spectator)
        {
            local_plyAttr.ply_team = (int)player_tracking_name.Unassigned;
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "ChangeTeam", Networking.LocalPlayer.playerId, (int)player_tracking_name.Unassigned, false);
        }
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

    public int[][] GetPlayersOnTeam(int team_id)
    {
        if (ply_tracking_dict_keys_arr == null || ply_tracking_dict_keys_arr.Length <= 0 || ply_tracking_dict_values_arr == null || ply_tracking_dict_values_arr.Length <= 0 || ply_tracking_dict_keys_arr.Length != ply_tracking_dict_values_arr.Length) { UnityEngine.Debug.Log("Invalid dictionary!"); return null; }
        return DictFindAllWithValue(team_id, ply_tracking_dict_keys_arr, ply_tracking_dict_values_arr, (int)dict_compare_name.Equals);
    }

    [NetworkCallable]
    public void ChangeTeam(int player_id, int new_team, bool from_ready_room_enter)
    {
        if (!Networking.IsOwner(gameObject)) { return; }
        //UnityEngine.Debug.Log("Change team request received :" + player_id + " -> " + new_team + " (" + from_ready_room_enter + ")");
        int change_index = DictIndexFromKey(player_id, ply_tracking_dict_keys_arr);
        if (change_index < 0) { UnityEngine.Debug.LogWarning("Couldn't find player to change teams for: " + player_id); return; }
        if (from_ready_room_enter && ply_tracking_dict_values_arr[change_index] >= 0) { return; } // Don't assign to 0 from ready room if they're already in-game
        ply_tracking_dict_values_arr[change_index] = new_team;
        ply_tracking_dict_values_str = ConvertIntArrayToString(ply_tracking_dict_values_arr);
        //UnityEngine.Debug.Log("[DICT_TEST]: Changing player " + player_id + " to team " + new_team + " (" + ply_tracking_dict_keys_str + " | " + ply_tracking_dict_values_str + ")");

        if (player_id == Networking.LocalPlayer.playerId) 
        {
            // Since the master never gets an OnDeserialization event, we can just change their personal team here
            if (local_plyAttr.ply_team == (int)player_tracking_name.WaitingForLobby && new_team >= 0) { PlaySFXFromArray(snd_ready_sfx_source, snd_ready_sfx_clips, (int)ready_sfx_name.JoinGame); }
            else if ((local_plyAttr.ply_team == (int)player_tracking_name.WaitingForLobby || local_plyAttr.ply_team >= 0) && new_team < 0 && new_team != local_plyAttr.ply_team) { PlaySFXFromArray(snd_ready_sfx_source, snd_ready_sfx_clips, (int)ready_sfx_name.LeaveGame); }
            else if (local_plyAttr.ply_team != (int)player_tracking_name.WaitingForLobby && new_team == (int)player_tracking_name.WaitingForLobby) { PlaySFXFromArray(snd_ready_sfx_source, snd_ready_sfx_clips, (int)ready_sfx_name.JoinMidGame); }
            else if (local_plyAttr.ply_team != (int)player_tracking_name.Spectator && new_team == (int)player_tracking_name.Spectator) { PlaySFXFromArray(snd_ready_sfx_source, snd_ready_sfx_clips, (int)ready_sfx_name.LeaveGame); }
            else if (local_plyAttr.ply_team != (int)player_tracking_name.Unassigned && new_team == (int)player_tracking_name.Unassigned) { PlaySFXFromArray(snd_ready_sfx_source, snd_ready_sfx_clips, (int)ready_sfx_name.LeaveGame); }

            local_plyAttr.ply_team = new_team;
            if (new_team == (int)player_tracking_name.Spectator) { local_plyAttr.ply_state = (int)player_state_name.Spectator; }
            else if (local_plyAttr.ply_state == (int)player_state_name.Spectator) { local_plyAttr.ply_state = (int)player_state_name.Inactive; }
            if (room_ready_script.warning_acknowledged) { room_ready_script.ZoneMarkerHighlight(true); }
        } 

        RefreshSetupUI();
        RequestSerialization();

        if (round_state == (int)round_state_name.Ongoing) 
        { 
            CheckForRoundGoal();
            
            if (option_gamemode == (int)gamemode_name.Infection && new_team == 1) 
            {
                // Infection has odd behavior with the initial respawn, but has proper distance on subsequent. We'll respawn a newly Infected player twice as a failsafe.
                PlayerAttributes plyAttr = FindPlayerAttributes(VRCPlayerApi.GetPlayerById(player_id));
                if (plyAttr != null && !plyAttr.ply_training) { plyAttr.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "TeleportLocalPlayerToGameSpawnZone"); }
                if (Networking.IsOwner(gameObject)) { round_extra_data++; RequestSerialization(); }
            }
        }

        
        //PlayerHitbox plyHitbox = GetPlayerHitboxFromID(player_id);
        //if (plyHitbox != null) { plyHitbox.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetTeamColor", new_team); }

    }

    // This is a last resort method used if we lose so much synchronization that we need to rebuild the player dictionaries entirely.
    public void ResetPlyDicts()
    {
        //if (!Networking.IsOwner(gameObject)) { return; }
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
        if (!Networking.IsOwner(gameObject)) { return; }
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

            int team_size_softcap = Mathf.Max(1, Mathf.Max(counts_per_team));
            // However, we use players_in_game_dict[0] instead of ply_tracking_dict_keys_arr because we only want to cap based on ACTIVE players
            int team_size_hardcap = (int)Mathf.Max(1.0f, Mathf.Ceil((float)players_in_game_dict[0].Length / (float)team_count));

            bool found_team = false;
            for (int j = team_count - 1; j >= 0; j--)
            {
                // If we are enforcing team limits, only distribute up to the limit for each team. Otherwise, distribute evenly.
                if (option_enforce_team_limits)
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
                    if (option_enforce_team_limits) { team_size_hardcap = option_team_limits_arr[j]; }

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
        local_plyAttr.ply_team = GetGlobalTeam(Networking.LocalPlayer.playerId); // Since the master never receives the serialization message, we'll assign their local team value here
        if (local_plyAttr.ply_team == (int)player_tracking_name.Spectator) { local_plyAttr.ply_state = (int)player_state_name.Spectator; }
        else if (local_plyAttr.ply_state == (int)player_state_name.Spectator) { local_plyAttr.ply_state = (int)player_state_name.Inactive; }
        RequestSerialization();
    }

    public void OOBFailsafe()
    {
        // If you are this far out of bounds, something has gone supremely wrong; just reposition so player is not trapped in an unusable VRChat
        float max_bound = 10000.0f;
        if (Mathf.Abs(Networking.LocalPlayer.GetPosition().x) > max_bound || Mathf.Abs(Networking.LocalPlayer.GetPosition().y) > max_bound || Mathf.Abs(Networking.LocalPlayer.GetPosition().z) > max_bound)
        {
            if (local_plyAttr != null 
                && (local_plyAttr.ply_state != (int)player_state_name.Inactive && local_plyAttr.ply_state != (int)player_state_name.Joined)
                && (round_state == (int)round_state_name.Ready || round_state == (int)round_state_name.Ongoing))
            { TeleportLocalPlayerToGameSpawnZone(); }
            else { TeleportLocalPlayerToReadyRoom(); }
        }
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

    /*public PlayerAttributes FindPlayerAttributes(VRCPlayerApi player)
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
    }*/

    public int GetGlobalTeam(int player_id)
    {
        return DictValueFromKey(player_id, ply_tracking_dict_keys_arr, ply_tracking_dict_values_arr);
    }

    public int[] GetEligibleSpawnsForPlayer(int player_id, bool force_use_team = false)
    {
        VRCPlayerApi player = VRCPlayerApi.GetPlayerById(player_id);
        if (player == null || map_selected > mapscript_list.Length || mapscript_list[map_selected] == null || mapscript_list[map_selected].map_spawnzones == null) { return null; }
        int[] spawnzones_from_full = new int[mapscript_list[map_selected].map_spawnzones.Length];
        ushort spawnzone_count = 0;

        for (int i = 0; i < mapscript_list[map_selected].map_spawnzones.Length; i++)
        {
            map_element_spawn spawnzone = mapscript_list[map_selected].map_spawnzones[i];
            if (spawnzone == null
                || (option_teamplay && (force_use_team || !option_enforce_team_limits) && spawnzone.team_id != GetGlobalTeam(player.playerId) && spawnzone.team_id >= 0)
                || (option_teamplay && (force_use_team || !option_enforce_team_limits) && spawnzone.team_id == -2) // FFA-only
                || (GetPlayersInGame()[0].Length < spawnzone.min_players && spawnzone.min_players > 0)
                || (spawnzone.enabled == false || spawnzone.gameObject.activeInHierarchy == false)
                ) { continue; }
            spawnzones_from_full[spawnzone_count] = i;
            spawnzone_count++;
        }

        int[] spawnzones = new int[spawnzone_count];
        for (int i = 0; i < spawnzone_count; i++)
        {
            spawnzones[i] = spawnzones_from_full[i];
        }

        return spawnzones;
    }

    public int GetSpawnFarthestFromPlayers(int player_id, int[] spawnzones, bool force_use_team = false)
    {
        VRCPlayerApi player = VRCPlayerApi.GetPlayerById(player_id);
        int[][] plyInGame = GetPlayersInGame(); //|| plyInGame.Length < 2 
        if (player == null || spawnzones == null || plyInGame == null || plyInGame[0].Length == 0 || spawnzones.Length == 0 || map_selected > mapscript_list.Length || mapscript_list[map_selected] == null || mapscript_list[map_selected].map_spawnzones == null) { return -1; }

        int team_id = GetGlobalTeam(player_id);
        int spawnzoneIndex = -1;
        LayerMask layers_to_hit = LayerMask.GetMask("PlayerHitbox");
        float maxDistanceFromZones = 0.0f;
        //float[] minDistancePerSpawnzone = new float[spawnzones.Length];
        for (int i = 0; i < spawnzones.Length; i++)
        {
            float plyDistanceMinInZone = -1;
            map_element_spawn spawnzone = mapscript_list[map_selected].map_spawnzones[spawnzones[i]];
            if (spawnzone == null) { continue; }
            int[] debug_test_list = new int[plyInGame[0].Length];
            for (int j = 0; j < plyInGame[0].Length; j++)
            {
                VRCPlayerApi test_player = VRCPlayerApi.GetPlayerById(plyInGame[0][j]);
                if (test_player == null || test_player == player || (option_teamplay && (force_use_team || !option_enforce_team_limits) && plyInGame[1][j] == team_id)) { continue; }
                float testDistance = Mathf.Abs(Vector3.Distance(spawnzone.transform.position, test_player.GetPosition()));
                if (plyDistanceMinInZone > testDistance || plyDistanceMinInZone < 0) { plyDistanceMinInZone = testDistance; }
                debug_test_list[j] = Mathf.RoundToInt(testDistance);
            }
            //UnityEngine.Debug.Log("[RESPAWN_TEST]: " + spawnzone.name + " distance from players: " + ConvertIntArrayToString(debug_test_list));
            if (maxDistanceFromZones < plyDistanceMinInZone) { maxDistanceFromZones = plyDistanceMinInZone; spawnzoneIndex = i; }
        }

        // If we couldn't find anything, then just return a random eligible spawn
        if (spawnzoneIndex == -1) { return UnityEngine.Random.Range(0, spawnzones.Length); }

        return spawnzoneIndex;
    }


    // -- Game Helper Functions --
    public void PlaySFXFromArray(AudioSource source, AudioClip[] clips, int index = -1, float pitch = 1.0f, bool is_music = false, float time_to_skip_to = 0.0f)
    {
        AudioClip clip_to_play = null;
        float volume_scale = 1.0f;
        if (local_ppp_options != null) { volume_scale = is_music ? local_ppp_options.music_volume : local_ppp_options.sound_volume; }

        if (clips == null || clips.Length <= 0) { return; }
        if (index < 0)
        {
            int randClip = UnityEngine.Random.Range(0, clips.Length);
            clip_to_play = clips[randClip];
        }
        else if (index < clips.Length && clips[index] != null)
        {
            clip_to_play = clips[index];
        }
        source.Stop();
        source.pitch = pitch;
        if (is_music)
        {
            music_clip_desired = clip_to_play; // We store this as a means of returning the music back to where it "should" be if we turn off music override

            bool allow_override = local_ppp_options != null && local_ppp_options.music_override >= 0;
            allow_override = allow_override && clips != snd_victory_music_clips && clips != snd_defeat_music_clips;
            allow_override = allow_override && !(clips == snd_infection_music_clips && index != (int)infection_music_name.Start);
            if (allow_override)
            {
                clips = snd_override_music_clips;
                if (clips != null && local_ppp_options.music_override < clips.Length)
                {
                    clip_to_play = clips[local_ppp_options.music_override];
                }
            }

            source.loop = true;
            source.clip = clip_to_play;
            source.volume = music_volume_default * volume_scale;
            //music_volume_stored = source.volume;
             music_clip_playing = clip_to_play;
            source.Play();
            source.time = time_to_skip_to;
        }
        else if (local_ppp_options != null) { source.PlayOneShot(clip_to_play, volume_scale); }
        else { source.PlayOneShot(clip_to_play, 1.0f); }

    }

    // Overloading method to allow for color as an optional parameter
    public void AddToLocalTextQueue(string input) 
    { 
        if (local_uiplytoself != null) { local_uiplytoself.AddToTextQueue(input); }
    }
    public void AddToLocalTextQueue(string input, Color color)
    {
        if (local_uiplytoself != null) { local_uiplytoself.AddToTextQueue(input, color); }
    }
    public void AddToLocalTextQueue(string input, Color color, float duration)
    {
        if (local_uiplytoself != null) { local_uiplytoself.AddToTextQueue(input, color, duration); }
    }
    public void AddToLocalTextQueue(string input, float duration)
    {
        if (local_uiplytoself != null) { local_uiplytoself.AddToTextQueue(input, Color.white, duration); }
    }

    [NetworkCallable]
    public void NetworkAddToTextQueue(string input, string arg0, Color color, float duration)
    {
        string localized_input = localizer.FetchText(input, input, arg0);

        if (local_uiplytoself != null) 
        { 
            if (localized_input.Contains(localizer.FetchText("NOTIFICATION_ELIMINATION", "$ARG0 has been eliminated!", "").Trim())
                && local_plyAttr != null 
                 && ((local_plyAttr.ply_state != (int)player_state_name.Alive && local_plyAttr.ply_state != (int)player_state_name.Inactive)
                 || local_plyAttr.ply_team < 0 || local_plyAttr.in_ready_room)
                )
            {
                return;
            }
            local_uiplytoself.AddToTextQueue(localized_input, color, duration); 
        }
    }

    public string ModifyModeDescription(string input)
    {
        var gamemode_description = input.ToUpper();
        gamemode_description = gamemode_description.Replace("$TIMER", round_length.ToString());
        gamemode_description = gamemode_description.Replace("$POINTS_A", option_gm_goal.ToString());
        if (option_gm_goal == 1) { gamemode_description = gamemode_description.Replace("POINTS", "POINT").Replace("KOS", "KO").Replace("KNOCKOUTS", "KNOCKOUT"); }
        gamemode_description = gamemode_description.Replace("$LIVES", plysettings_lives.ToString());
        if (plysettings_lives == 1) { gamemode_description = gamemode_description.Replace("LIVES", "LIFE"); }
        if (option_gamemode == (int)gamemode_name.BossBash && option_gm_config_a == 1) { gamemode_description = gamemode_description.Replace("TIMES", "TIME"); }
        return gamemode_description;
    }

    // -- Internal Helper Functions --
    public int PreallocNextAvailableIndex(int[] refs_arr)
    {
        if (refs_arr == null || refs_arr.Length == 0) { return -1; }
        int smallest_index_found = -1; int i = 0;
        while (i < refs_arr.Length)
        {
            if ((smallest_index_found + 1) == refs_arr[i]) { smallest_index_found = refs_arr[i]; i = 0; }
            else if (smallest_index_found + 1 >= refs_arr.Length) { return -1; }
            i++;
        }
        if (smallest_index_found + 1 >= refs_arr.Length) { return -1; }
        return (smallest_index_found + 1);
    }

    public GameObject PreallocAddSlot(int prealloc_obj_type)
    {
        GameObject return_obj = null;
        if (prealloc_obj_type == (int)prealloc_obj_name.ItemPowerup)
        {
            return_obj = global_powerup_arr[global_lowest_available_powerup_index].gameObject;
            ItemPowerup powerup = global_powerup_arr[global_lowest_available_powerup_index];
            global_powerup_refs[global_powerup_cnt] = global_lowest_available_powerup_index;
            powerup.ref_index = global_powerup_cnt;
            global_powerup_cnt++;
            global_lowest_available_powerup_index = PreallocNextAvailableIndex(global_powerup_refs);
        }
        else if (prealloc_obj_type == (int)prealloc_obj_name.UIHarmNumber)
        {
            return_obj = global_harmnumber_arr[global_lowest_available_harmnumber_index].gameObject;
            UIHarmNumber harmnumber = global_harmnumber_arr[global_lowest_available_harmnumber_index];
            global_harmnumber_refs[global_harmnumber_cnt] = global_lowest_available_harmnumber_index;
            harmnumber.ref_index = global_harmnumber_cnt;
            global_harmnumber_cnt++;
            global_lowest_available_harmnumber_index = PreallocNextAvailableIndex(global_harmnumber_refs);
        }

        return return_obj;
    }

    public void PreallocClearSlot(int prealloc_obj_type, int global_index, ref int ref_index)
    {
        if (prealloc_obj_type == (int)prealloc_obj_name.ItemPowerup)
        {
            if (ref_index > -1 && global_powerup_refs != null)
            {
                // Free the index from the refs table by swapping it with the most recent entry in the list
                if (global_powerup_cnt > 0 && global_powerup_cnt <= global_powerup_refs.Length)
                {
                    global_powerup_refs[ref_index] = global_powerup_refs[global_powerup_cnt - 1];
                    global_powerup_arr[global_powerup_refs[ref_index]].ref_index = ref_index;
                    global_powerup_refs[global_powerup_cnt - 1] = -1;
                    ref_index = -1;
                    global_powerup_cnt--;
                }

                // Free the index from the global table
                if (global_index < global_lowest_available_powerup_index || global_lowest_available_powerup_index == -1)
                {
                    global_lowest_available_powerup_index = global_index;
                }
            }
        }
        else if (prealloc_obj_type == (int)prealloc_obj_name.UIHarmNumber)
        {
            if (ref_index > -1 && global_harmnumber_refs != null)
            {
                // Free the index from the refs table by swapping it with the most recent entry in the list
                if (global_harmnumber_cnt > 0 && global_harmnumber_cnt <= global_harmnumber_refs.Length)
                {
                    global_harmnumber_refs[ref_index] = global_harmnumber_refs[global_harmnumber_cnt - 1];
                    global_harmnumber_arr[global_harmnumber_refs[ref_index]].ref_index = ref_index;
                    global_harmnumber_refs[global_harmnumber_cnt - 1] = -1;
                    ref_index = -1;
                    global_harmnumber_cnt--;
                }

                // Free the index from the global table
                if (global_index < global_lowest_available_harmnumber_index || global_lowest_available_harmnumber_index == -1)
                {
                    global_lowest_available_harmnumber_index = global_index;
                }
            }
        }
    }

    public void ResetAllObjOwners()
    {
        ply_object_owners = new int[(int)GLOBAL_CONST.UDON_MAX_PLAYERS];
        ply_object_plyattr = new PlayerAttributes[(int)GLOBAL_CONST.UDON_MAX_PLAYERS];
        ply_object_plyhitbox = new PlayerHitbox[(int)GLOBAL_CONST.UDON_MAX_PLAYERS];
        ply_object_plyweapon = new PlayerWeapon[(int)GLOBAL_CONST.UDON_MAX_PLAYERS];
        ply_object_secondaryweapon = new PlayerWeapon[(int)GLOBAL_CONST.UDON_MAX_PLAYERS];
        ply_object_uiplytoothers = new UIPlyToOthers[(int)GLOBAL_CONST.UDON_MAX_PLAYERS];
        ply_owners_cnt = 0;

        VRCPlayerApi[] players = new VRCPlayerApi[(int)GLOBAL_CONST.UDON_MAX_PLAYERS];
        VRCPlayerApi.GetPlayers(players);
        foreach (VRCPlayerApi player in players)
        {
            if (ply_owners_cnt < ply_object_owners.Length) { ply_object_owners[ply_owners_cnt] = player.playerId; }

            var plyAttributesObj = FindPlayerOwnedObject(player, "PlayerAttributes");
            var plyHitboxObj = FindPlayerOwnedObject(player, "PlayerHitbox");
            var plyWeaponObj = FindPlayerOwnedObject(player, "PlayerWeapon");
            var plySecondaryObj = FindPlayerOwnedObject(player, "SecondaryWeapon");
            var plyUIToOthers = FindPlayerOwnedObject(player, "UIPlyToOthers");

            if (plyAttributesObj != null && ply_owners_cnt < ply_object_plyattr.Length)
            {
                ply_object_plyattr[ply_owners_cnt] = plyAttributesObj.GetComponent<PlayerAttributes>();
                UnityEngine.Debug.Log("[OWNERS_TEST]: Found and added plyAttributesObj belonging to " + player.playerId + " (" + player.displayName + ") to index " + ply_owners_cnt);
            }
            if (plyHitboxObj != null && ply_owners_cnt < ply_object_plyhitbox.Length)
            {
                ply_object_plyhitbox[ply_owners_cnt] = plyHitboxObj.GetComponent<PlayerHitbox>();
                UnityEngine.Debug.Log("[OWNERS_TEST]: Found and added plyHitboxObj belonging to " + player.playerId + " (" + player.displayName + ") to index " + ply_owners_cnt);
            }
            if (plyWeaponObj != null && ply_owners_cnt < ply_object_plyweapon.Length)
            {
                ply_object_plyweapon[ply_owners_cnt] = plyWeaponObj.GetComponent<PlayerWeapon>();
                UnityEngine.Debug.Log("[OWNERS_TEST]: Found and added plyWeaponObj belonging to " + player.playerId + " (" + player.displayName + ") to index " + ply_owners_cnt);
            }
            if (plySecondaryObj != null && ply_owners_cnt < ply_object_secondaryweapon.Length)
            {
                ply_object_secondaryweapon[ply_owners_cnt] = plySecondaryObj.GetComponent<PlayerWeapon>();
                UnityEngine.Debug.Log("[OWNERS_TEST]: Found and added plySecondaryObj belonging to " + player.playerId + " (" + player.displayName + ") to index " + ply_owners_cnt);

            }
            if (plyUIToOthers != null && ply_owners_cnt < ply_object_uiplytoothers.Length)
            {
                ply_object_uiplytoothers[ply_owners_cnt] = plyUIToOthers.GetComponent<UIPlyToOthers>();
                UnityEngine.Debug.Log("[OWNERS_TEST]: Found and added plyUIToOthers belonging to " + player.playerId + " (" + player.displayName + ") to index " + ply_owners_cnt);
            }

            ply_owners_cnt++;
            UnityEngine.Debug.Log("[OWNERS_TEST]: Incrementing owner count to " + ply_owners_cnt);
        }
    }

    /*public void AddPlayerToObjOwners(VRCPlayerApi player, bool increment_counter = false)
    {
        if (player == null) { return; }

        UnityEngine.Debug.Log("[OWNERS_TEST]: Adding " + player.playerId + " (" + player.displayName + ") to object owners at index " + ply_owners_cnt);
        
        if (ply_object_owners == null || ply_owners_cnt <= 0 || ply_object_owners.Length < (int)GLOBAL_CONST.UDON_MAX_PLAYERS) 
        {
            UnityEngine.Debug.Log("[OWNERS_TEST]: New owners array of " + (int)GLOBAL_CONST.UDON_MAX_PLAYERS + " created.");
            ply_object_owners = new int[(int)GLOBAL_CONST.UDON_MAX_PLAYERS];
            ply_object_plyattr = new PlayerAttributes[(int)GLOBAL_CONST.UDON_MAX_PLAYERS];
            ply_object_plyhitbox = new PlayerHitbox[(int)GLOBAL_CONST.UDON_MAX_PLAYERS];
            ply_object_plyweapon = new PlayerWeapon[(int)GLOBAL_CONST.UDON_MAX_PLAYERS];
            ply_object_secondaryweapon = new PlayerWeapon[(int)GLOBAL_CONST.UDON_MAX_PLAYERS];
            ply_object_uiplytoothers = new UIPlyToOthers[(int)GLOBAL_CONST.UDON_MAX_PLAYERS];
        }

        // Ensure the ID is not already there
        for (int i = 0; i < ply_object_owners.Length; i++)
        {
            if (ply_object_owners[i] == player.playerId) { return; }
        }

        UnityEngine.Debug.Log("[OWNERS_TEST]: Player ID " + player.playerId + " (" + player.displayName + ") is not in the owners array of length " + ply_object_owners.Length + " (current owner count: " + ply_owners_cnt + ")");

        if (ply_owners_cnt < ply_object_owners.Length) { ply_object_owners[ply_owners_cnt] = player.playerId; }

        var plyAttributesObj = FindPlayerOwnedObject(player, "PlayerAttributes");
        var plyHitboxObj = FindPlayerOwnedObject(player, "PlayerHitbox");
        var plyWeaponObj = FindPlayerOwnedObject(player, "PlayerWeapon");
        var plySecondaryObj = FindPlayerOwnedObject(player, "SecondaryWeapon");
        var plyUIToOthers = FindPlayerOwnedObject(player, "UIPlyToOthers");

        if (plyAttributesObj != null && ply_owners_cnt < ply_object_plyattr.Length) 
        { 
            ply_object_plyattr[ply_owners_cnt] = plyAttributesObj.GetComponent<PlayerAttributes>();
            UnityEngine.Debug.Log("[OWNERS_TEST]: Found and added plyAttributesObj belonging to " + player.playerId + " (" + player.displayName + ") to index " + ply_owners_cnt);
        }
        if (plyHitboxObj != null && ply_owners_cnt < ply_object_plyhitbox.Length) 
        { 
            ply_object_plyhitbox[ply_owners_cnt] = plyHitboxObj.GetComponent<PlayerHitbox>();
            UnityEngine.Debug.Log("[OWNERS_TEST]: Found and added plyHitboxObj belonging to " + player.playerId + " (" + player.displayName + ") to index " + ply_owners_cnt);
        }
        if (plyWeaponObj != null && ply_owners_cnt < ply_object_plyweapon.Length) 
        { 
            ply_object_plyweapon[ply_owners_cnt] = plyWeaponObj.GetComponent<PlayerWeapon>();
            UnityEngine.Debug.Log("[OWNERS_TEST]: Found and added plyWeaponObj belonging to " + player.playerId + " (" + player.displayName + ") to index " + ply_owners_cnt);
        }
        if (plySecondaryObj != null && ply_owners_cnt < ply_object_secondaryweapon.Length) 
        { 
            ply_object_secondaryweapon[ply_owners_cnt] = plySecondaryObj.GetComponent<PlayerWeapon>();
            UnityEngine.Debug.Log("[OWNERS_TEST]: Found and added plySecondaryObj belonging to " + player.playerId + " (" + player.displayName + ") to index " + ply_owners_cnt);

        }
        if (plyUIToOthers != null && ply_owners_cnt < ply_object_uiplytoothers.Length) 
        { 
            ply_object_uiplytoothers[ply_owners_cnt] = plyUIToOthers.GetComponent<UIPlyToOthers>();
            UnityEngine.Debug.Log("[OWNERS_TEST]: Found and added plyUIToOthers belonging to " + player.playerId + " (" + player.displayName + ") to index " + ply_owners_cnt);
        }

        if (increment_counter) 
        {
            // This is typically handled by the end of OnPlayerJoined(), but if we want to manually add to the counter, this is an object
            ply_owners_cnt++;
            UnityEngine.Debug.Log("[OWNERS_TEST]: Incrementing owner count to " + ply_owners_cnt);
        } 
    }*/

    public void RemovePlayerFromObjOwners(int player_id)
    {
        if (ply_object_owners == null || ply_owners_cnt <= 0) { return; }
        int owner_index = -1;
        for (int i = 0; i < ply_object_owners.Length; i++)
        {
            if (ply_object_owners[i] == player_id) { owner_index = i; break; }
        }
        if (owner_index < 0) { return; }

        UnityEngine.Debug.Log("[PLAYER_LEAVE_TEST] Removing player ID " + player_id + " at ply_obj_owners index " + owner_index);

        for (int i = owner_index + 1; i < ply_object_owners.Length; i++)
        {
            ply_object_plyattr[i - 1] = ply_object_plyattr[i];
            ply_object_plyweapon[i - 1] = ply_object_plyweapon[i];
            ply_object_secondaryweapon[i - 1] = ply_object_secondaryweapon[i];
            ply_object_plyhitbox[i - 1] = ply_object_plyhitbox[i];
            ply_object_uiplytoothers[i - 1] = ply_object_uiplytoothers[i];
            ply_object_owners[i - 1] = ply_object_owners[i];
        }

        ply_object_plyattr[ply_object_owners.Length - 1] = null;
        ply_object_plyweapon[ply_object_owners.Length - 1] = null;
        ply_object_secondaryweapon[ply_object_owners.Length - 1] = null;
        ply_object_uiplytoothers[ply_object_owners.Length - 1] = null;
        ply_object_plyattr[ply_object_owners.Length - 1] = null;
        ply_object_owners[ply_object_owners.Length - 1] = 0;

        ply_owners_cnt--;

        UnityEngine.Debug.Log("[PLAYER_LEAVE_TEST] Removed player ID from owners [" + ConvertIntArrayToString(ply_object_owners) + "]");

    }

    public int GetPlayerObjIndexFromID(int player_id)
    {
        if (ply_object_owners == null || ply_object_owners.Length == 0) { return -1; }
        if (player_id < 0 || ply_owners_cnt <= 0) { return -1; } // Player IDs are always positive
        VRCPlayerApi player = VRCPlayerApi.GetPlayerById(player_id);
        if (player == null) { return -1; } // Make sure the player is still connected to the game
        for (int i = 0; i < ply_owners_cnt; i++)
        {
            if (ply_object_owners[i] == player_id) { return i; }
        }
        // If by the end of the search we find no owner, return invalid value
        return -1;
    }

    public PlayerAttributes FindPlayerAttributes(VRCPlayerApi player)
    {
        if (player == null) { return null; }
        //UnityEngine.Debug.Log("FindPlayerAttributes() attempted; ply_object_owners != null && ply_owners_cnt > 0 = " + (ply_object_owners != null && ply_owners_cnt > 0));
        if (ply_object_owners != null && ply_owners_cnt > 0 && player != null) { return GetPlayerAttributesFromID(player.playerId); }
        else
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
    }

    public PlayerAttributes GetPlayerAttributesFromID(int player_id)
    {
        VRCPlayerApi player = VRCPlayerApi.GetPlayerById(player_id);
        if (player == null) { return null; }
        if (ply_object_owners == null || ply_object_owners.Length == 0 || ply_object_plyattr == null || ply_object_plyattr.Length == 0) { return FindPlayerOwnedObject(player, "PlayerAttributes").GetComponent<PlayerAttributes>(); }
        //UnityEngine.Debug.Log("GetPlayerAttributesFromID() attempted for " + player_id + "; ply_object_owners = " + ConvertIntArrayToString(ply_object_owners));

        int ply_obj_index = GetPlayerObjIndexFromID(player_id);
        if (ply_obj_index < 0) { return FindPlayerOwnedObject(player, "PlayerAttributes").GetComponent<PlayerAttributes>(); }
        PlayerAttributes plyAttr = ply_object_plyattr[ply_obj_index];
        //UnityEngine.Debug.Log("GetPlayerAttributesFromID() fetched player attributes from index " + ply_obj_index + ": " + ply_object_plyattr[ply_obj_index]);

        return plyAttr;
    }
    
    public PlayerWeapon GetPlayerWeaponFromID(int player_id)
    {
        VRCPlayerApi player = VRCPlayerApi.GetPlayerById(player_id);
        if (player == null) { return null; }
        if (ply_object_owners == null || ply_object_owners.Length == 0 || ply_object_plyweapon == null || ply_object_plyweapon.Length == 0) { return FindPlayerOwnedObject(player, "PlayerWeapon").GetComponent<PlayerWeapon>(); }
        int ply_obj_index = GetPlayerObjIndexFromID(player_id);
        if (ply_obj_index < 0) { return FindPlayerOwnedObject(player, "PlayerWeapon").GetComponent<PlayerWeapon>(); }
        PlayerWeapon plyWeapon = ply_object_plyweapon[ply_obj_index];
        return plyWeapon;
    }

    public PlayerWeapon GetSecondaryWeaponFromID(int player_id)
    {
        VRCPlayerApi player = VRCPlayerApi.GetPlayerById(player_id);
        if (player == null) { return null; }
        if (ply_object_owners == null || ply_object_owners.Length == 0 || ply_object_secondaryweapon == null || ply_object_secondaryweapon.Length == 0) { return FindPlayerOwnedObject(player, "SecondaryWeapon").GetComponent<PlayerWeapon>(); }
        int ply_obj_index = GetPlayerObjIndexFromID(player_id);
        if (ply_obj_index < 0) { return FindPlayerOwnedObject(player, "SecondaryWeapon").GetComponent<PlayerWeapon>(); }
        PlayerWeapon plyWeapon = ply_object_secondaryweapon[ply_obj_index];
        return plyWeapon;
    }

    public PlayerHitbox GetPlayerHitboxFromID(int player_id)
    {
        VRCPlayerApi player = VRCPlayerApi.GetPlayerById(player_id);
        if (player == null) { return null; }
        if (ply_object_owners == null || ply_object_owners.Length == 0 || ply_object_plyhitbox == null || ply_object_plyhitbox.Length == 0) { return FindPlayerOwnedObject(player, "PlayerHitbox").GetComponent<PlayerHitbox>(); }
        int ply_obj_index = GetPlayerObjIndexFromID(player_id);
        if (ply_obj_index < 0) { return FindPlayerOwnedObject(player, "PlayerHitbox").GetComponent<PlayerHitbox>(); }
        PlayerHitbox plyHitbox = ply_object_plyhitbox[ply_obj_index];
        return plyHitbox;
    }

    public UIPlyToOthers GetUIPlyToOthersFromID(int player_id)
    {
        VRCPlayerApi player = VRCPlayerApi.GetPlayerById(player_id);
        if (player == null) { return null; }
        if (ply_object_owners == null || ply_object_owners.Length == 0 || ply_object_uiplytoothers == null || ply_object_uiplytoothers.Length == 0) { return FindPlayerOwnedObject(player, "UIPlyToOthers").GetComponent<UIPlyToOthers>(); }
        int ply_obj_index = GetPlayerObjIndexFromID(player_id);
        if (ply_obj_index < 0) { return FindPlayerOwnedObject(player, "UIPlyToOthers").GetComponent<UIPlyToOthers>(); }
        UIPlyToOthers plyToOthers = ply_object_uiplytoothers[ply_obj_index];
        return plyToOthers;
    }

    public void ResetPPPCanvasGlobal()
    {
        if (local_ppp_options != null)
        {
            local_ppp_options.ResetPPPCanvas();
        }
    }

    [NetworkCallable]
    public void DebugToggleObject(bool toggle, string debug_obj_name)
    {
        if (debug_obj_name == "UIPlyToSelf" && local_uiplytoself != null) { local_uiplytoself.gameObject.SetActive(toggle); }
        else if (debug_obj_name == "PlayerHitbox" && local_plyhitbox != null) { local_plyhitbox.ToggleHitbox(toggle); }
        else if (debug_obj_name == "PlayerWeapon" && local_plyweapon != null) { local_plyweapon.ToggleActive(toggle); }
        else if (debug_obj_name == "Scoreboard" && ui_round_scoreboard_canvas != null) { ui_round_scoreboard_canvas.gameObject.SetActive(toggle); }
        else if (debug_obj_name == "UIPlyToOthers") 
        { 
            foreach (UIPlyToOthers uiPlyToOthers in ply_object_uiplytoothers)
            {
                if (uiPlyToOthers != null) { uiPlyToOthers.gameObject.SetActive(toggle); }
            }
        }
        else if (debug_obj_name == "GameController") { debug_run_update = toggle; }
    }

    [NetworkCallable]
    public void DebugModifyVariable(int value, string debug_param_name)
    {
        if (debug_param_name == "DualWield" && local_plyAttr != null && local_plyweapon != null)
        {
            local_plyAttr.ply_dual_wield = IntToBool(value);
            local_plyweapon.UpdateStatsFromWeaponType();
            if (local_plyweapon.gameObject.activeInHierarchy && local_secondaryweapon != null && (round_state == (int)round_state_name.Ready || round_state == (int)round_state_name.Ongoing || local_plyAttr.ply_training))
            {
                local_secondaryweapon.ToggleActive(IntToBool(value));
                local_secondaryweapon.UpdateStatsFromWeaponType();
            }
        }
        else if (debug_param_name == "GamevarsImpulse" && local_uiplytoself != null)
        {
            local_uiplytoself.ui_check_gamevars_impulse = value / 1000.0f;
        }
    }

    public string PowerupTypeToStr(int in_powerup_type)
    {
        string output = "";
        if (in_powerup_type == (int)powerup_type_name.SizeUp) { output = localizer.FetchText("POWERUP_SIZEUP", "Size Up"); }
        else if (in_powerup_type == (int)powerup_type_name.SizeDown) { output = localizer.FetchText("POWERUP_SIZEDOWN", "Size Down"); }
        else if (in_powerup_type == (int)powerup_type_name.SpeedUp) { output = localizer.FetchText("POWERUP_SPEEDUP", "Speed Boost"); }
        else if (in_powerup_type == (int)powerup_type_name.AtkUp) { output = localizer.FetchText("POWERUP_ATKUP", "Attack Up"); }
        else if (in_powerup_type == (int)powerup_type_name.AtkDown) { output = localizer.FetchText("POWERUP_DEFUP", "Attack Down"); }
        else if (in_powerup_type == (int)powerup_type_name.DefUp) { output = localizer.FetchText("POWERUP_ATKDOWN", "Defense Up"); }
        else if (in_powerup_type == (int)powerup_type_name.DefDown) { output = localizer.FetchText("POWERUP_DEFDOWN", "Defense Down"); }
        else if (in_powerup_type == (int)powerup_type_name.LowGrav) { output = localizer.FetchText("POWERUP_LOWGRAV", "Low Gravity"); }
        else if (in_powerup_type == (int)powerup_type_name.PartialHeal) { output = localizer.FetchText("POWERUP_PARTIALHEAL", "Heal (50%)"); }
        else if (in_powerup_type == (int)powerup_type_name.FullHeal) { output = localizer.FetchText("POWERUP_FULLHEAL", "Heal (100%)"); }
        else if (in_powerup_type == (int)powerup_type_name.Multijump) { output = localizer.FetchText("POWERUP_MULTIJUMP", "Multi-Jump"); }
        else if (in_powerup_type == (int)powerup_type_name.HighGrav) { output = localizer.FetchText("POWERUP_HIGHGRAV", "High Gravity"); }
        else { output = "(PLACEHOLDER)"; }
        return output;
    }

    public string WeaponTypeToStr(int in_weapon_type)
    {
        string output = "";
        if (in_weapon_type == (int)weapon_type_name.PunchingGlove) { output = localizer.FetchText("WEAPON_PUNCHINGGLOVE", "Punching Glove"); }
        else if (in_weapon_type == (int)weapon_type_name.Bomb) { output = localizer.FetchText("WEAPON_BOMB", "Bomb"); }
        else if (in_weapon_type == (int)weapon_type_name.Rocket) { output = localizer.FetchText("WEAPON_ROCKET", "Rocket Launcher"); }
        else if (in_weapon_type == (int)weapon_type_name.BossGlove) { output = localizer.FetchText("WEAPON_BOSSGLOVE", "Big Boss Glove"); }
        else if (in_weapon_type == (int)weapon_type_name.HyperGlove) { output = localizer.FetchText("WEAPON_HYPERGLOVE", "Hyper Glove"); }
        else if (in_weapon_type == (int)weapon_type_name.MegaGlove) { output = localizer.FetchText("WEAPON_MEGAGLOVE", "Mega Glove"); }
        else if (in_weapon_type == (int)weapon_type_name.SuperLaser) { output = localizer.FetchText("WEAPON_SUPERLASER", "Superlaser"); }
        else if (in_weapon_type == (int)weapon_type_name.ThrowableItem) { output = localizer.FetchText("WEAPON_THROWABLEITEM", "Throwable Item"); }
        else { output = "(PLACEHOLDER)"; }
        return output;
    }

    public void ForceResetHitboxes()
    {
        if (ply_object_plyhitbox != null && ply_object_plyattr != null && ply_owners_cnt < ply_object_plyattr.Length && ply_owners_cnt < ply_object_plyhitbox.Length && ply_owners_cnt >= 0)
        {
            UnityEngine.Debug.Log("[PLAYER_JOIN_TEST] Resetting hitboxes");
            if (ply_object_plyhitbox[ply_owners_cnt] == null) { return; }
            
            for (int i = 0; i < ply_owners_cnt; i++)
            {
                if (ply_object_plyattr[i] != null && ply_object_plyattr[i].in_ready_room) { ply_object_plyhitbox[i].ToggleHitbox(false); continue; }
                ply_object_plyhitbox[i].ToggleHitbox(ply_object_plyhitbox[i].network_active);
            }
            //CheckPlyObjsActive();
        }
    }


}

