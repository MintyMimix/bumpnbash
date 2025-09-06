
using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Xml.Linq;
using UdonSharp;
using UnityEngine;
using UnityEngine.Windows;
using VRC.SDK3.Persistence;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;
using VRC.Udon.Common.Interfaces;

public enum player_state_name
{
    Inactive, Joined, Alive, Respawning, Dead, Spectator, ENUM_LENGTH
}

public class PlayerAttributes : UdonSharpBehaviour
{

    [NonSerialized] [UdonSynced] public byte ply_state;
    [NonSerialized] [UdonSynced] public float ply_dp;
    [NonSerialized] [UdonSynced] public float ply_dp_default;
    [NonSerialized] [UdonSynced] public ushort ply_lives;
    [NonSerialized] [UdonSynced] public ushort ply_points = 0;
    [NonSerialized] [UdonSynced] public ushort ply_deaths = 0;
    [NonSerialized] [UdonSynced] public int ply_team = (int)player_tracking_name.Unassigned;
    [NonSerialized] [UdonSynced] public bool ply_training = false;
    [NonSerialized] [UdonSynced] public bool in_spectator_area = false;
    [NonSerialized] [UdonSynced] public bool in_ready_room = true;

    [NonSerialized] public ushort ply_lives_local; // We create local versions of these variables for other clients to compare to. If there is a mismatch, we can have events fire off OnDeserialization().
    [NonSerialized] public ushort ply_points_local = 0;
    [NonSerialized] public ushort ply_deaths_local = 0;
    [NonSerialized] public int ply_team_local = 0;

    // While we aren't syncing the stats below right now, we may want to in the future for UI purposes
    [NonSerialized] [UdonSynced] public float ply_scale = 1.0f; // This is the one stat that needs to be synced most because it affects visuals
    [NonSerialized] public float ply_speed = 1.0f;
    [NonSerialized] [UdonSynced] public float ply_atk = 1.0f;
    [NonSerialized] [UdonSynced] public float ply_def = 1.0f;
    [NonSerialized] public float ply_grav = 1.0f;
    [NonSerialized] [UdonSynced] public int ply_jumps_add = 0;
    [NonSerialized] public int ply_jumps_tracking = 0;
    [NonSerialized] public bool ply_jump_pressed = false;
    [NonSerialized] public float ply_firerate = 1.0f;

    [NonSerialized] public float ply_respawn_duration;
    [NonSerialized] public VRCPlayerApi last_hit_by_ply;
    [NonSerialized] public float last_hit_by_duration = 20.0f;
    [NonSerialized] public float last_hit_by_timer = 0.0f;
    [NonSerialized] public int last_kill_ply = -1;
    [NonSerialized] public float last_kill_duration = 10.0f;
    [NonSerialized] public float last_kill_timer = 0.0f;

    [SerializeField] public GameController gameController;
    [NonSerialized] public float ply_respawn_timer = 0.0f;

    [NonSerialized] public int combo_receive, combo_send = 0;
    [NonSerialized] public float combo_send_duration = 2.0f;
    [NonSerialized] public float combo_send_timer = 0.0f;

    [NonSerialized] public float hazard_cooldown = 0.5f;
    [NonSerialized] public float hazard_timer = 0.0f;

    [NonSerialized] public bool powerups_are_resetting = false;

    [NonSerialized] public GameObject[] powerups_active;

    [NonSerialized] [UdonSynced] public float plyEyeHeight_default, plyEyeHeight_desired;
    [Tooltip("How long a size-changing animation should play on a player")]
    [SerializeField] public double plyEyeHeight_lerp_duration = 2.5f;
    [NonSerialized] public double plyEyeHeight_lerp_start_ms = 0.0f;
    [NonSerialized] public bool plyEyeHeight_change = false;

    [NonSerialized] public bool[] local_tutorial_message_bool;
    [NonSerialized] public string[] local_tutorial_message_str_desktop;
    [NonSerialized] public string[] local_tutorial_message_str_vr;

    [NonSerialized] [UdonSynced] public byte infection_special = 0;
    [NonSerialized] public float local_tick_timer = 0.0f;

    [NonSerialized] public bool tutorial_messages_ready = false;

    // To-Do: Have all projectile damage scale to a configurable factor, which is then auto-scaled to the # of players

    void Start()
    {
        if (gameController == null)
        {
            GameObject gcObj = GameObject.Find("GameController");
            if (gcObj != null) { gameController = gcObj.GetComponent<GameController>(); }
        }

        powerups_active = new GameObject[0];

        SetDefaultEyeHeight();
    }

    public override void OnDeserialization(DeserializationResult deserializationResult)
    {
        if (gameController != null && gameController.round_state == (int)round_state_name.Ongoing)
        {
            if (ply_lives_local != ply_lives || ply_points_local != ply_points || ply_deaths_local != ply_deaths || ply_team_local != ply_team)
            {
                ply_lives_local = ply_lives;
                ply_points_local = ply_points;
                ply_deaths_local = ply_deaths;
                ply_team_local = ply_team;
                gameController.RefreshGameUI();
                if (Networking.IsOwner(gameController.gameObject)) { gameController.CheckForRoundGoal(); } // Because we are already confirmed to be the game master, we can send this locally instead of as a networked event
            }
        }
    }

   

    private void Update()
    {

        // -- Only the owner should run the following --
        if (gameController == null)
        {
            GameObject gcObj = GameObject.Find("GameController");
            if (gcObj != null) { gameController = gcObj.GetComponent<GameController>(); }
            else { return; }
        }
        
        if (!Networking.IsOwner(gameObject)) { return; }

        // Handle player state
        if (ply_respawn_timer < ply_respawn_duration)
        {
            ply_respawn_timer += Time.deltaTime;
            // Update the UI accordingly
            if (gameController.local_uiplytoself != null) { gameController.local_uiplytoself.UI_Damage(); }
        }
        else if (ply_state == (int)player_state_name.Respawning)
        {
            ply_state = (int)player_state_name.Alive;
            // Update the UI accordingly
            if (gameController.local_uiplytoself != null) { gameController.local_uiplytoself.UI_Damage(); }
        }
        else if (ply_state == (int)player_state_name.Dead && gameController.round_state == (int)round_state_name.Ongoing)
        {
            //gameController.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "CheckForRoundGoal");
            ply_respawn_timer = 0.0f;
        }

        // Handle last hit by
        if (last_hit_by_timer < last_hit_by_duration && last_hit_by_ply != null)
        {
            last_hit_by_timer += Time.deltaTime;
        }
        else if (last_hit_by_timer >= last_hit_by_duration && last_hit_by_ply != null)
        {
            last_hit_by_ply = null;
        }

        // Handle last kill
        if (last_kill_timer < last_kill_duration && last_kill_ply > -1)
        {
            last_kill_timer += Time.deltaTime;
        }
        else if (last_kill_timer >= last_kill_duration && last_kill_ply > -1)
        {
            last_kill_ply = -1;
        }

        // Handle send combos
        if (combo_send_timer < combo_send_duration && combo_send > 0)
        {
            combo_send_timer += Time.deltaTime;
        }
        else if (combo_send_timer >= combo_send_duration && combo_send > 0)
        {
            combo_send = 0;
        }

        // Handle receive combos
        if (Networking.LocalPlayer.IsPlayerGrounded()) { combo_receive = 0; }

        // Handle hazard
        if (hazard_timer < hazard_cooldown) { hazard_timer += Time.deltaTime; }

        // Handle player stats
        // To-do: this more efficiently
        if (gameController.round_state == (int)round_state_name.Ready || gameController.round_state == (int)round_state_name.Ongoing || ply_training) {
            float koth_mod = 1.0f;
            if (gameController.option_gamemode == (int)gamemode_name.KingOfTheHill && ply_state == (int)player_state_name.Respawning && !ply_training && ply_respawn_duration > gameController.plysettings_respawn_duration) { koth_mod = 0.01f; }
            Networking.LocalPlayer.SetWalkSpeed(2.0f * ply_speed * koth_mod);
            Networking.LocalPlayer.SetRunSpeed(4.0f * ply_speed * koth_mod);
            Networking.LocalPlayer.SetStrafeSpeed(2.0f * ply_speed * koth_mod);
            Networking.LocalPlayer.SetGravityStrength(1.0f * ply_grav * (1.0f/koth_mod));
            Networking.LocalPlayer.SetJumpImpulse(4.0f + (1.0f - ply_grav)); // Default is 3.0f, but we want some verticality to our maps, so we'll make it 4.0
        }
        else
        {
            float spec_mod = 1.0f;
            if (in_spectator_area) { spec_mod = 4.5f; }
            Networking.LocalPlayer.SetWalkSpeed(2.0f * spec_mod);
            Networking.LocalPlayer.SetRunSpeed(4.0f * spec_mod);
            Networking.LocalPlayer.SetStrafeSpeed(2.0f * spec_mod);
            Networking.LocalPlayer.SetGravityStrength(1.0f);
            Networking.LocalPlayer.SetJumpImpulse(4.0f);
        }
        Networking.GetOwner(gameObject).SetManualAvatarScalingAllowed(true);

        // Hnadle multi-jump
        if (Networking.LocalPlayer.IsPlayerGrounded()) { ply_jumps_tracking = 0; }

        local_tick_timer += Time.deltaTime;
        if (local_tick_timer >= ((int)GLOBAL_CONST.TICK_RATE_MS / 1000.0f))
        {
            LocalPerTickUpdate();
            local_tick_timer = 0.0f;
        }

    }

    private void LocalPerTickUpdate()
    {
        // Update size
        if (plyEyeHeight_change && Networking.IsOwner(gameObject))
        {
            var plyCurrentEyeHeight = Networking.LocalPlayer.GetAvatarEyeHeightAsMeters();
            var lerp_delta = Networking.CalculateServerDeltaTime(Networking.GetServerTimeInSeconds(), plyEyeHeight_lerp_start_ms);
            if (plyCurrentEyeHeight != plyEyeHeight_desired && lerp_delta < plyEyeHeight_lerp_duration)
            {
                Networking.LocalPlayer.SetAvatarEyeHeightByMeters(Mathf.Lerp(plyCurrentEyeHeight, plyEyeHeight_desired, (float)(lerp_delta / plyEyeHeight_lerp_duration)));
            }
            else if (plyCurrentEyeHeight != plyEyeHeight_desired && lerp_delta >= plyEyeHeight_lerp_duration)
            {
                Networking.LocalPlayer.SetAvatarEyeHeightByMeters(plyEyeHeight_desired);
                if (ply_scale > gameController.largest_ply_scale) { gameController.SendCustomNetworkEvent(NetworkEventTarget.Owner, "UpdateLargestPlayer", ply_scale); }
                plyEyeHeight_change = false;
            }
            else if (plyCurrentEyeHeight == plyEyeHeight_desired) 
            {
                if (ply_scale > gameController.largest_ply_scale) { gameController.SendCustomNetworkEvent(NetworkEventTarget.Owner, "UpdateLargestPlayer", ply_scale); }
                plyEyeHeight_change = false; 
            }
        }
        else if (in_ready_room && !ply_training && plyEyeHeight_desired != plyEyeHeight_default && !plyEyeHeight_change) 
        { 
            LocalResetScale(); 
        }
    }

    public override void OnAvatarChanged(VRCPlayerApi player) {
        if (player != Networking.LocalPlayer || !Networking.IsOwner(gameObject)) { return; }
        SetDefaultEyeHeight();
    }

    public override void OnAvatarEyeHeightChanged(VRCPlayerApi player, float prevHeight)
    {
        if (player != Networking.LocalPlayer || !Networking.IsOwner(gameObject)) { return; }
        if (prevHeight == 0) { SetDefaultEyeHeight(); }
        // We should also allow users to change their default height in the ready roomw
        else if (gameController != null && 
            !(ply_state == (int)player_state_name.Alive || ply_state == (int)player_state_name.Respawning)
            && !ply_training
            && !plyEyeHeight_change // This is the key; if this is true, then we know it's the game saying to change the eye height, not the user
            )
        { SetDefaultEyeHeight(); }
        //else if (ply_scale == 1.0f && player.GetAvatarEyeHeightAsMeters() != prevHeight && plyEyeHeight_default != player.GetAvatarEyeHeightAsMeters()) { ResetDefaultEyeHeight(); }
    }

    public void TryHapticEvent(int haptic_event_type)
    {
        //GameObject weapon_obj = gameController.FindPlayerOwnedObject(Networking.LocalPlayer, "PlayerWeapon");
        //PlayerWeapon weapon_script = null; 
        if (gameController.local_plyweapon != null) { gameController.local_plyweapon.PlayHapticEvent(haptic_event_type); } //Debug.Log("TRY HAPTIC: " + haptic_event_type); }
    }

    [NetworkCallable]
    public void SetDefaultEyeHeight()
    {
        if (!Networking.IsOwner(gameObject)) { return; }
        ResetPowerups();

        float default_height = Mathf.Clamp(Networking.LocalPlayer.GetAvatarEyeHeightAsMeters(), Networking.LocalPlayer.GetAvatarEyeHeightMinimumAsMeters(), Networking.LocalPlayer.GetAvatarEyeHeightMaximumAsMeters());
        plyEyeHeight_default = default_height;
        plyEyeHeight_desired = plyEyeHeight_default;
        
    }

    public void LocalResetScale()
    {
        ply_scale = 1.0f;
        plyEyeHeight_desired = plyEyeHeight_default;
        plyEyeHeight_change = true;
    }

    [NetworkCallable]
    public void HitOtherPlayer(int attacker_id, int defender_id, float damage, int damage_type, Vector3 hitSpot)
    {
        // Only the attacker should have a registered hit
        if (attacker_id != Networking.LocalPlayer.playerId) { return; }
        gameController.PlaySFXFromArray(gameController.snd_game_sfx_sources[(int)game_sfx_name.HitSend], gameController.snd_game_sfx_clips[(int)game_sfx_name.HitSend], damage_type, 1 + 0.1f * (combo_send));
        combo_send++;

        TryHapticEvent((int)game_sfx_name.HitSend);
        // Damage indicator
        if (gameController.local_uiplytoself != null)
        {
            gameController.local_uiplytoself.ShowHarmNumber(defender_id, damage, hitSpot);
        }
    }

    [NetworkCallable]
    public void ReceiveDamage(float damage, Vector3 forceDirection, Vector3 hitSpot, int attacker_id, int damage_type, bool hit_self, byte extra_data)
    {
        //if (attacker_id == Networking.LocalPlayer.playerId) { return; }
        if (ply_state != (int)player_state_name.Alive) { return; }
        // We want to ensure hazards aren't processed
        if (damage_type == (int)damage_type_name.HazardBurn && (hazard_timer < hazard_cooldown)) { return; } 
        else { hazard_timer = 0.0f; }

        float calcDmg = damage; 
        Vector3 modForceDirection = forceDirection; // To-do: make this into a slider, game setting, or serialized field

        // Input damage should already have the attacker's attack & scale added onto it; we only handle defense from here
        calcDmg *= (1.0f / ply_def) * (1.0f / (ply_scale * gameController.scale_damage_factor));

        float baseLift = 0.66f; // 0.33f
        if (hit_self) { modForceDirection += new Vector3(0.0f, baseLift, 0.0f); }

        /*if (Networking.LocalPlayer.IsPlayerGrounded()) { modForceDirection += new Vector3(0.0f, baseLift, 0.0f); }
        else { modForceDirection += new Vector3(0.0f, baseLift / 2.0f, 0.0f); }
        if (hit_self) { modForceDirection += new Vector3(0.0f, baseLift, 0.0f); }*/
        if (!Networking.LocalPlayer.IsPlayerGrounded()) { baseLift *= 0.5f; }
        modForceDirection = new Vector3(modForceDirection.x, Mathf.Max(Mathf.Abs(modForceDirection.y), baseLift), modForceDirection.z);
        UnityEngine.Debug.Log("Resulting force direction: " + modForceDirection + " (input: " + forceDirection + ")");
        if (modForceDirection.magnitude < baseLift) { modForceDirection *= baseLift/modForceDirection.magnitude; }
        UnityEngine.Debug.Log("Resulting force direction after magnitude modification: " + modForceDirection);

        // Old formula: float xDmg = calcDmg + (ply_dp * (1.0f / ply_def));
        // We swapped to the new one because Defense was both strong and unintuitive; having it meant you could be at 100% and not be knocked out easily while also receiving nerfed damage.
        float xDmg = calcDmg + ply_dp;
        float calcMagnitude = 0.004f * Mathf.Pow(xDmg, 1.85f) + 8.0f;
        // (100.0f + (xDmg / 3.0f))
        // / (1 + Mathf.Exp(-0.02f * (xDmg - 100.0f)));
        UnityEngine.Debug.Log("Resulting force magnitude: " + calcMagnitude);
        calcMagnitude = Mathf.Max(calcMagnitude, Vector3.Dot(modForceDirection, Networking.LocalPlayer.GetVelocity()), Vector3.Dot(modForceDirection, -Networking.LocalPlayer.GetVelocity()));
        UnityEngine.Debug.Log("Resulting force magnitude after factoring velocity: " + calcMagnitude);

        Vector3 calcForce = modForceDirection;
        calcForce *= calcMagnitude;
        
        //Mathf.Pow((calcDmg + ply_dp) / 2.2f, 1.08f);

        // Don't apply additional force if this is a hazard or a throwable item
        if (damage_type != (int)damage_type_name.HazardBurn && damage_type != (int)damage_type_name.ItemHit)
        {
            //Networking.LocalPlayer.SetVelocity(calcForce * 0.5f);

            Networking.LocalPlayer.SetVelocity(calcForce * 0.5f);
        }

        // To-Do: make last hit by a function scaled based on damage (i.e. whoever dealt the most damage prior to the player hitting the ground gets kill credit)
        if (!hit_self) { 
            ply_dp += calcDmg; 
            gameController.PlaySFXFromArray(gameController.snd_game_sfx_sources[(int)game_sfx_name.HitReceive], gameController.snd_game_sfx_clips[(int)game_sfx_name.HitReceive], damage_type, 1 + 0.1f * (combo_receive));
            combo_receive++;
            if (attacker_id >= 0) {
                last_hit_by_ply = VRCPlayerApi.GetPlayerById(attacker_id);
                last_hit_by_timer = 0.0f;
                var plyAttr = gameController.FindPlayerAttributes(last_hit_by_ply);
                plyAttr.SendCustomNetworkEvent(NetworkEventTarget.All, "HitOtherPlayer", attacker_id, Networking.LocalPlayer.playerId, calcDmg, damage_type, Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position);
            }
        }
        TryHapticEvent((int)game_sfx_name.HitReceive);

        // Damage indicator
        if (gameController.local_uiplytoself != null)
        {
            gameController.local_uiplytoself.ShowPainIndicator(calcDmg, hitSpot);
        }

        // If this is a throwable item, apply a powerup to self
        if (damage_type == (int)damage_type_name.ItemHit)
        {
            //gameController.template_ItemSpawner.SetActive(true);
            ItemSpawner itemSpawnerTemplate = gameController.template_ItemSpawner.GetComponent<ItemSpawner>();
            itemSpawnerTemplate.item_spawn_index = extra_data;
            itemSpawnerTemplate.SpawnItem(extra_data, true);
            //if (extra_data < (int)powerup_type_name.ENUM_LENGTH) { itemSpawnerTemplate.child_powerup.OnTriggerEnter(gameController.local_plyhitbox.GetComponent<Collider>()); }
            //else { itemSpawnerTemplate.child_weapon.OnTriggerEnter(gameController.local_plyhitbox.GetComponent<Collider>()); }
            //itemSpawnerTemplate.DespawnItem((int)item_sfx_index.ItemExpire, -1, false);
            //gameController.template_ItemSpawner.SetActive(false);
        }
        
        // Update the UI accordingly
        if (gameController.local_uiplytoself != null) { gameController.local_uiplytoself.UI_Damage(); }
    }

    [NetworkCallable]
    public void KillOtherPlayer(int attackerPlyId, int defenderPlyId, bool defenderIsTraining)
    {
        //Debug.Log("Is owner? " + Networking.IsOwner(gameObject));
        //Debug.Log("LocalPlayer.playerId: " + Networking.LocalPlayer.playerId);
        //Debug.Log("Attacker ID: " + attackerPlyId);
        if (attackerPlyId != Networking.LocalPlayer.playerId) { return; }
        Debug.Log("We killed Defender ID: " + defenderPlyId);
        // Add points if we aren't on KOTH (which is capture time)
        if (gameController.option_gamemode != (int)gamemode_name.KingOfTheHill && !defenderIsTraining && !ply_training) 
        { 
            ply_points++; 
            if (gameController.option_gamemode == (int)gamemode_name.Clash)
            {
                if (!gameController.option_teamplay && ply_points == gameController.option_gm_goal - 1) 
                { 
                    gameController.SendCustomNetworkEvent(NetworkEventTarget.All, "NetworkAddToTextQueue", Networking.LocalPlayer.displayName + " is close to winning!", Color.red, 7.5f);
                }
                else if (gameController.option_teamplay && ply_team >= 0 && ply_team < gameController.team_names.Length)
                {
                    int team_points = 0;
                    gameController.CheckSingleTeamPoints(ply_team, ref team_points);
                    if (team_points == gameController.option_gm_goal - 1)
                    {
                        gameController.SendCustomNetworkEvent(NetworkEventTarget.All, "NetworkAddToTextQueue", gameController.team_names[ply_team] + " are close to winning!", (Color)gameController.team_colors_bright[ply_team], 7.5f);
                    }
                }
            }
        } 
        last_kill_timer = 0.0f;
        last_kill_ply = defenderPlyId;
        gameController.PlaySFXFromArray(gameController.snd_game_sfx_sources[(int)game_sfx_name.Kill], gameController.snd_game_sfx_clips[(int)game_sfx_name.Kill]);
        gameController.AddToLocalTextQueue("You knocked out " + VRCPlayerApi.GetPlayerById(defenderPlyId).displayName + "!");
        TryHapticEvent((int)game_sfx_name.Kill);

        if (gameController.highlight_cameras_snapped[1] == false)
        {
            gameController.SendCustomNetworkEvent(NetworkEventTarget.All, "SnapHighlightPhoto", 1, Networking.LocalPlayer.GetPosition(), Networking.LocalPlayer.GetRotation() * Vector3.right, ply_scale);
        }

        // If we are the game master, we don't get an OnDeserialization event for ourselves, so check the round goal whenever we die or get a KO
        if (Networking.IsOwner(gameController.gameObject)) { gameController.CheckForRoundGoal(); }
    }

    public void HandleLocalPlayerDeath()
    {
        gameController.PlaySFXFromArray(gameController.snd_game_sfx_sources[(int)game_sfx_name.Death], gameController.snd_game_sfx_clips[(int)game_sfx_name.Death]);
        TryHapticEvent((int)game_sfx_name.Death);
        ply_respawn_timer = 0;
        if (ply_training)
        {
            // If we're in training mode, do nothing
        }
        else if (ply_state == (int)player_state_name.Alive && gameController.round_state == (int)round_state_name.Ongoing)
        {
            ply_state = (int)player_state_name.Respawning;

            if (gameController.option_gamemode != (int)gamemode_name.FittingIn
                || (gameController.option_gamemode == (int)gamemode_name.FittingIn && last_hit_by_ply != null)) { ply_deaths++; }
            // Check if we are in a gamemode that tracks lives, and if so, if we are on the team that tracks lives
            if (gameController.option_gamemode == (int)gamemode_name.Survival || (ply_team == 1 && gameController.option_gamemode == (int)gamemode_name.BossBash))
            { 
                ply_lives--;
            }
            else if (gameController.option_gamemode == (int)gamemode_name.KingOfTheHill)
            {
                gameController.AddToLocalTextQueue("Slowed during respawn invulnerability! (" + Mathf.RoundToInt(ply_respawn_duration) + " seconds)", Color.cyan, ply_respawn_duration);
            }
        }
        else if (ply_state == (int)player_state_name.Dead || gameController.round_state == (int)round_state_name.Ready)
        {
            ply_state = (int)player_state_name.Respawning;
        }
        else if (ply_state == (int)player_state_name.Respawning)
        {
            //if (gameController.option_gamemode == (int)gamemode_name.FittingIn && local_respawn_count >= gameController.option_gm_goal) { ply_lives = 0; }
        }
        else
        {
            //UnityEngine.Debug.Log("Whoa, you died in an unusual way! Contact a developer!");
        }

        ResetPowerups();
        if (gameController.local_plyweapon != null) { gameController.local_plyweapon.ResetWeaponToDefault(); }

        // Manage behavior based on gamemode
        if (!ply_training && gameController.option_gamemode == (int)gamemode_name.Infection && ply_team != 1)
        {
            UnityEngine.Debug.Log("Requesting game master to change team to Infected...");
            gameController.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "ChangeTeam", Networking.LocalPlayer.playerId, 1, false);
            ply_team = 1;
            //ply_points = 0;
            InfectionStatReset();
        }
        else if (!ply_training && gameController.option_gamemode == (int)gamemode_name.Infection && ply_team == 1)
        {
            InfectionStatReset();
        }
        else if (!ply_training && gameController.option_gamemode == (int)gamemode_name.FittingIn && last_hit_by_ply != null)
        {
            ply_scale += (float)((float)gameController.option_gm_config_a / 100.0f);
            plyEyeHeight_lerp_start_ms = Networking.GetServerTimeInSeconds();
            plyEyeHeight_desired = plyEyeHeight_default * ply_scale;
            plyEyeHeight_change = true;
            if (ply_scale > (float)((float)gameController.option_gm_goal / 100.0f)) 
            { 
                ply_lives = 0;
                gameController.AddToLocalTextQueue("You've grown too big! (" + ply_scale * 100.0f + "% vs max of " + gameController.option_gm_goal + "%)");
            }
        }

        if (!ply_training && ply_lives > 0)
        {
            // Reset the player's damage value to default, unless this is Boss Bash or Fitting In and they fell off without being struck (anti-exploit measure)
            if (!(last_hit_by_ply == null && (gameController.option_gamemode == (int)gamemode_name.BossBash || gameController.option_gamemode == (int)gamemode_name.FittingIn)))
            { ply_dp = ply_dp_default; }
            // Edge case: if this is on infection, it's possible for this event to off after RoundEnd() [usually for the master].
            // So, check if our local team is 1, but the networked team is 0, and if the lives calculated from the networked team is 1 (the lives count for Infection).
            var total_lives = 0; var members_alive = 0;
            gameController.CheckSingleTeamLives(0, ref members_alive, ref total_lives);
            if (gameController.option_gamemode == (int)gamemode_name.Infection && ply_team == 1 && gameController.GetGlobalTeam(Networking.LocalPlayer.playerId) == 0
                && total_lives <= 1) { gameController.TeleportLocalPlayerToReadyRoom(); ply_state = (int)player_state_name.Dead; }
            else { gameController.TeleportLocalPlayerToGameSpawnZone(); }
        }
        else if (!ply_training)
        {
            gameController.TeleportLocalPlayerToReadyRoom();
            ply_state = (int)player_state_name.Dead;
            gameController.room_training_portal.SetActive(true);
        }
        else
        {
            ply_dp = ply_dp_default;
            gameController.TeleportLocalPlayerToTrainingHall();
        }

        if (last_hit_by_ply != null)
        {
            gameController.AddToLocalTextQueue("Knocked out by " + last_hit_by_ply.displayName + "!");
            var plyAttr = gameController.FindPlayerAttributes(last_hit_by_ply);
            if (plyAttr != null) { plyAttr.SendCustomNetworkEvent(NetworkEventTarget.Owner, "KillOtherPlayer", last_hit_by_ply.playerId, Networking.LocalPlayer.playerId, ply_training); }
            last_hit_by_ply = null;
        }
        else
        {
            gameController.AddToLocalTextQueue("Knocked out!");
        }

        if (gameController != null && gameController.local_plyweapon != null) { gameController.local_plyweapon.ResetWeaponToDefault(); }

        // If we are the game master, we don't get an OnDeserialization event for ourselves, so check the round goal whenever we die or get a KO
        if (Networking.IsOwner(gameController.gameObject)) { gameController.CheckForRoundGoal(); }
        else { gameController.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "CheckForRoundGoal"); }

    }

    public void ProcessPowerUp(GameObject powerup_template, bool is_add = false)
    {
        //powerups_active
        if (powerup_template == null || powerup_template.GetComponent<ItemPowerup>() == null) { return; }

        if (is_add) {

			// Create a new copy of the powerup with all its inherited properties, then make sure it's set to be a template
			ItemPowerup powerup = powerup_template.GetComponent<ItemPowerup>();
            if (powerup == null) { return; }
			
			for (int i = 0; i < powerup.powerup_stat_behavior.Length; i++)
            {
                //Debug.Log("PROCESSING POWERUP WITH STAT BEHAVIORS " + powerup.powerup_stat_behavior[i].ToString() + " AND STAT VALUES " + powerup.powerup_stat_value[i].ToString());
                switch (i)
                {
                    case (int)powerup_stat_name.Scale:
                        Vector2 scale_cap = new Vector2(0.15f, 1000.0f);
                        if (ply_training) { scale_cap.y = 5.0f; }
                        if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Set) 
                        {
                            powerup.powerup_stat_value[i] = Mathf.Clamp(powerup.powerup_stat_value[i], scale_cap.x, scale_cap.y);
                            ply_scale = powerup.powerup_stat_value[i]; 
                        }
                        else if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Add) 
                        {
                            if (ply_scale + powerup.powerup_stat_value[i] > scale_cap.y) { powerup.powerup_stat_value[i] = (scale_cap.y - ply_scale); }
                            else if (ply_scale + powerup.powerup_stat_value[i] < scale_cap.x && powerup.powerup_stat_value[i] < 0) { powerup.powerup_stat_value[i] = (scale_cap.x - ply_scale); }
                            else if (ply_scale + powerup.powerup_stat_value[i] < scale_cap.x && powerup.powerup_stat_value[i] > 0) { powerup.powerup_stat_value[i] = (ply_scale - scale_cap.x); }
                            ply_scale += powerup.powerup_stat_value[i]; 
                        }
                        else if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Multiply) 
                        {
                            if (powerup.powerup_stat_value[i] < 0) { powerup.powerup_stat_value[i] = 0; }
                            else if(ply_scale * powerup.powerup_stat_value[i] > scale_cap.y) { powerup.powerup_stat_value[i] = (scale_cap.y / ply_scale); }
                            else if (ply_scale * powerup.powerup_stat_value[i] < scale_cap.x) { powerup.powerup_stat_value[i] = (scale_cap.x / ply_scale); }
                            ply_scale *= powerup.powerup_stat_value[i]; 
                        }
                        // If after managing caps the result is a powerup that does nothing, just set its duration to be minimal
                        //if (powerup.powerup_stat_value[i] == 0) { powerup.powerup_duration = 0.001f; powerup.power }
                        plyEyeHeight_lerp_start_ms = Networking.GetServerTimeInSeconds();
                        plyEyeHeight_desired = plyEyeHeight_default * ply_scale;
                        plyEyeHeight_change = true;
                        break;
                    case (int)powerup_stat_name.Speed:
                        if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Set) { ply_speed = powerup.powerup_stat_value[i]; }
                        else if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Add) { ply_speed += powerup.powerup_stat_value[i]; }
                        else if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Multiply) { ply_speed *= powerup.powerup_stat_value[i]; }
                        break;
                    case (int)powerup_stat_name.Atk:
                        if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Set) { ply_atk = powerup.powerup_stat_value[i]; }
                        else if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Add) { ply_atk += powerup.powerup_stat_value[i]; }
                        else if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Multiply) { ply_atk *= powerup.powerup_stat_value[i]; }
                        break;
                    case (int)powerup_stat_name.Def:
                        if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Set) { ply_def = powerup.powerup_stat_value[i]; }
                        else if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Add) { ply_def += powerup.powerup_stat_value[i]; }
                        else if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Multiply) { ply_def *= powerup.powerup_stat_value[i]; }
                        break;
                    case (int)powerup_stat_name.Grav:
                        if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Set) { ply_grav = powerup.powerup_stat_value[i]; }
                        else if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Add) { ply_grav += powerup.powerup_stat_value[i]; }
                        else if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Multiply) { ply_grav *= powerup.powerup_stat_value[i]; }
                        break;
                    case (int)powerup_stat_name.Damage:
                        if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Set) { ply_dp = powerup.powerup_stat_value[i]; ply_dp = Mathf.Max(0.0f, ply_dp); }
                        else if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Add) { ply_dp += powerup.powerup_stat_value[i]; ply_dp = Mathf.Max(0.0f, ply_dp); }
                        else if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Multiply) { ply_dp *= powerup.powerup_stat_value[i]; ply_dp = Mathf.Max(0.0f, ply_dp); }
                        break;
                    case (int)powerup_stat_name.Jumps:
                        if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Set) { ply_jumps_add = Mathf.RoundToInt(powerup.powerup_stat_value[i]); }
                        else if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Add) { ply_jumps_add += Mathf.RoundToInt(powerup.powerup_stat_value[i]); }
                        else if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Multiply) { ply_jumps_add *= Mathf.RoundToInt(powerup.powerup_stat_value[i]); }
                        break;
                    default:
                        break;
                }
            }

            gameController.PlaySFXFromArray(powerup.item_snd_source, powerup.powerup_snd_clips, powerup.powerup_type);
            Debug.Log(gameObject.name + ": Attempting to play sound " + powerup.powerup_snd_clips[powerup.powerup_type].name + " for type " + powerup.powerup_type);
            powerups_active = GlobalHelperFunctions.AddToGameObjectArray(powerup_template, powerups_active);
		}

        else
        {
            ItemPowerup powerup = powerup_template.GetComponent<ItemPowerup>();
			for (int i = 0; i < powerup.powerup_stat_behavior.Length; i++)
            {
                switch (i)
                {
                    case (int)powerup_stat_name.Scale:
                        if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Add) { ply_scale -= powerup.powerup_stat_value[i]; }
                        else if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Multiply) { ply_scale /= powerup.powerup_stat_value[i]; }
                        plyEyeHeight_lerp_start_ms = Networking.GetServerTimeInSeconds();
                        plyEyeHeight_desired = plyEyeHeight_default * ply_scale;
                        plyEyeHeight_change = true;
                        break;
                    case (int)powerup_stat_name.Speed:
                        if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Add) { ply_speed -= powerup.powerup_stat_value[i]; }
                        else if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Multiply) { ply_speed /= powerup.powerup_stat_value[i]; }
                        break;
                    case (int)powerup_stat_name.Atk:
                        if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Add) { ply_atk -= powerup.powerup_stat_value[i]; }
                        else if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Multiply) { ply_atk /= powerup.powerup_stat_value[i]; }
                        break;
                    case (int)powerup_stat_name.Def:
                        if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Add) { ply_def -= powerup.powerup_stat_value[i]; }
                        else if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Multiply) { ply_def /= powerup.powerup_stat_value[i]; }
                        break;
                    case (int)powerup_stat_name.Grav:
                        if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Add) { ply_grav -= powerup.powerup_stat_value[i]; }
                        else if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Multiply) { ply_grav /= powerup.powerup_stat_value[i]; }
                        break;
                    case (int)powerup_stat_name.Jumps:
                        if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Add) { ply_jumps_add -= Mathf.RoundToInt(powerup.powerup_stat_value[i]); }
                        else if (powerup.powerup_stat_behavior[i] == (int)powerup_stat_behavior_name.Multiply) { ply_jumps_add /= Mathf.RoundToInt(powerup.powerup_stat_value[i]); }
                        break;
                    // We do not have a case for damage, because those are intended to be permanent
                    default:
                        break;
                }
            }
            //Debug.Log("Removing effects of active powerup of type " + powerup.powerup_type);
            powerups_active = GlobalHelperFunctions.RemoveEntryFromGameObjectArray(powerup.gameObject, powerups_active);
        }

    }

    public void ResetPowerups()
    {
        if (powerups_are_resetting) { return; }

        var index_iter = 0;
        var powerup_count = powerups_active.Length;
        powerups_are_resetting = true;
        // The issue is that this fails because the length is continously shortening. Instead, let's have it on a while loop with the iter++ until we reach the length of the initial
        while (index_iter < powerup_count) {
            if (powerups_active.Length <= 0) { break; }
            if (powerups_active[0] != null && powerups_active[0].GetComponent<ItemPowerup>() != null)
            {
                var powerup = powerups_active[0].GetComponent<ItemPowerup>();
                //Debug.Log("Found powerup at " + i + ": " + powerups_active[i - index_iter].name + "; type: " + powerups_active[i].GetComponent<ItemPowerup>().powerup_type + "; global index: " + powerups_active[i].GetComponent<ItemPowerup>().powerup_stored_global_index);
                //ProcessPowerUp(powerup.gameObject, false); // this may remove entries from the list as it occurs, resulting in errors.
                powerup.FadeOutAndDestroy();
			}
            else
            {
                //Debug.Log("Found powerup at " + i + " index that does not exist; resetting in array");
                powerups_active = GlobalHelperFunctions.RemoveIndexFromGameObjectArray(0, powerups_active);
            }
            index_iter++;
        }
        plyEyeHeight_lerp_start_ms = Networking.GetServerTimeInSeconds();
        plyEyeHeight_desired = plyEyeHeight_default * ply_scale;
        plyEyeHeight_change = true;
        powerups_are_resetting = false;
    }

    [NetworkCallable]
    public void SetPoints(ushort amount)
    {
        ply_points = amount;
    }

    public override void InputJump(bool value, UdonInputEventArgs args)
    {
        base.InputJump(value, args);
        if (value && !ply_jump_pressed) 
        {
            if (!Networking.LocalPlayer.IsPlayerGrounded() && ply_jumps_tracking < ply_jumps_add)
            {
                Vector3 plyVel = Networking.LocalPlayer.GetVelocity();
                Networking.LocalPlayer.SetVelocity(new Vector3(plyVel.x, 4.0f + (1.0f - ply_grav), plyVel.z));
                ply_jumps_tracking++;
            }
            ply_jump_pressed = true;
        }
        else if (!value && ply_jump_pressed) { ply_jump_pressed = false; }
    }

    public void SetupTutorialMessages()
    {
        if (gameController == null || gameController.local_plyweapon == null) { return; }

        local_tutorial_message_bool = new bool[(int)powerup_type_name.ENUM_LENGTH + (int)weapon_type_name.ENUM_LENGTH];
        local_tutorial_message_str_desktop = new string[(int)powerup_type_name.ENUM_LENGTH + (int)weapon_type_name.ENUM_LENGTH];
        local_tutorial_message_str_vr = new string[(int)powerup_type_name.ENUM_LENGTH + (int)weapon_type_name.ENUM_LENGTH];

        // Tutorial messages for all      
        local_tutorial_message_str_desktop[(int)powerup_type_name.SizeUp] = "Increases size, range, and attack/defense! Be a massive threat!";
        local_tutorial_message_str_desktop[(int)powerup_type_name.SizeDown] = "Decreases size, range, and attack/defense, but increases movement speed! Be a hard to hit menace!";
        local_tutorial_message_str_desktop[(int)powerup_type_name.SpeedUp] = "Dramatically increases movement speed! Nyoom!";
        local_tutorial_message_str_desktop[(int)powerup_type_name.AtkUp] = "Multiplies damage dealt by a large factor!";
        local_tutorial_message_str_desktop[(int)powerup_type_name.AtkDown] = "Divides damage dealt by a large factor!";
        local_tutorial_message_str_desktop[(int)powerup_type_name.DefUp] = "Divides damage received by a large factor!";
        local_tutorial_message_str_desktop[(int)powerup_type_name.DefDown] = "Multiplies damage received by a large factor!";
        local_tutorial_message_str_desktop[(int)powerup_type_name.LowGrav] = "Increases time spent in mid-air!";
        local_tutorial_message_str_desktop[(int)powerup_type_name.PartialHeal] = "Removes 50% of damage dealt to the user!";
        local_tutorial_message_str_desktop[(int)powerup_type_name.FullHeal] = "Removes 100% of damage dealt to the user!";
        local_tutorial_message_str_desktop[(int)powerup_type_name.Multijump] = "Grants an additional jump while in mid-air!";
        local_tutorial_message_str_desktop[(int)powerup_type_name.HighGrav] = "Decreases time spent in mid-air!";

        local_tutorial_message_str_desktop[(int)powerup_type_name.ENUM_LENGTH + (int)weapon_type_name.PunchingGlove] = "The default weapon. Push your fire key to knock opponents out of the arena! (Power: " + gameController.local_plyweapon.GetStatsFromWeaponType((int)weapon_type_name.PunchingGlove)[(int)weapon_stats_name.Hurtbox_Damage] + ")";
        local_tutorial_message_str_desktop[(int)powerup_type_name.ENUM_LENGTH + (int)weapon_type_name.Bomb] = "Push your fire key to toss it forward! It will detonate after " + gameController.local_plyweapon.GetStatsFromWeaponType((int)weapon_type_name.Bomb)[(int)weapon_stats_name.Projectile_Duration] + " seconds! (Power: " + gameController.local_plyweapon.GetStatsFromWeaponType((int)weapon_type_name.Bomb)[(int)weapon_stats_name.Hurtbox_Damage] + ")";
        local_tutorial_message_str_desktop[(int)powerup_type_name.ENUM_LENGTH + (int)weapon_type_name.Rocket] = "Fire off projectiles that will explode in a radius! (Power: " + gameController.local_plyweapon.GetStatsFromWeaponType((int)weapon_type_name.Rocket)[(int)weapon_stats_name.Hurtbox_Damage] + ")";
        local_tutorial_message_str_desktop[(int)powerup_type_name.ENUM_LENGTH + (int)weapon_type_name.BossGlove] = "Used by the Big Boss during the Boss Bash gamemode. Has a much bigger hitbox. Attack rate scales with # of players in-game! (Power: " + gameController.local_plyweapon.GetStatsFromWeaponType((int)weapon_type_name.BossGlove)[(int)weapon_stats_name.Hurtbox_Damage] + ")";
        local_tutorial_message_str_desktop[(int)powerup_type_name.ENUM_LENGTH + (int)weapon_type_name.HyperGlove] = "Hyper-fast attacks, but less damage! (Power: " + gameController.local_plyweapon.GetStatsFromWeaponType((int)weapon_type_name.HyperGlove)[(int)weapon_stats_name.Hurtbox_Damage] + ")";
        local_tutorial_message_str_desktop[(int)powerup_type_name.ENUM_LENGTH + (int)weapon_type_name.MegaGlove] = "Mega damage, but slow to fire! (Power: " + gameController.local_plyweapon.GetStatsFromWeaponType((int)weapon_type_name.MegaGlove)[(int)weapon_stats_name.Hurtbox_Damage] + ")";
        local_tutorial_message_str_desktop[(int)powerup_type_name.ENUM_LENGTH + (int)weapon_type_name.SuperLaser] = "Hold down your fire key to charge it up and fire a huge beam! (Power: " + gameController.local_plyweapon.GetStatsFromWeaponType((int)weapon_type_name.SuperLaser)[(int)weapon_stats_name.Hurtbox_Damage] + ")";
        local_tutorial_message_str_desktop[(int)powerup_type_name.ENUM_LENGTH + (int)weapon_type_name.ThrowableItem] = "Push your fire key to toss it forward! (Contains: $NAME)";

        for (int i = 0; i < (int)powerup_type_name.ENUM_LENGTH + (int)weapon_type_name.ENUM_LENGTH; i++)
        {
            local_tutorial_message_bool[i] = false;
            if (local_tutorial_message_str_desktop[i] != "" && i < (int)powerup_type_name.ENUM_LENGTH)
            {
                local_tutorial_message_str_desktop[i] = GlobalHelperFunctions.PowerupTypeToStr(i).ToUpper() + ": " + local_tutorial_message_str_desktop[i];
            }
            else if (local_tutorial_message_str_desktop[i] != "" && i >= (int)powerup_type_name.ENUM_LENGTH) 
            { 
                local_tutorial_message_str_desktop[i] = GlobalHelperFunctions.WeaponTypeToStr(i - (int)powerup_type_name.ENUM_LENGTH).ToUpper() + ": " + local_tutorial_message_str_desktop[i]; 
            }

            local_tutorial_message_str_vr[i] = local_tutorial_message_str_desktop[i].Replace("fire key", "Trigger");
        }

        // VR-specific messages
        local_tutorial_message_str_vr[(int)powerup_type_name.ENUM_LENGTH + (int)weapon_type_name.Bomb] = "Toss it by releasing your Grip! It will detonate after " + gameController.local_plyweapon.GetStatsFromWeaponType((int)weapon_type_name.Bomb)[(int)weapon_stats_name.Projectile_Duration] + " seconds!";
        local_tutorial_message_str_vr[(int)powerup_type_name.ENUM_LENGTH + (int)weapon_type_name.SuperLaser] = "Hold down your Trigger to charge it up and fire a huge beam!";
        local_tutorial_message_str_vr[(int)powerup_type_name.ENUM_LENGTH + (int)weapon_type_name.ThrowableItem] = "Toss it by releasing your Grip! (Contains: $NAME)";

        tutorial_messages_ready = true;
    }

    public void ResetTutorialMessage(int item_type = -1)
    {
        if (local_tutorial_message_bool == null) { return; }
        if (item_type < 0)
        {
            for (int i = 0; i < (int)powerup_type_name.ENUM_LENGTH + (int)weapon_type_name.ENUM_LENGTH; i++)
            {
                local_tutorial_message_bool[i] = false;
            }
        }
        else
        {
            if (item_type >= local_tutorial_message_bool.Length) { return; }
            local_tutorial_message_bool[item_type] = false;
        }
    }

    public void SendTutorialMessage(int item_type)
    {
        if (!tutorial_messages_ready && Networking.IsOwner(gameObject) && gameController != null) { SetupTutorialMessages(); }

        // Send a tutorial message
        if (tutorial_messages_ready && Networking.IsOwner(gameObject) && gameController != null && local_tutorial_message_bool != null && !local_tutorial_message_bool[item_type])
        {
            string display_str = local_tutorial_message_str_desktop[item_type];
            if (Networking.LocalPlayer.IsUserInVR()) { display_str = local_tutorial_message_str_vr[item_type]; }
            if (item_type == (int)powerup_type_name.ENUM_LENGTH + (int)weapon_type_name.ThrowableItem)
            {
                string item_name = "";
                if (gameController.local_plyweapon.weapon_extra_data < (int)powerup_type_name.ENUM_LENGTH) { item_name = GlobalHelperFunctions.PowerupTypeToStr(gameController.local_plyweapon.weapon_extra_data); }
                else { item_name = GlobalHelperFunctions.WeaponTypeToStr(gameController.local_plyweapon.weapon_extra_data - (int)powerup_type_name.ENUM_LENGTH); }
                display_str = display_str.Replace("$NAME", item_name);
            }

            Color display_color = Color.cyan;
            if (item_type >= (int)powerup_type_name.ENUM_LENGTH) { display_color = new Color(1.0f, 0.5f, 0.0f, 1.0f); }
            else if (item_type == (int)powerup_type_name.AtkDown || item_type == (int)powerup_type_name.DefDown || item_type == (int)powerup_type_name.HighGrav) { display_color = Color.red; }

            if (display_str != "")
            {
                gameController.AddToLocalTextQueue(display_str, display_color, 8.0f);
            }
            local_tutorial_message_bool[item_type] = true;
        }
    }

    public string GetTutorialMessage(int item_type)
    {
        // Send a tutorial message
        string display_str = "";
        if (Networking.IsOwner(gameObject) && gameController != null && local_tutorial_message_bool != null && !local_tutorial_message_bool[item_type])
        {
            display_str = local_tutorial_message_str_desktop[item_type];
            if (Networking.LocalPlayer.IsUserInVR()) { display_str = local_tutorial_message_str_vr[item_type]; }
            if (display_str != "")
            {
                string[] split_str = display_str.Split(" (Power: ");
                if (split_str != null && split_str.Length >= 2)
                {
                    display_str = split_str[0];
                }
                if (item_type == (int)powerup_type_name.ENUM_LENGTH + (int)weapon_type_name.ThrowableItem)
                {
                    display_str = display_str.Replace("(Contains: $NAME)", "Contains a random powerup or weapon! ");
                }
            }

            Color display_color = Color.cyan;
            if (item_type >= (int)powerup_type_name.ENUM_LENGTH) { display_color = new Color(1.0f, 0.5f, 0.0f, 1.0f); }

        }
        return display_str;
    }

    [NetworkCallable]
    public void BecomeZombig()
    {
        ResetToDefaultStats();
        gameController.TeleportLocalPlayerToGameSpawnZone();
        infection_special = 2;
        ply_dp = ply_dp_default;
        ply_scale *= 2.5f;
        ply_speed *= 1.33f;

        gameController.local_plyweapon.weapon_temp_ammo = -1;
        gameController.local_plyweapon.weapon_temp_duration = -1;
        gameController.local_plyweapon.weapon_temp_timer = 0.0f;
        gameController.local_plyweapon.weapon_type = (int)weapon_type_name.MegaGlove;
        gameController.local_plyweapon.weapon_extra_data = 0;

        if (gameController.local_uiplytoself != null && gameController.template_ItemSpawner != null) 
        {
            ItemWeapon iweapon = gameController.template_ItemSpawner.GetComponent<ItemSpawner>().child_weapon;
            gameController.local_uiplytoself.PTSWeaponSprite.sprite = iweapon.iweapon_sprites[(int)weapon_type_name.MegaGlove];
            gameController.PlaySFXFromArray(gameController.local_plyweapon.snd_source_weaponcharge, iweapon.iweapon_snd_clips, iweapon.iweapon_type);
        }
        gameController.local_plyweapon.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "UpdateStatsFromWeaponType");
        plyEyeHeight_desired = plyEyeHeight_default * ply_scale;
        plyEyeHeight_lerp_start_ms = Networking.GetServerTimeInSeconds();
        plyEyeHeight_change = true;

        gameController.AddToLocalTextQueue("You are the ZomBig! Crush the Survivors into dust!", gameController.team_colors_bright[1]);
    }

    [NetworkCallable]
    public void InfectionStatReset()
    {
        if (infection_special == 2)
        {
            // ZomBig
            ResetToDefaultStats();
            infection_special = 1; // We set this to 1 because GameController will automatically resolve it down to 0 if the player count condition is met
            gameController.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "CheckForZombigs", Networking.LocalPlayer.playerId);
            gameController.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "NetworkAddToTextQueue", "The ZomBig has been defeated!", Color.red, 5.0f);
        }
        else if (infection_special == 1)
        {
            // Patient Zero
            ply_speed = gameController.plysettings_speed * 1.33f;
            ply_def = gameController.plysettings_def * 0.6f;
            ply_atk = gameController.plysettings_atk * 1.5f;
        }
        else if (infection_special == 0)
        { 
            // Normal Zombie
            ply_speed = gameController.plysettings_speed * 0.9f;
            ply_def = gameController.plysettings_def * 0.4f;
            ply_atk = gameController.plysettings_atk * 0.5f;
        }
    }

    [NetworkCallable]
    public void ResetToDefaultStats()
    {
        ply_dp = ply_dp_default;
        ply_lives = gameController.plysettings_lives;
        ply_points = gameController.plysettings_points;
        ply_respawn_duration = gameController.plysettings_respawn_duration;
        ply_scale = gameController.plysettings_scale;
        ply_speed = gameController.plysettings_speed;
        ply_atk = gameController.plysettings_atk;
        ply_def = gameController.plysettings_def;
        ply_grav = gameController.plysettings_grav * gameController.mapscript_list[gameController.map_selected].map_gravity_scale;
        if (gameController.option_gamemode == (int)gamemode_name.BossBash && ply_team == 1)
        {
            //playerData.ply_lives = option_goal_value_b;
            ply_scale = gameController.plysettings_scale * gameController.plysettings_boss_scale_mod;
            ply_atk = gameController.plysettings_atk + gameController.plysettings_boss_atk_mod; // (ply_parent_arr[0].Length / 4.0f);
            ply_def = gameController.plysettings_def + gameController.plysettings_boss_def_mod; //+ Mathf.Max(0.0f, -0.2f + (ply_parent_arr[0].Length * 0.2f));
            ply_speed = gameController.plysettings_speed + gameController.plysettings_boss_speed_mod;
        }
        else
        {
            gameController.local_plyweapon.weapon_type_default = gameController.plysettings_weapon;
            gameController.local_plyweapon.weapon_type = gameController.local_plyweapon.weapon_type_default;
            if (gameController.local_plyweapon.weapon_type_default != (int)weapon_type_name.PunchingGlove)
            {
                gameController.local_plyweapon.weapon_temp_ammo = -1;
                gameController.local_plyweapon.weapon_temp_duration = -1;
            }
        }
        gameController.local_plyweapon.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "UpdateStatsFromWeaponType");
        plyEyeHeight_desired = plyEyeHeight_default * ply_scale;
        plyEyeHeight_lerp_start_ms = Networking.GetServerTimeInSeconds();
        plyEyeHeight_change = true;
    }


}
