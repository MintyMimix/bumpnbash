
using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.UIElements;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

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

public class GameController : UdonSharpBehaviour
{

    [SerializeField] public GameObject template_WeaponProjectile;
    [SerializeField] public GameObject template_WeaponHurtbox;
    [SerializeField] public GameObject template_ItemPowerup;
    [SerializeField] public Sprite Sprite_None;

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

    [SerializeField] public Transform item_spawn_points_parent;
    [NonSerialized] public Transform[] item_spawn_points;

    [NonSerialized][UdonSynced] public int round_state = 0;
    [NonSerialized][UdonSynced] public float round_length = 120.0f;
    [NonSerialized][UdonSynced] public float ready_length = 5.0f;
    [NonSerialized][UdonSynced] public float over_length = 10.0f;
    [NonSerialized][UdonSynced] public double round_start_ms = 0.0f;

    [UdonSynced] public bool option_teamplay = false; // Does this need to be synced?
    [UdonSynced] public int team_count = 1;

    [UdonSynced] public float plysettings_dp = 0.0f;
    [UdonSynced] public float plysettings_respawn_duration = 3.0f;
    [UdonSynced] public int plysettings_lives = 3;
    [UdonSynced] public float plysettings_scale = 1.0f;
    [UdonSynced] public float plysettings_speed = 1.0f;
    [UdonSynced] public float plysettings_atk = 1.0f;
    [UdonSynced] public float plysettings_def = 1.0f;
    [UdonSynced] public float plysettings_grav = 1.0f;
    [UdonSynced] public float scale_damage_factor = 1.0f;

    [NonSerialized] public float round_timer = 0.0f;

    [NonSerialized] [UdonSynced] public string ply_ready_str = "";
    [NonSerialized] public int[] ply_ready_arr;
    [NonSerialized] [UdonSynced] public string ply_in_game_str = "";
    [NonSerialized] public int[] ply_in_game_arr;

    [NonSerialized] public int[][] ply_teams_arr;

    public float powerup_spawn_impulse = 3.0f;
    public float powerup_spawn_chance = 0.33f;
    public int powerup_spawn_limit = 5;
    [NonSerialized] public double powerup_spawn_last_impulse_ms;
    [NonSerialized] public int powerup_spawn_count = 0;
    [NonSerialized] public GameObject[] powerup_list; // Note: this must be stored locally on each player's instance of the gameobject

    // -- Initialization --
    private void Start()
    {
        item_spawn_points = new Transform[0];
        ply_ready_arr = new int[0];
        ply_in_game_arr = new int[0];
        ply_teams_arr = new int[0][];
        powerup_list = new GameObject[powerup_spawn_limit]; // To-do: update list size based on powerup spawn limit configurable option

        //projectiles = new GameObject[0];
        //hurtboxes = new GameObject[0];
        snd_game_sfx_clips = new AudioClip[(int)game_sfx_index.ENUM_LENGTH][];
        snd_game_sfx_clips[(int)game_sfx_index.Death] = snd_game_sfx_clips_death;
        snd_game_sfx_clips[(int)game_sfx_index.Kill] = snd_game_sfx_clips_kill;
        snd_game_sfx_clips[(int)game_sfx_index.HitSend] = snd_game_sfx_clips_hitsend;
        snd_game_sfx_clips[(int)game_sfx_index.HitReceive] = snd_game_sfx_clips_hitreceive;
        snd_game_sfx_clips[(int)game_sfx_index.WeaponFire] = snd_game_sfx_clips_weaponfire;

        PlaySFXFromArray(snd_ready_music_source, snd_ready_music_clips);

        if (item_spawn_points_parent != null)
        {
            item_spawn_points = new Transform[item_spawn_points_parent.childCount];
            var item_index = 0;
            foreach (Transform t in item_spawn_points_parent.transform)
            {
                item_spawn_points[item_index] = t;
                item_index++;
            }
        }
        powerup_spawn_last_impulse_ms = Networking.GetServerTimeInSeconds();

    }

    public float[] GetStatsFromWeaponType(int weapon_type)
    {
        var weapon_stats = new float[(int)weapon_stats_name.ENUM_LENGTH];
        switch (weapon_type)
        {
            case (int)weapon_type_name.PunchingGlove:
                weapon_stats[(int)weapon_stats_name.Cooldown] = 1.1f;
                weapon_stats[(int)weapon_stats_name.Projectile_Distance] = 3.0f;
                weapon_stats[(int)weapon_stats_name.Projectile_Duration] = 0.1f;
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
        room_ready_txt.text =
            "Game State: " + round_state.ToString()
            + "\nPlayers Ready: " + ply_ready_arr.Length.ToString() + "\n {" + ply_ready_str + "}"
            + "\n Players In-Game: " + ply_in_game_arr.Length.ToString() + "\n {" + ply_in_game_str + "}"

        ;

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
        }

        // Master handling
        if (!Networking.LocalPlayer.isMaster) { return; }
        if (round_state == (int)round_state_name.Ready && round_timer >= ready_length)
        {
            round_start_ms = Networking.GetServerTimeInSeconds();
            round_state = (int)round_state_name.Ongoing;
            RequestSerialization();
        }
        else if (round_state == (int)round_state_name.Ongoing && round_timer >= round_length)
        {
            round_start_ms = Networking.GetServerTimeInSeconds();
            round_state = (int)round_state_name.Over;
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "RoundEnd");
            //RequestSerialization();
        }
        else if (round_state == (int)round_state_name.Ongoing && round_timer < round_length)
        {
            HandlePowerupChances();
        }
        else if (round_state == (int)round_state_name.Over && round_timer >= ready_length)
        {
            round_start_ms = Networking.GetServerTimeInSeconds();
            round_state = (int)round_state_name.Start;
            RequestSerialization();
        }
    }

    public override void OnDeserialization()
    {
        //if (players_active_str.Length > 0) 
        //{ 
        //    players_active = ConvertStrToIntArray(players_active_str); 
        //}

        // To-do: Why is everyone checking this? Shouldn't only the master care?
        //CheckAllPlayerLives();
        ply_ready_arr = ConvertStrToIntArray(ply_ready_str);
        ply_in_game_arr = ConvertStrToIntArray(ply_in_game_str);
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
        }
    }

    public int[][] DebugTeamArray(int[] trackingArr)
    {
        if (!Networking.LocalPlayer.isMaster) { return new int[0][]; }
        if (team_count <= 0) { Debug.LogError("Team count is <= 0!"); return new int[0][]; } // This should never happen
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
    }

    public void HandlePowerupChances()
    {
        if (Networking.CalculateServerDeltaTime(Networking.GetServerTimeInSeconds(), powerup_spawn_last_impulse_ms) >= powerup_spawn_impulse)
        {
            var spawn_offset = new Vector3(0.0f, 0.5f, 0.0f); // To-do: make this per-map
            var roll_for_powerup = UnityEngine.Random.Range(0, 100);

            // To-do: make type and duration go against a table of powerup cards with differing rarities and stats
            var roll_for_powerup_type = UnityEngine.Random.Range(0, (int)item_powerup_name.ENUM_LENGTH);

            if (((float)roll_for_powerup/100.0f) <= powerup_spawn_chance && powerup_spawn_count < powerup_spawn_limit)
            {
                // Check for unoccupied spawn points
                var unoccupied_spawns = new Transform[0];
                var powerup_count = 0;
                for (int i = 0; i < powerup_list.Length; i++)
                {
                    if (powerup_list[i] != null) { powerup_count++; }
                }
                if (item_spawn_points.Length - powerup_count <= 0) 
                {
                    Debug.LogWarning("No valid spawns for powerup; check to make sure your powerup spawn limit (" + powerup_spawn_count + ") does not exceed spawns on map (" + item_spawn_points.Length + ")!");
                    powerup_spawn_limit = item_spawn_points.Length;
                    return;
                }
                else { unoccupied_spawns = new Transform[item_spawn_points.Length - powerup_count]; }
                var spawn_iter = 0;

                for (int j = 0; j < item_spawn_points.Length; j++)
                {
                    var is_valid_spawn = true;
                    for (int k = 0; k < powerup_list.Length; k++)
                    {
                        if (powerup_list[k] == null) { continue; }
                        else if (powerup_list[k].transform.position == item_spawn_points[j].position + spawn_offset) { is_valid_spawn = false; break; }
                    }
                    if (is_valid_spawn)
                    {
                        unoccupied_spawns[spawn_iter] = item_spawn_points[j];
                        spawn_iter++;
                    }
                }

                var roll_for_spawn_point = UnityEngine.Random.Range(0, unoccupied_spawns.Length);

                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "NetworkCreatePowerup"
                    , roll_for_powerup_type
                    , unoccupied_spawns[roll_for_spawn_point].position + spawn_offset);
            }
            powerup_spawn_last_impulse_ms = Networking.GetServerTimeInSeconds();
        }

    }

    // -- Round Management --
    [NetworkCallable]
    public void NetworkRoundStart()
    {
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
            playerData.ply_lives = plysettings_lives;
            playerData.ply_respawn_duration = plysettings_respawn_duration;
            playerData.ply_scale = plysettings_scale;
            playerData.ply_speed = plysettings_speed;
            playerData.ply_atk = plysettings_atk;
            playerData.ply_def = plysettings_def;
            playerData.ply_grav = plysettings_grav;
            playerData.ply_state = (int)player_state_name.Alive;
            if (!option_teamplay) { playerData.ply_team = 0; }
            // To-Do: Function to validate spawn zones



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


    [NetworkCallable]
    public void CheckAllPlayerLives()
    {
        if (!Networking.LocalPlayer.isMaster) { return; }
        if (round_state != (int)round_state_name.Ongoing) { return; }
        if (team_count <= 0) { Debug.LogError("Team count is <= 0!"); return; } // This should never happen

        var plyAlivePerTeam = new int[team_count];
        var teams_alive = 0;
        var total_players_alive = 0;
        for (int i = 0; i < ply_in_game_arr.Length; i++)
        {
            var player = VRCPlayerApi.GetPlayerById(ply_in_game_arr[i]);
            if (player == null) { continue; }
            var plyAttributes = FindPlayerAttributes(player);
            if (plyAttributes == null) { continue; }
            if (plyAttributes.ply_lives > 0)
            {
                total_players_alive++;
                if (plyAttributes.ply_team > team_count) { Debug.LogError("Player is on team " + plyAttributes.ply_team.ToString() + ", but the game only has " + team_count.ToString() + " teams!"); continue; }
                if (plyAttributes.ply_team >= 0) { plyAlivePerTeam[plyAttributes.ply_team]++; } // Should only ever error out in ClientSim, but just in case, let's make sure it's not index < 0
            }
        }
        for (int j = 0; j < team_count; j++)
        {
            if (plyAlivePerTeam[j] > 0) { teams_alive++; }
        }

        bool teamplayOver = option_teamplay &&
        (
            (teams_alive <= 1 && team_count > 1) ||
            (teams_alive <= 0 && team_count == 1) ||
            (team_count <= 0)
        );
        bool freeForAllOver = !option_teamplay &&
            (
                (total_players_alive <= 1 && ply_in_game_arr.Length > 1) ||
                (total_players_alive <= 0 && ply_in_game_arr.Length == 1) ||
                (ply_in_game_arr.Length == 0)
            );

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
                TeleportLocalPlayerToReadyRoom();

            }
        }

        for (int j = 0; j < powerup_list.Length; j++) 
        {
            if (powerup_list[j] == null) { continue; }
            SendDestroyPowerup(powerup_list[j].GetComponent<ItemPowerup>().powerup_stored_global_index, (int)item_powerup_destroy_reason_code.ItemExpire, false ); 
        }

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
                if (weaponObj == null || weaponObj.GetComponent<PlayerWeapon>() == null)
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

    [NetworkCallable]
    public void NetworkCreatePowerup(int powerup_type, Vector3 spawnPos)
    {
        var powerupObj = Instantiate(template_ItemPowerup);
        var powerup = powerupObj.GetComponent<ItemPowerup>();

        powerup.item_type = (int)item_type_name.Powerup;
        powerup.item_spawn_ms = Networking.GetServerTimeInSeconds();
        powerup.item_spawn_duration = 20.0f; // To-do: Should this be configurable?
        powerup.powerup_type = powerup_type;
        powerup.powerup_duration = 10.0f; // To-do: Should this be configurable?
        powerup.gameController = this;

        powerupObj.transform.position = spawnPos;
        powerupObj.SetActive(true);
        powerup.SetPowerupStats(powerup_type);

        var storedIndex = -1;
        powerup_list = AddToStaticGameObjectArray(powerupObj, powerup_list, out storedIndex);
        if (storedIndex == -1) { UnityEngine.Debug.LogError("Attempted to assign valid powerup to list, but encountered no free space!"); return; }
        powerup.powerup_stored_global_index = storedIndex;
        powerup_spawn_count++;
        //powerup.item_snd_source.maxDistance = 0.5f;
        //PlaySFXFromArray(powerup.item_snd_source, powerup.item_snd_clips, (int)item_snd_clips_name.Spawn);

    }

    [NetworkCallable]
    public void NetworkDestroyPowerup(int stored_index, int reason_code, bool playSound)
    {
        
        if (powerup_list[stored_index] != null && powerup_list[stored_index].GetComponent<ItemPowerup>() != null) {
            var powerup = powerup_list[stored_index].GetComponent<ItemPowerup>();

            if (reason_code == (int)item_powerup_destroy_reason_code.OtherPickup) {
                // We do not want to process this if it was an otherpickup event and we are the ones who triggered it. Would normally use Others on network event, but this fails because GameController is always master-owned.
                if (powerup.powerup_owner_id == Networking.LocalPlayer.playerId) { return; }
                // If we are the instance master, we also do not want to destroy it; instead, we want to trigger it without giving ourselves the powerup, and halt the behavior until prompted to destroy it.
                else if (Networking.LocalPlayer.isMaster) {
                    powerup.powerup_ignore = true;
                    powerup.ApplyPowerup();
                    return;
                }
            }

            if (playSound) { PlaySFXFromArray(powerup.item_snd_source, powerup.item_snd_clips, reason_code); }
            powerup.item_snd_source.GetComponent<ItemSound>().FinalPlay();
            Destroy(powerup_list[stored_index]);
            powerup_spawn_count--;
        }
        powerup_list[stored_index] = null;
    }

    public void SendDestroyPowerup(int stored_index, int reason_code, bool playSound) {

        SendCustomNetworkEvent(NetworkEventTarget.All, "NetworkDestroyPowerup", stored_index, reason_code, playSound);
    
    }

    // -- Player Tracking --
    [NetworkCallable]
    public void ManipulatePlyTrackingArray(int player_id, int ply_tracking_arr_id, bool op_add)
    {
        //Debug.Log("WE ARE MANIPULATING ARRAY " + ply_tracking_arr_id + " AND WILL " + op_add + " ADD TO IT");
        if (op_add) { AddPlyToTrackingArray(player_id, ply_tracking_arr_id); }
        else { RemovePlyFromTrackingArray(player_id, ply_tracking_arr_id); }
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

}

